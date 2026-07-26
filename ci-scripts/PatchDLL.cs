using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;

// Compatible with Mono.Cecil 0.9.x (Unity's bundled version)
// Key differences from 0.10+: no ReadWrite, no Dispose, Write(stream), no CoreLibrary
class Patcher {
    static int Main(string[] args) {
        string dllPath = args[0];
        string managedDir = args.Length > 1 ? args[1] : "/opt/unity/Editor/Data/Managed";
        string mode = args.Length > 2 ? args[2] : "rendering";
        Console.WriteLine("Patching (" + mode + "): " + dllPath);
        
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(dllPath));
        resolver.AddSearchDirectory(managedDir);
        string unityModules = Path.Combine(managedDir, "UnityEngine");
        if (Directory.Exists(unityModules)) resolver.AddSearchDirectory(unityModules);
        
        AssemblyDefinition asm;
        try {
            // Cecil 0.9.x: ReadAssembly with ReaderParameters (no ReadWrite option)
            var rp = new ReaderParameters();
            rp.AssemblyResolver = resolver;
            rp.ReadingMode = ReadingMode.Immediate;
            asm = AssemblyDefinition.ReadAssembly(dllPath, rp);
        } catch (Exception ex) { 
            Console.WriteLine("  Skip: " + ex.Message); 
            return 0; 
        }
        
        int patched = 0;
        
        if (mode == "rendering") {
            // Fix UnityEngine.Experimental.Rendering -> UnityEngine.Rendering
            foreach (var typeRef in asm.MainModule.GetTypeReferences()) {
                if (typeRef.Namespace == "UnityEngine.Experimental.Rendering") {
                    Console.WriteLine("  Fix: " + typeRef.FullName);
                    typeRef.Namespace = "UnityEngine.Rendering";
                    patched++;
                }
            }
        } else if (mode == "localization") {
            // Fix streamingAssetsPath references to persistentDataPath
            foreach (var type in asm.MainModule.Types) {
                PatchType(type, asm, ref patched);
            }
        }
        
        if (patched > 0) { 
            // Cecil 0.9.x: Write to a temp file then replace (no in-place write)
            string tmpPath = dllPath + ".tmp";
            asm.Write(tmpPath);
            // Close the assembly reference before replacing
            asm = null;
            GC.Collect();
            File.Delete(dllPath);
            File.Move(tmpPath, dllPath);
            Console.WriteLine("  Done: " + patched + " refs patched"); 
        } else {
            Console.WriteLine("  No changes needed");
        }
        return 0;
    }
    
    static void PatchType(TypeDefinition type, AssemblyDefinition asm, ref int patched) {
        foreach (var method in type.Methods) {
            if (!method.HasBody) continue;
            var il = method.Body.GetILProcessor();
            var instructions = method.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++) {
                var instr = instructions[i];
                // Look for calls to Application.get_streamingAssetsPath
                if (instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) {
                    var methodRef = instr.Operand as MethodReference;
                    if (methodRef != null && 
                        methodRef.Name == "get_streamingAssetsPath" &&
                        methodRef.DeclaringType.FullName == "UnityEngine.Application") {
                        Console.WriteLine("  Fix localization in: " + type.Name + "." + method.Name);
                        
                        // Replace streamingAssetsPath with persistentDataPath
                        var persistentMethod = new MethodReference(
                            "get_persistentDataPath", 
                            asm.MainModule.TypeSystem.String,
                            methodRef.DeclaringType);
                        instr.Operand = persistentMethod;
                        
                        // After the call, append "/StreamingAssets" using String.Concat
                        var stringType = asm.MainModule.TypeSystem.String;
                        // Find mscorlib reference for System.String
                        var mscorlibRef = asm.MainModule.TypeSystem.String.Scope;
                        var stringTypeRef = new TypeReference("System", "String", asm.MainModule, mscorlibRef);
                        var concatMethod = new MethodReference("Concat", stringType, stringTypeRef);
                        concatMethod.Parameters.Add(new ParameterDefinition(stringType));
                        concatMethod.Parameters.Add(new ParameterDefinition(stringType));
                        
                        var ldstr = il.Create(OpCodes.Ldstr, "/StreamingAssets");
                        var concat = il.Create(OpCodes.Call, concatMethod);
                        il.InsertAfter(instr, ldstr);
                        il.InsertAfter(ldstr, concat);
                        i += 2; // Skip the inserted instructions
                        patched++;
                    }
                }
            }
        }
        // Process nested types
        foreach (var nested in type.NestedTypes) {
            PatchType(nested, asm, ref patched);
        }
    }
}
