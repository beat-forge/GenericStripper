using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;

namespace GenericStripper.Modules.BeatSaber;

public class BeatSaber : IModule
{
    private readonly HttpClient _client;

    public BeatSaber(string gamePath)
    {
        GamePath = gamePath;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "GenericStripper");
    }

    public string GameName => "Beat Saber";
    public string GamePath { get; }

    public void StripDll(string file, string outDir, params string[] resolveDirs)
    {
        if (!File.Exists(file)) throw new FileNotFoundException("Failed to find assembly to strip!", file);
        var fileInf = new FileInfo(file);

        var bsAssemblyModule = new BsAssemblyModule(GamePath, file, resolveDirs);
        bsAssemblyModule.Virtualize();
        bsAssemblyModule.Strip();

        var relativePath = Path.GetRelativePath(GamePath, fileInf.FullName);
        var outAssembly = Path.Combine(outDir, relativePath);
        if (!Directory.Exists(Path.GetDirectoryName(outAssembly)))
            Directory.CreateDirectory(Path.GetDirectoryName(outAssembly) ?? string.Empty);
        
        bsAssemblyModule.Write(outAssembly);
    }

    public async Task StripAllDlls(string outDir)
    {
        await InstallBsipa();

        if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
        
        var bsLibsDir = Path.Combine(GamePath, "Libs");
        var bsManagedDir = Path.Combine(GamePath, "Beat Saber_Data", "Managed");

        var libAssemblies = Directory.GetFiles(bsLibsDir, "*.dll", SearchOption.AllDirectories);
        var managedAssemblies = Directory.GetFiles(bsManagedDir, "*.dll", SearchOption.AllDirectories);

        AnsiConsole.Progress()
            .Start(ctx =>
                {
                    var task = ctx.AddTask("[salmon1]Stripping assemblies...[/]", new ProgressTaskSettings
                    {
                        MaxValue = libAssemblies.Length + managedAssemblies.Length
                    });

                    foreach (var assembly in libAssemblies.Concat(managedAssemblies))
                    {
                        var assemblyInf = new FileInfo(assembly);
                        var relativePath = Path.GetRelativePath(GamePath, assemblyInf.FullName);
                        var outAssembly = Path.Combine(outDir, relativePath);
                        if (File.Exists(outAssembly))
                        {
                            task.Increment(1);
                            AnsiConsole.MarkupLine($"[gray]Skipped {assemblyInf.Name}[/]");
                            continue;
                        }
                        
                        StripDll(assembly, outDir, bsLibsDir, bsManagedDir);
                        task.Increment(1);
                        AnsiConsole.MarkupLine($"[teal]Stripped {assemblyInf.Name}[/]");
                    }
                }
            );
    }


    private async Task InstallBsipa()
    {
        if (File.Exists(Path.Combine(GamePath, "IPA.exe")))
        {
            AnsiConsole.MarkupLine("[green]BSIPA already installed, skipping...[/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Installing BSIPA...", async ctx =>
            {
                var res = await _client.GetAsync(
                    "https://api.github.com/repos/nike4613/BeatSaber-IPA-Reloaded/releases/latest");
                if (res.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to get latest BSIPA release!");

                var latestRelease =
                    JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(await res.Content.ReadAsStringAsync());
                if (latestRelease == null) throw new Exception("Failed to parse BSIPA release!");

                var assets = latestRelease["assets"] as JArray;
                var arch = RuntimeInformation.OSArchitecture.ToString().ToLower();
                var asset = assets?.FirstOrDefault(x => x["name"]?.ToString().Contains(arch) ?? false);

                if (asset == null) throw new Exception("Failed to find a BSIPA asset for this system!");

                var assetRes = await _client.GetAsync(asset["browser_download_url"]?.ToString());
                if (assetRes.StatusCode != HttpStatusCode.OK) throw new Exception("Failed to download BSIPA asset!");

                await using var assetStream = await assetRes.Content.ReadAsStreamAsync();
                using var archive = new ZipArchive(assetStream);
                archive.ExtractToDirectory(GamePath);

                if (!File.Exists(Path.Combine(GamePath, "IPA.exe")))
                    throw new Exception("Failed to extract BSIPA asset!");

                // todo: prevent BSIPA from outputting to stdout
                var bsipa = new Process
                {
                    StartInfo =
                    {
                        FileName = Path.Combine(GamePath, "IPA.exe"),
                        WorkingDirectory = GamePath,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Arguments = "--nowait --force"
                    }
                };

                bsipa.Start();
                await bsipa.WaitForExitAsync();

                if (bsipa.ExitCode != 0) throw new Exception("Failed to install BSIPA!");
                ctx.Status("[green]BSIPA installed![/]");
            });
    }
}