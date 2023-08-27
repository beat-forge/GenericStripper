namespace GenericStripper.Modules;

public interface IModule
{
    public string GameName { get; }

    public string GamePath { get; }

    protected void StripDll(string file, string outDir, params string[] resolveDirs);
    
    public Task StripAllDlls(string outDir);
}