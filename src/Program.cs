using log4net;
using log4net.Config;
using System;
using System.IO;

namespace Doway.Tools.Robinhood
{
    class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));
        static void Main(string[] args)
        {
            GlobalContext.Properties["appname"] = "Robinhood";
            XmlConfigurator.ConfigureAndWatch(new FileInfo("log4net.config"));
            try
            {
                var copier = new WebsiteCopier(args[0], args[1]);
                copier.StartCopy();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex.Message, ex);
            }
            finally
            {
                Console.ReadLine();
            }
        }
    }
}
