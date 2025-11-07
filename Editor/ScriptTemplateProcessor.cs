using UnityEngine;
using UnityEditor;
using System.IO;

namespace SFR.AutoNamespace.Editor
{
    public class ScriptTemplateProcessor : UnityEditor.AssetModificationProcessor
    {
        private static void OnWillCreateAsset(string assetPath)
        {
            assetPath = assetPath.Replace(".meta", "");

            if (!assetPath.EndsWith(".cs"))
                return;

            // Wait for Unity to finish creating the file
            EditorApplication.delayCall += () =>
            {
                ProcessScriptFile(assetPath);
            };
        }

        private static void ProcessScriptFile(string assetPath)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            // Verify the file exists
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"File not found: {fullPath}");
                return;
            }

            string content = File.ReadAllText(fullPath);

            // If it already has a namespace, do nothing
            if (content.Contains("namespace "))
                return;

            // Detect the namespace based on the folder
            string folderPath = Path.GetDirectoryName(assetPath);
            string namespaceStr = GenerateNamespace(folderPath);

            // Add the namespace
            content = AddNamespaceToScript(content, namespaceStr);

            // Save the file
            File.WriteAllText(fullPath, content);
            AssetDatabase.Refresh();
        }

        private static string GenerateNamespace(string folderPath)
        {
            folderPath = folderPath.Replace("Assets\\", "").Replace("Assets/", "").Replace("Assets", "");

            folderPath = folderPath.Replace("\\", "/");

            while (folderPath.Contains("//"))
                folderPath = folderPath.Replace("//", "/");

            folderPath = folderPath.Trim('/').Trim('\\').Trim();

            if (string.IsNullOrEmpty(folderPath))
                return "DefaultNamespace";

            string namespaceStr = folderPath.Replace("/", ".");

            return namespaceStr;
        }

        private static string AddNamespaceToScript(string content, string namespaceStr)
        {

            string[] lines = content.Split('\n');
            int lastUsingLine = -1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("using "))
                    lastUsingLine = i;
            }

            string beforeNamespace = "";
            string insideNamespace = "";

            if (lastUsingLine >= 0)
            {
                beforeNamespace = string.Join("\n", lines, 0, lastUsingLine + 1);
                insideNamespace = string.Join("\n", lines, lastUsingLine + 1, lines.Length - lastUsingLine - 1);
            }
            else
            {
                insideNamespace = content;
            }

            insideNamespace = IndentCode(insideNamespace.Trim(), 1);


            string result = beforeNamespace;
            if (!string.IsNullOrEmpty(beforeNamespace))
                result += "\n";

            result += "\nnamespace " + namespaceStr + "\n{\n";
            result += insideNamespace;
            result += "\n}\n";

            return result;
        }

        private static string IndentCode(string code, int levels)
        {
            string indent = new string(' ', levels * 4);
            string[] lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    lines[i] = indent + lines[i];
            }

            return string.Join("\n", lines);
        }
    }
}