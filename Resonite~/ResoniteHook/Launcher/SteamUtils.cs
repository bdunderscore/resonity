using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace nadena.dev.resonity.remote.bootstrap;

public static class SteamUtils
{
    public static string? GetGamePath(uint appId)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Windows.GetGameInstallPath(appId);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Linux.GetGameInstallPath(appId);
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOS.GetGameInstallPath(appId);
        }

        return null;
    }

    #region Windows Implementation
    [SupportedOSPlatform("windows")]
    private static class Windows
    {
        public static string? GetGameInstallPath(uint appId)
        {
            var steamPath = GetSteamPath();
            if (steamPath == null) return null;

            var libraryPaths = GetSteamLibraryFolders(steamPath);
            foreach (var libPath in libraryPaths)
            {
                var manifestPath = Path.Combine(libPath, "steamapps", $"appmanifest_{appId}.acf");
                
                if (!TryParseManifest(manifestPath, out var installDir))
                    continue;
                
                if (installDir != null)
                    return Path.Combine(libPath, "steamapps", "common", installDir);
            }
            return null;
        }

        private static string? GetSteamPath()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return key?.GetValue("SteamPath")?.ToString()?.Replace('/', '\\');
            }
            catch
            {
                return null;
            }
        }

        private static List<string> GetSteamLibraryFolders(string steamPath)
        {
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            return ParseVdfFile(vdfPath, "path");
        }
    }
    #endregion

    #region Linux Implementation
    [SupportedOSPlatform("linux")]
    private static class Linux
    {
        public static string? GetGameInstallPath(uint appId)
        {
            var steamPath = GetSteamPath();
            if (steamPath == null) return null;

            var libraryPaths = GetSteamLibraryFolders(steamPath);
            foreach (var libPath in libraryPaths)
            {
                var manifestPath = Path.Combine(libPath, "steamapps", $"appmanifest_{appId}.acf");
                
                if (!TryParseManifest(manifestPath, out var installDir))
                    continue;
                
                if (installDir != null)
                    return Path.Combine(libPath, "steamapps", "common", installDir);
            }
            return null;
        }

        private static string? GetSteamPath()
        {
            var paths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam")
            };

            return paths.FirstOrDefault(Directory.Exists);
        }

        private static List<string> GetSteamLibraryFolders(string steamPath)
        {
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            return ParseVdfFile(vdfPath, "path");
        }
    }
    #endregion

    #region macOS Implementation
    [SupportedOSPlatform("macos")]
    private static class MacOS
    {
        public static string? GetGameInstallPath(uint appId)
        {
            var steamPath = GetSteamPath();
            if (steamPath == null) return null;

            var libraryPaths = GetSteamLibraryFolders(steamPath);
            foreach (var libPath in libraryPaths)
            {
                var manifestPath = Path.Combine(libPath, "steamapps", $"appmanifest_{appId}.acf");
                
                if (!TryParseManifest(manifestPath, out var installDir))
                    continue;
                
                if (installDir != null)
                    return Path.Combine(libPath, "steamapps", "common", installDir);
            }
            return null;
        }

        private static string? GetSteamPath()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                "Steam"
            );
            return Directory.Exists(path) ? path : null;
        }

        private static List<string> GetSteamLibraryFolders(string steamPath)
        {
            var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            return ParseVdfFile(vdfPath, "path");
        }
    }
    #endregion

    #region Common Methods
    private static List<string> ParseVdfFile(string vdfPath, string key)
    {
        var results = new List<string>();
        if (!File.Exists(vdfPath)) return results;

        try
        {
            var content = File.ReadAllText(vdfPath);
            var matches = Regex.Matches(
                content,
                $@"""{key}""[\s\t]+""([^""]+)""",
                RegexOptions.Multiline
            );

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    results.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
                }
            }
        }
        catch
        {
            Console.WriteLine($"Failed to parse vdf file: {vdfPath}");
        }
        return results;
    }

    private static bool TryParseManifest(string manifestPath, out string? installDir)
    {
        installDir = null;
        if (!File.Exists(manifestPath)) return false;

        try
        {
            var content = File.ReadAllText(manifestPath);
            var match = Regex.Match(
                content,
                @"""installdir""[\s\t]+""([^""]+)""",
                RegexOptions.Multiline
            );

            if (match.Success)
            {
                installDir = match.Groups[1].Value.Trim();
                return true;
            }
        }
        catch
        {
            Console.WriteLine($"Failed to parse manifest file: {manifestPath}");
        }
        return false;
    }
    #endregion
}