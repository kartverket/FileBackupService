using System;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace FileBackupService
{
    public class MyWatcher
    {
       // List of watchers, there will be one watcher for each filefilter entry;
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();

        #region AppConfigParameters
        /// <summary>
        /// Directory for watching for files to backup
        /// </summary>
        private string SourceDirectory = ConfigurationManager.AppSettings["SourceDirectory"]; 
        /// <summary>
        /// Directory for backing up files
        /// </summary>
        private string DestinationDirectory = ConfigurationManager.AppSettings["DestinationDirectory"];
        /// <summary>
        /// Filters for files to be backed up
        /// </summary>
        private string[] FileFilter = ConfigurationManager.AppSettings["FileFilter"].Split(',');
        /// <summary>
        /// Name on Source in Windows Eventlog
        /// </summary>
        private string EventLogSource = ConfigurationManager.AppSettings["EventLogSource"];
        #endregion AppConfigParameters

        public MyWatcher()
        {
            foreach (var item in FileFilter)
            {
                //creates a new watcher for each filefilter, as long as filter is not only whitespaces
                if (item.Trim().Length>0)
                {
                    try
                    {
                        var w = new FileSystemWatcher();
                        w.Path = SourceDirectory;
                        w.NotifyFilter = NotifyFilters.FileName |
                                    NotifyFilters.LastAccess |
                                     NotifyFilters.LastWrite |
                                     NotifyFilters.DirectoryName;
                        w.Filter = item.Trim();
                        watchers.Add(w);
                    }
                    catch (Exception e)
                    {
                        var sb = new StringBuilder();
                        sb.Append("Error creating watcher for filefilter: " + item.Trim() +".");
                        sb.AppendLine();
                        sb.Append(e.Message);
                        WriteErrorToEventLog(sb.ToString());
                       
                    }
                }

            }
        }

        /// <summary>
        /// Start watching folder for given files, defined by filter, to backup.
        /// SourceDirectory, DestinationDirectory and filters are defined in App.config file
        /// </summary>
        public void Watch()
        {
            foreach (var item in watchers)
            {
                item.Created += new FileSystemEventHandler(OnCreated);
                item.EnableRaisingEvents = true;
                var sb = new StringBuilder();
                sb.AppendFormat("Filter {0} is appended to filewathcer",item.Filter);
                sb.AppendLine();
                sb.Append("Files that match this filter will be copied to destination folder");
                WriteInfoToEventLog(sb.ToString());

            }

        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
                //Check if file is locked
                var locked = IsFileLocked(new FileInfo(e.FullPath));
                var retries = 0;
                //if file is locked, retry max 20 times
                while (locked && retries < 20)
                {
                    Thread.Sleep(1000);
                    locked = IsFileLocked(new FileInfo(e.FullPath));
                    retries += 1;
                }

                //Copy file to destination directory
                try
                {
                    File.Copy(e.FullPath, DestinationDirectory + "\\" + Path.GetFileName(e.FullPath), true);
                //Write copy operation to eventlog
                WriteOperationToEventLog(e.FullPath, DestinationDirectory);

                }
                catch (Exception ex)
                {
                    //Feilmeldingen som kommer er at filen som forsøkes å kopieres, den er i bruk av en annen process.
                   // File.WriteAllText(@"C:\temp\FolderToCopyTo\readLog.txt", ex.ToString());

                    //write error to eventlog
                    WriteErrorToEventLog(ex.ToString());
                }
     


        }

        /// <summary>
        /// Write given string to eventlog Application, Level Error.
        /// </summary>
        /// <param name="_s"></param>
        private void WriteErrorToEventLog(string _s)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = EventLogSource;
             //   eventLog.WriteEntry(_s, EventLogEntryType.Warning, 1001);
                eventLog.WriteEntry(_s, EventLogEntryType.Error, 1002);

            }
        }

        /// <summary>
        /// Write given string to eventlog Application, Level Information.
        /// </summary>
        /// <param name="_info"></param>
        private void WriteInfoToEventLog(string _info)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = EventLogSource;

                eventLog.WriteEntry(_info, EventLogEntryType.Information, 1000);

            }
        }

        /// <summary>
        /// file that is copied: Writes filepath and destination to eventlog Application, Level Information.
        /// </summary>
        /// <param name="_filePath"></param>
        /// <param name="_destination"></param>
        private void WriteOperationToEventLog(string _filePath, string _destination)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = EventLogSource;
               
                //create string that is written to eventlog
                var sb = new StringBuilder();
                sb.Append(_filePath);
                sb.AppendLine();
                sb.Append("is successfully copied to");
                sb.AppendLine();
                sb.Append(_destination);

                eventLog.WriteEntry(sb.ToString(), EventLogEntryType.Information, 1000);

            }
        }

        /// <summary>
        /// Returns true if file is locked, returns false if file is not locked
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

    }
}