namespace ZedLauncher.Services;

public interface IUpdateService
{
    Task<string> FetchReleaseVersionAsync();

    Task UpdateAsync(IProgress<int>? progress = null);

    bool IsExecutableLocked();
}