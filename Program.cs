namespace ZedLauncher
{
    using System.Diagnostics;
    using System.Text.Json;

    public static class Program
    {
        private static readonly string ExecutablePath = Path.Combine(Environment.CurrentDirectory, "zed.exe");
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, "version.txt");

        private const string DownloadUrl =
            "https://github.com/deevus/zed-windows-builds/releases/latest/download/zed.exe";

        private const string ReleasesUrl = "https://api.github.com/repos/deevus/zed-windows-builds/releases?per_page=1";

        private static readonly HttpClient Client = new()
        {
            DefaultRequestHeaders =
            {
                UserAgent = { new System.Net.Http.Headers.ProductInfoHeaderValue("ZedLauncher", "1.0") }
            }
        };

        public static async Task Main()
        {
            await Start();
        }

        public static async ValueTask Start()
        {
            try
            {
                if (!IsExecutableLocked())
                {
                    Console.WriteLine("Checking for updates...");
                    var currentVersion = ReadCurrentVersion();
                    var releaseVersion = await FetchReleaseVersion();

                    if (releaseVersion != currentVersion)
                    {
                        Console.WriteLine($"Version {releaseVersion} is available (current {currentVersion})");
                        Console.WriteLine("Downloading update...");
                        await Update();

                        Console.WriteLine($"Version {releaseVersion} is downloaded!");
                        WriteCurrentVersion(releaseVersion);
                    }
                }

                Console.WriteLine("Starting the application...");
                RunApp();
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();

                Environment.Exit(1);
            }
        }

        private static void RunApp()
        {
            Process.Start(ExecutablePath);
            Environment.Exit(0);
        }

        private static bool IsExecutableLocked()
        {
            if (!File.Exists(ExecutablePath))
            {
                return false;
            }

            try
            {
                using var stream = File.Open(ExecutablePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                return false;
            }
            catch
            {
                return true;
            }
        }

        private static async ValueTask<string> FetchReleaseVersion()
        {
            var response = await Client.GetAsync(ReleasesUrl);
            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<GitHubRelease[]>(json);
            if (data is null)
            {
                throw new Exception("Data is null");
            }

            var item = data.FirstOrDefault();
            if (item is null)
            {
                throw new Exception("Item is null");
            }

            return item.Name;
        }

        private static async ValueTask Update()
        {
            using var response = await Client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(
                ExecutablePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                8192,
                true
            );

            var buffer = new byte[8192];
            int bytesRead;
            long totalRead = 0;
            var lastProgress = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;

                if (totalBytes <= 0)
                {
                    continue;
                }

                var progress = (int)((double)totalRead / totalBytes * 100);
                if (progress == lastProgress)
                {
                    continue;
                }

                lastProgress = progress;
                Console.WriteLine($"{progress:F1}%");
            }
        }

        private static string ReadCurrentVersion()
        {
            return File.Exists(VersionPath)
                ? File.ReadAllText(VersionPath)
                : "unknown";
        }

        private static void WriteCurrentVersion(string version)
        {
            File.WriteAllText(VersionPath, version);
        }
    }
}