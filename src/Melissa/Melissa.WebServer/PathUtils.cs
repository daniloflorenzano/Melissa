namespace Melissa.WebServer;

public static class PathUtils
{
    public static DirectoryInfo TryGetSolutionDirectoryInfo(string? currentPath = null)
    {
        var directory = new DirectoryInfo(
            currentPath ?? Directory.GetCurrentDirectory());
        while (directory != null && !directory.GetFiles("*.slnx").Any())
        {
            directory = directory.Parent;
        }
        return directory;
    }
}