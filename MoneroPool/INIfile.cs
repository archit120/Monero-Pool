using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace MoneroPool
{
    public class IniFile
    {
        public string path;

        /// <summary>
        /// INIFile Constructor.
        /// </summary>
        /// <PARAM name="INIPath"></PARAM>
        /// 
        public IniFile(string INIPath)
        {
            if (!System.IO.File.Exists(INIPath))
            {
             FileStream fs1 =   System.IO.File.Create(INIPath);
             fs1.Dispose();
            }
            path = INIPath;
        }
        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// Section name
        /// <PARAM name="Key"></PARAM>
        /// Key Name
        /// <PARAM name="Value"></PARAM>
        /// Value Name
        public void IniWriteValue(string Key, string Value)
        {
            if (IniReadValue(Key) == "" && !File.ReadAllText(path).Contains(Key))
            {
                if (File.ReadAllText(path).EndsWith("\n"))
                    File.AppendAllText(path, Key + "=" + Value.Replace("\r\n", ""));
                else
                    File.AppendAllText(path, "\r\n" + Key + "=" + Value.Replace("\r\n", ""));

            }
            else
            {
                string[] Lines = File.ReadAllLines(path);
                for (int i = 0; i < Lines.Length; i++)
                {
                    if (Lines[i].StartsWith(Key))
                    {
                        Lines[i] = Key + "=" + Value;
                        break;
                    }
                }
                File.WriteAllLines(path, Lines);
            }
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <PARAM name="Section"></PARAM>
        /// <PARAM name="Key"></PARAM>
        /// <PARAM name="Path"></PARAM>
        /// <returns></returns>
        public string IniReadValue(string Key)
        {
            string[] Lines = File.ReadAllLines(path);
            if (Lines.Where(line => line.StartsWith(Key)).ToArray().Length > 0)
                return Lines.Where(line => line.StartsWith(Key)).Take(1).ToArray()[0].Replace(Key + "=", "");
            else throw new Exception(string.Format("Couldn't find {0} key in config. Check your config file", Key));
        }
    }
}
