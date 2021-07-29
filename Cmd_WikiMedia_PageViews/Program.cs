using ClassLibrary_Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Net;
using ClassLibrary_Business;
using System.Diagnostics;

namespace Cmd_WikiMedia_PageViews
{
    class Program
    {
        static void Main(string[] args)
        {
            bool previous_validations = true;
            string previous_validations_error = "";
            string unexpected_error = "";

            B_BD_PageViews objB_BD_PageViews = new B_BD_PageViews();
            objB_BD_PageViews.MsgInfo("Starting process");
            //Recovering global variables from App.config
            string BD_FILE_PATH = ConfigurationManager.AppSettings["BD_FILE_PATH"].Trim('\\');
            string BD_FILE_NAME = ConfigurationManager.AppSettings["BD_FILE_NAME"].Trim('\\');
            string DOWNLOADS_PATH = ConfigurationManager.AppSettings["DOWNLOADS_PATH"].Trim('\\');
            string URL_BASE = ConfigurationManager.AppSettings["URL_BASE"].Trim('\\');

            int ONLY_SEE_REPORT = 0;
            int DOMAIN_CODE = 0;
            int PAGE_TITLE = 0;
            int COUNT_VIEWS = 0;
            int NUMBER_COLUMNS_GZ = 0;
            int index_year = 0;
            int index_month = 0;
            int index_day = 0;
            int index_hour = 0;
            int MANUAL_DOWNLOAD = 0;
            int MANUAL_YEAR = 0;
            int MANUAL_MONTH = 0;
            int MANUAL_DAY = 0;

            try
            {
                ONLY_SEE_REPORT = Convert.ToInt32(ConfigurationManager.AppSettings["ONLY_SEE_REPORT"]);

                DOMAIN_CODE = Convert.ToInt32(ConfigurationManager.AppSettings["DOMAIN_CODE"]);
                PAGE_TITLE = Convert.ToInt32(ConfigurationManager.AppSettings["PAGE_TITLE"]);
                COUNT_VIEWS = Convert.ToInt32(ConfigurationManager.AppSettings["COUNT_VIEWS"]);
                NUMBER_COLUMNS_GZ = Convert.ToInt32(ConfigurationManager.AppSettings["NUMBER_COLUMNS_GZ"]);

                index_year = Convert.ToInt32(ConfigurationManager.AppSettings["index_year"]);
                index_month = Convert.ToInt32(ConfigurationManager.AppSettings["index_month"]);
                index_day = Convert.ToInt32(ConfigurationManager.AppSettings["index_day"]);
                index_hour = Convert.ToInt32(ConfigurationManager.AppSettings["index_hour"]);

                MANUAL_DOWNLOAD = Convert.ToInt32(ConfigurationManager.AppSettings["MANUAL_DOWNLOAD"]);
                MANUAL_YEAR = Convert.ToInt32(ConfigurationManager.AppSettings["MANUAL_YEAR"]);
                MANUAL_MONTH = Convert.ToInt32(ConfigurationManager.AppSettings["MANUAL_MONTH"]);
                MANUAL_DAY = Convert.ToInt32(ConfigurationManager.AppSettings["MANUAL_DAY"]);
            }
            catch(Exception ex)
            {
                previous_validations = false;
                previous_validations_error = "a non-numeric value was assigned to a numeric variable in the app.config";
            }

            string PATH_REPORT = ConfigurationManager.AppSettings["PATH_REPORT"].Trim('\\');
            string NAME_REPORT = ConfigurationManager.AppSettings["NAME_REPORT"].Trim('\\');

            string BD_FULL_FILE_PATH = BD_FILE_PATH + "\\" + BD_FILE_NAME;
            E_BD_PageViews BD_RECOVERED = new E_BD_PageViews();

            if(NUMBER_COLUMNS_GZ < DOMAIN_CODE || NUMBER_COLUMNS_GZ < PAGE_TITLE || NUMBER_COLUMNS_GZ < COUNT_VIEWS)
            {
                previous_validations = false; previous_validations_error = "index of DOMAIN_CODE, PAGE_TITLE or COUNT_VIEWS can't be greater than NUMBER_COLUMNS_GZ";
            }

            if (ONLY_SEE_REPORT != 0 && ONLY_SEE_REPORT != 1)
            {
                previous_validations = false; previous_validations_error = "ONLY_SEE_REPORT only can be 0 or 1";
            }

            if (MANUAL_DOWNLOAD != 0 && MANUAL_DOWNLOAD != 1)
            {
                previous_validations = false; previous_validations_error = "MANUAL_DOWNLOAD only can be 0 or 1";
            }

            try
            {
                DateTime test = new DateTime(MANUAL_YEAR, MANUAL_MONTH, MANUAL_DAY);
            }
            catch(Exception ex)
            {
                previous_validations = false;
                previous_validations_error = "MANUAL_YEAR, MANUAL_MONTH or MANUAL_DAY have a invalid value";
            }

            if (!Directory.Exists(BD_FILE_PATH)) { previous_validations = false; previous_validations_error = "Path assigned to BD_FILE_PATH does not exist"; }
            if (!Directory.Exists(DOWNLOADS_PATH)) { previous_validations = false; previous_validations_error = "Path assigned to DOWNLOADS_PATH does not exist"; }
            if (!Directory.Exists(PATH_REPORT)) { previous_validations = false; previous_validations_error = "Path assigned to PATH_REPORT does not exist"; }

            if (!objB_BD_PageViews.IsValidFileName(BD_FILE_NAME)) { previous_validations = false; previous_validations_error = "BD_FILE_NAME does not have a valid file name"; }
            if (!objB_BD_PageViews.IsValidFileName(NAME_REPORT)) { previous_validations = false; previous_validations_error = "NAME_REPORT does not have a valid file name"; }

            if (previous_validations)
            {
                //Retrieving bin file that should contain the bd, if the file does not exist, an empty object will be taken as the bd
                objB_BD_PageViews.MsgInfo("Retrieving BD");
                List<E_AllHours> lst = new List<E_AllHours>();
                List<string> lstInvalidos = new List<string>();
                if (File.Exists(BD_FULL_FILE_PATH))
                {
                    FileInfo fi = new FileInfo(BD_FULL_FILE_PATH);
                    if (fi.Length > 0)
                    {
                        BD_RECOVERED = objB_BD_PageViews.Deserialize(BD_FULL_FILE_PATH);
                    }
                }

                //In case you only want to see the report without update the database
                if (ONLY_SEE_REPORT == 0)
                {
                    //Web scrapping will be applied to navigate through all the links within the root page where the compressed files are hosted
                    objB_BD_PageViews.MsgInfo("Starting web scrapping");
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    string html = string.Empty;
                    List<string> LstURLDumpFiles = new List<string>(); //URL of the files that we are going to download
                    List<string> LstURL_to_scrap = new List<string>(); //URL of links that we need to scrap
                    LstURL_to_scrap.Add(URL_BASE); //We start with the base URL

                    do
                    {
                        string url = LstURL_to_scrap.FirstOrDefault();
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                        request.UserAgent = "C# console client";

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            //Read html content of the URL and save it into string var
                            html = reader.ReadToEnd();
                        }

                        List<string> lsturl = objB_BD_PageViews.FindHrefs(html);
                        foreach (var item in lsturl)
                        {
                            Console.WriteLine(url.Trim('/') + "/" + item.Trim('/'));
                            if (item.Contains(".gz"))
                            {
                                LstURLDumpFiles.Add(url.Trim('/') + "/" + item.Trim('/'));
                            }
                            else
                            {
                                LstURL_to_scrap.Add(url.Trim('/') + "/" + item.Trim('/'));
                            }
                        }
                        LstURL_to_scrap.Remove(url);

                    } while (LstURL_to_scrap.Count > 0);

                    string inputFilename, inputFilenameGZ;
                    E_UPLOADED_FILE objFile;
                    int contador = 0, year, month, day, hour;

                    objB_BD_PageViews.MsgInfo("Starting compressed file download");
                    foreach (string URL_File_GZ in LstURLDumpFiles)
                    {
                        inputFilenameGZ = DOWNLOADS_PATH + "\\" + URL_File_GZ.Split('/')[URL_File_GZ.Split('/').Length - 1];
                        inputFilename = inputFilenameGZ.Replace(".gz", "");

                        //Retrieving name and date of the file being read
                        objFile = new E_UPLOADED_FILE();
                        try
                        {
                            objFile.NAME = inputFilename.Split('\\')[inputFilename.Split('\\').Length - 1];
                            year = Convert.ToInt32(objFile.NAME.Substring(index_year, 4));
                            month = Convert.ToInt32(objFile.NAME.Substring(index_month, 2));
                            day = Convert.ToInt32(objFile.NAME.Substring(index_day, 2));
                            hour = Convert.ToInt32(objFile.NAME.Substring(index_hour, 2));
                            objFile.date = new DateTime(year, month, day, hour, 0, 0);
                            objFile.URL = URL_File_GZ;
                        }catch(Exception ex)
                        {
                            unexpected_error = string.Format("Unexpected Error with file {0}: {1}", objFile.NAME, ex.Message);
                        }

                        if (unexpected_error == "")
                        {
                            //We guarantee that we will not take any duplicate files
                            if (!BD_RECOVERED.UPLOADED_FILES.Exists(x => x == objFile.NAME))
                            {
                                objB_BD_PageViews.MsgInfo("Processing url: " + objFile.URL);
                                contador++;
                                if (File.Exists(inputFilenameGZ))
                                {
                                    File.Delete(inputFilenameGZ);
                                }
                                if (File.Exists(inputFilename))
                                {
                                    File.Delete(inputFilename);
                                }
                                objB_BD_PageViews.MsgInfo("Downloading");
                                using (var client = new WebClient())
                                {
                                    client.DownloadFile(URL_File_GZ, inputFilenameGZ);
                                }
                                FileInfo fileToDecompress = new FileInfo(inputFilenameGZ);
                                objB_BD_PageViews.MsgInfo("Unzipping");
                                objB_BD_PageViews.Decompress(fileToDecompress);
                                File.Delete(fileToDecompress.FullName);


                                // Read each line of the file into a string array. Each element of the array is one line of the file
                                objB_BD_PageViews.MsgInfo("Reading file in Array");
                                string[] lines = System.IO.File.ReadAllLines(inputFilename);

                                // Read the array contents by using a foreach loop.
                                E_AllHours pivot;
                                int Total = lines.Count();
                                foreach (string line in lines)
                                {
                                    // Save the array data in an object that will be added to a list
                                    pivot = new E_AllHours();
                                    string[] words = line.Split(' ');
                                    try
                                    {
                                        //Taking the example provided as the correct format, we determine that the correct number of columns is 4
                                        if (words.Length == NUMBER_COLUMNS_GZ)
                                        {
                                            pivot.DOMAIN_CODE = words[DOMAIN_CODE];
                                            pivot.PAGE_TITLE = words[PAGE_TITLE];
                                            pivot.COUNT_VIEWS = Convert.ToInt32(words[COUNT_VIEWS]);
                                            lst.Add(pivot);
                                        }
                                        else
                                        {
                                            lstInvalidos.Add(line);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        unexpected_error = string.Format("Unexpected Error with file {0}, wrong index assignment: {1}", objFile.NAME, ex.Message);
                                        break;
                                    }
                                }

                                if (unexpected_error == "")
                                {
                                    //we add the collected data to an object that we use as a database
                                    BD_RECOVERED.SUMMARY_VIEWS.AddRange(lst);
                                    objFile.Upload_date = DateTime.Now;
                                    BD_RECOVERED.UPLOADED_FILES.Add(objFile.NAME);
                                    File.Delete(inputFilename);

                                    //we save the data collected in the database every 5 files so as not to slow down the process
                                    if (contador % 5 == 0 || contador == LstURLDumpFiles.Count)
                                    {
                                        objB_BD_PageViews.MsgInfo("Start of saving in bd");
                                        BD_RECOVERED.INVALID_LINES = lstInvalidos;
                                        //We use LINQ to calculate the data requested in the request
                                        objB_BD_PageViews.MsgInfo("Summarizing visits in bd");
                                        BD_RECOVERED.SUMMARY_VIEWS = BD_RECOVERED.SUMMARY_VIEWS
                                                                   .GroupBy(x => new { x.DOMAIN_CODE, x.PAGE_TITLE })
                                                                   .Select(cl => new E_AllHours
                                                                   {
                                                                       DOMAIN_CODE = cl.First().DOMAIN_CODE,
                                                                       PAGE_TITLE = cl.First().PAGE_TITLE,
                                                                       COUNT_VIEWS = cl.Sum(c => c.COUNT_VIEWS),
                                                                   }).ToList();

                                        objB_BD_PageViews.MsgInfo("Saving bd to binary file");
                                        //we create a new binary file with the updated data
                                        if (File.Exists(BD_FULL_FILE_PATH + "_new"))
                                        {
                                            File.Delete(BD_FULL_FILE_PATH + "_new");
                                        }
                                        objB_BD_PageViews.Serialize(BD_RECOVERED, BD_FULL_FILE_PATH + "_new");
                                        //once the new database is saved, we delete the old database and rename the updated database
                                        if (File.Exists(BD_FULL_FILE_PATH))
                                        {
                                            File.Delete(BD_FULL_FILE_PATH);
                                        }
                                        File.Move(BD_FULL_FILE_PATH + "_new", BD_FULL_FILE_PATH);
                                    }
                                }
                                else
                                {
                                    objB_BD_PageViews.MsgInfo(unexpected_error);
                                    unexpected_error = "";
                                }
                            }
                            else
                            {
                                objB_BD_PageViews.MsgInfo("Skipped URL: " + objFile.URL);
                            }
                        }
                        else
                        {
                            objB_BD_PageViews.MsgInfo(unexpected_error);
                            unexpected_error = "";
                        }
                    }
                }

                if (BD_RECOVERED.SUMMARY_VIEWS.Count > 0)
                {
                    objB_BD_PageViews.MsgInfo("Generating report");
                    string ReportFullPath = PATH_REPORT + "\\" + NAME_REPORT + ".csv";
                    if (File.Exists(ReportFullPath))
                    {
                        File.Delete(ReportFullPath);
                    }

                    List<E_AllHours> dataToExcel = BD_RECOVERED.SUMMARY_VIEWS.OrderByDescending(x => x.COUNT_VIEWS).ToList();

                    using (StreamWriter sw = new StreamWriter(ReportFullPath))
                    {
                        objB_BD_PageViews.CreateHeader(dataToExcel, sw);
                        objB_BD_PageViews.CreateRows(dataToExcel, sw);
                    }

                    Process.Start(new ProcessStartInfo("notepad.exe", ReportFullPath) { UseShellExecute = true });
                }
                else
                {
                    objB_BD_PageViews.MsgInfo("Database is empty");
                }
            }
            else
            {
                objB_BD_PageViews.MsgInfo(previous_validations_error);
                Console.WriteLine("Press any key to continue....");
                Console.ReadKey();
            }
        }
    }
}
