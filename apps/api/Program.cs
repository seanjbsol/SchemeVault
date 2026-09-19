using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SchemeVault.Api.Auth;
using SchemeVault.Api.Billing;
using SchemeVault.Api.Data;
using SchemeVault.Api.Domain;
using SchemeVault.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpRequestContext>();
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<HttpRequestContext>());
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<HttpRequestContext>());

var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=schemevault.dev.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Set Jwt:SigningKey (or Jwt__SigningKey) to a secret of at least 32 characters. See README.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = "role",
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.Configure<SubscriptionApiOptions>(
    builder.Configuration.GetSection(SubscriptionApiOptions.SectionName));
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<SubscriptionGateFilter>();
builder.Services.AddScoped<ProGateFilter>();
RegisterSubscriptionClient(builder);

builder.Services.AddControllers(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
        options.Filters.AddService<SubscriptionGateFilter>();
        options.Filters.AddService<ProGateFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SchemeVault API",
        Version = "v1",
        Description = "UK multi-scheme contractor compliance vault and renewal OS."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT from POST /api/auth/login. Paste the token only (Swagger prefixes Bearer).",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EvidenceService>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<AccidentService>();
builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<QuestionnaireService>();
builder.Services.AddScoped<RenewalService>();
builder.Services.AddScoped<SchemeQueryService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<TenantService>();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var error = feature?.Error;
        var expose = app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing");
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ExceptionHandler");
        logger.LogError(error, "Unhandled exception");

        context.Response.ContentType = "application/problem+json";
        if (error is TenantAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { title = "Not found." });
            return;
        }

        if (error is InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { title = error.Message });
            return;
        }

        if (error is UnauthorizedAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { title = error.Message });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var message = expose ? error?.ToString() : "An unexpected error occurred.";
        await context.Response.WriteAsJsonAsync(new { title = message });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "SchemeVault API",
    utc = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapControllers();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    await db.Database.MigrateAsync();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await SeedData.ApplyAsync(db, userManager, logger);
}

app.Run();

static void RegisterSubscriptionClient(WebApplicationBuilder builder)
{
    var subscription = builder.Configuration.GetSection(SubscriptionApiOptions.SectionName)
        .Get<SubscriptionApiOptions>() ?? new SubscriptionApiOptions();

    if (subscription.UseStub)
    {
        builder.Services.AddSingleton<ISubscriptionClient, StubSubscriptionClient>();
        return;
    }

    builder.Services.AddHttpClient<ISubscriptionClient, SubscriptionClient>((sp, client) =>
    {
        var opts = sp.GetRequiredService<IOptions<SubscriptionApiOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(opts.BaseUrl) &&
            Uri.TryCreate(opts.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri))
        {
            client.BaseAddress = baseUri;
        }

        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });
}

public partial class Program;
