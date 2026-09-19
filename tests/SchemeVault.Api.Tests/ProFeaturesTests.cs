using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace SchemeVault.Api.Tests;

public sealed class ProSubscriptionApiFactory : ApiFactory
{
    protected override string StubPlan => "Pro";
}

public abstract class ProApiTestBase : IAsyncLifetime
{
    protected ProSubscriptionApiFactory Factory { get; } = new();
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

    protected async Task<AuthPayload> RegisterAsync(string organisation, string email)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            organisationName = organisation,
            fullName = "Test User",
            email,
            password = "TestPassw0rd!"
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        var payload = JsonSerializer.Deserialize<AuthPayload>(body, Json);
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

public sealed class ForceProApiFactory : ApiFactory
{
    protected override string StubPlan => "Starter";
    protected override string ForcePro => "true";
}

public class ProEntitlementGateTests : ApiTestBase
{
    [Theory]
    [InlineData("/api/questionnaires")]
    [InlineData("/api/accidents")]
    [InlineData("/api/equipment")]
    [InlineData("/api/lost-hours")]
    public async Task Starter_receives_402_on_pro_routes(string path)
    {
        var auth = await RegisterAsync("Starter Plant Ltd", $"starter-{Guid.NewGuid():N}@schemevault.test");
        var response = await Client.SendAsync(Authed(HttpMethod.Get, path, auth));
        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Pro", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/billing/checkout", body, StringComparison.Ordinal);
        Assert.Contains("requiredPlan", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Starter_can_still_use_vault_and_schemes()
    {
        var auth = await RegisterAsync("Starter Vault Ltd", $"vault-{Guid.NewGuid():N}@schemevault.test");
        var evidence = await Client.SendAsync(Authed(HttpMethod.Get, "/api/evidence", auth));
        evidence.EnsureSuccessStatusCode();
        var schemes = await Client.SendAsync(Authed(HttpMethod.Get, "/api/schemes", auth));
        schemes.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Starter_cannot_export_multi_scheme_pack()
    {
        var auth = await RegisterAsync("Starter Export Ltd", $"export-{Guid.NewGuid():N}@schemevault.test");
        var request = Authed(HttpMethod.Post, "/api/questionnaires/export", auth);
        request.Content = JsonContent.Create(new { schemeCodes = new[] { "CHAS" } });
        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
    }
}

public class ForceProStubTests : IAsyncLifetime
{
    private readonly ForceProApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ForcePro_exposes_pro_plan_and_features_in_stub_mode()
    {
        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            organisationName = "Demo Pro Ltd",
            fullName = "Test User",
            email = "forcepro@schemevault.test",
            password = "TestPassw0rd!"
        });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthPayload>(ApiTestBase.SharedJson);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/billing/entitlements");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("isPro").GetBoolean());
        Assert.Equal("Pro", doc.RootElement.GetProperty("plan").GetString());
        Assert.Equal("pro", doc.RootElement.GetProperty("planCode").GetString(), ignoreCase: true);
        var features = doc.RootElement.GetProperty("features").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("questionnaires", features);
        Assert.Contains("accidents", features);
        Assert.Contains("equipment", features);

        var questionnaires = new HttpRequestMessage(HttpMethod.Get, "/api/questionnaires");
        questionnaires.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var q = await _client.SendAsync(questionnaires);
        q.EnsureSuccessStatusCode();
    }
}

public class QuestionnaireApiTests : ProApiTestBase
{
    [Fact]
    public async Task Completing_a_questionnaire_generates_markdown_and_pdf()
    {
        var auth = await RegisterAsync("Fenland Questionnaires Ltd", "qs@fenland.test");
        var list = await Client.SendAsync(Authed(HttpMethod.Get, "/api/questionnaires", auth));
        list.EnsureSuccessStatusCode();
        using var listDoc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.True(listDoc.RootElement.GetArrayLength() >= 4);

        var save = Authed(HttpMethod.Post, "/api/questionnaires/schemes/CHAS/responses", auth);
        save.Content = JsonContent.Create(new
        {
            generate = true,
            answers = SampleAnswers()
        });
        var created = await Client.SendAsync(save);
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, createdBody);
        using var createdDoc = JsonDocument.Parse(createdBody);
        var id = createdDoc.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Generated", createdDoc.RootElement.GetProperty("status").GetString());
        var markdown = createdDoc.RootElement.GetProperty("generatedMarkdown").GetString();
        Assert.Contains("working draft", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not an official", markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fenland Questionnaires Ltd", markdown, StringComparison.Ordinal);

