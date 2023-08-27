namespace GenericStripper.Modules;

public interface IModule
{
    public string GameName { get; }

    public string GamePath { get; }

    public void StripDll(string file, string outDir, params string[] resolveDirs);
}