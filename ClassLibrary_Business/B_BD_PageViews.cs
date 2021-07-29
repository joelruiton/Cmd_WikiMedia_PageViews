using ClassLibrary_Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace ClassLibrary_Business
{
    public class B_BD_PageViews
    {
        public List<string> FindHrefs(string input)
        {
            int MANUAL_DOWNLOAD = Convert.ToInt32(ConfigurationManager.AppSettings["MANUAL_DOWNLOAD"]);
            string MANUAL_YEAR, MANUAL_MONTH, MANUAL_DAY;
            if (MANUAL_DOWNLOAD == 1)
            {
                MANUAL_YEAR = ConfigurationManager.AppSettings["MANUAL_YEAR"].ToString();
                MANUAL_MONTH = ConfigurationManager.AppSettings["MANUAL_MONTH"].ToString();
                MANUAL_DAY = ConfigurationManager.AppSettings["MANUAL_DAY"].ToString();
            }
            else
            {
                //subtract 5 hours to make sure to bring all the data when changing from one month to another
                MANUAL_YEAR = DateTime.Today.AddHours(-5).ToString("yyyy");
                MANUAL_MONTH = DateTime.Today.AddHours(-5).ToString("MM");
                MANUAL_DAY = DateTime.Today.AddHours(-5).ToString("dd");
            }
            List<string> lst = new List<string>();
            Regex regex = new Regex("href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", RegexOptions.IgnoreCase);
            Match match;
            for (match = regex.Match(input); match.Success; match = match.NextMatch())
            {
                foreach (Group group in match.Groups)
                {
                    //Unnecessary links are filtered and only the days that interest us are considered
                    if (!group.ToString().Contains("href") && !group.ToString().Contains("..")
                        && !group.ToString().Contains("read") && !group.ToString().Contains("projectview")
                        && (group.ToString().EndsWith(MANUAL_YEAR + "/") 
                            || group.ToString().EndsWith("-" + MANUAL_MONTH + "/") 
                            || group.ToString().Contains(MANUAL_YEAR + MANUAL_MONTH + MANUAL_DAY)))
                    {
                        lst.Add(group.ToString());
                    }
                }
            }
            return lst;
        }

        //Unzip gz
        public void Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName = currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length);

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }
            }
        }

        //Serialize to bin file
        public void Serialize(E_BD_PageViews obj, String filename)
        {
            //Create the stream to add object into it.  
            System.IO.Stream ms = File.OpenWrite(filename);
            //Format the object as Binary  

            BinaryFormatter formatter = new BinaryFormatter();
            //It serialize the employee object  
            formatter.Serialize(ms, obj);
            ms.Flush();
            ms.Close();
            ms.Dispose();
        }

        //Deserialize bin file to object
        public E_BD_PageViews Deserialize(String filename)
        {
            //Format the object as Binary  
            BinaryFormatter formatter = new BinaryFormatter();

            //Reading the file from the server  
            FileStream fs = File.Open(filename, FileMode.Open);

            object obj = formatter.Deserialize(fs);
            E_BD_PageViews deserialized_obj = (E_BD_PageViews)obj;
            fs.Flush();
            fs.Close();
            fs.Dispose();
            return deserialized_obj;
        }

        //Create headers of CSV
        public void CreateHeader<T>(List<T> list, StreamWriter sw)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            for (int i = 0; i < properties.Length - 1; i++)
            {
                sw.Write(properties[i].Name + ",");
            }
            var lastProp = properties[properties.Length - 1].Name;
            sw.Write(lastProp + sw.NewLine);
        }

        //Create body of CSV
        public void CreateRows<T>(List<T> list, StreamWriter sw)
        {
            foreach (var item in list)
            {
                PropertyInfo[] properties = typeof(T).GetProperties();
                for (int i = 0; i < properties.Length - 1; i++)
                {
                    var prop = properties[i];
                    sw.Write(prop.GetValue(item) + ",");
                }
                var lastProp = properties[properties.Length - 1];
                sw.Write(lastProp.GetValue(item) + sw.NewLine);
            }
        }

        //Check if the string is a valid name for a file
        public bool IsValidFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length > 1 && name[1] == ':')
            {
                if (name.Length < 4 || name.ToLower()[0] < 'a' || name.ToLower()[0] > 'z' || name[2] != '\\') return false;
                name = name.Substring(3);
            }
            if (name.StartsWith("\\\\")) name = name.Substring(1);
            if (name.EndsWith("\\") || !name.Trim().Equals(name) || name.Contains("\\\\") ||
                name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
            return true;
        }

        //Print informative message
        public void MsgInfo(string message)
        {
            Console.WriteLine(string.Format("({0}): {1}", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), message));
        }
    }
}
