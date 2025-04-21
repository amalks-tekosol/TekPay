using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using sgEftInterface;
using sgEftInterface.Configuration;
using System.Text;
using Newtonsoft.Json.Linq;
using static System.TimeZoneInfo;
using System.Data;
using System.Security.Cryptography;
using System.Xml.Serialization;
using static TerminalConnector.FileProcessor;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.ComponentModel;
using System.Web.UI.WebControls;
using System.Runtime.InteropServices;
using System.IO.Ports;


namespace TerminalConnector
{
    public class MyDataModel
    {
        public int transaction_id { get; set; }
        public string transaction_code { get; set; }
        public string action_name { get; set; }
        public DateTime transaction_date { get; set; }
        public float transaction_amount { get; set; }
        public string payment_method { get; set; }
        public string payment_button { get; set; }
        public string location_code { get; set; }
        public string currency_code { get; set; }
        public string transaction_source { get; set; }
        public string authCode { get; set; }
        public string param_1 { get; set; }
        public string param_2 { get; set; }
        public string param_3 { get; set; }
        public string param_4 { get; set; }
        public string param_5 { get; set; }
        public string reference_code { get; set; }
        public string payment_integrator { get; set; }

    }

    public class Item
    {
        public string name { get; set; }
        public string path { get; set; }
    }

    public class MashreqData
    {
        public string transactionAmount { get; set; }
        public string mrefvalue { get; set; }
    }

    public class ResponseData
    {
        public int StatusCode { get; set; }
        public string ResponseMessage { get; set; }
        public string CommandType { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string CardSchemeName { get; set; }
        public string MaskCardNumber { get; set; }
        public string AuthCode { get; set; }
        public string TID { get; set; }
        public string SID { get; set; }
        public Object eFT { get; set; }
    }

    public class FileProcessor
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
        //[DllImport("madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr api_RequestCOMTrxn(int port, int rate, int noParity, int dataBits, int stopBits, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);


