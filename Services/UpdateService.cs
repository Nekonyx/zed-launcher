using System.Text.Json;
using ZedLauncher.Configuration;
using ZedLauncher.Exceptions;

namespace ZedLauncher.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly LauncherConfig _config;

    public UpdateService(HttpClient httpClient, LauncherConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<string> FetchReleaseVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(_config.ReleasesUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json);

            return releases?.FirstOrDefault()?.Name
                   ?? throw new InvalidOperationException("No releases found");
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateException("Failed to fetch release information", ex);
        }
        catch (JsonException ex)
        {
            throw new UpdateException("Failed to parse release information", ex);
        }
    }

    public async Task UpdateAsync(IProgress<int>? progress = null)
    {
        try
        {
            using var response =
                await _httpClient.GetAsync(_config.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(
                _config.ExecutablePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 8192,
                useAsync: true
            );

            await CopyWithProgressAsync(contentStream, fileStream, totalBytes, progress);
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateException("Failed to download update", ex);
        }
        catch (IOException ex)
        {
            throw new UpdateException("Failed to write update file", ex);
        }
    }

    private static async Task CopyWithProgressAsync(
        Stream source,
        Stream destination,
        long totalBytes,
        IProgress<int>? progress)
    {
        var buffer = new byte[8192];
        int bytesRead;
        long totalRead = 0;
        var lastProgress = -1;

        while ((bytesRead = await source.ReadAsync(buffer)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            if (totalBytes <= 0 || progress == null)
            {
                continue;
            }

            var currentProgress = (int)((double)totalRead / totalBytes * 100);
            if (currentProgress == lastProgress)
            {
                continue;
            }

            progress.Report(currentProgress);
            lastProgress = currentProgress;
        }
    }

    public bool IsExecutableLocked()
    {
        if (!File.Exists(_config.ExecutablePath))
            return false;

        try
        {
            using var stream = File.Open(_config.ExecutablePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }
}