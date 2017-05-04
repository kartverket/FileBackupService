using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

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