        var pdf = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/questionnaires/responses/{id}/pdf", auth));
        pdf.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes[..4]));
    }

    [Fact]
    public async Task Multi_scheme_export_combines_generated_packs()
    {
        var auth = await RegisterAsync("Humber Pack Ltd", "pack@humber.test");
        foreach (var code in new[] { "CHAS", "AVETTA" })
        {
            var save = Authed(HttpMethod.Post, $"/api/questionnaires/schemes/{code}/responses", auth);
            save.Content = JsonContent.Create(new { generate = true, answers = SampleAnswers() });
            var created = await Client.SendAsync(save);
            Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
        }

        var export = Authed(HttpMethod.Post, "/api/questionnaires/export", auth);
        export.Content = JsonContent.Create(new { schemeCodes = new[] { "CHAS", "AVETTA" } });
        var response = await Client.SendAsync(export);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes[..4]));
    }

    [Fact]
    public async Task Second_tenant_cannot_read_first_tenant_questionnaire()
    {
        var alpha = await RegisterAsync("Alpha QS Ltd", "alpha-qs@schemevault.test");
        var bravo = await RegisterAsync("Bravo QS Ltd", "bravo-qs@schemevault.test");
        var save = Authed(HttpMethod.Post, "/api/questionnaires/schemes/CHAS/responses", alpha);
        save.Content = JsonContent.Create(new { generate = true, answers = SampleAnswers() });
        var created = await Client.SendAsync(save);
        created.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = doc.RootElement.GetProperty("id").GetGuid();

        var bravoGet = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/questionnaires/responses/{id}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoGet.StatusCode);
        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/questionnaires/responses", bravo));
        var bravoBody = await bravoList.Content.ReadAsStringAsync();
        Assert.DoesNotContain(id.ToString(), bravoBody, StringComparison.OrdinalIgnoreCase);
    }

    internal static Dictionary<string, string> SampleAnswers() => new()
    {
        ["companyName"] = "Fenland Questionnaires Ltd",
        ["tradingAddress"] = "12 Wharf Road, Wisbech, PE13 1AA",
        ["headcount"] = "14",
        ["workDescription"] = "Mechanical and electrical installation on commercial sites.",
        ["hsResponsible"] = "Sam Reed, director",
        ["hsPolicy"] = "Yes — signed by the director in January.",
        ["elInsurance"] = "Aviva, expires 31 March 2027",
        ["plInsurance"] = "Hiscox, £10 million, expires 31 March 2027",
        ["competence"] = "CSCS cards checked, training matrix reviewed quarterly.",
        ["rams"] = "Supervisor writes RAMS; director signs; copy in the site folder.",
        ["accidents"] = "Reported to the director the same day, written in the accident book. RIDDOR decided by the director with an adviser.",
        ["firstAid"] = "Two appointed first-aiders in the yard; kits in the office and van.",
        ["enforcement"] = "No"
    };
}

public class AccidentApiTests : ProApiTestBase
{
    [Fact]
    public async Task Accident_and_lost_hours_are_tenant_scoped()
    {
        var alpha = await RegisterAsync("Alpha Safety Ltd", "alpha-hs@schemevault.test");
        var bravo = await RegisterAsync("Bravo Safety Ltd", "bravo-hs@schemevault.test");

        var create = Authed(HttpMethod.Post, "/api/accidents", alpha);
        create.Content = JsonContent.Create(new
        {
            occurredOn = DateTimeOffset.UtcNow.AddDays(-2),
            location = "Alpha yard",
            severity = "LostTime",
            status = "Open",
            description = "Private Alpha incident — do not leak.",
            lostHours = 6.5
        });
        var created = await Client.SendAsync(create);
        var createdBody = await created.Content.ReadAsStringAsync();
        Assert.True(created.IsSuccessStatusCode, createdBody);
        using var createdDoc = JsonDocument.Parse(createdBody);
        var accidentId = createdDoc.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(6.5m, createdDoc.RootElement.GetProperty("lostHours").GetDecimal());

        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/accidents", bravo));
        bravoList.EnsureSuccessStatusCode();
        var bravoBody = await bravoList.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Private Alpha incident", bravoBody, StringComparison.Ordinal);
        Assert.DoesNotContain(accidentId.ToString(), bravoBody, StringComparison.OrdinalIgnoreCase);

        var bravoGet = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/accidents/{accidentId}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoGet.StatusCode);

        var hours = await Client.SendAsync(Authed(HttpMethod.Get, "/api/lost-hours", bravo));
        hours.EnsureSuccessStatusCode();
        using var hoursDoc = JsonDocument.Parse(await hours.Content.ReadAsStringAsync());
        Assert.Equal(0m, hoursDoc.RootElement.GetProperty("totalHours").GetDecimal());

