using System.ServiceProcess;

namespace FileBackupService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new FileBackupService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