        public async Task<string> DoTransaction(string jsonData, Action<string> callback)
        {
            Log("started 1.1");
            ResponseData responseData = new ResponseData();
            try
            {
                var resultData = string.Empty;


                StringBuilder _sb;
                MyDataModel data = JsonConvert.DeserializeObject<MyDataModel>(jsonData);

                string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TekConfig.txt");

                var lines = File.ReadAllLines(configFilePath);
                var config = lines.Select(line => line.Split('='))
                                  .ToDictionary(parts => parts[0], parts => parts[1]);

                string currentDirectory = config["FolderPath"];


                string fileName = $"terminal_connector_dll_paths.txt";
                string filePath = Path.Combine(currentDirectory, fileName);

                string fileContent;
                try
                {
                    fileContent = File.ReadAllText(filePath);
                }
                catch (Exception ex)
                {
                    throw new IOException("Error reading the file." + ex, ex);

                }

                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    return string.Empty;
                }

                var items = JsonConvert.DeserializeObject<List<Item>>(fileContent);

                var itemDictionary = items.ToDictionary(item => item.name, item => item.path);
                var textContent = "";

                Log("started : " + data.payment_integrator);

                if (itemDictionary.TryGetValue(data.payment_integrator, out string path))
                {
                    textContent = path;
                }
                else
                {
                    string msg = $"The transaction using '{data.payment_integrator}' cannot be completed.";
                    Log("1 | " + msg);
                    throw new TypeLoadException(msg);

                }

                string dllPath = currentDirectory + textContent;

                if (data.action_name == null)
                {
                    Log("2 | " + data.action_name + " cannot be completed.");
                    throw new ArgumentException(data.action_name + " cannot be completed.");
                }

                string methodName = data.action_name;

                if (!File.Exists(dllPath))
                {
                    throw new FileNotFoundException("The transaction has been declined." + dllPath, dllPath);
                }

                LogRequest(data.transaction_code, data.transaction_amount, data.action_name, data.payment_integrator);

                if (data.payment_integrator == "Mashreq")
                {
                    tracemsg = string.Empty;
                    resultTracemsg = string.Empty;
                    mashreqResultData = string.Empty;
                    Log("Mashreq Started");
                    var result = "";

                    #region mashreq
                    try
                    {


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


                        if (actionName == "PURCHASE")
                        {
                            Log("AMOUNT : " + Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100));
                            transactionAmount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                            ret = _eft.Purchase(transactionAmount, mRefLbl);
                            //await Task.Run(() =>
                            //{
                            //    int result = _eft.Purchase(transactionAmount, mRefLbl);
                            //    OnPurchaseCompleted?.Invoke(result); // Send result back to Form1
                            //});

                        }
                        else if (actionName == "REFUND")
                        {
                            transactionAmount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                            string authCode = data.authCode.ToString();
                            ret = _eft.Refund(transactionAmount, mRefLbl, authCode);
                        }
                        else if (actionName == "LAST TRANSACTION STATUS")
                        {
                            mRefLbl = GetLastTransactionCode();
                            ret = _eft.LastTransactionStatus(mRefLbl);
                        }
                        else if (actionName == "VOID")
                        {
                            transactionAmount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                            ret = _eft.VoidRefund(transactionAmount, null, mRefLbl);
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

                        LogResult(resultData);
                    }
                    catch (TargetInvocationException ex)
                    {
                        Log("Ex: " + ex.InnerException?.Message);
                        Log("Ex: " + ex.InnerException?.StackTrace);
                    }
                    Log("STEP : RETURNING");
                    #endregion
                    return resultData;
                }
                else if (data.payment_integrator == "Sample Payment Simulator")
                {
                    Log("Others Started");

                    Assembly assembly = Assembly.LoadFrom(dllPath);

                    Log("dll path : " + dllPath);

                    MethodInfo method = null;
                    Log("Assembly.Gettype : " + assembly.GetTypes());
                    foreach (Type type in assembly.GetTypes())
                    {
                        Log("Type : " + type);
                        method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        if (method != null)
                        {
                            break;
                        }
                    }

                    if (method == null)
                    {
                        throw new MissingMethodException($"Error in payment method '{methodName}'.");
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    //if (parameters.Length != 9 || parameters[0].ParameterType != typeof(int) || parameters[1].ParameterType != typeof(DateTime) || parameters[2].ParameterType != typeof(float) || parameters[3].ParameterType != typeof(string) || parameters[4].ParameterType != typeof(string) || parameters[5].ParameterType != typeof(string) || parameters[6].ParameterType != typeof(string) || parameters[7].ParameterType != typeof(string) || parameters[8].ParameterType != typeof(string) || )
                    //{
                    //    throw new TargetParameterCountException($"Transaction has been declined due to insufficient data.");
                    //}

                    Log("Paramters : " + parameters.ToString());

                    object instance = Activator.CreateInstance(method.DeclaringType);

                    object[] methodArgs = new object[] { data.transaction_id, data.transaction_code, data.action_name, data.transaction_date, data.transaction_amount, data.payment_method, data.payment_button, data.currency_code, data.location_code, data.transaction_source, data.reference_code, data.authCode };

                    var task = (Task<string>)method.Invoke(instance, methodArgs);
                    string result = task.GetAwaiter().GetResult();
                    Log(result.ToString());

                    //                 var parsedJson = JsonConvert.DeserializeObject<JObject>(result);


                    //                 var resultData = parsedJson["ResultData"]?[0]?[0];
                    //                 if (resultData != null)
                    //                 {

                    //responseData.CommandType = resultData["CommandType"]?.ToString();
                    //string TransactionDate = resultData["TransactionDate"]?.ToString();
                    //string TransactionTime = resultData["TransactionTime"]?.ToString();
                    //                     if (!string.IsNullOrEmpty(TransactionDate) && !string.IsNullOrEmpty(TransactionTime))
                    //                     {
                    //                         responseData.TransactionDate = $"{TransactionDate.Substring(0, 10)}T{TransactionTime}";
                    //                     }
                    //                     else
                    //                     {
                    //                         responseData.TransactionDate = null; // Or handle as needed, e.g., default value
                    //                     }
                    //                     responseData.Amount = resultData["Amount"]?.ToString();
                    //responseData.Currency = resultData["Currency"]?.ToString();
                    //responseData.CardSchemeName = resultData["CardSchemeName"]?.ToString();
                    //responseData.MaskCardNumber = resultData["MaskCardNumber"]?.ToString();
                    //responseData.AuthCode = resultData["AuthCode"]?.ToString();
                    //responseData.TID = resultData["TID"]?.ToString();
                    //                 }



                    //string decodedXml = HttpUtility.HtmlDecode(result.Trim('[', ']'));

                    //string unescapedXml = WebUtility.HtmlDecode(decodedXml);


                    #region json to xml
                    //latest codes check
                    //                    string partialDecoded = result.ToString().Trim('[', ']');
                    //                    string htmlDecoded = HttpUtility.HtmlDecode(partialDecoded);
                    //                    string fullyDecoded = Regex.Unescape(htmlDecoded);
                    //                    string cleanedXml = fullyDecoded.Trim();

                    //                    byte[] bomBytes = Encoding.UTF8.GetPreamble();
                    //                    if (Encoding.UTF8.GetPreamble().SequenceEqual(bomBytes))
                    //                    {
                    //                        Log("Detected BOM. Removing...");
                    //                        cleanedXml = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(cleanedXml));
                    //                    }

                    //                    cleanedXml = Regex.Replace(cleanedXml, @"[^\u0000-\u007F]+", "").Trim();

                    //                    byte[] utf16Bytes = Encoding.Unicode.GetBytes(cleanedXml);
                    //                    string utf16DecodedXml = Encoding.Unicode.GetString(utf16Bytes);


                    //                    string xml = @"<?xml version=""1.0"" encoding=""utf-16""?>
                    //<EFTData>
                    //  <CommandType>100</CommandType>
                    //  <ErrorCode>E000</ErrorCode>
                    //  <SaleToEpp>0</SaleToEpp>
                    //  <ResponseCode>APPROVED</ResponseCode>
                    //  <TxnDescription>Purchase transaction</TxnDescription>
                    //  <HostActionCode>99</HostActionCode>
                    //  <HostActionCodeMsg>DECLINE BY CARD</HostActionCodeMsg>
                    //  <Reversed>0</Reversed>
                    //  <TransactionDate>081222</TransactionDate>
                    //  <TransactionTime>134523</TransactionTime>
                    //  <SequenceNo>13</SequenceNo>
                    //  <CardSchemeName>AMEX</CardSchemeName>
                    //  <MaskCardNumber>3456********9778</MaskCardNumber>
                    //  <ExpiryDate>809</ExpiryDate>
                    //  <CardHolderName>Alfredo Buan</CardHolderName>
                    //  <Amount>1200.00</Amount>
                    //  <TipAmount>100.00</TipAmount>
                    //  <CupBilledAmt>1200.00</CupBilledAmt>
                    //  <CupDiscountAmt>0.00</CupDiscountAmt>
                    //  <CupFinalAmt>1200.00</CupFinalAmt>
                    //  <Currency>784</Currency>
                    //  <TxnStatus>OK</TxnStatus>
                    //  <AuthCode>12a456</AuthCode>
                    //  <EntryMode>I</EntryMode>
                    //  <EMVData>
                    //    <ApplicationLabel>Visa Credit</ApplicationLabel>
                    //    <AID>A0000000031010</AID>
                    //    <TVR>80000000</TVR>
                    //    <TSI>F800</TSI>
                    //    <AC>D42C9089D90119B8</AC>
                    //    <CID>40</CID>
                    //  </EMVData>
                    //  <DccData>
                    //    <Exch_Rate>3.6</Exch_Rate>
                    //    <DCC_Currency>AED</DCC_Currency>
                    //    <DCC_Amount>1200.00</DCC_Amount>
                    //  </DccData>
                    //  <CHVerify>1</CHVerify>
                    //  <TID>345678</TID>
                    //  <MID>123456789012</MID>
                    //  <InvoiceNo>123456</InvoiceNo>
                    //  <BatchNo>1</BatchNo>
                    //  <MREFLabel>MREF LABEL:</MREFLabel>
                    //  <MREFValue>AB123456</MREFValue>
                    //  <ReceiptDataMerchant>Merchant Receipt</ReceiptDataMerchant>
                    //  <ReceiptDataCustomer>Customer Receipt</ReceiptDataCustomer>
                    //  <TransactionNo>123456789</TransactionNo>
                    //  <EDWTID>10000001</EDWTID>
                    //  <EDWMID>123456789</EDWMID>
                    //  <RRN>123456789012</RRN>
                    //  <MerchantTranId>12345678</MerchantTranId>
                    //</EFTData>";

                    //                    Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {utf16DecodedXml}{Environment.NewLine}");

                    //                    try
                    //                    {
                    //                        XmlDocument xmlDoc = new XmlDocument();
                    //                        xmlDoc.LoadXml(utf16DecodedXml); // This will throw an exception if the XML is invalid
                    //                    }
                    //                    catch (Exception ex)
                    //                    {
                    //                        Log($"XML Validation Error: {ex.Message}");
                    //}



                    //XmlSerializer serializer = new XmlSerializer(typeof(EFTData));

                    //    EFTData eftData = new EFTData();
                    //    using (StringReader reader = new StringReader(utf16DecodedXml))
                    //    {
                    //        eftData = (EFTData)serializer.Deserialize(reader);

                    //        // Now you can access the EFTData object for further processing.
                    //    }
                    #endregion


                    JObject jsonObject = JObject.Parse(result);

                    //JObject jsonObject = (JObject)jsonArray[0];

                    JObject eftData = jsonObject["ResultData"]?.First as JObject;

                    string status = jsonObject["StatusCode"].ToString();
                    string ResponseMessage = jsonObject["ResponseMessage"].ToString();

                    //if (Convert.ToInt32(status) != 205)
                    //{


                    int StatusCode = Convert.ToInt32(status);
                    responseData.StatusCode = StatusCode;

                    responseData.CommandType = eftData["CommandType"]?.ToString() switch
                    {
                        "100" => "Purchase",
                        "101" => "Refund",
                        "119" => "Void",
                        _ => "Failed"
                    };

                    if (StatusCode != 200)
                    {
                        responseData.Amount = eftData["Amount"]?.ToString();
                        responseData.Currency = eftData["Currency"]?.ToString();
                        responseData.TID = eftData["TID"]?.ToString();
                        responseData.TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        responseData.ResponseMessage = eftData["ResponseCode"]?.ToString();
                        string transactionDate = eftData["TransactionDate"]?.ToString();
                        string transactionTime = eftData["TransactionTime"]?.ToString();

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

                        responseData.Amount = eftData["Amount"]?.ToString();
                        responseData.Currency = eftData["Currency"]?.ToString();
                        responseData.CardSchemeName = eftData["CardSchemeName"]?.ToString();
                        responseData.MaskCardNumber = eftData["MaskCardNumber"]?.ToString();
                        responseData.AuthCode = eftData["AuthCode"]?.ToString();
                        responseData.TID = eftData["TID"]?.ToString();
                        responseData.SID = null;
                    }

                    responseData.ResponseMessage = ResponseMessage;
                    responseData.eFT = eftData;

                    resultData = JsonConvert.SerializeObject(responseData, Newtonsoft.Json.Formatting.Indented);
                    //}
                    //else
                    //{
                    //    resultData = result;
                    //}

                    Log("RESULT : " + resultData);

                    LogResult(resultData);

                    return resultData;
                }
                else if (data.payment_integrator == "Geidea")
                {
                    string amount = "";
                    amount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                    //SetDllDirectory(@"C:\Work\Terminal Connector V1\Terminal Connector\Geidea");
                    //string request = "100;1;1!\0";
                    //byte[] inReqBuff = Encoding.ASCII.GetBytes(request);
                    //int geidea_result = api_RequestCOMTrxn(6, 115200, 0, 8, 0, inReqBuff, 8, 0);
                    //return "Success";
                    try
                    {
                        byte bPort = 6; // COM3
                        uint dwBaudRate = 115200;
                        byte bParity = 0; // No parity
                        byte bDataBits = 8;
                        byte bStopBits = 0; // One stop bit
                        //byte[] inReqBuff = Encoding.ASCII.GetBytes("100;1;1!"); // Input buffer
                        //int inReqLen = 8; // Length of inReqBuff
                        int txtype = 0; // Purchase transaction

                        // Initialize optional parameters as null or empty (adjust based on DLL requirements)
                        string TextString;
                        byte[] intval = new byte[1];
                        byte[] panNo = new byte[23];
                        byte[] purAmount = new byte[13];
                        byte[] stanNo = new byte[7];
                        byte[] dataTime = new byte[13];
                        byte[] expDate = new byte[5];
                        byte[] trxRrn = new byte[13];
                        byte[] authCode = new byte[7];
                        byte[] rspCode = new byte[4];
                        byte[] terminalId = new byte[17];
                        byte[] schemeId = new byte[3];
                        byte[] merchantId = new byte[16];
                        byte[] addtlAmount = new byte[13];
                        byte[] ecrrefno = new byte[17];
                        byte[] version = new byte[10];
                        byte[] outRespLen = new byte[1];
                        StringBuilder outResp = new StringBuilder(15000);

                        // Initialize output response

                        // Call the native function
                        //StringBuilder outresp = new StringBuilder(1024);

                        string actionName = data.action_name;
                        int transaction_type = 0;
                        string rrn = string.Empty;
                        byte[] inReqBuff = null;
                        if (actionName == "PURCHASE")
                        {
                            inReqBuff = Encoding.ASCII.GetBytes(amount + @";1;1!");
                            transaction_type = 0;
                        }
                        else if (actionName == "REFUND")
                        {
                            rrn = data.transaction_code;
                            string refundDate = DateTime.Now.ToString("ddMMyyyy");
                            inReqBuff = Encoding.ASCII.GetBytes(amount + @";" + rrn + ";" + refundDate + @";1,1!");
                            transaction_type = 2;
                        }
                        else if (actionName == "REFUND WITH CARD")
                        {
                            rrn = data.transaction_code;
                            string refundDate = DateTime.Now.ToString("ddMMyyyy");
                            string cardNumber = data.param_1;
                            inReqBuff = Encoding.ASCII.GetBytes(amount + ";" + rrn + ";" + refundDate +";"+ cardNumber+ @";1,1!");
                            //inReqBuff = Encoding.ASCII.GetBytes(@"100;123456781234;13112018;123456******4567;1;1!");
                            transaction_type = 2;
                            
                        }
                        //else if (actionName == "REFUND")
                        //{
                        //    inReqBuff = Encoding.ASCII.GetBytes(amount + @";1;1,1!");
                        //}
                        intval[0] = (byte)inReqBuff.Length;
                        int inReqLen = inReqBuff.Length;
                        intval[0] = 47;
                        //var Result = api_RequestCOMTrxn(
                        //    6,              // COM port
                        //    115200,         // Baud rate
                        //    0,              // No parity
                        //    8,              // Data bits
                        //    0,              // Stop bits
                        //    inReqBuff,      // Request buffer
                        //    intval,   // Request length
                        //    transaction_type,              // Transaction type
                        //    panNo,
                        //    purAmount,
                        //    stanNo,
                        //    dataTime,
                        //    expDate,
                        //    trxRrn,
                        //    authCode,
                        //    rspCode,
                        //    terminalId,
                        //    schemeId,
                        //    merchantId,
                        //    addtlAmount,
                        //    ecrrefno,
                        //    version,
                        //    outResp,
                        //    outRespLen
                        //);
                        int GeideaPort = Convert.ToInt32(config["GeideaPort"]);
                        int GeideaBaudRate = Convert.ToInt32(config["GeideaBaudRate"]);

                        CallRequestCOMTrxnAsync(
                            GeideaPort,              // COM port
                            GeideaBaudRate,         // Baud rate
                            0,              // No parity
                            8,              // Data bits
                            0,              // Stop bits
                            inReqBuff,      // Request buffer
                            intval,   // Request length
                            transaction_type,              // Transaction type
                            panNo,
                            purAmount,
                            stanNo,
                            dataTime,
                            expDate,
                            trxRrn,
                            authCode,
                            rspCode,
                            terminalId,
                            schemeId,
                            merchantId,
                            addtlAmount,
                            ecrrefno,
                            version,
                            outResp,
                            outRespLen,
                            callback
                        );

                        GeideaTransactionResponse geideaResponse = new GeideaTransactionResponse
                        {
                            PanNo = Encoding.ASCII.GetString(panNo).TrimEnd('\0'),
                            PurAmount = Encoding.ASCII.GetString(purAmount).TrimEnd('\0'),
                            StanNo = Encoding.ASCII.GetString(stanNo).TrimEnd('\0'),
                            DataTime = Encoding.ASCII.GetString(dataTime).TrimEnd('\0'),
                            ExpDate = Encoding.ASCII.GetString(expDate).TrimEnd('\0'),
                            TrxRrn = Encoding.ASCII.GetString(trxRrn).TrimEnd('\0'),
                            AuthCode = Encoding.ASCII.GetString(authCode).TrimEnd('\0'),
                            RspCode = Encoding.ASCII.GetString(rspCode).TrimEnd('\0'),
                            TerminalId = Encoding.ASCII.GetString(terminalId).TrimEnd('\0'),
                            SchemeId = Encoding.ASCII.GetString(schemeId).TrimEnd('\0'),
                            MerchantId = Encoding.ASCII.GetString(merchantId).TrimEnd('\0'),
                            AddtlAmount = Encoding.ASCII.GetString(addtlAmount).TrimEnd('\0'),
                            EcrRefNo = Encoding.ASCII.GetString(ecrrefno).TrimEnd('\0'),
                            Version = Encoding.ASCII.GetString(version).TrimEnd('\0'),
                            //PanNo = panNo.ToString(),
                            //PurAmount = purAmount.ToString(),
                            //StanNo = stanNo.ToString(),
                            //DataTime = dataTime.ToString(),
                            //ExpDate = expDate.ToString(),
                            //TrxRrn = trxRrn.ToString(),
                            //AuthCode = authCode.ToString(),
                            //RspCode = rspCode.ToString(),
                            //TerminalId = terminalId.ToString(),
                            //SchemeId = schemeId.ToString(),
                            //MerchantId = merchantId.ToString(),
                            //AddtlAmount = addtlAmount.ToString(),
                            //EcrRefNo = ecrrefno.ToString(),
                            //Version = version.ToString(),
                            OutResp = outResp.ToString(),
                            OutRespLen = outRespLen.ToString()
                        };

                        GeideaResponseData geideaResponseMessage = new GeideaResponseData();
                        ResponseData geideaResponseData = new ResponseData();
                        geideaResponseData.eFT = geideaResponse;
                        geideaResponseData.MaskCardNumber = geideaResponse.PanNo;

                        if (geideaResponse.DataTime.Length == 13)
                            geideaResponse.DataTime = geideaResponse.DataTime.PadRight(14, '0');

                        string formattedDate = string.Empty;
                        if (DateTime.TryParseExact(geideaResponse.DataTime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime parsed))
                        {
                            formattedDate = parsed.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        geideaResponseData.TransactionDate = formattedDate;
                        geideaResponseData.TID = geideaResponse.TrxRrn;
                        geideaResponseData.AuthCode = geideaResponse.AuthCode;
                        geideaResponseData.SID = geideaResponse.SchemeId;
                        geideaResponseData.ResponseMessage = geideaResponseMessage.GetErrorDescription(geideaResponse.RspCode);
                        geideaResponseData.StatusCode = Convert.ToInt32(geideaResponse.RspCode);
                        var geideaResultData = JsonConvert.SerializeObject(geideaResponseData);

                        var giedea_result = outResp;


                        return geideaResultData;

                    }
                    catch (Exception ex)
                    {
                        return "Failure";
                    }
                }
                else
                {
                    Log("Others Started");

                    Assembly assembly = Assembly.LoadFrom(dllPath);

                    MethodInfo method = null;
                    foreach (Type type in assembly.GetTypes())
                    {
                        method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        if (method != null)
                        {
                            break;
                        }
                    }

                    if (method == null)
                    {
                        throw new MissingMethodException($"Error in payment method '{methodName}'.");
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    //if (parameters.Length != 9 || parameters[0].ParameterType != typeof(int) || parameters[1].ParameterType != typeof(DateTime) || parameters[2].ParameterType != typeof(float) || parameters[3].ParameterType != typeof(string) || parameters[4].ParameterType != typeof(string) || parameters[5].ParameterType != typeof(string) || parameters[6].ParameterType != typeof(string) || parameters[7].ParameterType != typeof(string) || parameters[8].ParameterType != typeof(string) || )
                    //{
                    //    throw new TargetParameterCountException($"Transaction has been declined due to insufficient data.");
                    //}

                    object instance = Activator.CreateInstance(method.DeclaringType);

                    object[] methodArgs = new object[] { data.transaction_id, data.transaction_code, data.action_name, data.transaction_date, data.transaction_amount, data.payment_method, data.payment_button, data.currency_code, data.location_code, data.transaction_source, data.reference_code, data.authCode };

                    var result = (string)method.Invoke(instance, methodArgs);

                    //                 var parsedJson = JsonConvert.DeserializeObject<JObject>(result);


                    //                 var resultData = parsedJson["ResultData"]?[0]?[0];
                    //                 if (resultData != null)
                    //                 {

                    //responseData.CommandType = resultData["CommandType"]?.ToString();
                    //string TransactionDate = resultData["TransactionDate"]?.ToString();
                    //string TransactionTime = resultData["TransactionTime"]?.ToString();
                    //                     if (!string.IsNullOrEmpty(TransactionDate) && !string.IsNullOrEmpty(TransactionTime))
                    //                     {
                    //                         responseData.TransactionDate = $"{TransactionDate.Substring(0, 10)}T{TransactionTime}";
                    //                     }
                    //                     else
                    //                     {
                    //                         responseData.TransactionDate = null; // Or handle as needed, e.g., default value
                    //                     }
                    //                     responseData.Amount = resultData["Amount"]?.ToString();
                    //responseData.Currency = resultData["Currency"]?.ToString();
                    //responseData.CardSchemeName = resultData["CardSchemeName"]?.ToString();
                    //responseData.MaskCardNumber = resultData["MaskCardNumber"]?.ToString();
                    //responseData.AuthCode = resultData["AuthCode"]?.ToString();
                    //responseData.TID = resultData["TID"]?.ToString();
                    //                 }



                    //string decodedXml = HttpUtility.HtmlDecode(result.Trim('[', ']'));

                    //string unescapedXml = WebUtility.HtmlDecode(decodedXml);
                    string partialDecoded = result.Trim('[', ']');
                    string htmlDecoded = HttpUtility.HtmlDecode(partialDecoded);
                    string fullyDecoded = Regex.Unescape(htmlDecoded);
                    string cleanedXml = fullyDecoded.Trim();

                    byte[] bomBytes = Encoding.UTF8.GetPreamble();
                    if (Encoding.UTF8.GetPreamble().SequenceEqual(bomBytes))
                    {
                        Log("Detected BOM. Removing...");
                        cleanedXml = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(cleanedXml));
                    }

                    cleanedXml = Regex.Replace(cleanedXml, @"[^\u0000-\u007F]+", "").Trim();

                    byte[] utf16Bytes = Encoding.Unicode.GetBytes(cleanedXml);
                    string utf16DecodedXml = Encoding.Unicode.GetString(utf16Bytes);


                    string xml = @"<?xml version=""1.0"" encoding=""utf-16""?>
<EFTData>
  <CommandType>100</CommandType>
  <ErrorCode>E000</ErrorCode>
  <SaleToEpp>0</SaleToEpp>
  <ResponseCode>APPROVED</ResponseCode>
  <TxnDescription>Purchase transaction</TxnDescription>
  <HostActionCode>99</HostActionCode>
  <HostActionCodeMsg>DECLINE BY CARD</HostActionCodeMsg>
  <Reversed>0</Reversed>
  <TransactionDate>081222</TransactionDate>
  <TransactionTime>134523</TransactionTime>
  <SequenceNo>13</SequenceNo>
  <CardSchemeName>AMEX</CardSchemeName>
  <MaskCardNumber>3456********9778</MaskCardNumber>
  <ExpiryDate>809</ExpiryDate>
  <CardHolderName>Alfredo Buan</CardHolderName>
  <Amount>1200.00</Amount>
  <TipAmount>100.00</TipAmount>
  <CupBilledAmt>1200.00</CupBilledAmt>
  <CupDiscountAmt>0.00</CupDiscountAmt>
  <CupFinalAmt>1200.00</CupFinalAmt>
  <Currency>784</Currency>
  <TxnStatus>OK</TxnStatus>
  <AuthCode>12a456</AuthCode>
  <EntryMode>I</EntryMode>
  <EMVData>
    <ApplicationLabel>Visa Credit</ApplicationLabel>
    <AID>A0000000031010</AID>
    <TVR>80000000</TVR>
    <TSI>F800</TSI>
    <AC>D42C9089D90119B8</AC>
    <CID>40</CID>
  </EMVData>
  <DccData>
    <Exch_Rate>3.6</Exch_Rate>
    <DCC_Currency>AED</DCC_Currency>
    <DCC_Amount>1200.00</DCC_Amount>
  </DccData>
  <CHVerify>1</CHVerify>
  <TID>345678</TID>
  <MID>123456789012</MID>
  <InvoiceNo>123456</InvoiceNo>
  <BatchNo>1</BatchNo>
  <MREFLabel>MREF LABEL:</MREFLabel>
  <MREFValue>AB123456</MREFValue>
  <ReceiptDataMerchant>Merchant Receipt</ReceiptDataMerchant>
  <ReceiptDataCustomer>Customer Receipt</ReceiptDataCustomer>
  <TransactionNo>123456789</TransactionNo>
  <EDWTID>10000001</EDWTID>
  <EDWMID>123456789</EDWMID>
  <RRN>123456789012</RRN>
  <MerchantTranId>12345678</MerchantTranId>
</EFTData>";

                    Log($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {xml}{Environment.NewLine}");

                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xml); // This will throw an exception if the XML is invalid
                    }
                    catch (Exception ex)
                    {
                        Log($"XML Validation Error: {ex.Message}");
                    }



                    XmlSerializer serializer = new XmlSerializer(typeof(EFTData));

                    EFTData eftData = new EFTData();
                    using (StringReader reader = new StringReader(xml))
                    {
                        eftData = (EFTData)serializer.Deserialize(reader);

                        // Now you can access the EFTData object for further processing.
                    }
                    string parsedDate = DateTime.ParseExact(eftData.TransactionDate, "yyMMdd", null).ToString("yyyy-MM-dd");
                    string parsedTime = DateTime.ParseExact(eftData.TransactionTime, "HHmmss", null).ToString("HH:mm:ss");
                    string transactionDateTime = $"{parsedDate}" + " " + $"{parsedTime}";

                    List<ResponseData> response = new List<ResponseData>
                        {
                            new ResponseData
                            {
                                StatusCode = eftData.ErrorCode == "E000" ? 200 : 500,
                                ResponseMessage = eftData.ResponseCode,
                                CommandType = "PURCHASE",
                                TransactionDate = transactionDateTime,
                                Amount = eftData.Amount.ToString(),
                                Currency = eftData.Currency,
                                CardSchemeName = eftData.CardSchemeName,
                                MaskCardNumber = eftData.MaskCardNumber,
                                AuthCode = eftData.AuthCode,
                                TID = eftData.TID,
                                SID = null,
                                eFT = new EFTData
                                {
                                    CommandType = "PURCHASE",
                                    ErrorCode = eftData.ErrorCode,
                                    ResponseCode = eftData.ResponseCode,
                                    TxnDescription = eftData.TxnDescription,
                                    TransactionDate = eftData.TransactionDate,
                                    TransactionTime = eftData.TransactionTime,
                                    SequenceNo = eftData.SequenceNo.ToString(),
                                    CardSchemeName = eftData.CardSchemeName,
                                    MaskCardNumber = eftData.MaskCardNumber,
                                    ExpiryDate = eftData.ExpiryDate,
                                    CardHolderName = eftData.CardHolderName,
                                    Amount = eftData.Amount,
                                    TipAmount = eftData.TipAmount,
                                    CupBilledAmt = eftData.CupBilledAmt,
                                    CupDiscountAmt = eftData.CupDiscountAmt,
                                    CupFinalAmt = eftData.CupFinalAmt,
                                    Currency = eftData.Currency,
                                    TxnStatus = eftData.TxnStatus,
                                    AuthCode = eftData.AuthCode,
                                    InvoiceNo = eftData.InvoiceNo,
                                    SaleToEpp = eftData.SaleToEpp.ToString(),
                                    HostActionCode = eftData.HostActionCode,
                                    HostActionCodeMsg = eftData.HostActionCodeMsg,
                                    Reversed = eftData.Reversed,
                                    EntryMode = eftData.EntryMode,
                                    DCCStatus = "1", // Example, adjust if needed
						            CHVerify = eftData.CHVerify,
                                    TID = eftData.TID,
                                    MID = eftData.MID,
                                    BatchNo = eftData.BatchNo,
                                    MREFLabel = eftData.MREFLabel,
                                    MREFValue = eftData.MREFValue,
                                    ReceiptDataMerchant = eftData.ReceiptDataMerchant,
                                    ReceiptDataCustomer = eftData.ReceiptDataCustomer,
                                    TransactionNo = eftData.TransactionNo,
                                    EDWTID = eftData.EDWTID,
                                    EDWMID = eftData.EDWMID,
                                    RRN = eftData.RRN,
                                    MerchantTranId = eftData.MerchantTranId,
                                    EMVData = eftData.EMVData,
                                    DccData = eftData.DccData
                                }
                            }
                        };

                    string jsonResponse = JsonConvert.SerializeObject(response);

                    //ResponseData resData = new ResponseData();
                    //resData = response;


                    //return result;
                    Log("Data deserialized : " + response);

                    // Set the file path within the current directory
                    string inputFile = Path.Combine(currentDirectory, "log.txt");
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {response}{Environment.NewLine}";
                    File.AppendAllText(inputFile, logEntry);

                    return jsonResponse;

                }

            }
            catch (Exception ex)
            {
                Log(ex.Message + "|" + ex);
                responseData.ResponseMessage = ex.Message;
                responseData.StatusCode = 500;
                var resultData = JsonConvert.SerializeObject(responseData);

                return resultData;
            }



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

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(mashreqResultData);

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

        //private void ListenForIntermediateMessages()
        //{
        //    while (!_worker.CancellationPending)
        //    {
        //        Thread.Sleep(500); // Simulate listening
        //    }
        //}

        public static void CallRequestCOMTrxnAsync(int port, int rate, int noParity, int dataBits, int stopBits, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen, Action<string> callback)
        {
            Task.Run(() =>
            {
                api_RequestCOMTrxn(
                            6,              // COM port
                            115200,         // Baud rate
                            0,              // No parity
                            8,              // Data bits
                            0,              // Stop bits
                            inOutBuff,      // Request buffer
                            intval,   // Request length
                            trnxType,              // Transaction type
                            panNo,
                            purAmount,
                            stanNo,
                            dataTime,
                            expDate,
                            trxRrn,
                            authCode,
                            rspCode,
                            terminalId,
                            schemeId,
                            merchantId,
                            addtlAmount,
                            ecrrefno,
                            version,
                            outResp,
                            outRespLen); // your parameters here


                GeideaTransactionResponse geideaResponse = new GeideaTransactionResponse
                {
                    PanNo = Encoding.ASCII.GetString(panNo).TrimEnd('\0'),
                    PurAmount = Encoding.ASCII.GetString(purAmount).TrimEnd('\0'),
                    StanNo = Encoding.ASCII.GetString(stanNo).TrimEnd('\0'),
                    DataTime = Encoding.ASCII.GetString(dataTime).TrimEnd('\0'),
                    ExpDate = Encoding.ASCII.GetString(expDate).TrimEnd('\0'),
                    TrxRrn = Encoding.ASCII.GetString(trxRrn).TrimEnd('\0'),
                    AuthCode = Encoding.ASCII.GetString(authCode).TrimEnd('\0'),
                    RspCode = Encoding.ASCII.GetString(rspCode).TrimEnd('\0'),
                    TerminalId = Encoding.ASCII.GetString(terminalId).TrimEnd('\0'),
                    SchemeId = Encoding.ASCII.GetString(schemeId).TrimEnd('\0'),
                    MerchantId = Encoding.ASCII.GetString(merchantId).TrimEnd('\0'),
                    AddtlAmount = Encoding.ASCII.GetString(addtlAmount).TrimEnd('\0'),
                    EcrRefNo = Encoding.ASCII.GetString(ecrrefno).TrimEnd('\0'),
                    Version = Encoding.ASCII.GetString(version).TrimEnd('\0'),
                    OutResp = outResp.ToString(),
                    OutRespLen = outRespLen.ToString()
                };

                GeideaResponseData geideaResponseMessage = new GeideaResponseData();
                ResponseData geideaResponseData = new ResponseData();
                geideaResponseData.eFT = geideaResponse;
                geideaResponseData.MaskCardNumber = geideaResponse.PanNo;

                if (geideaResponse.DataTime.Length == 13)
                    geideaResponse.DataTime = geideaResponse.DataTime.PadRight(14, '0');

                string formattedDate = string.Empty;
                if (DateTime.TryParseExact(geideaResponse.DataTime, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out DateTime parsed))
                {
                    formattedDate = parsed.ToString("yyyy-MM-dd HH:mm:ss");
                }
                geideaResponseData.TransactionDate = formattedDate;
                geideaResponseData.TID = geideaResponse.TrxRrn;
                geideaResponseData.AuthCode = geideaResponse.AuthCode;
                geideaResponseData.SID = geideaResponse.SchemeId;
                geideaResponseData.ResponseMessage = geideaResponseMessage.GetErrorDescription(geideaResponse.RspCode);
                geideaResponseData.StatusCode = Convert.ToInt32(geideaResponse.RspCode);
                var geideaResultData = JsonConvert.SerializeObject(geideaResponseData);

                callback?.Invoke(geideaResultData);
                //var giedea_result = outResp;
                //return geideaResultData;
            });
        }

        private string createResult()
        {
            ResponseData responseData = new ResponseData();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(mashreqResultData);

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

        private void TraceMessages(object sender, TraceMessageEventArgs e)
        {

            tracemsg += string.Format("{0}: {1}", e.TraceDate, e.Trace);


        }

        private void EftInterface_OnConnectivity(object sender, EventArgs e)
        {
            //Message.Show(sender);
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

        private void LogResult(string message)
        {
            try
            {
                string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logsDirectory))
                {
                    Directory.CreateDirectory(logsDirectory);
                }

                string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                string logFilePath = Path.Combine(logsDirectory, logFileName);

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging error: {ex.Message}");
            }
        }


        private void LogRequest(string transactionCode, float amount, string action_name, string payment_integrator)
        {
            try
            {
                string logsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RequestLogs");
                if (!Directory.Exists(logsDirectory))
                {
                    Directory.CreateDirectory(logsDirectory);
                }

                string logFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
                string logFilePath = Path.Combine(logsDirectory, logFileName);

                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Transactioncode : {transactionCode}, Amount : {amount}, Action Name : {action_name}, Payemnt Integartor : {payment_integrator}";

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logging error: {ex.Message}");
            }
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

        private bool IsFinalMessage(string xmlData)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);

            XmlNode errorCodeNode = xmlDoc.SelectSingleNode("//ErrorCode");

            return errorCodeNode != null;
        }

