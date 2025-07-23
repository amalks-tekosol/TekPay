using Newtonsoft.Json;
using sgEftInterface.Configuration;
using sgEftInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using Newtonsoft.Json.Linq;

namespace TekPay
{
    public class MashreqConnect
    {
        public string tracemsg = string.Empty;
        public string resultTracemsg = string.Empty;
        public string mashreqResultData = string.Empty;
        public string transactionAmount = string.Empty;
        public string mRefLbl = string.Empty;
        //private static readonly ManualResetEvent traceEventReceived = new ManualResetEvent(false);  // For waiting
        EftInterface _eft;
        MashreqErrorData _mashreqErrorData = new MashreqErrorData();
        private BackgroundWorker _worker;
        public event Action<string> OnMessageReceived;
        public event Action<int> OnPurchaseCompleted;
        ResponseModel responseData = new ResponseModel();

        public async Task<string> MashreqTrn(RequestModel data)
        {
            var resultData = string.Empty;
            tracemsg = string.Empty;
            resultTracemsg = string.Empty;
            mashreqResultData = string.Empty;
            Log("Mashreq Started");
            var result = "";

            StringBuilder _sb;

            #region mashreq
            try
            {
                //Fetching the config values
                string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TekConfig.txt");
                var lines = File.ReadAllLines(configFilePath);
                var config = lines.Select(line => line.Split('='))
                                  .ToDictionary(parts => parts[0], parts => parts[1]);

                string comport = config["MashreqComport"];
                string InterMediateXml = @"C:\Terminal Connector V1\Terminal Connector\Mashreq\InterMediateXML.xml";
                _eft = EftInterface.CreateEftInterface(comport, InterMediateXml);
                _sb = new StringBuilder();
                Log("Connect : " + _eft);

                if (_worker != null && _worker.IsBusy)
                {
                    _worker.CancelAsync();
                    await Task.Delay(500); // Give time for cleanup
                    _worker.Dispose();
                    _worker = null;
                }



                _eft.OnTrace += TraceMessages;

                _eft.OnTransactionStatus -= IntermediateMessages;
                _eft.OnTransactionStatus += IntermediateMessages;

                _worker = new BackgroundWorker();
                _worker.WorkerSupportsCancellation = true;
                _worker.DoWork += (sender, e) => ListenForIntermediateMessages();
                _worker.RunWorkerAsync();


                //BackgroundWorker worker = new BackgroundWorker();
                //worker.DoWork += PaymentDeviceProcessStartMethod;
                //worker.RunWorkerCompleted += PaymentDeviceProcessEndMethod;
                //worker.RunWorkerAsync();

                _eft.OnUSBDisconnection += EftInterface_OnConnectivity;
                _eft.OnUSBConnection += EftInterface_OnConnectivity;

                Log("STEP : CONNECTIVITY");

                int TransactionTimeout = Convert.ToInt32(config["TransactionTimeout"]);
                int ResponseTimeout = Convert.ToInt32(config["ResponseTimeout"]);
                Log("STEP : CONNECTIVITY - TransactionTimeout : " + TransactionTimeout + " ResponseTimeout : " + ResponseTimeout);
                _eft.ControllerWait = ResponseTimeout;

                _eft.TransactionTimeout = TransactionTimeout;
                _eft.EnableLog = InterfaceConfig.GetKey("EnableLog");

                _eft.IsBusy = false;

                Log("AMOUNT : " + Convert.ToString((int)(data.transaction_amount) * 100));
                Log("MREF VALUE : " + data.transaction_code);
                var ret = 0;

                mRefLbl = data.transaction_code.ToString();

                string actionName = data.action_name;


                //if (!_worker.IsBusy)
                //{
                //    _worker.RunWorkerAsync();
                //}


                switch (actionName)
                {
                    case "PURCHASE":
                        Log("AMOUNT : " + Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100));
                        transactionAmount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                        ret = _eft.Purchase(transactionAmount, mRefLbl);
                        //await Task.Run(() =>
                        //{
                        //    int result = _eft.Purchase(transactionAmount, mRefLbl);
                        //    OnPurchaseCompleted?.Invoke(result); // Send result back to Form1
                        //});
                        break;

                    case "REFUND":
                        transactionAmount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                        string authCode = data.authCode.ToString();
                        ret = _eft.Refund(transactionAmount, mRefLbl, authCode);
                        break;

                    case "LAST TRANSACTION STATUS":
                        mRefLbl = GetLastTransactionCode();
                        ret = _eft.LastTransactionStatus(mRefLbl);
                        break;

                    case "VOID":
                        transactionAmount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                        ret = _eft.VoidRefund(transactionAmount, null, mRefLbl);
                        break;
                }


                //traceEventReceived.Reset();

                #region traceEventReceived
                //bool eventReceived = traceEventReceived.WaitOne(TransactionTimeout);
                //if (!eventReceived)
                //{

                //}
                #endregion

                Log("RESULT : " + Convert.ToString(ret));
                if (ret == 1)
                {
                    //XmlDocument xmlDoc = new XmlDocument();
                    //xmlDoc.LoadXml(mashreqResultData);

                    //string jsonString = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented);


                    //JObject jsonObject = JObject.Parse(jsonString);

                    //int StatusCode = jsonObject["EFTData"]?["ErrorCode"]?.ToString() == "E000" ? 200 : 500;
                    //responseData.CommandType = jsonObject["EFTData"]?["CommandType"]?.ToString() switch
                    //{
                    //    "100" => "Purchase",
                    //    "101" => "Refund",
                    //    "119" => "Void",
                    //    _ => "Failed"
                    //};
                    //if (StatusCode != 200)
                    //{
                    //    responseData.Amount = jsonObject["EFTData"]?["Amount"]?.ToString();
                    //    responseData.Currency = jsonObject["EFTData"]?["Currency"]?.ToString();
                    //    responseData.TID = jsonObject["EFTData"]?["TID"]?.ToString();
                    //    responseData.TransactionDate = DateTime.Now.ToString();
                    //}
                    //else
                    //{
                    //    responseData.ResponseMessage = jsonObject["EFTData"]?["ResponseCode"]?.ToString();
                    //    string transactionDate = jsonObject["EFTData"]?["TransactionDate"]?.ToString();
                    //    string transactionTime = jsonObject["EFTData"]?["TransactionTime"]?.ToString();
                    //    if (!string.IsNullOrEmpty(transactionDate) && !string.IsNullOrEmpty(transactionTime))
                    //    {
                    //        DateTime parsedDate = DateTime.ParseExact(transactionDate, "yyMMdd", null);
                    //        DateTime parsedTime = DateTime.ParseExact(transactionTime, "HHmmss", null);

                    //        DateTime fullDateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day,
                    //                                             parsedTime.Hour, parsedTime.Minute, parsedTime.Second);

                    //        responseData.TransactionDate = fullDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    //    }
                    //    else
                    //    {
                    //        responseData.TransactionDate = "Invalid Date";
                    //    }
                    //    responseData.Amount = jsonObject["EFTData"]?["Amount"]?.ToString();
                    //    responseData.Currency = jsonObject["EFTData"]?["Currency"]?.ToString();
                    //    responseData.CardSchemeName = jsonObject["EFTData"]?["CardSchemeName"]?.ToString();
                    //    responseData.MaskCardNumber = jsonObject["EFTData"]?["MaskCardNumber"]?.ToString();
                    //    responseData.AuthCode = jsonObject["EFTData"]?["AuthCode"]?.ToString();
                    //    responseData.TID = jsonObject["EFTData"]?["TID"]?.ToString();
                    //    responseData.SID = null;
                    //}
                    //responseData.eFT = jsonObject;

                    //string response = JsonConvert.SerializeObject(responseData);

                    //string response = createResult();

                    //resultData = response;
                    //Log("RESULT : " + response);

                    //responseData.ResponseMessage = _mashreqErrorData.GetErrorDescription(jsonObject["EFTData"]?["ErrorCode"]?.ToString());
                    //responseData.StatusCode = StatusCode;
                    //resultData = JsonConvert.SerializeObject(responseData);

                    Log("Purchase acknowledgement successfull.");
                    responseData.ResponseMessage = "Purchase acknowledgement successfull.";
                    responseData.StatusCode = 200;
                    resultData = JsonConvert.SerializeObject(responseData);
                }
                else
                {
                    Log("Error in the transaction");
                    responseData.ResponseMessage = "Error in transaction.";
                    responseData.StatusCode = 500;
                    resultData = JsonConvert.SerializeObject(responseData);
                }

                //LogResult(resultData);
            }
            catch (Exception ex)
            {
                Log("Error in the transaction : "+ex.Message);
            }
            Log("STEP : RETURNING");
            #endregion
            return null;
        }