        var alphaHours = await Client.SendAsync(Authed(HttpMethod.Get, "/api/lost-hours", alpha));
        using var alphaHoursDoc = JsonDocument.Parse(await alphaHours.Content.ReadAsStringAsync());
        Assert.Equal(6.5m, alphaHoursDoc.RootElement.GetProperty("totalHours").GetDecimal());
    }

    [Fact]
    public async Task Extra_lost_hours_can_link_to_an_accident()
    {
        var auth = await RegisterAsync("Hours Ltd", "hours@schemevault.test");
        var create = Authed(HttpMethod.Post, "/api/accidents", auth);
        create.Content = JsonContent.Create(new
        {
            occurredOn = DateTimeOffset.UtcNow,
            location = "Site A",
            severity = "MinorInjury",
            description = "Cut finger, first aid only.",
            lostHours = 0
        });
        var created = await Client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        var accidentId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var hours = Authed(HttpMethod.Post, "/api/lost-hours", auth);
        hours.Content = JsonContent.Create(new
        {
            occurredOn = DateTimeOffset.UtcNow,
            hours = 2,
            reason = "Hospital wait",
            accidentId
        });
        var added = await Client.SendAsync(hours);
        added.EnsureSuccessStatusCode();

        var accident = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/accidents/{accidentId}", auth));
        using var doc = JsonDocument.Parse(await accident.Content.ReadAsStringAsync());
        Assert.Equal(2m, doc.RootElement.GetProperty("lostHours").GetDecimal());
    }
}

public class EquipmentApiTests : ProApiTestBase
{
    [Fact]
    public async Task Equipment_overdue_flag_and_tenant_isolation()
    {
        var alpha = await RegisterAsync("Alpha Plant Register Ltd", "alpha-eq@schemevault.test");
        var bravo = await RegisterAsync("Bravo Plant Register Ltd", "bravo-eq@schemevault.test");

        var create = Authed(HttpMethod.Post, "/api/equipment", alpha);
        create.Content = JsonContent.Create(new
        {
            name = "Alpha secret MEWP",
            category = "MEWP",
            serialNumber = "ALPHA-HIDDEN",
            calibrationDueOn = DateTimeOffset.UtcNow.AddDays(-5),
            serviceDueOn = DateTimeOffset.UtcNow.AddDays(90)
        });
        var created = await Client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        using var createdDoc = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdDoc.RootElement.GetProperty("id").GetGuid();
        Assert.True(createdDoc.RootElement.GetProperty("isOverdue").GetBoolean());
        Assert.Equal("Red", createdDoc.RootElement.GetProperty("trafficLight").GetString());

        var overdue = await Client.SendAsync(Authed(HttpMethod.Get, "/api/equipment?overdue=true", alpha));
        overdue.EnsureSuccessStatusCode();
        var overdueBody = await overdue.Content.ReadAsStringAsync();
        Assert.Contains("Alpha secret MEWP", overdueBody, StringComparison.Ordinal);

        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, "/api/equipment", bravo));
        var bravoBody = await bravoList.Content.ReadAsStringAsync();
        Assert.DoesNotContain("ALPHA-HIDDEN", bravoBody, StringComparison.Ordinal);

        var bravoGet = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/equipment/{id}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoGet.StatusCode);

        var bravoDelete = await Client.SendAsync(Authed(HttpMethod.Delete, $"/api/equipment/{id}", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoDelete.StatusCode);

        var inDate = Authed(HttpMethod.Post, "/api/equipment", alpha);
        inDate.Content = JsonContent.Create(new
        {
            name = "First-aid kit",
            category = "FirstAid",
            serviceDueOn = DateTimeOffset.UtcNow.AddDays(120)
        });
        (await Client.SendAsync(inDate)).EnsureSuccessStatusCode();

        var delete = await Client.SendAsync(Authed(HttpMethod.Delete, $"/api/equipment/{id}", alpha));
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        var gone = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/equipment/{id}", alpha));
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}

