using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.XPath;

namespace DynDns
{
    class DynDnsClient
    {
        public const string SERVICE_NAME = "ZoneEditDynDnsClient";
        private const int SLEEP_MS_OK = 60000;
        private const int SLEEP_MS_ERROR = 15 * 60000;
        private const string IP_DETECT_PREFIX = "Current IP Address: ";
        private const string IP_DETECT_URL = "http://dynamic.zoneedit.com/checkip.html";
        private const string IP_UPDATE_URL = "https://dynamic.zoneedit.com/auth/dynamic.html?host={0}&dnsto={1}";

        private static string LogFile { get; set; }

        private static string CurrentIp { get; set; }

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
            // set up error handling
            AppDomain.CurrentDomain.UnhandledException += AppDomainUnhandledException;
            
            // set up logging
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            LogFile = string.Format("{0}\\DynDnsClient.log", path);

            // log startup message
            Log("Starting");

            // install or uninstall, if requested (-i or -u)
            MyInstallerBase installer = new MyInstallerBase(typeof(DynDnsClient), Log);
            bool didInstall = installer.Install(args);
            if (didInstall)
            {
                return; // quit after an installation
            }

            // run direct or as console (-c)
            MyServiceBase service = new MyServiceBase(SERVICE_NAME, MainAction, Log);
            service.Run(args);

            // log shutdown message (graceful)
            Log("Exiting");
		}

        private static void LogString(string s)
        {
            string msg = string.Format("{0}\t{1}{2}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                s.Replace('\t', ' ').Replace(Environment.NewLine, "|"),
                Environment.NewLine);

            Console.Write(msg);
            File.AppendAllText(LogFile, msg);
        }
        private static void Log(object o)
        {
            var ex = o as Exception;
            if (ex != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("ERROR: " + ex.Message);
                sb.AppendLine(ex.StackTrace);

                LogString(sb.ToString());
            }
            else if (o != null)
            {
                LogString(o.ToString());
            }
            else
            {
                LogString("(null)");
            }
        }
        private static void AppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log(e.ExceptionObject);
        }
        private static string GetSetting(string key, bool required)
        {
            string value = ConfigurationManager.AppSettings[key];
            if (required && string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(key);
            }
            return value;
        }

        private static int? MainAction()
        {
            string user = GetSetting("ZoneEditDns_User", true);
            string pass = GetSetting("ZoneEditDns_Pass", true);
            string hosts = GetSetting("ZoneEditDns_Hosts", true);

            // detect IP
            string newIp = null;
            string newIpData = null;
            try
            {
                using (var wc = new WebClient())
                {
                    newIpData = wc.DownloadString(IP_DETECT_URL);
                }
            }
            catch (Exception e)
            {
                Log("Error while detecting current IP:");
                Log(e);
                return SLEEP_MS_ERROR;
            }

            // parse IP response
            string[] ipLines = newIpData.Split(new string[]{"<br>"}, StringSplitOptions.None);
            foreach (var line in ipLines)
            {
                int prefindex = line.IndexOf(IP_DETECT_PREFIX);
                if (prefindex >= 0)
                {
                    newIp = line.Substring(prefindex + IP_DETECT_PREFIX.Length);
                    break;
                }
            }
            if (string.IsNullOrEmpty(newIp))
            {
                Log("Failed to parse current IP");
                return SLEEP_MS_ERROR;
            }
            if (newIp.Count(c => c == '.') != 3)
            {
                Log("Current IP is invalid: " + newIp);
                return SLEEP_MS_ERROR;
            }
            //newIp = "1.2.3.4"; // TESTING

            // check IP freshness
            if (CurrentIp == newIp)
            {
                // IP is current, sleep for a minute
                return SLEEP_MS_OK;
            }

            // update IP (it may not be current)
            string updateData = null;
            try
            {
                using (var wc = new WebClient())
                {
                    string updateUrl = string.Format(IP_UPDATE_URL, hosts, newIp);

                    CredentialCache creds = new CredentialCache();
                    creds.Add(new Uri(updateUrl), "Basic", new NetworkCredential(user, pass));

                    wc.Credentials = creds;
                    updateData = wc.DownloadString(updateUrl);
                }
            }
            catch (Exception e)
            {
                Log("Error while updating current IP:");
                Log(e);
                return SLEEP_MS_ERROR;
            }

            // check update response
            bool hadErrors = false;
            var doc = new XPathDocument(new StringReader("<root>" + updateData + "</root>"));
            var nav = doc.CreateNavigator();
            var errors = nav.Select("/root/ERROR");
            while (errors.MoveNext())
            {
                hadErrors = true;
                Log("Update failure: " + errors.Current.OuterXml);
            }
            var successes = nav.Select("/root/SUCCESS");
            while (successes.MoveNext())
            {
                Log("Update success: " + successes.Current.OuterXml);
            }

            // IP was updated
            if (hadErrors)
            {
                // back off a long while
                return SLEEP_MS_ERROR;
            }
            else
            {
                // update current IP
                CurrentIp = newIp;

                // back off a short while
                return SLEEP_MS_OK;
            }
        }
	}
}