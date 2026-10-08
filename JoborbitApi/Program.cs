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
//  Build the app pipeline
// ─────────────────────────────────────────────────────────
var app = builder.Build();

// Global exception handler → ProblemDetails
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
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