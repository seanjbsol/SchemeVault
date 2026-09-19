using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchemeVault.Api.Data;

namespace SchemeVault.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"schemevault-tests-{Guid.NewGuid():N}.db");

    protected virtual string StubStatus => "active";
    protected virtual string StubPlan => "Starter";
    protected virtual string ForcePro => "false";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Issuer", "SchemeVault");
        builder.UseSetting("Jwt:Audience", "SchemeVault.Mobile");
        builder.UseSetting("Jwt:SigningKey", "TESTING-ONLY-do-not-use-in-production-schemevault-key");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("SubscriptionApi:UseStub", "true");
        builder.UseSetting("SubscriptionApi:ProductCode", "SchemeVault");
        builder.UseSetting("SubscriptionApi:StubStatus", StubStatus);
        builder.UseSetting("SubscriptionApi:StubPlan", StubPlan);
        builder.UseSetting("SubscriptionApi:ForcePro", ForcePro);

        builder.ConfigureServices(services =>
        {
            RemoveDbContext(services);
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseSqlite($"Data Source={_dbPath}");
                options.UseApplicationServiceProvider(sp);
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        await SeedData.EnsureSchemesAsync(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // best-effort cleanup
        }
    }

    private static void RemoveDbContext(IServiceCollection services)
    {
        var victims = services
            .Where(d =>
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.Contains("DbContextOptions", StringComparison.Ordinal)))
            .ToList();
        foreach (var d in victims)
        {
            services.Remove(d);
        }
    }
}

public abstract class ApiTestBase : IAsyncLifetime
{
    protected ApiFactory Factory { get; } = new();
    protected HttpClient Client { get; private set; } = null!;
    internal static readonly JsonSerializerOptions SharedJson = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected static JsonSerializerOptions Json => SharedJson;

    public async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<AuthPayload> RegisterAsync(
        string organisation,
        string email,
        string password = "TestPassw0rd!",
        string fullName = "Test User")
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organisationName = organisation,
            fullName,
            email,
            password
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Register failed ({response.StatusCode}): {body}");
        var payload = JsonSerializer.Deserialize<AuthPayload>(body, Json);
        Assert.NotNull(payload);
        return payload!;
    }

    protected async Task<AuthPayload> LoginAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(Json);
        Assert.NotNull(payload);
        return payload!;
    }

    protected HttpRequestMessage Authed(HttpMethod method, string url, AuthPayload auth)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return request;
    }
}

public sealed class AuthPayload
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public AuthUser User { get; set; } = null!;
}

