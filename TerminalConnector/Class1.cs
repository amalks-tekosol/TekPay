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

namespace TerminalConnector
{
	public class MyDataModel
	{
		public int transaction_id { get; set; }
		public string transaction_code { get; set; }
		public string action_name { get; set; }
		public DateTime? transaction_date { get; set; }
		public float transaction_amount { get; set; }
		public string? payment_method { get; set; }
		public string? payment_button { get; set; }
		public string? location_code { get; set; }
        public string? currency_code { get; set; }
        public string? transaction_source { get; set; }
		public string? authCode { get; set; }
        public string? param_1 { get; set; }
        public string? param_2 { get; set; }
        public string? param_3 { get; set; }
        public string? param_4 { get; set; }
        public string? param_5 { get; set; }
        public string? reference_code { get; set; }

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
		public EFTData eFT {  get; set; }
	}

	//public class ResponseDetails
	//{
 //       public string CommandType { get; set; }
 //       public string TransactionDate { get; set; }
 //       public string TransactionTime { get; set; }
 //       public string Amount { get; set; }
 //       public string Currency { get; set; }
 //       public string CardSchemeName { get; set; }
 //       public string MaskCardNumber { get; set; }
 //       public string AuthCode { get; set; }
 //       public string TID { get; set; }
 //   }

	public class FileProcessor
	{
		

