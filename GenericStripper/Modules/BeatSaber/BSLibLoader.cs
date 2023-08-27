using Mono.Cecil;

namespace GenericStripper.Modules.BeatSaber;

public class BsLibLoader : BaseAssemblyResolver
{
    public BsLibLoader(string gamePath)
    {
        _ = new LibLoader(gamePath);
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
    {
        LibLoader.SetupAssemblyFilenames();
        if (LibLoader.FilenameLocations == null) throw new Exception("Failed to setup assembly filenames!");

        if (LibLoader.FilenameLocations.TryGetValue($"{name.Name}.{name.Version}.dll", out var path))
        {
            if (File.Exists(path)) return AssemblyDefinition.ReadAssembly(path, parameters);
        }
        else if (LibLoader.FilenameLocations.TryGetValue($"{name.Name}.dll", out path))
        {
            if (File.Exists(path)) return AssemblyDefinition.ReadAssembly(path, parameters);
        }

        return base.Resolve(name, parameters);
    }
}