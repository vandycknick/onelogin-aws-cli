using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace OneLoginAws.Utils
{
    public class IniFile : Dictionary<string, Dictionary<string, string>>
    {
        public static IniFile Open(string filePath) => new IniFile(filePath);

        public static IniFile Open(IFileInfo fileInfo) => new IniFile(fileInfo);

        private readonly string? _filePath;
        private readonly IFileInfo? _fileInfo;

        /// <summary>
        /// Initialize an INI file
        /// Load it if it exists
        /// </summary>
        /// <param name="path">Full path where the INI file has to be read from or written to</param>
        private IniFile(string path)
        {
            _filePath = path;

            if (!File.Exists(path))
                return;

            using var stream = File.OpenRead(_filePath);
            using var reader = new StreamReader(stream);
            Load(reader);
        }

        private IniFile(IFileInfo fileInfo)
        {
            _fileInfo = fileInfo;
            using var reader = _fileInfo.OpenText();
            Load(reader);
        }

        private void Load(StreamReader reader)
        {
            var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            this[""] = currentSection;

            var index = 0;
            var line = "";
            while ((line = reader.ReadLine()) != null)
            {
                index++;
                if (line.StartsWith(";") || string.IsNullOrWhiteSpace(line))
                {
                    currentSection.Add($";{index}", line);
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    this[line.Substring(1, line.Length - 2)] = currentSection;
                    continue;
                }

                var idx = line.IndexOf("=");
                if (idx == -1)
                    currentSection[line] = "";
                else
                    currentSection[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
            }
        }

        /// <summary>
        /// Get a parameter value at the root level
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <returns></returns>
        public string GetValue(string key)
        {
            return GetValue(key, "", "");
        }

        /// <summary>
        /// Get a parameter value in the section
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <param name="section">section</param>
        /// <returns></returns>
        public string GetValue(string key, string section) => GetValue(key, section, "");


        /// <summary>
        /// Returns a parameter value in the section, with a default value if not found
        /// </summary>
        /// <param name="key">parameter key</param>
        /// <param name="section">section</param>
        /// <param name="default">default value</param>
        /// <returns></returns>
        public string GetValue(string key, string section, string @default)
        {
            if (!ContainsKey(section))
                return @default;

            if (!this[section].ContainsKey(key))
                return @default;

            return this[section][key];
        }

        private Stream OpenFileStream()
        {
            if (_filePath is string)
                return File.OpenWrite(_filePath);

            if (_fileInfo is object)
                return _fileInfo.OpenWrite();

            throw new NotSupportedException("Only writing to path or IFileInfo supported. It should not be possible to land here!");
        }

        /// <summary>
        /// Save the INI file
        /// </summary>
        public void Save() => Save(Encoding.Default);

        /// <summary>
        /// Save the INI file with encoding
        /// </summary>
        public void Save(Encoding encoding)
        {
            using var stream = OpenFileStream();
            using var writer = new StreamWriter(stream, encoding);

            var sb = new StringBuilder();
            KeyValuePair<string, string>? lastValueWritten = null;

            foreach (var section in this)
            {
                if (section.Key != "")
                {
                    // Bit of a hack to make sure section are always spaced out
                    if (lastValueWritten.HasValue && !string.IsNullOrEmpty(lastValueWritten.Value.Value))
                    {
                        sb.AppendLine();
                    }

                    sb.AppendFormat("[{0}]", section.Key);
                    sb.AppendLine();
                }


                foreach (var keyValue in section.Value)
                {
                    if (keyValue.Key.StartsWith(";"))
                    {
                        sb.Append(keyValue.Value);
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendFormat("{0} = {1}", keyValue.Key, keyValue.Value);
                        sb.AppendLine();
                    }

                    lastValueWritten = keyValue;
                }

                writer.Write(sb);
                sb.Clear();
            }
        }


        /// <summary>
        /// Creates a new section if it does not already exist
        /// </summary>
        /// <param name="section">parameter section</param>
        public void CreateSectionIfNotExists(string section)
        {
            if (!ContainsKey(section))
            {
                Add(section, new Dictionary<string, string>());
            }
        }

        /// <summary>
        /// Get all the keys names in a section
        /// </summary>
        /// <param name="section">section</param>
        /// <returns></returns>
        public string[] GetKeys(string section)
        {
            if (!ContainsKey(section))
                return new string[0];

            return this[section].Keys.ToArray();
        }

        /// <summary>
        /// Get all the section names of the INI file
        /// </summary>
        /// <returns></returns>
        public string[] GetSections() => Keys.Where(t => t != "").ToArray();
    }
}