public class PhotoApiTests : ProApiTestBase
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public async Task Photos_attach_to_evidence_and_are_isolated()
    {
        var alpha = await RegisterAsync("Alpha Photos Ltd", "alpha-ph@schemevault.test");
        var bravo = await RegisterAsync("Bravo Photos Ltd", "bravo-ph@schemevault.test");

        var create = Authed(HttpMethod.Post, "/api/evidence", alpha);
        create.Content = JsonContent.Create(new { title = "Site photo folder", category = "Photo" });
        var created = await Client.SendAsync(create);
        created.EnsureSuccessStatusCode();
        var evidenceId = JsonDocument.Parse(await created.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var upload = Authed(HttpMethod.Post, $"/api/evidence/{evidenceId}/photos", alpha);
        upload.Content = PhotoContent();
        var uploaded = await Client.SendAsync(upload);
        var uploadedBody = await uploaded.Content.ReadAsStringAsync();
        Assert.True(uploaded.IsSuccessStatusCode, uploadedBody);
        var photoId = JsonDocument.Parse(uploadedBody).RootElement.GetProperty("id").GetGuid();

        var bravoList = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/evidence/{evidenceId}/photos", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoList.StatusCode);

        var bravoFile = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/photos/{photoId}/file", bravo));
        Assert.Equal(HttpStatusCode.NotFound, bravoFile.StatusCode);

        var alphaFile = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/photos/{photoId}/file", alpha));
        alphaFile.EnsureSuccessStatusCode();
        Assert.Equal("image/png", alphaFile.Content.Headers.ContentType?.MediaType);
        var bytes = await alphaFile.Content.ReadAsByteArrayAsync();
        Assert.Equal(Png, bytes);

        var evidence = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/evidence/{evidenceId}", alpha));
        using var ev = JsonDocument.Parse(await evidence.Content.ReadAsStringAsync());
        Assert.Equal(1, ev.RootElement.GetProperty("photoCount").GetInt32());
    }

    [Fact]
    public async Task Accident_and_equipment_photos_round_trip()
    {
        var auth = await RegisterAsync("Photo Kit Ltd", "kit-ph@schemevault.test");

        var accidentReq = Authed(HttpMethod.Post, "/api/accidents", auth);
        accidentReq.Content = JsonContent.Create(new
        {
            occurredOn = DateTimeOffset.UtcNow,
            location = "Bay 2",
            severity = "NearMiss",
            description = "Dropped stillage, no injury."
        });
        var accident = await Client.SendAsync(accidentReq);
        accident.EnsureSuccessStatusCode();
        var accidentId = JsonDocument.Parse(await accident.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var accPhoto = Authed(HttpMethod.Post, $"/api/accidents/{accidentId}/photos", auth);
        accPhoto.Content = PhotoContent("yard.png");
        (await Client.SendAsync(accPhoto)).EnsureSuccessStatusCode();

        var eqReq = Authed(HttpMethod.Post, "/api/equipment", auth);
        eqReq.Content = JsonContent.Create(new { name = "Harness A", category = "PPE", serviceDueOn = DateTimeOffset.UtcNow.AddDays(10) });
        var eq = await Client.SendAsync(eqReq);
        eq.EnsureSuccessStatusCode();
        var eqId = JsonDocument.Parse(await eq.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var eqPhoto = Authed(HttpMethod.Post, $"/api/equipment/{eqId}/photos", auth);
        eqPhoto.Content = PhotoContent("label.png");
        (await Client.SendAsync(eqPhoto)).EnsureSuccessStatusCode();

        var accList = await Client.SendAsync(Authed(HttpMethod.Get, $"/api/accidents/{accidentId}/photos", auth));
        accList.EnsureSuccessStatusCode();
        Assert.Equal(1, JsonDocument.Parse(await accList.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
    }

    private static MultipartFormDataContent PhotoContent(string name = "photo.png")
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", name);
        return content;
    }
}

public class EntitlementsNormalizerTests
{
    [Fact]
    public void ForcePro_promotes_starter_to_pro_features()
    {
        var dto = SchemeVault.Api.Billing.EntitlementsDto.Create("active", "Starter", "starter");
        var applied = SchemeVault.Api.Billing.EntitlementsNormalizer.Apply(
            dto,
            new SchemeVault.Api.Billing.SubscriptionApiOptions { ForcePro = true, StubPlan = "Starter" });
        Assert.True(applied.IsPro);
        Assert.Equal("Pro", applied.Plan);
        Assert.Contains("questionnaires", applied.Features);
        Assert.Contains("multi_scheme_export", applied.Features);
    }

    [Fact]
    public void Feature_flags_from_qck_grant_pro_feature_without_plan_rename_if_already_named()
    {
        var dto = SchemeVault.Api.Billing.EntitlementsDto.Create(
            "active",
            "Starter",
            "starter",
            ["questionnaires"]);
        var applied = SchemeVault.Api.Billing.EntitlementsNormalizer.Apply(
            dto,
            new SchemeVault.Api.Billing.SubscriptionApiOptions());
        Assert.False(applied.IsPro);
        Assert.True(SchemeVault.Api.Billing.EntitlementsNormalizer.HasFeature(applied, "questionnaires"));
        Assert.False(SchemeVault.Api.Billing.EntitlementsNormalizer.HasFeature(applied, "accidents"));
    }
}
