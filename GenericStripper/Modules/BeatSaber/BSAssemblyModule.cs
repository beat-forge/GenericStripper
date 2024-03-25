using Mono.Cecil;

namespace GenericStripper.Modules.BeatSaber;

public class BsAssemblyModule
{
    private readonly BsLibLoader _bslibLoader;
    private readonly FileInfo _file;
    private readonly ModuleDefinition _module;

    public BsAssemblyModule(string gamePath, string fileName, params string[] resolverDirs)
    {
        _bslibLoader = new BsLibLoader(gamePath);

        _file = new FileInfo(fileName);
        if (!_file.Exists) throw new FileNotFoundException("Failed to find assembly to strip!", fileName);

        _module = LoadModules(resolverDirs);
    }

    private ModuleDefinition LoadModules(IEnumerable<string> directories)
    {
        _bslibLoader.AddSearchDirectory(_file.Directory?.FullName ??
                                        throw new Exception("Failed to get assembly directory!"));
        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory)) continue;
            _bslibLoader.AddSearchDirectory(directory);
        }

        ReaderParameters parameters = new()
        {
            AssemblyResolver = _bslibLoader,
            ReadingMode = ReadingMode.Immediate,
            ReadWrite = false,
            InMemory = true
        };

        return ModuleDefinition.ReadModule(_file.FullName, parameters);
    }

    public void Virtualize()
    {
        foreach (var type in _module.Types) VirtualizeType(type);
    }

    private void VirtualizeType(TypeDefinition type)
    {
        if (type.IsSealed) type.IsSealed = false;

        if (type.IsInterface) return;
        if (type.IsAbstract) return;

        foreach (var subType in type.NestedTypes) VirtualizeType(subType);

        foreach (var group in type.Methods.Where(m => m.IsManaged && m is
                 {
                     IsIL: true, IsStatic: false, IsVirtual: false, IsAbstract: false, IsAddOn: false,
                     IsConstructor: false, IsSpecialName: false, IsGenericInstance: false, HasOverrides: false
                 }).GroupBy(m => m.Name))
        {
            foreach (var m in group)
            {
                bool hasAnyInParam = m.Parameters.FirstOrDefault(p => p.IsIn) != null;
                if (group.Count() > 1 && hasAnyInParam) continue;

                m.IsVirtual = !m.IsPrivate || !hasAnyInParam;
                m.IsPublic = true;
                m.IsPrivate = false;
                m.IsNewSlot = true;
                m.IsHideBySig = true;
            }
        }

        foreach (var field in type.Fields.Where(field => field.IsPrivate)) field.IsFamily = true;
    }

    public void Strip()
    {
        foreach (var type in _module.Types) StripType(type);
    }

    private static void StripType(TypeDefinition type)
    {
        foreach (var m in type.Methods.Where(m => m.Body != null))
        {
            m.Body.Instructions.Clear();
            m.Body.InitLocals = false;
            m.Body.Variables.Clear();
        }

        foreach (var subType in type.NestedTypes) StripType(subType);
    }

    public void Write(string outFile)
    {
        _module.Write(outFile);
    }
}