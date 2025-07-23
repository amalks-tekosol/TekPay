using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    public class ResultLogging
    {
        public void LogResult(string message)
        {
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            string logFilePath = Path.Combine(logsDirectory, logFileName);

            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            try
            {
                

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine();
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine();  
                    writer.WriteLine(logEntry);
                }
            }
        }
    }
}
