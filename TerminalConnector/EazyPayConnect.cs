using Newtonsoft.Json;
using PAXDLL.PAXDLL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    public class EazyPayConnect
    {
        private object paxInstance;
        private Type paxType;
        private readonly string easyPayResultData = string.Empty;
        ePOS pax = new ePOS();
        public EazyPayConnect()
        {
            InitializePaxDll();
        }
        private void InitializePaxDll()
        {
            string dllPath = @"C:\Work\Tek-Pay\Tek-Pay\Easy Pay\PAXDLL.dll";

            if (!File.Exists(dllPath))
                throw new FileNotFoundException("PAXDLL not found", dllPath);

            Assembly paxAssembly = Assembly.LoadFrom(dllPath);
            paxType = paxAssembly.GetType("PAXDLL.PAXDLL.ePOS");

            if (paxType == null)
                throw new Exception("Type 'ePOS' not found in PAXDLL");

            paxInstance = Activator.CreateInstance(paxType);
        }
        public async Task<string> EazyPayTrn(RequestModel data)
        {
            try
            {
                #region by reference




                //pax.SetVariable(1, "EPOS0001"); // ePOS Invoice 
                //pax.SetVariable(2, "2000");     // TXN Amount
                //pax.SetVariable(44, "0");       // Terminal Index

                //bool saleSent = pax.PAX_Sale();

                //if (!saleSent)
                //{
                //    return easyPayResultData;
                //}

                //// Thread.Sleep(2000); // wait for terminal to respond

                //string result = pax.PAX_GetTransactionResult();

                ////pax1.dispose();
                ////pax.PAX_ClearBuffer();
                //easyPayResultData = result;
                //return easyPayResultData;

                #endregion

                #region by reflection

                //string dllPath = @"C:\Work\Tek-Pay\Tek-Pay\Easy Pay\PAXDLL.dll";

                //if (!File.Exists(dllPath))
                //    throw new FileNotFoundException("PAXDLL not found", dllPath);

                //// Load the DLL
                //Assembly paxAssembly = Assembly.LoadFrom(dllPath);

                //// Get the type
                //Type paxType = paxAssembly.GetType("PAXDLL.PAXDLL.ePOS");
                //if (paxType == null)
                //    throw new Exception("Type 'ePOS' not found in PAXDLL");

                //// Create an instance
                //object paxInstance = Activator.CreateInstance(paxType);

                //MethodInfo[] methods = paxType.GetMethods();
                //foreach (MethodInfo method in methods)
                //{
                //    Log("Method: " + method.Name);
                //}


                MethodInfo initMethod = paxType.GetMethod("PAX_Initialisation");
                bool init = (bool)initMethod.Invoke(paxInstance, null);

                if (!init)
                {
                    var cancellationMessage = new ResponseModel
                    {
                        ResponseMessage = "Device not initialised.",
                        StatusCode = 409,
                        eFT = null
                    };
                    Log("Device not initialised.");
                    return JsonConvert.SerializeObject(cancellationMessage);
                }

                // Call SetVariable(1, "EPOS0001"), (2, "2000"), (44, "0")
                MethodInfo setVarMethod = paxType.GetMethod("SetVariable");



                string actionName = data.action_name;

                string invoiceNo = data.transaction_code;
                float amountCal = data.transaction_amount * 100;
                string amount = amountCal.ToString();

                switch (data.action_name)
                {
                    case "PURCHASE":


                        setVarMethod.Invoke(paxInstance, new object[] { 1, invoiceNo });
                        setVarMethod.Invoke(paxInstance, new object[] { 2, amount });
                        setVarMethod.Invoke(paxInstance, new object[] { 44, "0" });

                        MethodInfo saleMethod = paxType.GetMethod("PAX_Sale");
                        bool saleSent = (bool)saleMethod.Invoke(paxInstance, null);
                        if (!saleSent)
                        {
                            Log("Sale transaction failed.");
                            var cancellationMessage = new ResponseModel
                            {
                                ResponseMessage = "Sale transaction failed.",
                                StatusCode = 409,
                                eFT = null
                            };
                            return JsonConvert.SerializeObject(cancellationMessage); // or your easyPayResultData
                        }
                        break;
                    case "REFUND":

                        string rrn = data.authCode.ToString();
                        setVarMethod.Invoke(paxInstance, new object[] { 1, invoiceNo });
                        setVarMethod.Invoke(paxInstance, new object[] { 2, amount });
                        setVarMethod.Invoke(paxInstance, new object[] { 44, "0" });

                        PropertyInfo rrnProperty = paxType.GetProperty("PAX_RefrenceNumber");
                        rrnProperty?.SetValue(paxInstance, rrn);

                        MethodInfo refundMethod = paxType.GetMethod("PAX_Refund");
                        bool refundSent = (bool)refundMethod.Invoke(paxInstance, null);

                        if (!refundSent)
                        {
                            PropertyInfo responseMsgProp = paxType.GetProperty("PAX_ResponseMsg");
                            string responseMsg = responseMsgProp?.GetValue(paxInstance)?.ToString();
                            Log("Refund failed : " + responseMsg);
                            var cancellationMessage = new ResponseModel
                            {
                                ResponseMessage = "Refund failed : " + responseMsg,
                                StatusCode = 409,
                                eFT = null
                            };
                            return JsonConvert.SerializeObject(cancellationMessage); // or your easyPayResultData
                        }
                        break;
                    case "LAST TRANSACTION RESPONSE":
                        MethodInfo lastTrnMethod = paxType.GetMethod("PAX_LastTransactionWtihResponse");
                        bool lastTransSent = (bool)lastTrnMethod.Invoke(paxInstance, null);

                        if (!lastTransSent)
                        {
                            PropertyInfo responseMsgProp = paxType.GetProperty("PAX_ResponseMsg");
                            string responseMsg = responseMsgProp?.GetValue(paxInstance)?.ToString();
                            Log("Last transaction response failed : " + responseMsg);
                            var cancellationMessage = new ResponseModel
                            {
                                ResponseMessage = "Refund failed : " + responseMsg,
                                StatusCode = 409,
                                eFT = null
                            };
                            return JsonConvert.SerializeObject(cancellationMessage); // or your easyPayResultData
                        }
                        break;
                    case "VOID":
                        MethodInfo voidMethod = paxType.GetMethod("PAX_LastTransactionWtihResponse");
                        bool voidSent = (bool)voidMethod.Invoke(paxInstance, null);

                        if (!voidSent)
                        {
                            PropertyInfo responseMsgProp = paxType.GetProperty("PAX_ResponseMsg");
                            string responseMsg = responseMsgProp?.GetValue(paxInstance)?.ToString();
                            Log("Last transaction response failed : " + responseMsg);
                            var cancellationMessage = new ResponseModel
                            {
                                ResponseMessage = "Refund failed : " + responseMsg,
                                StatusCode = 409,
                                eFT = null
                            };
                            return JsonConvert.SerializeObject(cancellationMessage); // or your easyPayResultData
                        }
                        break;
                }

                // Call PAX_Sale


                // Optionally: Thread.Sleep(2000); if terminal needs time to respond

                // Call PAX_GetTransactionResult

                MethodInfo resultMethod = paxType.GetMethod("PAX_GetTransactionResult");
                string resultJson = resultMethod.Invoke(paxInstance, null)?.ToString();

                PropertyInfo responseCodeProp = paxType.GetProperty("PAX_ResponseCode");
                string responseCode = responseCodeProp?.GetValue(paxInstance)?.ToString();

                PropertyInfo responseMessageProp = paxType.GetProperty("PAX_ResponseMsg");
                string responseMessage = responseMessageProp?.GetValue(paxInstance)?.ToString();

                PropertyInfo posInvoiceProp = paxType.GetProperty("PAX_PosInvoice");
                string posInvoice = posInvoiceProp?.GetValue(paxInstance)?.ToString();

                PropertyInfo ePosInvoiceProp = paxType.GetProperty("PAX_ePosInvoice");
                string ePosInvoice = ePosInvoiceProp?.GetValue(paxInstance)?.ToString();

                PropertyInfo txnDateTimeProp = paxType.GetProperty("PAX_TxnDateTime");
                string txnDateTime = txnDateTimeProp?.GetValue(paxInstance)?.ToString();


                Log(resultJson);

                EazyPayResponse easyPayResponse = JsonConvert.DeserializeObject<EazyPayResponse>(resultJson);
                string referenceNo = easyPayResponse?.Response?.RefrenceNumber;


                string rawTime = easyPayResponse?.Response?.TrxTime;
                DateTime time = DateTime.ParseExact(rawTime, "HHmmss", CultureInfo.InvariantCulture);

                ResponseModel responseModel = new ResponseModel();
                responseModel.StatusCode = !string.IsNullOrEmpty(responseCode) ? Convert.ToInt32(responseCode) : 0;
                responseModel.ResponseMessage = easyPayResponse?.Response?.ResponseDesc;
                responseModel.CommandType = "";
                responseModel.TransactionDate = "";
                responseModel.TransactionTime = txnDateTime.ToString();
                responseModel.Amount = (Convert.ToInt32(easyPayResponse?.Response?.TrxAmount) / 100).ToString();

                responseModel.Currency = easyPayResponse?.Response?.Currency;
                responseModel.CardSchemeName = easyPayResponse?.Response?.CardName;
                responseModel.MaskCardNumber = easyPayResponse?.Response?.MaskedCardNo;
                responseModel.AuthCode = easyPayResponse?.Response?.AuthCode;
                responseModel.TID = easyPayResponse?.Response?.TID;
                responseModel.SID = "";

                EazyPayCommonRes eazyPayCommonRes = new EazyPayCommonRes();
                eazyPayCommonRes.responseCode = responseCode;
                eazyPayCommonRes.responseMessage = responseMessage;
                eazyPayCommonRes.ePosInvoice = ePosInvoice;
                eazyPayCommonRes.posInvoice = posInvoice;
                eazyPayCommonRes.txnDateTimeProp = txnDateTime;
                eazyPayCommonRes.actEasyPayRes = resultJson;

                EazyPayCommonRes eazyPayLogging = new EazyPayCommonRes();
                eazyPayLogging.responseCode = responseCode;
                eazyPayLogging.responseMessage = responseMessage;
                eazyPayLogging.ePosInvoice = ePosInvoice;
                eazyPayLogging.posInvoice = posInvoice;
                eazyPayLogging.txnDateTimeProp = txnDateTime;

                string eazyPayLogJson = JsonConvert.SerializeObject(eazyPayLogging);

                string eazyPayResultJson = JsonConvert.SerializeObject(eazyPayCommonRes);

                Log("Logging : " + eazyPayLogJson);
                Log("Common Result : " + eazyPayResultJson);

                responseModel.eFT = eazyPayResultJson;

                LogClass logClass = new LogClass();
                logClass.StatusCode = !string.IsNullOrEmpty(responseCode) ? Convert.ToInt32(responseCode) : 0;
                logClass.ResponseMessage = easyPayResponse?.Response?.ResponseDesc;
                logClass.CommandType = "";
                logClass.TransactionDate = "";
                logClass.TransactionTime = time.ToString();
                logClass.Amount = (Convert.ToInt32(easyPayResponse?.Response?.TrxAmount) / 100).ToString();
                logClass.Currency = easyPayResponse?.Response?.Currency;
                logClass.AuthCode = easyPayResponse?.Response?.AuthCode;
                logClass.TID = easyPayResponse?.Response?.TID;
                logClass.SID = "";
                logClass.resultData = eazyPayLogJson;

                string logJson = JsonConvert.SerializeObject(logClass);

                ResultLogging resultLogging = new ResultLogging();
                resultLogging.LogResult(logJson);

                string eazyPayResult = JsonConvert.SerializeObject(responseModel);

                // note: need to remove this line when goes to production
                Log(logJson);

                return eazyPayResult;

                #endregion
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return easyPayResultData;
            }
        }

        public async Task<string> EazyPayInitialization(RequestModel data)
        {
            //bool init = pax.PAX_Initialisation();

            //if (!init)
            //{
            //    var cancellationMessage = new ResponseModel
            //    {
            //        ResponseMessage = "Device not initilised.",
            //        StatusCode = 409,
            //        eFT = null
            //    };

            //    var cancellationResult = JsonConvert.SerializeObject(cancellationMessage);
            //    //callback?.Invoke(cancellationResult);
            //    return cancellationResult;
            //}

            //Call PAX_Initialisation
            MethodInfo initMethod = paxType.GetMethod("PAX_Initialisation");
            bool init = (bool)initMethod.Invoke(paxInstance, null);

            if (!init)
            {
                var cancellationMessage = new ResponseModel
                {
                    ResponseMessage = "Device not initialised.",
                    StatusCode = 409,
                    eFT = null
                };
                Log("Device not initialised.");
                return JsonConvert.SerializeObject(cancellationMessage);
            }
            return easyPayResultData;
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

        public class EazyPayCommonRes
        {
            public string responseCode { get; set; }
            public string responseMessage { get; set; }
            public string posInvoice { get; set; }
            public string ePosInvoice { get; set; }
            public string txnDateTimeProp { get; set; }
            public string actEasyPayRes { get; set; }
        }
    }
}
