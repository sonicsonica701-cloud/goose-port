using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;

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
        // Also search the project Plugins directory for cross-references
        string pluginsDir = Path.Combine(Path.GetDirectoryName(dllPath));
        if (Directory.Exists(pluginsDir)) resolver.AddSearchDirectory(pluginsDir);
        
        AssemblyDefinition asm;
        try {
            asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters {
                ReadWrite = true, AssemblyResolver = resolver, ReadingMode = ReadingMode.Immediate
            });
        } catch (Exception ex) { Console.WriteLine("  Skip: " + ex.Message); return 0; }
        
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
            // Find all methods that reference "streamingAssetsPath" string or call Application.streamingAssetsPath
            foreach (var type in asm.MainModule.Types) {
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
                                // Replace with call to Application.persistentDataPath + "/StreamingAssets"
                                Console.WriteLine("  Fix localization in: " + type.Name + "." + method.Name);
                                
                                // Change to persistentDataPath
                                var persistentMethod = new MethodReference("get_persistentDataPath", 
                                    methodRef.ReturnType, methodRef.DeclaringType);
                                instr.Operand = persistentMethod;
                                
                                // After the call, we need to append "/StreamingAssets"
                                // Insert: ldstr "/StreamingAssets" then call String.Concat
                                var stringType = asm.MainModule.TypeSystem.String;
                                var concatMethod = new MethodReference("Concat", stringType, 
                                    new TypeReference("System", "String", asm.MainModule, asm.MainModule.TypeSystem.CoreLibrary));
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
            }
        }
        
        if (patched > 0) { 
            asm.Write(); 
            Console.WriteLine("  Done: " + patched + " refs patched"); 
        } else {
            Console.WriteLine("  No changes needed");
        }
        asm.Dispose();
        return 0;
    }
}
