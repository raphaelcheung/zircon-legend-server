using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.WebApi.Auth
{
    /// <summary>
    /// 在 ini 配置文件中定向写入 [section] 下的 key=value,保留其余内容、注释与原有编码。
    ///
    /// 之所以不使用 ConfigReader.Save():该方法会按 Config 类的属性整文件重新生成内容
    /// (ConfigReader.cs 的 SaveConfig 用全新字典重建),会丢失 ini 中的注释和未映射的键,
    /// 这也是 Program.cs 中 ConfigReader.Save() 被注释掉的原因。
    ///
    /// 本类刻意不依赖服务端其它类型,以便独立测试。
    /// </summary>
    internal static class IniSecretStore
    {
        /// <summary>
        /// 写入或更新指定段的键值。失败时返回 false 并给出原因,调用方应回退到内存临时值。
        /// </summary>
        public static bool TryWrite(string path, string section, string key, string value, out string error)
        {
            error = string.Empty;

            try
            {
                string fullPath = Path.GetFullPath(path);

                List<string> lines;
                Encoding encoding;

                if (File.Exists(fullPath))
                {
                    if (!TryReadLines(fullPath, out lines, out encoding, out error)) return false;
                }
                else
                {
                    string? directory = Path.GetDirectoryName(fullPath);

                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    lines = new List<string>();
                    encoding = new UTF8Encoding(false);
                }

                string header = "[" + section + "]";

                // ConfigReader 的解析语义是"后出现的同名段覆盖先前的",故定位最后一个目标段
                int sectionStart = -1;

                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i] == header) sectionStart = i;
                }

                if (sectionStart < 0)
                {
                    if (lines.Count > 0 && lines[lines.Count - 1].Length > 0) lines.Add(string.Empty);

                    lines.Add(header);
                    lines.Add(key + "=" + value);
                }
                else
                {
                    int sectionEnd = lines.Count;

                    for (int i = sectionStart + 1; i < lines.Count; i++)
                    {
                        if (IsSectionHeader(lines[i]))
                        {
                            sectionEnd = i;
                            break;
                        }
                    }

                    // 段内同名键同样以最后一个为准,因此定位最后一次出现
                    int existing = -1;

                    for (int i = sectionStart + 1; i < sectionEnd; i++)
                    {
                        int separator = lines[i].IndexOf('=');

                        if (separator > 0 && lines[i].Substring(0, separator).Trim() == key)
                            existing = i;
                    }

                    if (existing >= 0)
                        lines[existing] = key + "=" + value;
                    else
                        lines.Insert(sectionEnd, key + "=" + value);
                }

                File.WriteAllLines(fullPath, lines, encoding);

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool TryReadLines(string path, out List<string> lines, out Encoding encoding, out string error)
        {
            lines = new List<string>();
            encoding = new UTF8Encoding(false);
            error = string.Empty;

            byte[] bytes = File.ReadAllBytes(path);

            int preamble = 0;

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                encoding = new UTF8Encoding(true);
                preamble = 3;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = new UnicodeEncoding(false, true);
                preamble = 2;
            }
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                encoding = new UnicodeEncoding(true, true);
                preamble = 2;
            }
            else
            {
                try
                {
                    // 无 BOM 时必须是合法 UTF-8,否则改写会损坏非 UTF-8(如 GBK)编写的中文注释
                    new UTF8Encoding(false, true).GetString(bytes);
                }
                catch (DecoderFallbackException)
                {
                    error = "配置文件不是 UTF-8/UTF-16 编码,改写可能损坏内容,已跳过自动写入";
                    return false;
                }
            }

            string text = encoding.GetString(bytes, preamble, bytes.Length - preamble);

            foreach (string line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
                lines.Add(line);

            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0) lines.RemoveAt(lines.Count - 1);

            return true;
        }

        private static bool IsSectionHeader(string line)
        {
            return line.Length >= 3 && line[0] == '[' && line[line.Length - 1] == ']';
        }
    }
}
