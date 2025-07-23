using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    public class Response
    {
        public string TrxDate { get; set; }
        public string TrxTime { get; set; }
        public string RefrenceNumber { get; set; }
        public string OrderId { get; set; }
        public string isMultiMerchantEnabled { get; set; }
        public string BatchNo { get; set; }
        public string ActionCode { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseDesc { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorDescription { get; set; }
        public string APPVersion { get; set; }
        public string EDCTrxNo { get; set; }
        public string TrxrequiredField { get; set; }
        public string MID { get; set; }
        public string TID { get; set; }
        public string MerchantName { get; set; }
        public string MerchantAddress1 { get; set; }
        public string MerchantAddress2 { get; set; }
        public string MerchantAddress3 { get; set; }
        public string CardName { get; set; }
        public string MaskedCardNo { get; set; }
        public string CardExpiryDate { get; set; }
        public string CardHolderName { get; set; }
        public string CardHolderVerf { get; set; }
        public string TrxAmount { get; set; }
        public string Currency { get; set; }
        public string LocalCurrencyDecimal { get; set; }
        public string TrxStatus { get; set; }
        public string AuthCode { get; set; }
        public string EntryMode { get; set; }
        public string IsReversed { get; set; }
        public string IsEPP { get; set; }
        public string OSVersion { get; set; }
        public string EMVVersion { get; set; }
        public string APPLabel { get; set; }
        public string RRN { get; set; }
        public string EMVAID { get; set; }
        public string EMVTVR { get; set; }
        public string EMVTSI { get; set; }
        public string EMVAC { get; set; }
        public string ACInfo { get; set; }
    }

    public class EazyPayResponse
    {
        public Response Response { get; set; }
    }
}
