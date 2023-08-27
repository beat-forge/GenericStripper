using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GenericStripper.Modules;

[SuppressMessage("Interoperability",
    "SYSLIB1054:Use \'LibraryImportAttribute\' instead of \'DllImportAttribute\' to generate P/Invoke marshalling code at compile time")]
public class LibLoader
{
    public LibLoader(string gamePath)
    {
        GamePath = gamePath;
    }

    private static string GamePath { get; set; } = string.Empty;
    private static string LibPath => Path.Combine(GamePath, "Libs");
    private static string NativePath => Path.Combine(LibPath, "Native");
    internal static Dictionary<string, string>? FilenameLocations { get; private set; }

    public static void SetupAssemblyFilenames()
    {
        if (FilenameLocations != null) return;

        FilenameLocations = new Dictionary<string, string>();
        var files = Directory.GetFiles(LibPath, "*.dll", SearchOption.AllDirectories);
        files = files.Where(f => !f.StartsWith(NativePath)).ToArray();

        foreach (var file in files)
        {
            var filename = Path.GetFileName(file);
            FilenameLocations[filename] = file;
        }

        if (Directory.Exists(NativePath))
        {
            var ptr = AddDllDirectory(NativePath);
            if (ptr == IntPtr.Zero) throw new Exception("Failed to add Native directory to DLL search path!");

            var nativeDirectories = Directory.GetDirectories(NativePath, "*", SearchOption.AllDirectories);
            foreach (var nativeDirectory in nativeDirectories)
            {
                ptr = AddDllDirectory(nativeDirectory);
                if (ptr == IntPtr.Zero)
                    throw new Exception($"Failed to add Native directory to DLL search path: {nativeDirectory}");
            }
        }

        var unityData = Directory.EnumerateDirectories(GamePath, "*_Data", SearchOption.TopDirectoryOnly).First();
        var unityPlugins = Path.Combine(unityData, "Plugins");
        if (Directory.Exists(unityPlugins))
        {
            var ptr = AddDllDirectory(unityPlugins);
            if (ptr == IntPtr.Zero)
                throw new Exception($"Failed to add Unity Plugins directory to DLL search path: {unityPlugins}");
        }

        foreach (var dir in Environment.GetEnvironmentVariable("path")?.Split(Path.PathSeparator) ??
                            Array.Empty<string>())
        {
            if (!Directory.Exists(dir)) continue;

            var ptr = AddDllDirectory(dir);
            if (ptr == IntPtr.Zero) throw new Exception($"Failed to add directory to DLL search path: {dir}");
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr AddDllDirectory(string lpPathName);
}