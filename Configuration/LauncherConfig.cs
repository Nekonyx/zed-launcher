namespace ZedLauncher.Configuration;

public class LauncherConfig
{
    public string ExecutablePath { get; } = Path.Combine(Environment.CurrentDirectory, "zed.exe");
    public string VersionPath { get; } = Path.Combine(Environment.CurrentDirectory, "version.txt");

    public string DownloadUrl { get; } =
        "https://github.com/deevus/zed-windows-builds/releases/latest/download/zed.exe";

    public string ReleasesUrl { get; } =
        "https://api.github.com/repos/deevus/zed-windows-builds/releases?per_page=1";

    public string UserAgent { get; } = "ZedLauncher/1.0";
}