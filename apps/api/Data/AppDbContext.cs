using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

        // SQLite cannot ORDER BY DateTimeOffset. Store as binary locally; SQL Server uses native types.
        if (Database.IsSqlite())
        {
            var converter = new DateTimeOffsetToBinaryConverter();
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType
                    .GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    builder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion(converter);
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
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=schemevault.dev.db")
            .Options;
        return new AppDbContext(options, new NullTenantProvider());
    }
}
