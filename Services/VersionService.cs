namespace ZedLauncher.Services
{
    public class VersionService : IVersionService
    {
        private readonly string _versionPath;

        public VersionService(string versionPath)
        {
            _versionPath = versionPath;
        }

        public string ReadCurrentVersion()
        {
            return File.Exists(_versionPath)
                ? File.ReadAllText(_versionPath)
                : "unknown";
        }

        public void WriteCurrentVersion(string version)
        {
            File.WriteAllText(_versionPath, version);
        }
    }
}