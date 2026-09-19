using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Data;

public static class CatalogueIds
{
    public static readonly Guid Chas = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Constructionline = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SafeContractor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid Avetta = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid Smas = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public static readonly Guid DemoTenant = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    public static readonly Guid DemoUser = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
}

public static class SeedData
{
    public const string DemoEmail = "demo@schemevault.test";
    public const string DemoPassword = "DemoPassw0rd!";
    public const string DemoOrganisation = "Northern Plant Hire Ltd";

    public static async Task ApplyAsync(AppDbContext db, UserManager<ApplicationUser> users, ILogger logger)
    {
        await EnsureSchemesAsync(db);
        await EnsureDemoTenantAsync(db, users, logger);
    }

    public static async Task EnsureSchemesAsync(AppDbContext db)
    {
        if (!await db.Schemes.AnyAsync())
        {
            db.Schemes.AddRange(CreateSchemes());
            await db.SaveChangesAsync();
        }

        if (!await db.GapChecklistTemplates.AnyAsync())
        {
            db.GapChecklistTemplates.AddRange(CreateTemplates());
            await db.SaveChangesAsync();
        }
    }

    public static async Task ProvisionTenantGapsAsync(AppDbContext db, Guid tenantId, DateTimeOffset now)
    {
        var templates = await db.GapChecklistTemplates.AsNoTracking().OrderBy(t => t.SortOrder).ToListAsync();
        var existing = await db.GapChecklistItems
            .IgnoreQueryFilters()
            .Where(g => g.TenantId == tenantId)
            .Select(g => g.TemplateId)
            .ToListAsync();

        var toAdd = templates
            .Where(t => !existing.Contains(t.Id))
            .Select(t => new GapChecklistItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SchemeId = t.SchemeId,
                TemplateId = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = GapItemStatus.Missing,
                UpdatedAt = now
            });