		public async Task<string> ProcessFileAsync(string jsonData)
		{
			Log("started 1.1");
			ResponseData responseData = new ResponseData();
			try
			{
				EftInterface _eft;
				StringBuilder _sb;
				MyDataModel data = JsonConvert.DeserializeObject<MyDataModel>(jsonData);

				string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");

				var lines = File.ReadAllLines(configFilePath);
				var config = lines.Select(line => line.Split('='))
								  .ToDictionary(parts => parts[0], parts => parts[1]);

				string currentDirectory = config["FolderPath"];

				//string folderPath = Path.Combine(currentDirectory, "Terminal Connector");

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
				#region
				// Remove outer curly braces and split by closing and opening braces
				//var entries = fileContent.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				//                        .Select(entry => entry.Trim())
				//                        .ToArray();

				//string textContent = null;

				//// Loop through each entry to find the name and associated link
				//foreach (var entry in entries)
				//{
				//    // Split each entry by hyphen to separate name and link
				//    var content = entry.Split(new[] { '-' }, 2);
				//    if (content.Length == 2)
				//    {
				//        string name = content[0].Trim();
				//        string link = content[1].Trim();

				//        // Check if the name matches the desired name
				//        if (name.Equals(data.payment_processor, StringComparison.OrdinalIgnoreCase))
				//        {
				//            textContent = link;
				//            break; // Exit loop once the match is found
				//        }
				//    }
				//}

				//if (string.IsNullOrEmpty(textContent))
				//{
				//    throw new InvalidOperationException($"No matching entry found for payment mode '{data.payment_mode}'.");
				//}

				//// Split the textContent to get DLL path and method name
				//string[] parts = textContent.Split('|');
				//if (parts.Length != 2)
				//{
				//    throw new ArgumentException("The file content format is incorrect. Expected format: 'dllPath|Namespace.ClassName.MethodName'." + textContent);
				//}

				//string dllPath = parts[0];
				//string methodFullName = parts[1];

				//if (!File.Exists(dllPath))
				//{
				//    throw new FileNotFoundException("DLL not found at specified path.", dllPath);
				//}
				#endregion

				var items = JsonConvert.DeserializeObject<List<Item>>(fileContent);

				var itemDictionary = items.ToDictionary(item => item.name, item => item.path);
				var textContent = "";

				Log("started : " + config["PaymentIntegrator"]);

				if (itemDictionary.TryGetValue(config["PaymentIntegrator"], out string path))
				{
					textContent = path;
				}
				else
				{
					string msg = $"The transaction using '{config["PaymentIntegrator"]}' cannot be completed.";
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

				#region
				//if (data.payment_processor == "Mashreq")
				//{
				//    string exePath = dllPath; // Path to your .exe file

				//    // Load the .exe as an assembly
				//    Assembly assembly = Assembly.LoadFrom(exePath);

				//    // Discover the type that contains the method (assuming it's public and accessible)
				//    Type[] types = assembly.GetTypes();

				//    foreach (Type type in types)
				//    {
				//        Log($"Type: {type.FullName}");
				//    }
				//    Type targetType = Array.Find(types, t => t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null);


				//    if (targetType == null)
				//    {
				//        throw new Exception("Target type not found.");
				//    }

				//    // Create an instance of the type
				//    object instance = Activator.CreateInstance(targetType);

				//    if (instance == null)
				//    {
				//        throw new Exception("Failed to create an instance of the type.");
				//    }


				//    MashreqData mashreqData = new MashreqData();
				//    mashreqData.transactionAmount = Convert.ToString((int)(data.transaction_amount) * 100);
				//    mashreqData.mrefvalue = data.mrefvalue;


				//    // Find the method info for "Purchase"
				//    MethodInfo method = targetType.GetMethod(methodName);

				//    if (method == null)
				//    {
				//        throw new Exception($"Method '{methodName}' not found.");
				//    }

				//    // Prepare any parameters needed for the method
				//    object[] methodArgs = new object[] { mashreqData.transactionAmount, mashreqData.mrefvalue };

				//    // Invoke the method
				//    var result = await (Task<string>)method.Invoke(instance, methodArgs);

				//    return result;
				//}
				#endregion
				if (config["PaymentIntegrator"] == "Mashreq")
				{
					Log("Mashreq Started");
					var result = "";

					#region mashreq
					try
					{
						string InterMediateXml = @"C:\Work\Terminal Connector V1\Terminal Connector\Mashreq\InterMediateXML.xml";
						_eft = EftInterface.CreateEftInterface("COM" + "4", InterMediateXml);
						_sb = new StringBuilder();
						Log("1");

						_eft.OnUSBDisconnection += EftInterface_OnConnectivity;
						_eft.OnUSBConnection += EftInterface_OnConnectivity;


						//_eft = EftInterface.CreateEftInterface("COM" + InterfaceConfig.GetKey("COMPORT"), InterfaceConfig.GetKey("InterMediateXml"));
						//--  _eft.OnUSBDisconnection += EftInterface_OnConnectivity;
						//SetTextValue(txtRXTXMessage, string.Format("{0}: {1}", DateTime.Now.ToString("dd-MMyyyy HH:mm:ss:fff"), sender));
						//--  _eft.OnUSBConnection += EftInterface_OnConnectivity;
						//setting timeout for the response message
						//-- _eft.ControllerWait = int.Parse("9500");
						//setting a timeout for the transaction
						//-- _eft.TransactionTimeout = int.Parse("180000");
						//-- _eft.EnableLog = InterfaceConfig.GetKey("EnableLog");


						//_eft.OnTrace += _eft.GetTerminalInfo();




						var res = _eft.GetTerminalInfo();
						//result = Convert.ToString(_eft.GetTerminalInfo());
						Log(Convert.ToString(result));
					}
					catch (TargetInvocationException ex)
					{
						Log(ex.InnerException?.Message);
						Log(ex.InnerException?.StackTrace);
					}
					#endregion
					#region
					//Assembly assembly = Assembly.LoadFrom(@"C:\Amal\Projects\DLL\sgEftInterface.dll");


					//Type[] types = assembly.GetTypes();


					//foreach (Type type in types)
					//{
					//    Log($"Type: {type.FullName}");
					//}


					//// Find the type that contains the method
					//Type targetType = null;
					//foreach (Type type in assembly.GetTypes())
					//{
					//    Log(Convert.ToString(type));
					//    // Check if the type contains the method
					//    MethodInfo methodninfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
					//    Log(Convert.ToString(methodninfo));
					//    if (methodninfo != null)
					//    {
					//        targetType = type;
					//        Log($"Found target type: {targetType.FullName}");
					//        break;
					//    }
					//}

					//if (targetType == null)
					//{
					//    Log("Type containing the method not found." + "|" + targetType + "|" + methodName);
					//}

					//// Create an instance of the found type
					//object instance = Activator.CreateInstance(targetType);

					//// Get the method info
					//MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
					//if (method == null)
					//{
					//    Log("Method not found.");
					//}

					//// Invoke the method
					//var result = await (Task<string>)method.Invoke(instance, null);

					////var result = await (Task<string>)method.Invoke(instance, methodArgs);
					#endregion
					return Convert.ToString(result);
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

					if (config["PaymentIntegrator"] == "Mashreq")
					{
						MashreqData mashreqData = new MashreqData();
						mashreqData.transactionAmount = Convert.ToString((int)(data.transaction_amount) * 100);
						mashreqData.mrefvalue = data.transaction_code;



						// Prepare any parameters needed for the method
						object[] methodArgs = new object[] { mashreqData.transactionAmount, mashreqData.mrefvalue };

						// Invoke the method
						var result = await (Task<string>)method.Invoke(instance, methodArgs);

						XmlSerializer serializer = new XmlSerializer(typeof(EFTData));

						EFTData eftData = new EFTData();
						using (StringReader reader = new StringReader(result))
						{
							eftData = (EFTData)serializer.Deserialize(reader);

							// Now you can access the EFTData object for further processing.
						}

						ResponseData response = new ResponseData
						{
							StatusCode = eftData.ErrorCode == "E000" ? 200 : 500,
							ResponseMessage = eftData.ResponseCode,
							CommandType = "PURCHASE",
							TransactionDate = Convert.ToString(DateTime.ParseExact(eftData.TransactionDate, "MMddyy", null) +" "+ DateTime.ParseExact(eftData.TransactionTime, "hhmmss", null)),
							Amount = eftData.Amount.ToString(),
							Currency = eftData.Currency,
							CardSchemeName = eftData.CardSchemeName,
							MaskCardNumber = eftData.MaskCardNumber,
							AuthCode = eftData.AuthCode,
							TID = eftData.TID,
							SID = null,
							eFT = 
								new EFTData
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
						};

						return result;
					}
					else
					{

						object[] methodArgs = new object[] { data.transaction_id, data.transaction_code, data.action_name, data.transaction_date, data.transaction_amount, data.payment_method, data.payment_button, data.currency_code, data.location_code, data.transaction_source, data.reference_code, data.authCode };

						var result = await (Task<string>)method.Invoke(instance, methodArgs);

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
						string transactionDateTime = $"{parsedDate}"+ " " + $"{parsedTime}";

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

				return "";
			}
			catch (Exception ex)
			{
				Log(ex.Message);
				responseData.ResponseMessage = ex.Message;
				responseData.StatusCode = 500;
				var resultData = System.Text.Json.JsonSerializer.Serialize(responseData);

				return resultData;
			}



		}

		private void EftInterface_OnConnectivity(object sender, EventArgs e)
		{
			//Message.Show(sender);
		}

		private void Log(string message)
		{
			try
			{
				var path = @"C:\Work\Terminal Connector V1\middleware_logs.txt";
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
	}
}
