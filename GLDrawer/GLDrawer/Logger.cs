using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace GLDrawer
{
    static class Logger
    {

        private static string FileName;
        private static Queue<string> LogItems = new Queue<string>();
        private static Thread LogThread = null;

        static Logger()
        {
            // start the log writing thread
            try
            {
                LogThread = new Thread(ThreadLog);
                LogThread.IsBackground = true;
                LogThread.Start();

                // show startup log message
                AppendLog("----------------------------------------------------------------------");
            }
            catch (Exception err)
            {
                Console.WriteLine("Error starting log thread : " + err.Message);
            }
        }

        public static void AppendLog(string sText)
        {
            string sLogItem = DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToLongTimeString() + " - " + sText;
            lock (LogItems)
                LogItems.Enqueue(sLogItem);
        }

        private static void ThreadLog(object o)
        {
            string sLogItem = "";
            bool bIsItem;

            while (true)
            {
                // grab a log item, if there is one to get...
                bIsItem = false;
                lock (LogItems)
                {
                    if (LogItems.Count != 0)
                    {
                        sLogItem = LogItems.Dequeue();
                        bIsItem = true;
                    }
                }

                if (bIsItem)
                {
                    // log it...
                    try
                    {
                        StreamWriter sw = new StreamWriter(FileName, true, Encoding.UTF8);
                        sw.WriteLine(sLogItem);
                        sw.Close();
                    }
                    catch (Exception err)
                    {
                        FileName = "Error : " + err.Message;
                    }
                }

                Thread.Sleep(1);
            }
        }
    }
}
