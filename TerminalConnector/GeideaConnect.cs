using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static TekPay.TekPay;

namespace TekPay
{
    public class GeideaConnect
    {   
        //[DllImport("madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr api_RequestCOMTrxn(int port, int rate, int noParity, int dataBits, int stopBits, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        //[DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        //public static extern IntPtr api_CheckStatus(int port, int rate, int noParity, int dataBits, int stopBits, byte[] inOutBuff, byte[] intval, int trnxType, byte[] panNo, byte[] purAmount, byte[] stanNo, byte[] dataTime, byte[] expDate, byte[] trxRrn, byte[] authCode, byte[] rspCode, byte[] terminalId, byte[] schemeId, byte[] merchantId, byte[] addtlAmount, byte[] ecrrefno, byte[] version, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder outResp, byte[] outRespLen);

        [DllImport(@"madaapi_v1_9.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int api_CheckStatus(int port, int rate, int noParity, int dataBits, int stopBits, byte[] inOutBuff, byte[] intval);

        public async Task<string> GeideaTrn(RequestModel data)
        {
            string working_method = "sync";

            //Fetching the config values
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TekConfig.txt");
            var lines = File.ReadAllLines(configFilePath);
            var config = lines.Select(line => line.Split('='))
                              .ToDictionary(parts => parts[0], parts => parts[1]);

            int GeideaPort = Convert.ToInt32(config["GeideaPort"]);
            int GeideaBaudRate = Convert.ToInt32(config["GeideaBaudRate"]);
            byte[] Checkintval = new byte[1];
            byte[] CheckinReqBuff = null;
            CheckinReqBuff = Encoding.ASCII.GetBytes("04!");
            //Buffer.BlockCopy(input, 0, CheckinReqBuff, 0, input.Length);
            Checkintval[0] = (byte)CheckinReqBuff.Length;

            int deviceStatus = 0;

            switch (working_method)
            {
                case "async":
                    deviceStatus = await Task.Run(() =>
                        api_CheckStatus(
                            GeideaPort,
                            GeideaBaudRate,
                            0,
                            8,
                            0,
                            CheckinReqBuff,
                            Checkintval
                        )
                    );
                    break;

                case "sync":
                default:
                    deviceStatus = api_CheckStatus(
                        GeideaPort,
                        GeideaBaudRate,
                        0,
                        8,
                        0,
                        CheckinReqBuff,
                        Checkintval
                    );
                    break;
            }


            if (deviceStatus != 0)
            {
                var cancellationMessage = new ResponseModel
                {
                    ResponseMessage = "Another transaction in process.",
                    StatusCode = 409,
                    eFT = null
                };

                var cancellationResult = JsonConvert.SerializeObject(cancellationMessage);
                //callback?.Invoke(cancellationResult);
                return cancellationResult;
            }
            else
            {

                string amount = "";
                amount = Convert.ToString(Convert.ToInt32(data.transaction_amount) * 100);
                try
                {
                    byte bParity = 0; // No parity
                    byte bDataBits = 8;
                    byte bStopBits = 0; // One stop bit

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


                    string actionName = data.action_name;
                    int transaction_type = 0;
                    string rrn = string.Empty;
                    byte[] inReqBuff = null;
                    switch (actionName)
                    {
                        case "PURCHASE":
                            inReqBuff = Encoding.ASCII.GetBytes(amount + @";1;1!");
                            transaction_type = 0;
                            break;

                        case "REFUND":
                            rrn = data.transaction_code;
                            string refundDate = DateTime.Now.ToString("ddMMyyyy");
                            inReqBuff = Encoding.ASCII.GetBytes(amount + @";" + rrn + ";" + refundDate + @";1;1!");
                            transaction_type = 2;
                            break;

                        case "REFUND WITH CARD":
                            rrn = data.transaction_code;
                            string refundDateCard = DateTime.Now.ToString("ddMMyyyy");
                            string cardNumber = data.param_1;
                            inReqBuff = Encoding.ASCII.GetBytes(amount + ";" + rrn + ";" + refundDateCard + ";" + cardNumber + @";1;1!");
                            transaction_type = 2;
                            break;
                    }
                    intval[0] = (byte)inReqBuff.Length;
                    Log(inReqBuff.Length.ToString());

                    //var statusCheck =  api_CheckStatus(GeideaPort, GeideaBaudRate, 0, 8, 0, Encoding.ASCII.GetBytes("04!"), intval);

                    if (working_method == "async")
                    {
                        //    CallRequestCOMTrxnAsync(
                        //    GeideaPort,              // COM port
                        //    GeideaBaudRate,         // Baud rate
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
                        //    outRespLen,
                        //    callback
                        //);
                    }
                    else
                    {
                        var geideaResult = api_RequestCOMTrxn(
                            6,              // COM port
                            115200,         // Baud rate
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
                            outRespLen);
                    }

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
                    ResponseModel geideaResponseData = new ResponseModel();
                    var geideaResultData = string.Empty;
                    if (geideaResponse.RspCode == string.Empty)
                    {
                        geideaResponseData.ResponseMessage = "Transaction has been cancelled.";
                        geideaResponseData.StatusCode = 205;
                        geideaResponseData.eFT = null;
                        geideaResultData = JsonConvert.SerializeObject(geideaResponseData);
                        return geideaResultData;
                    }

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
                    geideaResponseData.StatusCode = Convert.ToInt32(geideaResponse.RspCode == "000" ? "200" : geideaResponse.RspCode);
                    geideaResultData = JsonConvert.SerializeObject(geideaResponseData);

                    //LogResult(geideaResultData);

                    return geideaResultData;

                }
                catch (Exception ex)
                {
                    return "Failure : " + ex.Message;
                }
            }
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

    }
}
