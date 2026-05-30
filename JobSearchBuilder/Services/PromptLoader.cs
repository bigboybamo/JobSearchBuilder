using System;
using System.IO;

namespace JobSearchBuilder.Services
{
    public class PromptLoader
    {
        private readonly string _promptsRoot;

        public PromptLoader(string promptsRoot = null)
        {
            _promptsRoot = string.IsNullOrWhiteSpace(promptsRoot)
                ? FindPromptsRoot()
                : promptsRoot;
        }

        public string Load(string promptName, string version)
        {
            if (string.IsNullOrWhiteSpace(promptName)) throw new ArgumentException("Prompt name is required.", "promptName");
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Prompt version is required.", "version");

            string path = Path.Combine(_promptsRoot, promptName, version + ".xml");
            if (!File.Exists(path))
                throw new FileNotFoundException("Prompt template not found.", path);

            return File.ReadAllText(path);
        }

        private static string FindPromptsRoot()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                string candidate = Path.Combine(current, "prompts");
                if (Directory.Exists(candidate))
                    return candidate;

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                    break;

                current = parent.FullName;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prompts");
        }
    }
}
