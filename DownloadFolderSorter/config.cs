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
    public class configData
    {
        // TODO: Add your variables here
        public string downloadFolder = "";
        public List<Matching> Matches = new List<Matching>();

        public configData()
        {
            // TODO: Add initilization logic here

        }
    }

    public static class config
    {
        static string configPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\config.json";
        public static configData Data = new configData();

        static config()
        {
            if (config.Exists())
                config.Load();
            else
                config.Data = new configData();
        }

        public static string getConfigPath()
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
                Data = JsonConvert.DeserializeObject<configData>(File.ReadAllText(configPath));
            else
                Data = new configData();
        }
        public static new string ToString()
        {
            string output = "";

            FieldInfo[] Infos = typeof(configData).GetFields();
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
