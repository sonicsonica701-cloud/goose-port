using Mono.Cecil;
using System;
using System.IO;

// Mono.Cecil 0.9.5 compatible (Unity 2020.3 docker image)
class Patcher {
    static int Main(string[] args) {
        if (args.Length < 1) {
            Console.WriteLine("Usage: PatchDLL.exe <dll> [managedDir] [mode]");
            return 1;
        }
        string dllPath = args[0];
        string managedDir = args.Length > 1 ? args[1] : "/opt/unity/Editor/Data/Managed";
        string mode = args.Length > 2 ? args[2] : "rendering";
        
        Console.WriteLine("Patching (" + mode + "): " + dllPath);
        
        if (!File.Exists(dllPath)) {
            Console.WriteLine("  File not found: " + dllPath);
            return 1;
        }
        
        try {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath));
            resolver.AddSearchDirectory(managedDir);
            string unityModules = Path.Combine(managedDir, "UnityEngine");
            if (Directory.Exists(unityModules))
                resolver.AddSearchDirectory(unityModules);
            
            // Read the DLL bytes first, then load from memory
            byte[] dllBytes = File.ReadAllBytes(dllPath);
            var ms = new MemoryStream(dllBytes);
            var rp = new ReaderParameters();
            rp.AssemblyResolver = resolver;
            
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(ms, rp);
            
            int patched = 0;
            
            if (mode == "rendering") {
                foreach (var typeRef in asm.MainModule.GetTypeReferences()) {
                    if (typeRef.Namespace == "UnityEngine.Experimental.Rendering") {
                        Console.WriteLine("  Fix: " + typeRef.FullName);
                        typeRef.Namespace = "UnityEngine.Rendering";
                        patched++;
                    }
                }
            }
            // localization mode removed for now - too complex for 0.9.x Cecil
            
            if (patched > 0) {
                // Write to a new MemoryStream then save to disk
                var outStream = new MemoryStream();
                asm.Write(outStream);
                File.WriteAllBytes(dllPath, outStream.ToArray());
                Console.WriteLine("  Done: " + patched + " refs patched");
            } else {
                Console.WriteLine("  No changes needed");
            }
            
            return 0;
        } catch (Exception ex) {
            Console.WriteLine("  ERROR: " + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
    }
}
