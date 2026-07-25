#!/bin/bash
set -e
echo "=== Patching PostProcessing DLL references ==="

# Find PostProcessing runtime DLL
PP_DLL=$(find /project/Assets -name "Unity.Postprocessing.Runtime.dll" -o -name "PostProcessing.dll" 2>/dev/null | head -1)
if [ -z "$PP_DLL" ]; then
  # Could be in Packages or Library
  PP_DLL=$(find /project -name "Unity.Postprocessing.Runtime.dll" -o -name "PostProcessing.dll" 2>/dev/null | grep -v "Library/PackageCache" | head -1)
fi

if [ -z "$PP_DLL" ]; then
  echo "PostProcessing DLL not found, checking for source files instead..."
  # If it's source-based post processing, we need to fix the .cs references
  find /project/Assets -path "*PostProcess*" -name "*.cs" | head -5
  # Fix source files that reference the old namespace
  find /project/Assets -name "*.cs" -exec grep -l "UnityEngine.Experimental.Rendering.RenderPipelineAsset" {} \; 2>/dev/null | while read F; do
    echo "Fixing source: $F"
    sed -i 's/UnityEngine\.Experimental\.Rendering\.RenderPipelineAsset/UnityEngine.Rendering.RenderPipelineAsset/g' "$F"
    sed -i 's/UnityEngine\.Experimental\.Rendering\.RenderPipeline/UnityEngine.Rendering.RenderPipeline/g' "$F"
  done
  # Also fix any asmdef references
  find /project/Assets -name "*.asmdef" -exec grep -l "Unity.RenderPipelines" {} \; 2>/dev/null | head -5
  echo "Source fix complete"
  exit 0
fi

echo "Found DLL: $PP_DLL"
ls -la "$PP_DLL"

# Check if Mono.Cecil is available
CECIL_DLL=$(find /opt/unity -name "Mono.Cecil.dll" 2>/dev/null | head -1)
if [ -z "$CECIL_DLL" ]; then
  echo "Mono.Cecil not found in Unity install, trying NuGet..."
  # Use mono and download cecil
  apt-get install -y -qq mono-complete 2>/dev/null || true
  CECIL_DLL=$(find / -name "Mono.Cecil.dll" 2>/dev/null | head -1)
fi

if [ -n "$CECIL_DLL" ]; then
  echo "Using Mono.Cecil: $CECIL_DLL"
  # Write a C# patcher script
  cat > /tmp/PatchPP.cs << 'CSEOF'
using Mono.Cecil;
using System;
using System.Linq;

class Patcher {
    static int Main(string[] args) {
        if (args.Length < 1) { Console.Error.WriteLine("Usage: PatchPP <dll>"); return 1; }
        string dllPath = args[0];
        Console.WriteLine($"Patching: {dllPath}");
        
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(System.IO.Path.GetDirectoryName(dllPath));
        
        var readerParams = new ReaderParameters { AssemblyResolver = resolver };
        var asm = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
        bool patched = false;
        
        foreach (var module in asm.Modules) {
            foreach (var typeRef in module.GetTypeReferences()) {
                // Fix RenderPipelineAsset reference
                if (typeRef.Namespace == "UnityEngine.Experimental.Rendering" && 
                    (typeRef.Name == "RenderPipelineAsset" || typeRef.Name == "RenderPipeline")) {
                    Console.WriteLine($"  Fixing: {typeRef.FullName}");
                    typeRef.Namespace = "UnityEngine.Rendering";
                    patched = true;
                }
            }
        }
        
        if (patched) {
            asm.Write(dllPath + ".patched");
            System.IO.File.Copy(dllPath + ".patched", dllPath, true);
            System.IO.File.Delete(dllPath + ".patched");
            Console.WriteLine("  DLL patched successfully!");
        } else {
            Console.WriteLine("  No references to patch found.");
        }
        return 0;
    }
}
CSEOF
  # Compile and run
  mcs -r:"$CECIL_DLL" /tmp/PatchPP.cs -out:/tmp/PatchPP.exe 2>/dev/null && \
  mono /tmp/PatchPP.exe "$PP_DLL" || echo "Cecil patch failed, falling back to binary patch"
else
  echo "No Mono.Cecil available, using binary sed patch..."
fi

# Binary sed fallback - replace the namespace string directly in the DLL
# "UnityEngine.Experimental.Rendering" -> "UnityEngine.Rendering\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00"
# Both are type references stored as UTF-8 strings in the DLL metadata
# The old string is longer, so we pad with nulls

# Actually for binary patching we need to be careful about metadata tables
# Let's try a simpler approach: just use sed on any .cs source that references it
echo "=== Also fixing any .cs source references ==="
find /project/Assets -name "*.cs" -exec grep -l "Experimental\.Rendering" {} \; 2>/dev/null | while read F; do
  echo "  Fixing: $F"
  sed -i 's/UnityEngine\.Experimental\.Rendering/UnityEngine.Rendering/g' "$F"
done

# Fix the PostProcessLayer.cs and PostProcessVolume.cs specifically
find /project/Assets -name "PostProcessLayer.cs" -o -name "PostProcessVolume.cs" -o -name "PostProcessRenderContext.cs" | while read F; do
  echo "  Checking: $F"
  grep -n "RenderPipelineAsset\|RenderPipeline" "$F" | head -5
done
