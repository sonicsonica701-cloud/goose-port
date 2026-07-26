using Mono.Cecil;
using System;
using System.IO;
class Patcher {
    static int Main(string[] args) {
        string dllPath = args[0];
        string managedDir = args.Length > 1 ? args[1] : "/opt/unity/Editor/Data/Managed";
        Console.WriteLine("Patching: " + dllPath);
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath));
        resolver.AddSearchDirectory(managedDir);
        string unityModules = Path.Combine(managedDir, "UnityEngine");
        if (Directory.Exists(unityModules)) resolver.AddSearchDirectory(unityModules);
        AssemblyDefinition asm;
        try {
            asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters {
                ReadWrite = true, AssemblyResolver = resolver, ReadingMode = ReadingMode.Immediate
            });
        } catch (Exception ex) { Console.WriteLine("  Skip: " + ex.Message); return 0; }
        int patched = 0;
        foreach (var typeRef in asm.MainModule.GetTypeReferences()) {
            if (typeRef.Namespace == "UnityEngine.Experimental.Rendering") {
                Console.WriteLine("  Fix: " + typeRef.FullName);
                typeRef.Namespace = "UnityEngine.Rendering";
                patched++;
            }
        }
        if (patched > 0) { asm.Write(); Console.WriteLine("  Done: " + patched + " refs"); }
        asm.Dispose();
        return 0;
    }
}