        db.GapChecklistItems.AddRange(toAdd);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoTenantAsync(AppDbContext db, UserManager<ApplicationUser> users, ILogger logger)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == CatalogueIds.DemoTenant))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Id = CatalogueIds.DemoTenant,
            Name = DemoOrganisation,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = CatalogueIds.DemoUser,
            TenantId = tenant.Id,
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            FullName = "Alex Harper",
            CreatedAt = now
        };

        var create = await users.CreateAsync(user, DemoPassword);
        if (!create.Succeeded)
        {
            throw new InvalidOperationException("Failed to seed demo user: " + string.Join("; ", create.Errors.Select(e => e.Description)));
        }

        db.Memberships.Add(new Membership
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = MembershipRole.Owner,
            CreatedAt = now
        });

        await db.SaveChangesAsync();
        await ProvisionTenantGapsAsync(db, tenant.Id, now);

        var el = new EvidenceItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Title = "Employers' Liability insurance certificate",
            Category = "Insurance",
            Notes = "Aviva — indemnity £10m. Renewal via broker in Grimsby.",
            ExpiresOn = now.AddDays(200),
            CreatedAt = now,
            UpdatedAt = now
        };
        var hsPolicy = new EvidenceItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Title = "Health and safety policy (signed)",
            Category = "Policy",
            Notes = "Reviewed annually by the responsible director.",
            ExpiresOn = now.AddDays(90),
            CreatedAt = now,
            UpdatedAt = now
        };
        var rams = new EvidenceItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Title = "Standard RAMS pack — plant hire",
            Category = "RAMS",
            Notes = "Site-specific RAMS still required per contract.",
            CreatedAt = now,
            UpdatedAt = now
        };
        db.EvidenceItems.AddRange(el, hsPolicy, rams);

        db.Accreditations.AddRange(
            new Accreditation
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                SchemeId = CatalogueIds.Chas,
                Status = AccreditationStatus.Active,
                ExpiresOn = now.AddDays(45),
                LastSubmittedOn = now.AddDays(-320),
                MembershipNumber = "CHAS-88421",
                Notes = "Standard assessment. Watch the 45-day window.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Accreditation
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                SchemeId = CatalogueIds.Constructionline,
                Status = AccreditationStatus.Active,
                ExpiresOn = now.AddDays(140),
                LastSubmittedOn = now.AddDays(-220),
                MembershipNumber = "CL-102938",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Accreditation
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                SchemeId = CatalogueIds.SafeContractor,
                Status = AccreditationStatus.Expired,
                ExpiresOn = now.AddDays(-12),
                LastSubmittedOn = now.AddDays(-380),
                MembershipNumber = "SC-55102",
                Notes = "Lapsed — reapply before the next principal-contractor tender.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Accreditation
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                SchemeId = CatalogueIds.Smas,
                Status = AccreditationStatus.Active,
                ExpiresOn = now.AddDays(22),
                LastSubmittedOn = now.AddDays(-340),
                MembershipNumber = "SMAS-22011",
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync();

        var chasEl = await db.GapChecklistItems.IgnoreQueryFilters()
            .FirstAsync(g => g.TenantId == tenant.Id && g.SchemeId == CatalogueIds.Chas && g.Title.Contains("Employers"));
        chasEl.Status = GapItemStatus.Complete;
        chasEl.EvidenceItemId = el.Id;
        chasEl.UpdatedAt = now;

        var chasHs = await db.GapChecklistItems.IgnoreQueryFilters()
            .FirstAsync(g => g.TenantId == tenant.Id && g.SchemeId == CatalogueIds.Chas && g.Title.Contains("Health and safety policy"));
        chasHs.Status = GapItemStatus.Complete;
        chasHs.EvidenceItemId = hsPolicy.Id;
        chasHs.UpdatedAt = now;

        var clRams = await db.GapChecklistItems.IgnoreQueryFilters()
            .FirstAsync(g => g.TenantId == tenant.Id && g.SchemeId == CatalogueIds.Constructionline && g.Title.Contains("RAMS"));
        clRams.Status = GapItemStatus.Complete;
        clRams.EvidenceItemId = rams.Id;
        clRams.UpdatedAt = now;

        var accident = new Accident
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            OccurredOn = now.AddDays(-18),
            Location = "Grimsby yard — wash bay",
            Severity = AccidentSeverity.LostTime,
            Status = AccidentStatus.Closed,
            Description = "Operative slipped on the wash-bay floor after a hose leak. First aid given on site. Recorded here as a working log, not a RIDDOR filing.",
            InjuredPerson = "Site operative (initials only: J.P.)",
            ImmediateAction = "Spill cleared, extra matting laid, toolbox talk the next morning.",
            LostHours = 8,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Accidents.Add(accident);
        db.LostHours.Add(new LostHoursEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            AccidentId = accident.Id,
            OccurredOn = now.AddDays(-18),
            Hours = 8,
            Reason = "Lost-time after yard slip",
            Notes = "One shift. No RIDDOR assumed; a competent person should confirm.",
            CreatedAt = now
        });

        db.Equipment.AddRange(
            new EquipmentItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Genie S-65 boom (hired-in typical)",
                Category = "MEWP",
                SerialNumber = "GS65-10422",
                CalibrationDueOn = now.AddDays(-12),
                ServiceDueOn = now.AddDays(40),
                Notes = "LOLER thorough examination overdue — flagged so it is not sent to site.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new EquipmentItem
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Name = "Yard first-aid kit",
                Category = "FirstAid",
                SerialNumber = "FA-YARD-1",
                ServiceDueOn = now.AddDays(120),
                Notes = "Contents checked monthly.",
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync();
        logger.LogInformation("Seeded demo tenant {Org} ({Email} / {Password})", DemoOrganisation, DemoEmail, DemoPassword);
    }

    private static IEnumerable<Scheme> CreateSchemes() =>
    [
        new()
        {
            Id = CatalogueIds.Chas,
            Code = "CHAS",
            Name = "CHAS",
            Provider = "Alcumus CHAS",
            IsSsipStyle = true,
            SortOrder = 1,
            Description = "Contractors Health and Safety Assessment Scheme. Common SSIP member used by UK principal contractors to pre-qualify H&S competence."
        },
        new()
        {
            Id = CatalogueIds.Constructionline,
            Code = "CONSTRUCTIONLINE",
            Name = "Constructionline",
            Provider = "Constructionline (Capita)",
            IsSsipStyle = true,
            SortOrder = 2,
            Description = "UK construction procurement register covering finance, work categories, and SSIP-aligned health and safety questions."
        },
        new()
        {
            Id = CatalogueIds.SafeContractor,
            Code = "SAFECONTRACTOR",
            Name = "SafeContractor",
            Provider = "Alcumus SafeContractor",
            IsSsipStyle = true,
            SortOrder = 3,
            Description = "SSIP-aligned contractor accreditation covering H&S management, insurance, and sector-specific risk."
        },
        new()
        {
            Id = CatalogueIds.Avetta,
            Code = "AVETTA",
            Name = "Avetta",
            Provider = "Avetta",
            IsSsipStyle = false,
            SortOrder = 4,
            Description = "Client-specific supply-chain pre-qualification. Evidence packs are often reused from SSIP schemes but each client portal still needs its own answers."
        },
        new()
        {
            Id = CatalogueIds.Smas,
            Code = "SMAS",
            Name = "SMAS Worksafe",
            Provider = "SMAS",
            IsSsipStyle = true,
            SortOrder = 5,
            Description = "SSIP member scheme widely specified by UK contractors. Mutual recognition (Deem to Satisfy) still leaves portal and document duplication."
        }
    ];

    private static IEnumerable<GapChecklistTemplate> CreateTemplates()
    {
        var common = new (string Title, string Description, string Hint)[]
        {
            ("Employers' Liability insurance", "Current EL certificate meeting the scheme minimum indemnity.", "PDF of the schedule showing indemnity limit and expiry."),
            ("Public Liability insurance", "Current PL certificate, typically £5m–£10m depending on the client.", "PDF of the schedule; check hired-in plant extensions."),
            ("Health and safety policy", "Signed policy dated within the last 12 months, proportionate to headcount.", "Signed PDF plus a one-page organisation chart."),
            ("RAMS / risk assessments", "Generic and, where relevant, activity-specific risk assessments and method statements.", "Current RAMS pack; site-specific versions live in the vault by contract."),
            ("Training and competence records", "CSCS/CPCS (or equivalent) and a training matrix for people who go to site.", "Matrix plus sample cards; do not upload personal data you do not need."),
            ("First aid and accident reporting", "First-aid provision and RIDDOR / accident procedure.", "Named first-aiders and the last 12 months of incident summary.")
        };

        var schemes = new[]
        {
            CatalogueIds.Chas, CatalogueIds.Constructionline, CatalogueIds.SafeContractor,
            CatalogueIds.Avetta, CatalogueIds.Smas
        };

        foreach (var schemeId in schemes)
        {
            for (var i = 0; i < common.Length; i++)
            {
                var row = common[i];
                yield return new GapChecklistTemplate
                {
                    Id = Guid.NewGuid(),
                    SchemeId = schemeId,
                    Title = row.Title,
                    Description = row.Description,
                    EvidenceHint = row.Hint,
                    SortOrder = i + 1
                };
            }
        }
    }
}
