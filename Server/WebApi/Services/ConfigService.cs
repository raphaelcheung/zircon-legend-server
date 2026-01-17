using Server.Envir;

namespace Server.WebApi.Services
{
    /// <summary>
    /// Service for managing Server.ini configuration
    /// </summary>
    public class ConfigService
    {
        private readonly string _configPath = "./datas/Server.ini";

        /// <summary>
        /// Get the full content of Server.ini
        /// </summary>
        public string GetConfigContent()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    return File.ReadAllText(_configPath);
                }
                return "";
            }
            catch (Exception ex)
            {
                SEnvir.Log($"Error reading config: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Save content to Server.ini
        /// </summary>
        public (bool success, string message) SaveConfigContent(string content)
        {
            try
            {
                // Backup original file
                if (File.Exists(_configPath))
                {
                    var backupPath = _configPath + ".bak";
                    File.Copy(_configPath, backupPath, true);
                }

                // Write new content
                File.WriteAllText(_configPath, content);

                return (true, "Configuration saved successfully. Restart the server to apply changes.");
            }
            catch (Exception ex)
            {
                SEnvir.Log($"Error saving config: {ex.Message}");
                return (false, $"Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Get configuration as sections
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> GetConfigSections()
        {
            var sections = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "General";

            try
            {
                if (!File.Exists(_configPath)) return sections;

                var lines = File.ReadAllLines(_configPath);
                sections[currentSection] = new Dictionary<string, string>();

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("#"))
                        continue;

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                        if (!sections.ContainsKey(currentSection))
                        {
                            sections[currentSection] = new Dictionary<string, string>();
                        }
                        continue;
                    }

                    var eqIndex = trimmedLine.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var key = trimmedLine.Substring(0, eqIndex).Trim();
                        var value = trimmedLine.Substring(eqIndex + 1).Trim();
                        sections[currentSection][key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                SEnvir.Log($"Error parsing config: {ex.Message}");
            }

            return sections;
        }

        /// <summary>
        /// Update a specific configuration value (or add if not exists)
        /// </summary>
        public (bool success, string message) UpdateConfigValue(string section, string key, string value)
        {
            try
            {
                var content = GetConfigContent();
                var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

                bool inSection = string.IsNullOrEmpty(section);
                bool found = false;
                int sectionEndIndex = -1;
                int sectionStartIndex = -1;
                string sectionHeader = $"[{section}]";

                for (int i = 0; i < lines.Count; i++)
                {
                    var trimmedLine = lines[i].Trim();

                    if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                    {
                        if (inSection && sectionStartIndex >= 0)
                        {
                            // 记录当前 section 结束位置（下一个 section 开始前）
                            sectionEndIndex = i;
                        }
                        inSection = trimmedLine.Equals(sectionHeader, StringComparison.OrdinalIgnoreCase);
                        if (inSection)
                        {
                            sectionStartIndex = i;
                        }
                        continue;
                    }

                    if (inSection && trimmedLine.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = $"{key}={value}";
                        found = true;
                        break;
                    }
                }

                // 如果在最后一个 section 中但没找到，sectionEndIndex 为文件末尾
                if (inSection && sectionStartIndex >= 0 && sectionEndIndex < 0)
                {
                    sectionEndIndex = lines.Count;
                }

                if (!found)
                {
                    // 如果 section 存在但 key 不存在，添加到 section 末尾
                    if (sectionStartIndex >= 0)
                    {
                        lines.Insert(sectionEndIndex, $"{key}={value}");
                        found = true;
                    }
                    else
                    {
                        // section 不存在，添加新 section
                        lines.Add("");
                        lines.Add(sectionHeader);
                        lines.Add($"{key}={value}");
                        found = true;
                    }
                }

                var newContent = string.Join(Environment.NewLine, lines);
                return SaveConfigContent(newContent);
            }
            catch (Exception ex)
            {
                SEnvir.Log($"Error updating config: {ex.Message}");
                return (false, $"Failed to update configuration: {ex.Message}");
            }
        }
    }
}
