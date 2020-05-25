using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DownloadFolderSorter
{
    public class ConfigData
    {
        // TODO: Add your variables here
        public string downloadFolder = "";
        public int sortDelay = 3000;
        public List<Matching> Matches = new List<Matching>();

        public ConfigData()
        {
            // TODO: Add initilization logic here

        }
    }

    public static class Config
    {
        private static string configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\config.json";
        private static readonly object FileLock;
        private static ConfigData data;
        public static ConfigData Data
        {
            get
            {
                lock (FileLock)
                {
                    return data;
                }
            }
        }

        static Config()
        {
            FileLock = new object();
            Load();
        }

        public static string GetConfigPath()
        {
            return configPath;
        }
        public static void SetConfigPath(string newPath)
        {
            lock (FileLock)
            {
                if (File.Exists(newPath))
                {
                    configPath = newPath;
                    Load();
                }
            }
        }
        public static bool Exists()
        {
            lock (FileLock)
            {
                return File.Exists(configPath);
            }
        }
        public static void Save()
        {
            lock (FileLock)
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(Data));
            }
        }
        public static void Load()
        {
            lock (FileLock)
            {
                if (Exists())
                    data = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configPath));
                else
                    data = new ConfigData();
            }
        }
        public static new string ToString()
        {
            string output = "";

            FieldInfo[] Infos = typeof(ConfigData).GetFields();
            foreach (FieldInfo info in Infos)
            {
                output += "\n" + info.Name + ": ";

                if (info.FieldType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(info.FieldType))
                {
                    output += "\n";
                    IEnumerable a = (IEnumerable)info.GetValue(Data);
                    IEnumerator e = a.GetEnumerator();
                    e.Reset();
                    while (e.MoveNext())
                        output += e.Current + "\n"; ;
                }
                else
                {
                    output += info.GetValue(Data) + "\n";
                }
            }

            return output;
        }
    }
}