public sealed class AuthUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class HealthTests : ApiTestBase
{
    [Fact]
    public async Task Health_returns_ok_without_auth()
    {
        var response = await Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("SchemeVault API", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resource_endpoints_require_auth()
    {
        var response = await Client.GetAsync("/api/evidence");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class AuthAndTenantTests : ApiTestBase
{
    [Fact]
    public async Task Register_creates_tenant_and_owner_jwt_claims()
    {
        var auth = await RegisterAsync("Humber Civils Ltd", "owner@humber.test");
        Assert.Equal("Humber Civils Ltd", auth.User.TenantName);
        Assert.Equal("Owner", auth.User.Role);
        Assert.NotEqual(Guid.Empty, auth.User.TenantId);

        var me = await Client.SendAsync(Authed(HttpMethod.Get, "/api/auth/me", auth));
        me.EnsureSuccessStatusCode();
        var user = await me.Content.ReadFromJsonAsync<AuthUser>(Json);
        Assert.Equal(auth.User.TenantId, user!.TenantId);
        Assert.Equal("Owner", user.Role);
    }
}

public class TenantIsolationTests : ApiTestBase
{
    [Fact]
    public async Task Second_tenant_cannot_read_or_mutate_first_tenant_evidence()
    {
        var alpha = await RegisterAsync("Alpha Plant Ltd", "alpha@schemevault.test");
        var bravo = await RegisterAsync("Bravo Scaffolding Ltd", "bravo@schemevault.test");
        Assert.NotEqual(alpha.User.TenantId, bravo.User.TenantId);

        var create = Authed(HttpMethod.Post, "/api/evidence", alpha);
        create.Content = JsonContent.Create(new
        {
            title = "Alpha EL certificate",
            category = "Insurance",
            notes = "Private to Alpha"
        });
        var created = await Client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var evidenceId = createdDoc.RootElement.GetProperty("id").GetGuid();

        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/evidence", bravo));
        bravoList.EnsureSuccessStatusCode();
        using var bravoDoc = JsonDocument.Parse(await bravoList.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, bravoDoc.RootElement.ValueKind);
        Assert.Equal(0, bravoDoc.RootElement.GetArrayLength());

        var bravoGet = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/evidence/{evidenceId}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoGet.StatusCode);
        var bravoBody = await bravoGet.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Alpha EL certificate", bravoBody, StringComparison.Ordinal);

        var bravoPut = Authed(HttpMethod.Put, $"/api/evidence/{evidenceId}", bravo);
        bravoPut.Content = JsonContent.Create(new { title = "Hijacked", category = "Insurance" });
        var putResponse = await Client.SendAsync(bravoPut);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);

        var bravoDelete = await Client.SendAsync(Authed(HttpMethod.Delete, $"/api/evidence/{evidenceId}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoDelete.StatusCode);

        var alphaGet = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/evidence/{evidenceId}", alpha));
        alphaGet.EnsureSuccessStatusCode();
        var alphaBody = await alphaGet.Content.ReadAsStringAsync();
        Assert.Contains("Alpha EL certificate", alphaBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Second_tenant_cannot_see_first_tenant_renewals()
    {
        var alpha = await RegisterAsync("Alpha Plant Ltd", "alpha2@schemevault.test");
        var bravo = await RegisterAsync("Bravo Scaffolding Ltd", "bravo2@schemevault.test");

        var schemeList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/schemes", alpha));
        schemeList.EnsureSuccessStatusCode();
        using var schemes = JsonDocument.Parse(await schemeList.Content.ReadAsStringAsync());
        var chasId = schemes.RootElement.EnumerateArray()
            .First(s => s.GetProperty("code").GetString() == "CHAS")
            .GetProperty("id")
            .GetGuid();

        var create = Authed(HttpMethod.Post, "/api/renewals", alpha);
        create.Content = JsonContent.Create(new
        {
            schemeId = chasId,
            status = "Active",
            expiresOn = DateTimeOffset.UtcNow.AddDays(30),
            membershipNumber = "ALPHA-SECRET-99"
        });
        var created = await Client.SendAsync(create);
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, createdBody);
        using var createdDoc = JsonDocument.Parse(createdBody);
        var renewalId = createdDoc.RootElement.GetProperty("id").GetGuid();

        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/renewals", bravo));
        bravoList.EnsureSuccessStatusCode();
        var bravoBody = await bravoList.Content.ReadAsStringAsync();
        Assert.DoesNotContain("ALPHA-SECRET-99", bravoBody, StringComparison.Ordinal);

        var bravoGet = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/renewals/{renewalId}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoGet.StatusCode);

        var dashboard = await Client.SendAsync(Authed(HttpMethod.Get, "/api/dashboard", bravo));
        dashboard.EnsureSuccessStatusCode();
        var dashBody = await dashboard.Content.ReadAsStringAsync();
        Assert.DoesNotContain("ALPHA-SECRET-99", dashBody, StringComparison.Ordinal);
        Assert.Contains("Bravo Scaffolding Ltd", dashBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Client_cannot_stamp_another_tenant_id_onto_new_evidence()
    {
        var alpha = await RegisterAsync("Alpha Plant Ltd", "alpha3@schemevault.test");
        var bravo = await RegisterAsync("Bravo Scaffolding Ltd", "bravo3@schemevault.test");

        var create = Authed(HttpMethod.Post, "/api/evidence", bravo);
        create.Content = JsonContent.Create(new
        {
            title = "Should stay in Bravo",
            category = "Policy",
            tenantId = alpha.User.TenantId
        });
        var created = await Client.SendAsync(create);
        created.EnsureSuccessStatusCode();

        var alphaList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/evidence", alpha));
        var alphaBody = await alphaList.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Should stay in Bravo", alphaBody, StringComparison.Ordinal);

        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/evidence", bravo));
        var bravoBody = await bravoList.Content.ReadAsStringAsync();
        Assert.Contains("Should stay in Bravo", bravoBody, StringComparison.Ordinal);
    }
}

public class VerticalSliceTests : ApiTestBase
{
    [Fact]
    public async Task New_tenant_gets_scheme_catalogue_and_can_crud_renewal()
    {
        var auth = await RegisterAsync("Fenland M&E Ltd", "ops@fenland.test");
        var schemes = await Client.SendAsync(Authed(HttpMethod.Get, "/api/schemes", auth));
        schemes.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await schemes.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetArrayLength() >= 5);

        var smas = doc.RootElement.EnumerateArray().First(s => s.GetProperty("code").GetString() == "SMAS");
        var smasId = smas.GetProperty("id").GetGuid();
        Assert.True(smas.GetProperty("missingGaps").GetInt32() > 0);

        var create = Authed(HttpMethod.Post, "/api/renewals", auth);
        create.Content = JsonContent.Create(new
        {
            schemeId = smasId,
            status = "InProgress",
            notes = "Pack in progress"
        });
        var created = await Client.SendAsync(create);
        created.EnsureSuccessStatusCode();

        var list = await Client.SendAsync(Authed(HttpMethod.Get, "/api/renewals", auth));
        var listBody = await list.Content.ReadAsStringAsync();
        Assert.Contains("Pack in progress", listBody, StringComparison.Ordinal);
    }
}

public sealed class InactiveSubscriptionApiFactory : ApiFactory
{
    protected override string StubStatus => "canceled";
}

public abstract class InactiveSubscriptionTestBase : IAsyncLifetime
{
    protected InactiveSubscriptionApiFactory Factory { get; } = new();
    protected HttpClient Client { get; private set; } = null!;
    protected static readonly JsonSerializerOptions Json = ApiTestBase.SharedJson;

    public async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        Factory.Dispose();
        return Task.CompletedTask;
    }
}

public class BillingStubTests : ApiTestBase
{
    [Fact]
    public async Task Entitlements_in_stub_mode_are_active_starter()
    {
        var auth = await RegisterAsync("Fenland Billing Ltd", "billing@fenland.test");
        var response = await Client.SendAsync(Authed(HttpMethod.Get, "/api/billing/entitlements", auth));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("active", doc.RootElement.GetProperty("status").GetString(), ignoreCase: true);
        Assert.Equal("Starter", doc.RootElement.GetProperty("plan").GetString());
        Assert.False(doc.RootElement.GetProperty("isPro").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Owner_can_open_stub_checkout_and_portal_urls()
    {
        var auth = await RegisterAsync("Humber Billing Ltd", "owner@humber-billing.test");

        var checkout = Authed(HttpMethod.Post, "/api/billing/checkout", auth);
        checkout.Content = JsonContent.Create(new
        {
            successUrl = "https://schemevault.test/billing/success",
            cancelUrl = "https://schemevault.test/billing/cancel"
        });
        var checkoutResponse = await Client.SendAsync(checkout);
        checkoutResponse.EnsureSuccessStatusCode();
        using var checkoutDoc = JsonDocument.Parse(await checkoutResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            SchemeVault.Api.Billing.StubSubscriptionClient.CheckoutUrl,
            checkoutDoc.RootElement.GetProperty("url").GetString());

        var portal = Authed(HttpMethod.Post, "/api/billing/portal", auth);
        portal.Content = JsonContent.Create(new { returnUrl = "https://schemevault.test/settings" });
        var portalResponse = await Client.SendAsync(portal);
        portalResponse.EnsureSuccessStatusCode();
        using var portalDoc = JsonDocument.Parse(await portalResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            SchemeVault.Api.Billing.StubSubscriptionClient.PortalUrl,
            portalDoc.RootElement.GetProperty("url").GetString());
    }
}

public class SubscriptionGateTests : InactiveSubscriptionTestBase
{
    [Fact]
    public async Task Inactive_subscription_returns_402_with_checkout_pointer()
    {
        var register = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organisationName = "Past Due Plant Ltd",
            fullName = "Test User",
            email = "pastdue@schemevault.test",
            password = "TestPassw0rd!"
        });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthPayload>(Json);
        Assert.NotNull(auth);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/evidence");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var evidence = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.PaymentRequired, evidence.StatusCode);
        var body = await evidence.Content.ReadAsStringAsync();
        Assert.Contains("/api/billing/checkout", body, StringComparison.Ordinal);
        Assert.Contains("canceled", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Billing_and_auth_remain_available_when_subscription_is_inactive()
    {
        var register = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organisationName = "Lapsed Scaffolding Ltd",
            fullName = "Test User",
            email = "lapsed@schemevault.test",
            password = "TestPassw0rd!"
        });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthPayload>(Json);
        Assert.NotNull(auth);

        var me = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        me.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var meResponse = await Client.SendAsync(me);
        meResponse.EnsureSuccessStatusCode();

        var entitlements = new HttpRequestMessage(HttpMethod.Get, "/api/billing/entitlements");
        entitlements.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var entitlementsResponse = await Client.SendAsync(entitlements);
        entitlementsResponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await entitlementsResponse.Content.ReadAsStringAsync());
        Assert.Equal("canceled", doc.RootElement.GetProperty("status").GetString(), ignoreCase: true);
        Assert.False(doc.RootElement.GetProperty("isActive").GetBoolean());

        var checkout = new HttpRequestMessage(HttpMethod.Post, "/api/billing/checkout");
        checkout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        checkout.Content = JsonContent.Create(new
        {
            successUrl = "https://schemevault.test/ok",
            cancelUrl = "https://schemevault.test/cancel"
        });
        var checkoutResponse = await Client.SendAsync(checkout);
        checkoutResponse.EnsureSuccessStatusCode();
    }
}
