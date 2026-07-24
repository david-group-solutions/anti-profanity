namespace DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;

public static class PathHelpers
{
    public static string ResolveHomeDirectory(string path)
    {
        if (!path.StartsWith('~'))
            return Path.GetFullPath(path);

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        path = string.Concat(homeDir, path.AsSpan(1));

        return Path.GetFullPath(path);
    }
}
