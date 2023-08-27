using System.Diagnostics.CodeAnalysis;
using GenericStripper.Modules;
using GenericStripper.Modules.BeatSaber;
using Spectre.Console;
using Spectre.Console.Cli;

namespace GenericStripper;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal abstract class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<MainCommand>();
        return app.Run(args);
    }

    internal sealed class MainCommand : AsyncCommand<MainCommand.Settings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
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

            var module = settings.Module.ToLower();
            var path = settings.Path;
            var outDir = settings.Out;

            IModule? mod = module switch
            {
                "beatsaber" => new BeatSaber(path),
                _ => null
            };

            if (mod == null)
            {
                AnsiConsole.MarkupLine("[red]Invalid module specified![/]");
                return 1;
            }

            await mod.StripAllDlls(outDir);
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