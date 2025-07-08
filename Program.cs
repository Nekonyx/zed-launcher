using System.Diagnostics;
using ZedLauncher.Configuration;
using ZedLauncher.Services;

namespace ZedLauncher;

public static class Program
{
    public static async Task Main()
    {
        var launcher = new Launcher();
        await launcher.RunAsync();
    }
}

public class Launcher
{
    private readonly LauncherConfig _config;
    private readonly UpdateService _updateService;
    private readonly VersionService _versionService;

    public Launcher()
    {
        _config = new LauncherConfig();

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_config.UserAgent);

        _updateService = new UpdateService(httpClient, _config);
        _versionService = new VersionService(_config.VersionPath);
    }

    public async Task RunAsync()
    {
        try
        {
            if (!_updateService.IsExecutableLocked())
            {
                await CheckAndUpdateAsync();
            }

            Console.WriteLine("Starting the application...");
            StartApp();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            Environment.Exit(1);
        }
    }

    private async Task CheckAndUpdateAsync()
    {
        Console.WriteLine("Checking for updates...");

        var currentVersion = _versionService.ReadCurrentVersion();
        var releaseVersion = await _updateService.FetchReleaseVersionAsync();

        if (releaseVersion != currentVersion)
        {
            Console.WriteLine($"Version {releaseVersion} is available (current {currentVersion})");
            Console.WriteLine("Downloading update...");

            var progress = new Progress<int>(i => Console.WriteLine($"{i}% complete"));

            await _updateService.UpdateAsync(progress);

            Console.WriteLine($"Version {releaseVersion} downloaded successfully!");
            _versionService.WriteCurrentVersion(releaseVersion);
        }
        else
        {
            Console.WriteLine("Already up to date.");
        }
    }

    private void StartApp()
    {
        var startInfo = new ProcessStartInfo(_config.ExecutablePath)
        {
            UseShellExecute = true
        };

        Process.Start(startInfo);
        Environment.Exit(0);
    }
}