        public class EMVData
        {
            public string ApplicationLabel { get; set; }
            public string AID { get; set; }
            public string TVR { get; set; }
            public string TSI { get; set; }
            public string AC { get; set; }
            public string CID { get; set; }
        }

        public class DccData
        {
            public decimal Exch_Rate { get; set; }
            public string DCC_Currency { get; set; }
            public decimal DCC_Amount { get; set; }
        }

        public class EFTData
        {
            public string CommandType { get; set; }
            public string ErrorCode { get; set; }
            public string ResponseCode { get; set; }
            public string TxnDescription { get; set; }
            public string TransactionDate { get; set; }
            public string TransactionTime { get; set; }
            public string SequenceNo { get; set; }
            public string CardSchemeName { get; set; }
            public string MaskCardNumber { get; set; }
            public string ExpiryDate { get; set; }
            public string CardHolderName { get; set; }
            public decimal Amount { get; set; }
            public decimal TipAmount { get; set; }
            public decimal CupBilledAmt { get; set; }
            public decimal CupDiscountAmt { get; set; }
            public decimal CupFinalAmt { get; set; }
            public string Currency { get; set; }
            public string TxnStatus { get; set; }
            public string AuthCode { get; set; }
            public string InvoiceNo { get; set; }
            public string SaleToEpp { get; set; }
            public string HostActionCode { get; set; }
            public string HostActionCodeMsg { get; set; }
            public string Reversed { get; set; }
            public string EntryMode { get; set; }
            public string DCCStatus { get; set; }
            public string CHVerify { get; set; }
            public EMVData EMVData { get; set; }
            public DccData DccData { get; set; }
            public string TID { get; set; }
            public string MID { get; set; }
            public string BatchNo { get; set; }
            public string MREFLabel { get; set; }
            public string MREFValue { get; set; }
            public string ReceiptDataMerchant { get; set; }
            public string ReceiptDataCustomer { get; set; }
            public string TransactionNo { get; set; }
            public string EDWTID { get; set; }
            public string EDWMID { get; set; }
            public string RRN { get; set; }
            public string MerchantTranId { get; set; }
        }

        public class GeideaTransactionResponse
        {
            public string PanNo { get; set; }
            public string PurAmount { get; set; }
            public string StanNo { get; set; }
            public string DataTime { get; set; }
            public string ExpDate { get; set; }
            public string TrxRrn { get; set; }
            public string AuthCode { get; set; }
            public string RspCode { get; set; }
            public string TerminalId { get; set; }
            public string SchemeId { get; set; }
            public string MerchantId { get; set; }
            public string AddtlAmount { get; set; }
            public string EcrRefNo { get; set; }
            public string Version { get; set; }
            public string OutResp { get; set; }
            public string OutRespLen { get; set; }
        }
    }
}
