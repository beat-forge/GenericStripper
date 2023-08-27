using GenericStripper.Modules.BeatSaber;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GenericStripper;

internal abstract class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<MainCommand>();
        return app.Run(args);
    }

    internal sealed class MainCommand : Command<MainCommand.Settings>
    {
        public override int Execute(CommandContext context, Settings settings)
        {
            if (settings.Module == null)
            {
                AnsiConsole.MarkupLine("[red]No module specified![/]");
                return 1;
            }

            if (settings.Path == null)
            {
                AnsiConsole.MarkupLine("[red]No path specified![/]");
                return 1;
            }

            if (settings.Out == null)
            {
                AnsiConsole.MarkupLine("[red]No output directory specified![/]");
                return 1;
            }


            var bs = new BeatSaber(settings.Path);
            bs.InstallBsipa().Wait();

            var bsLibsDir = Path.Combine(bs.GamePath, "Libs");
            var bsManagedDir = Path.Combine(bs.GamePath, "Beat Saber_Data", "Managed");

            var outputDir = Path.Combine(bs.GamePath, settings.Out);
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            var libAssemblies = Directory.GetFiles(bsLibsDir, "*.dll", SearchOption.AllDirectories);
            var managedAssemblies = Directory.GetFiles(bsManagedDir, "*.dll", SearchOption.AllDirectories);

            foreach (var assembly in libAssemblies.Concat(managedAssemblies))
                bs.StripDll(assembly, outputDir, bsLibsDir, bsManagedDir);

            Console.WriteLine("Done!");

            return 0;
        }

        public sealed class Settings : CommandSettings
        {
            [CommandOption("-m|--module")] public string? Module { get; init; }

            [CommandOption("-p|--path")] public string? Path { get; init; }

            [CommandOption("-o|--out")] public string? Out { get; init; }
        }
    }
}