using JobsApi.Application.Abstractions;
using JobsApi.Application.Dtos;

namespace JobsApi.Infrastructure.ExternalJobs;

/// <summary>
/// Built-in sample jobs hardcoded into the application. Always available
/// even if every external provider is down AND the database isn't connected.
/// Gives new visitors "hope" — they see real-looking job listings on first
/// visit instead of an empty page.
///
/// 100 jobs across diverse categories: Technology, Design, Product, Data,
/// Marketing, Sales, Operations, Finance, HR, Customer Success.
/// </summary>
public class SampleJobsProvider : IExternalJobProvider
{
    public string Name => "JobOrbit";

    private static readonly IReadOnlyList<ExternalJobDto> Jobs = BuildSampleJobs();

    public Task<IReadOnlyList<ExternalJobDto>> FetchAsync(
        ExternalJobsQueryDto query,
        CancellationToken ct)
    {
        IEnumerable<ExternalJobDto> filtered = Jobs;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(j =>
                j.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || j.CompanyName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || j.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
                || j.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var loc = query.Location.Trim();
            filtered = filtered.Where(j =>
                j.Location.Contains(loc, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Remote.HasValue)
        {
            filtered = filtered.Where(j => j.Remote == query.Remote.Value);
        }

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var result = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<IReadOnlyList<ExternalJobDto>>(result);
    }

    // ─── Build 100 sample jobs ────────────────────────────────────

    private static IReadOnlyList<ExternalJobDto> BuildSampleJobs()
    {
        var rng = new Random(42);  // deterministic seed — same jobs every restart
        var now = DateTime.UtcNow;

        var companies = new[]
        {
            ("Northstar Labs", "San Francisco, US"),
            ("Paperplane", "Remote"),
            ("Orbit Market", "New York, US"),
            ("Clover Health", "Austin, US"),
            ("Kite Finance", "Toronto, CA"),
            ("Kindred Energy", "Berlin, DE"),
            ("Acme Corp", "London, UK"),
            ("Lumen Tech", "Remote"),
            ("Brightpath", "Seattle, US"),
            ("Nimbus AI", "Remote"),
            ("Verdant Foods", "Amsterdam, NL"),
            ("Stellar Software", "Boston, US"),
            ("Maple Studio", "Vancouver, CA"),
            ("Cobalt Systems", "Remote"),
            ("Driftwood Co", "Lisbon, PT"),
            ("Echo Labs", "Singapore"),
            ("Frost & Co", "Stockholm, SE"),
            ("Glow Labs", "Dubai, UAE"),
            ("Harbor Digital", "Sydney, AU"),
            ("Ironwood", "Remote"),
        };

        var titlesByCategory = new Dictionary<string, string[]>
        {
            ["Engineering"] = new[]
            {
                "Senior Frontend Engineer", "Backend Engineer (Node.js)", "Full Stack Developer",
                "Senior React Developer", "Mobile Engineer (iOS)", "Mobile Engineer (Android)",
                "DevOps Engineer", "Site Reliability Engineer", "Platform Engineer",
                "Staff Software Engineer", "Engineering Manager", "Principal Engineer",
                "JavaScript Engineer", "TypeScript Engineer", "Python Engineer",
            },
            ["Design"] = new[]
            {
                "Senior Product Designer", "UX Researcher", "UI Designer",
                "Design System Lead", "Brand Designer", "Motion Designer",
                "Senior UX Designer", "Visual Designer", "Product Design Manager",
            },
            ["Product"] = new[]
            {
                "Product Manager", "Senior Product Manager", "Product Owner",
                "Technical Product Manager", "Group Product Manager", "Associate PM",
            },
            ["Data"] = new[]
            {
                "Data Scientist", "Data Engineer", "ML Engineer",
                "Senior Data Analyst", "Analytics Engineer", "Head of Data",
                "Research Scientist", "Applied Scientist",
            },
            ["Marketing"] = new[]
            {
                "Growth Marketing Lead", "Content Strategist", "SEO Specialist",
                "Brand Marketing Manager", "Performance Marketer", "Social Media Manager",
            },
            ["Sales"] = new[]
            {
                "Account Executive", "Sales Development Rep", "Customer Success Manager",
                "Solutions Engineer", "Sales Manager", "Enterprise Account Executive",
            },
            ["Operations"] = new[]
            {
                "Operations Manager", "Chief of Staff", "Program Manager",
                "People Operations Partner", "Talent Recruiter",
            },
        };

        var jobTypesByCategory = new Dictionary<string, string[]>
        {
            ["Engineering"] = new[] { "full_time", "remote" },
            ["Design"] = new[] { "full_time" },
            ["Product"] = new[] { "full_time" },
            ["Data"] = new[] { "full_time", "remote" },
            ["Marketing"] = new[] { "full_time", "contract" },
            ["Sales"] = new[] { "full_time" },
            ["Operations"] = new[] { "full_time" },
        };

        var tagsByCategory = new Dictionary<string, string[]>
        {
            ["Engineering"] = new[] { "react", "typescript", "node.js", "python", "aws", "docker", "kubernetes", "go", "java", "graphql" },
            ["Design"] = new[] { "figma", "sketch", "design systems", "prototyping", "research", "ui/ux", "branding" },
            ["Product"] = new[] { "roadmap", "agile", "scrum", "discovery", "strategy", "analytics" },
            ["Data"] = new[] { "sql", "python", "tableau", "spark", "pytorch", "tensorflow", "etl" },
            ["Marketing"] = new[] { "content", "seo", "paid social", "google ads", "analytics", "copywriting" },
            ["Sales"] = new[] { "saas", "b2b", "enterprise", "outbound", "inbound", "crm" },
            ["Operations"] = new[] { "operations", "people ops", "recruiting", "hr", "program management" },
        };

        var descriptions = new[]
        {
            "Join a team building the next generation of {cat} tools used by thousands of customers worldwide. You'll own critical features end-to-end, collaborate cross-functionally, and ship to production weekly.",
            "We're a fast-growing startup looking for someone who thrives in ambiguity and loves turning complex problems into elegant solutions. You'll work directly with the founders and shape the product roadmap.",
            "Help us build delightful experiences for our users. We value craft, attention to detail, and a strong product mindset. You'll partner with designers, PMs, and engineers to deliver impact.",
            "We're scaling rapidly and need an experienced {cat} professional to lead initiatives, mentor team members, and drive measurable outcomes. Remote-friendly with flexible hours.",
            "Be the founding {cat} hire at a well-funded Series A startup. You'll set the standard, define processes, and grow the team as we scale to millions of users.",
            "Work on meaningful problems with a team that values ownership, transparency, and continuous learning. We offer competitive comp, equity, and a culture of growth.",
        };

        var jobs = new List<ExternalJobDto>();
        var id = 1;

        foreach (var (company, location) in companies)
        {
            // Each company gets 5 jobs across different categories
            var categories = titlesByCategory.Keys.OrderBy(_ => rng.Next()).Take(5).ToList();
            foreach (var cat in categories)
            {
                var title = titlesByCategory[cat][rng.Next(titlesByCategory[cat].Length)];
                var isRemote = location == "Remote" || rng.NextDouble() < 0.4;
                var jobLocation = isRemote && rng.NextDouble() < 0.5 ? "Remote" : location;
                var tags = tagsByCategory[cat]
                    .OrderBy(_ => rng.Next())
                    .Take(rng.Next(2, 5))
                    .ToArray();
                var jobTypes = jobTypesByCategory[cat];
                var description = descriptions[rng.Next(descriptions.Length)]
                    .Replace("{cat}", cat.ToLowerInvariant());
                var publishedDaysAgo = rng.Next(1, 30);

                jobs.Add(new ExternalJobDto(
                    ExternalId: $"sample-{id:D3}",
                    Title: title,
                    CompanyName: company,
                    Description: description,
                    Location: jobLocation,
                    Remote: isRemote || jobLocation == "Remote",
                    Tags: tags,
                    JobTypes: jobTypes,
                    Url: $"https://example.com/jobs/sample-{id:D3}",
                    PublishedAt: now.AddDays(-publishedDaysAgo),
                    Source: "JobOrbit"
                ));
                id++;

                if (jobs.Count >= 100) break;
            }
            if (jobs.Count >= 100) break;
        }

        return jobs;
    }
}
