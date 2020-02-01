using System.IO;
using UnityEditor.Compilation;

namespace Unity.Labs.MARS.CodeGen
{
    static class CodeGenerationShared
    {
        public const string Indent = "    ";

        internal static string TemplatesFolder = GetTemplatesFolder();
        internal static string OutputFolder = GetOutputFolder();

        internal static string GetDictionaryPool(CodeGenerationTypeData data)
        {
            return $"Pools.{data.MemberPrefix}Results";
        }

        public static void EnsureOutputFolder()
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);
        }

        static string GetTemplatesFolder()
        {
            const string marsEditorAssembly = "Unity.Labs.MARS.Editor";
            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(marsEditorAssembly);
            var asmdefFolder = Path.GetDirectoryName(asmdefPath);
            return $"{asmdefFolder}/Scripts/CodeGen/Templates/";
        }

        static string GetOutputFolder()
        {
            const string marsRuntimeAssembly = "Unity.Labs.MARS";
            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(marsRuntimeAssembly);
            var asmdefFolder = Path.GetDirectoryName(asmdefPath);
            return $"{asmdefFolder}/Scripts/Generated/";
        }
    }
}
