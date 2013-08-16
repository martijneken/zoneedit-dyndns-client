using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Text;

namespace DynDns
{
    /// <summary>
    /// Adapted from this thread about self-installing code:
    /// https://groups.google.com/group/microsoft.public.dotnet.languages.csharp/browse_thread/thread/4d45e9ea5471cba4/4519371a77ed4a74?hl=en
    /// </summary>
    public class MyInstallerBase
    {
        private Type ProgramToInstall { get; set; }
        private LogAction CbLog { get; set; }

        public delegate void LogAction(object o);

        public MyInstallerBase(Type program, LogAction la)
        {
            this.ProgramToInstall = program;
            this.CbLog = la;
        }

        public bool Install(string[] args)
        {
            // parse args
            bool install = false;
            bool uninstall = false;
            foreach (string arg in args) 
            {
                switch (arg.ToLower().TrimStart('-', '/'))
                { 
                    case "i": 
                    case "install": 
                        install = true;
                        break; 
                    case "u": 
                    case "uninstall": 
                        uninstall = true;
                        break;
                }
            }

            // do (un)installation if requested
            if (install)
            {
                DoInstall(true, args);
            }
            if (uninstall)
            {
                DoInstall(false, args);
            }
            return install || uninstall;
        }

        private void DoInstall(bool add, string[] args)
        {
            try
            {
                this.CbLog(add ? "Installing application..." : "Uninstalling application...");
                this.CbLog("This must be run as an Administrator!");

                using (AssemblyInstaller inst = new AssemblyInstaller(
                    this.ProgramToInstall.Assembly, args))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;

                    try
                    {
                        if (add)
                        {
                            inst.Install(state);
                            inst.Commit(state);
                        }
                        else
                        {
                            inst.Uninstall(state);
                        }

                        this.CbLog("Installation successful!");
                    }
                    catch
                    {
                        this.CbLog("Installation error:");
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                this.CbLog(ex);
            }
        }
    }
}
