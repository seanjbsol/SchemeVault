using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Domain;

namespace SchemeVault.Api.Data;

public class AppDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Captured at construction so global query filters are SQL-translatable
    /// (simple bool/Guid fields), not closures over <see cref="HttpContext"/>.
    /// </summary>
    private readonly Guid _tenantId;
    private readonly bool _applyTenantFilter;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenant)
        : base(options)
    {
        _tenantId = tenant.TenantId;
        _applyTenantFilter = tenant.IsAvailable;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Scheme> Schemes => Set<Scheme>();
    public DbSet<GapChecklistTemplate> GapChecklistTemplates => Set<GapChecklistTemplate>();
    public DbSet<EvidenceItem> EvidenceItems => Set<EvidenceItem>();
    public DbSet<Accreditation> Accreditations => Set<Accreditation>();
    public DbSet<GapChecklistItem> GapChecklistItems => Set<GapChecklistItem>();
    public DbSet<PhotoAttachment> Photos => Set<PhotoAttachment>();
    public DbSet<Accident> Accidents => Set<Accident>();
    public DbSet<LostHoursEntry> LostHours => Set<LostHoursEntry>();
    public DbSet<EquipmentItem> Equipment => Set<EquipmentItem>();
    public DbSet<QuestionnaireResponse> QuestionnaireResponses => Set<QuestionnaireResponse>();

    /// <summary>
    /// Identity 10 passkeys (schema v3). Forced here so design-time and tests that
    /// rebuild <see cref="DbContextOptions"/> still get a valid model.
    /// </summary>
    protected override Version SchemaVersion => IdentitySchemaVersions.Version3;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Membership>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Memberships).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.User).WithMany(u => u.Memberships).HasForeignKey(x => x.UserId);
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<Scheme>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(40).IsRequired();
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Provider).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        });

        builder.Entity<GapChecklistTemplate>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Scheme).WithMany(s => s.GapTemplates).HasForeignKey(x => x.SchemeId);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            e.Property(x => x.EvidenceHint).HasMaxLength(500).IsRequired();
        });

        builder.Entity<EvidenceItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.EvidenceItems).HasForeignKey(x => x.TenantId);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(80).IsRequired();
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<Accreditation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.SchemeId }).IsUnique();
            e.HasOne(x => x.Tenant).WithMany(t => t.Accreditations).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.Scheme).WithMany(s => s.Accreditations).HasForeignKey(x => x.SchemeId);
            e.Property(x => x.MembershipNumber).HasMaxLength(80);
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<GapChecklistItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.GapItems).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.Scheme).WithMany().HasForeignKey(x => x.SchemeId);
            e.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.EvidenceItem).WithMany().HasForeignKey(x => x.EvidenceItemId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<PhotoAttachment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.Photos).HasForeignKey(x => x.TenantId);
            e.HasIndex(x => new { x.TenantId, x.OwnerKind, x.OwnerId });
            e.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.StoredFileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.Caption).HasMaxLength(300);
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<Accident>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.Accidents).HasForeignKey(x => x.TenantId);
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            e.Property(x => x.InjuredPerson).HasMaxLength(200);
            e.Property(x => x.ImmediateAction).HasMaxLength(2000);
            e.Property(x => x.LostHours).HasPrecision(8, 2);
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<LostHoursEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.LostHours).HasForeignKey(x => x.TenantId);
            e.HasOne(x => x.Accident).WithMany(a => a.LostHoursEntries).HasForeignKey(x => x.AccidentId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Hours).HasPrecision(8, 2);
            e.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<EquipmentItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.Equipment).HasForeignKey(x => x.TenantId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Category).HasMaxLength(80).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(80);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        builder.Entity<QuestionnaireResponse>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Tenant).WithMany(t => t.QuestionnaireResponses).HasForeignKey(x => x.TenantId);
            e.Property(x => x.SchemeCode).HasMaxLength(40).IsRequired();
            e.Property(x => x.AnswersJson).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.SchemeCode, x.UpdatedAt });
            e.HasQueryFilter(x => !_applyTenantFilter || x.TenantId == _tenantId);
        });

        // SQLite cannot ORDER BY DateTimeOffset. Store as binary locally; SQL Server uses native types.
        if (Database.IsSqlite())
        {
            var converter = new DateTimeOffsetToBinaryConverter();
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (entityType.IsOwned())
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(converter);
                    }
                }
            }
        }
    }
}

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without HTTP context.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(options =>
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
        var provider = services.BuildServiceProvider();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=schemevault.dev.db")
            .UseApplicationServiceProvider(provider)
            .Options;
        return new AppDbContext(options, new NullTenantProvider());
    }
}
