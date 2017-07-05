using System.ServiceProcess;

namespace FileBackupService
{
    public partial class FileBackupService : ServiceBase
    {
        MyWatcher mw = new MyWatcher();
        public FileBackupService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            mw.Watch();
        }

        protected override void OnStop()
        {
        }
    }
}
