namespace ZedLauncher.Services;

public interface IVersionService
{
    string ReadCurrentVersion();

    void WriteCurrentVersion(string version);
}