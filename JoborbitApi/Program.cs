using System.Text;
using FluentValidation;
using JoborbitApi.Middlewares;
using JobsApi.Application.Validators;
using JobsApi.Infrastructure.Extensions;
using JobsApi.Infrastructure.Options;
using JobsApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────
//  Logging
// ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ─────────────────────────────────────────────────────────
//  Infrastructure (DbContext, options, repos, auth services)
// ─────────────────────────────────────────────────────────
builder.Services.AddJobsApiInfrastructure(builder.Configuration);

// FluentValidation auto-register all validators in Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();

// ─────────────────────────────────────────────────────────
//  MVC + Swagger
// ─────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JobOrbit API",
        Version = "v1",
        Description = "JobOrbit API — authentication, job listings, applications."
    });

    // JWT auth button in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste your JWT below."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

// ─────────────────────────────────────────────────────────
//  JWT Bearer authentication
// ─────────────────────────────────────────────────────────
var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt configuration section is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────────────────
//  CORS — allow any *.onrender.com subdomain (for the frontend) plus
//  localhost for local development. This avoids hardcoding the exact
//  frontend URL since Render assigns random suffixes.
// ─────────────────────────────────────────────────────────
const string CorsPolicyName = "joborbit-frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                // Allow any Render subdomain
                if (origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    && origin.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase))
                    return true;
                // Allow localhost on any port (dev only)
                if (origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ─────────────────────────────────────────────────────────
//  Build the app pipeline
// ─────────────────────────────────────────────────────────
var app = builder.Build();

// Global exception handler → ProblemDetails
app.UseMiddleware<ExceptionMiddleware>();

// CORS must be called BEFORE auth and endpoint routing
app.UseCors(CorsPolicyName);

// Lightweight health probe for Render / Docker / k8s
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

// Root route — friendly landing so visiting the URL in a browser doesn't 404.
// (The service is an API-only backend; this just makes the root URL informative.)
app.MapGet("/", () => Results.Ok(new
{
    name = "JobOrbit API",
    version = "1.0",
    status = "running",
    docs = "/swagger (Development only)",
    endpoints = new
    {
        health = "/health",
        login = "POST /api/account/login",
        register = "POST /api/account/register",
        me = "GET /api/account/me (requires auth)",
        jobs_external = "GET /api/jobs/external",
        jobs_search = "GET /api/jobs",
        employer_jobs = "GET/POST/PUT/DELETE /api/employer/jobs (requires Employer role)",
        resume = "GET/PUT/DELETE /api/resume (requires auth)",
        resume_ai = "POST /api/resume/generate-bullets (requires auth + OpenAI key)"
    },
    timestamp = DateTime.UtcNow
}));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─────────────────────────────────────────────────────────
//  Seed test user (won't crash the app if Postgres is down)
// ─────────────────────────────────────────────────────────
try
{
    await app.Services.SeedTestUserAsync();
    Log.Information("Database seeding completed.");
}
catch (Exception ex)
{
    Log.Warning(ex, "Could not seed test user — is Postgres running?");
}

app.Run();