namespace SchemeVault.Api.Services;

public interface IFileStorage
{
    Task<(string StoredFileName, long Size)> SaveAsync(
        Guid tenantId,
        Guid evidenceId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(Guid tenantId, string storedFileName, CancellationToken cancellationToken);

    Task DeleteAsync(Guid tenantId, string storedFileName, CancellationToken cancellationToken);
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IConfiguration configuration, IWebHostEnvironment env, ILogger<LocalFileStorage> logger)
    {
        var configured = configuration["Storage:RootPath"] ?? "App_Data/uploads";
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(env.ContentRootPath, configured);
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public async Task<(string StoredFileName, long Size)> SaveAsync(
        Guid tenantId,
        Guid evidenceId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "upload.bin";
        }

        var stored = $"{evidenceId:N}_{safeName}";
        var dir = TenantDir(tenantId);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, stored);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, cancellationToken);
        var size = file.Length;
        _logger.LogInformation("Stored evidence file for tenant {TenantId} at {Path} ({Size} bytes)", tenantId, stored, size);
        return (stored, size);
    }

    public Task<Stream?> OpenReadAsync(Guid tenantId, string storedFileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(TenantDir(tenantId), Path.GetFileName(storedFileName));
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(Guid tenantId, string storedFileName, CancellationToken cancellationToken)
    {
        var path = Path.Combine(TenantDir(tenantId), Path.GetFileName(storedFileName));
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string TenantDir(Guid tenantId) => Path.Combine(_root, tenantId.ToString("N"));
}
