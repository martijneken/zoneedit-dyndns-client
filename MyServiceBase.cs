using System;
using System.Net;
using System.ServiceProcess;
using System.Threading;

namespace DynDns
{
    partial class MyServiceBase : ServiceBase
    {
        private static int MAX_CONNECTIONS = 1;
        private static int EXIT_WAIT_MSEC = 120000;
        private static int MAIN_LOOP_MSEC = 60000; // default / error timeout

        private Thread _mainLoopThread;
        private bool _stopping = false;

        private MainAction CbMain { get; set; }
        private LogAction CbLog { get; set; }

        public delegate int? MainAction();
        public delegate void LogAction(object o);

        public MyServiceBase(string name, MainAction ma, LogAction la)
        {
            // InitializeComponent
            this.ServiceName = name;
            this.CbMain = ma;
            this.CbLog = la;
        }

        /// <summary>
        /// Start service, possibly in console
        /// </summary>
        /// <param name="args"></param>
        public void Run(string[] args)
        {
            // parse args
            bool console = false;
            foreach (string arg in args) 
            { 
                switch (arg.ToLower().TrimStart('-','/'))
                { 
                    case "c":
                    case "console":
                        console = true;
                        break; 
                }
            }

            // run
            if (console)
            {
                // running as console
                Console.Out.WriteLine("Starting service");
                this.OnStart(args);

                Console.Out.WriteLine("Hit enter to exit");
                Console.In.ReadLine();
                Console.Out.WriteLine("Waiting for graceful exit");
                this.Stop();
            }
            else
            {
                // real deal, running as service
                ServiceBase.Run(this);
            }
        }

        /// <summary>
        /// Service start callback
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            //ServicePointManager.DefaultConnectionLimit = MAX_CONNECTIONS;
            //ServicePointManager.MaxServicePoints = MAX_CONNECTIONS;
            //ThreadPool.SetMaxThreads(1, 1);

            _mainLoopThread = new Thread(new ThreadStart(MainLoop));
            _mainLoopThread.Start();
        }

        protected override void OnStop()
        {
            _stopping = true;
            base.OnStop();

            // wait for main thread to stop
            _mainLoopThread.Join(EXIT_WAIT_MSEC);
            _mainLoopThread.Abort();
        }

        private void MainLoop()
        {
            while (!_stopping)
            {
                // work
                int? sleep = null;
                try
                {
                    // do that thing you do
                    sleep = this.CbMain();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // die
                    return;
                }
                catch (System.Exception ex)
                {
                    this.CbLog(ex);
                }

                // sleep
                Thread.Sleep(sleep ?? MAIN_LOOP_MSEC);
            }
        }
    }
}
