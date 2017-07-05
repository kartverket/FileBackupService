using System;
using System.IO;
using System.Threading;
using System.Configuration;
using System.Diagnostics;
using System.Text;

namespace FileBackupService
{
    public class MyWatcher
    {
        private FileSystemWatcher reginaWatcher, borettWatcher;

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
        /// Filter for files to be backed up
        /// </summary>
        private string ReginaFilter = ConfigurationManager.AppSettings["ReginaFilter"];
        /// <summary>
        /// Filter for files to be backed up
        /// </summary>
        private string BorettFilter = ConfigurationManager.AppSettings["BorettFilter"];
        #endregion AppConfigParameters

        public MyWatcher()
        {
            this.reginaWatcher = new FileSystemWatcher();
            this.borettWatcher = new FileSystemWatcher();
        }
        public MyWatcher(string _sourceDirectory, string _destinationDirectory)
        {

            this.reginaWatcher = new FileSystemWatcher();
            this.borettWatcher = new FileSystemWatcher();

            this.SourceDirectory = _sourceDirectory;
            this.DestinationDirectory = _destinationDirectory;
        }

        /// <summary>
        /// Start watching folder for given files, defined by filter, to backup.
        /// SourceDirectory, DestinationDirectory and filters are defined in App.config file
        /// </summary>
        public void Watch()
        {
            reginaWatcher.Path = SourceDirectory;
            reginaWatcher.NotifyFilter = NotifyFilters.FileName |
                        NotifyFilters.LastAccess |
                         NotifyFilters.LastWrite |
                         NotifyFilters.DirectoryName;
            reginaWatcher.Filter = ReginaFilter; 
            reginaWatcher.Created += new FileSystemEventHandler(OnCreated);
            // reginaWatcher.Changed += new FileSystemEventHandler(OnCreated);
            reginaWatcher.EnableRaisingEvents = true;

            borettWatcher.Path = SourceDirectory;
            borettWatcher.NotifyFilter = NotifyFilters.FileName |
                        NotifyFilters.LastAccess |
                         NotifyFilters.LastWrite |
                         NotifyFilters.DirectoryName;
            borettWatcher.Filter = BorettFilter; 
            borettWatcher.Created += new FileSystemEventHandler(OnCreated);

            borettWatcher.EnableRaisingEvents = true;
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
                eventLog.Source = "FileBackupService";
             //   eventLog.WriteEntry(_s, EventLogEntryType.Warning, 1001);
                eventLog.WriteEntry(_s, EventLogEntryType.Error, 1002);

            }
        }

        /// <summary>
        /// Write given string to eventlog Application, Level Information.
        /// </summary>
        /// <param name="_s"></param>
        private void WriteOperationToEventLog(string _filePath, string _destination)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "FileBackupService";
               
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