        private void IntermediateMessages(object sender, IntermediateResponseDataEventArgs e)
        {

            if (_worker == null || _worker.CancellationPending)
                return;

            mashreqResultData = e.XmlData;
            Log("Intermediate Message : " + mashreqResultData);

            //OnMessageReceived?.Invoke(mashreqResultData);

            ////traceEventReceived.Set();
            //if (IsFinalMessage(e.XmlData))
            //{
            //    Log("Final Data Received");
            //    traceEventReceived.Set();
            //    _worker.CancelAsync();
            //}

            //OnMessageReceived?.Invoke(DateTime.Now.ToString("hh:mm:ss tt") + " - " + e.XmlData);

            //XmlDocument doc = new XmlDocument();
            //doc.LoadXml(mashreqResultData);

            XmlDocument doc = new XmlDocument();

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, // ⛔ Block DTDs
                XmlResolver = null                       // ⛔ Block external entities
            };

            using (var stringReader = new StringReader(mashreqResultData))
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                doc.Load(xmlReader);
            }

            string resultValue = string.Empty;
            string resultDes = string.Empty;

            XmlNode errorCodeNode = doc.SelectSingleNode("//ErrorCode");
            if (errorCodeNode != null)
            {

                resultValue = errorCodeNode.InnerText;  // If ErrorCode exists, return it
                resultDes = _mashreqErrorData.GetErrorDescription(resultValue);
            }
            else
            {
                XmlNode commandTypeNode = doc.SelectSingleNode("//CommandType");
                resultValue = commandTypeNode != null ? commandTypeNode.InnerText : "Not Found";
                resultDes = _mashreqErrorData.GetCommandDescription(resultValue);
            }

            OnMessageReceived?.Invoke(resultDes);
            //if(resultValue == "014")
            //{
            //    string response = createResult();
            //    Log("Error Data Received");
            //    //traceEventReceived.Set();
            //    _worker.CancelAsync();
            //}

            if (IsFinalMessage(mashreqResultData))
            {
                Log("Final Data Started");
                string response = createResult();
                Log("Final Data Received");
                //traceEventReceived.Set();
                _worker.CancelAsync();
                _worker.Dispose();      // Immediately free resources
                _worker = null;
            }
        }

        private bool IsFinalMessage(string xmlData)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);

            XmlNode errorCodeNode = xmlDoc.SelectSingleNode("//ErrorCode");

            return errorCodeNode != null;
        }

        private string createResult()
        {
            ResponseModel responseData = new ResponseModel();
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(mashreqResultData);

            XmlDocument xmlDoc = new XmlDocument();

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, // ⛔ Block DTDs
                XmlResolver = null                       // ⛔ Block external entities
            };

            using (var stringReader = new StringReader(mashreqResultData))
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                xmlDoc.Load(xmlReader);
            }

            string jsonString = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.Indented);


            JObject jsonObject = JObject.Parse(jsonString);

            int StatusCode = jsonObject["EFTData"]?["ErrorCode"]?.ToString() == "E000" ? 200 : 500;
            responseData.CommandType = jsonObject["EFTData"]?["CommandType"]?.ToString() switch
            {
                "100" => "Purchase",
                "101" => "Refund",
                "119" => "Void",
                _ => "Failed"
            };
            if (StatusCode != 200)
            {
                responseData.Amount = jsonObject["EFTData"]?["Amount"]?.ToString();
                responseData.Currency = jsonObject["EFTData"]?["Currency"]?.ToString();
                responseData.TID = jsonObject["EFTData"]?["TID"]?.ToString();
                responseData.TransactionDate = DateTime.Now.ToString();
            }
            else
            {
                responseData.ResponseMessage = jsonObject["EFTData"]?["ResponseCode"]?.ToString();
                string transactionDate = jsonObject["EFTData"]?["TransactionDate"]?.ToString();
                string transactionTime = jsonObject["EFTData"]?["TransactionTime"]?.ToString();
                if (!string.IsNullOrEmpty(transactionDate) && !string.IsNullOrEmpty(transactionTime))
                {
                    DateTime parsedDate = DateTime.ParseExact(transactionDate, "yyMMdd", null);
                    DateTime parsedTime = DateTime.ParseExact(transactionTime, "HHmmss", null);

                    DateTime fullDateTime = new DateTime(parsedDate.Year, parsedDate.Month, parsedDate.Day,
                                                         parsedTime.Hour, parsedTime.Minute, parsedTime.Second);

                    responseData.TransactionDate = fullDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    responseData.TransactionDate = "Invalid Date";
                }
                responseData.Amount = jsonObject["EFTData"]?["Amount"]?.ToString();
                responseData.Currency = jsonObject["EFTData"]?["Currency"]?.ToString();
                responseData.CardSchemeName = jsonObject["EFTData"]?["CardSchemeName"]?.ToString();
                responseData.MaskCardNumber = jsonObject["EFTData"]?["MaskCardNumber"]?.ToString();
                responseData.AuthCode = jsonObject["EFTData"]?["AuthCode"]?.ToString();
                responseData.TID = jsonObject["EFTData"]?["TID"]?.ToString();
                responseData.SID = null;
            }
            responseData.eFT = jsonObject;
            responseData.ResponseMessage = _mashreqErrorData.GetErrorDescription(jsonObject["EFTData"]?["ErrorCode"]?.ToString());
            responseData.StatusCode = StatusCode;

            string response = JsonConvert.SerializeObject(responseData);
            return response;
        }

        private void ListenForIntermediateMessages()
        {
            //while (_worker != null && !_worker.CancellationPending)
            //{
            //    if (!string.IsNullOrEmpty(mashreqResultData))
            //    {
            //        Log("Intermediate Message Received.");
            //        XmlDocument doc = new XmlDocument();
            //        doc.LoadXml(mashreqResultData);

            //        string resultValue = string.Empty;
            //        string resultDes = string.Empty;

            //        XmlNode errorCodeNode = doc.SelectSingleNode("//ErrorCode");
            //        if (errorCodeNode != null)
            //        {

            //            resultValue = errorCodeNode.InnerText;  // If ErrorCode exists, return it
            //            resultDes = _mashreqErrorData.GetErrorDescription(resultValue);
            //        }
            //        else
            //        {
            //            XmlNode commandTypeNode = doc.SelectSingleNode("//CommandType");
            //            resultValue = commandTypeNode != null ? commandTypeNode.InnerText : "Not Found";
            //            resultDes = _mashreqErrorData.GetCommandDescription(resultValue);
            //        }

            //        OnMessageReceived?.Invoke(resultDes);
            //        mashreqResultData = ""; // Clear after sending
            //    }

            //    Thread.Sleep(500);
            //}

            //Log("Background Worker Stopped.");

            //if (_worker != null)
            //{
            //    _worker.Dispose();
            //    _worker = null;
            //}
        }
        private void EftInterface_OnConnectivity(object sender, EventArgs e)
        {
            //Message.Show(sender);
        }


        private void Log(string message)
        {
            try
            {
                var path = @"C:\Terminal Connector V1\middleware_logs.txt";
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine($"{DateTime.Now}: {message}");
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during logging
                Console.WriteLine($"Logging error: {ex.Message}");
            }
        }

        private void TraceMessages(object sender, TraceMessageEventArgs e)
        {
            tracemsg += string.Format("{0}: {1}", e.TraceDate, e.Trace);
        }
        private string GetLastTransactionCode()
        {
            string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RequestLogs");

            // Start checking from today's log file
            DateTime currentDate = DateTime.Now;

            while (true) // Loop until we find a valid transaction
            {
                string logFileName = currentDate.ToString("yyyy-MM-dd") + ".txt";
                string logFilePath = Path.Combine(logsDirectory, logFileName);

                if (File.Exists(logFilePath))
                {
                    string lastTransaction = ReadLastTransaction(logFilePath);
                    if (!string.IsNullOrEmpty(lastTransaction))
                    {
                        return lastTransaction; // Return the first valid transaction found
                    }
                }

                // Move to the previous date
                currentDate = currentDate.AddDays(-1);

                // Stop if no more log files exist in the directory
                if (!Directory.GetFiles(logsDirectory, "*.txt").Any(file => file.Contains(currentDate.ToString("yyyy-MM-dd"))))
                    break;
            }

            return null; // No valid transaction found
        }

        private string ReadLastTransaction(string filePath)
        {
            try
            {
                // Read file in reverse order to get the last valid transaction first
                var lines = File.ReadLines(filePath).Reverse();

                foreach (var line in lines)
                {
                    if (line.Contains("Action Name : PURCHASE") && line.Contains("Payment Integartor : Mashreq"))
                    {
                        string[] parts = line.Split(',');

                        foreach (string part in parts)
                        {
                            if (part.Trim().StartsWith("Transactioncode"))
                            {
                                return part.Split(':')[1].Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading log file: {ex.Message}");
            }
            return null;
        }
    }
}

