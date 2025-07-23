using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    public class LogClass
    {
        public int StatusCode { get; set; }
        public string ResponseMessage { get; set; }
        public string CommandType { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string AuthCode { get; set; }
        public string TID { get; set; }
        public string SID { get; set; }
        public Object resultData { get; set; }

    }
}
