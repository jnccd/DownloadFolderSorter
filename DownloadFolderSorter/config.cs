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
        public List<Matching> Matches = new List<Matching>();

        public ConfigData()
        {
            // TODO: Add initilization logic here

        }
    }

    public static class Config
    {
        static readonly string configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\config.json";
        public static ConfigData Data = new ConfigData();

        static Config()
        {
            if (Config.Exists())
                Config.Load();
            else
                Config.Data = new ConfigData();
        }

        public static string GetConfigPath()
        {
            return configPath;
        }
        public static bool Exists()
        {
            return File.Exists(configPath);
        }
        public static void Save()
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(Data));
        }
        public static void Load()
        {
            if (Exists())
                Data = JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configPath));
            else
                Data = new ConfigData();
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
