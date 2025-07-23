using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    internal class GeideaResponseData
    {
        private static readonly Dictionary<string, string> responseCodes = new Dictionary<string, string>
        {
            { "000", "Approved / Accepted" },
            { "001", "Approved" },
            { "003", "Approved" },
            { "007", "Approved" },
            { "087", "Approved" },
            { "089", "Approved" },
            { "100", "Declined" },
            { "101", "Expired Card, Contact your bank" },
            { "102", "Declined" },
            { "103", "Declined" },
            { "104", "Restricted card, transaction not allowed" },
            { "105", "Declined" },
            { "106", "PIN Tries Exceeded" },
            { "107", "Declined" },
            { "108", "Declined" },
            { "109", "Declined" },
            { "110", "Declined" },
            { "111", "Declined, Contact your bank" },
            { "112", "Declined" },
            { "114", "Declined" },
            { "115", "Transaction not allowed" },
            { "116", "DECLINED" },
            { "117", "INVALID PIN" },
            { "118", "Declined, Contact your bank" },
            { "119", "Transaction not permitted to cardholder" },
            { "120", "Declined" },
            { "121", "Exceeds withdrawal amount limit" },
            { "122", "Declined" },
            { "123", "Declined, limits exceeded" },
            { "125", "Declined, Contact your bank" },
            { "126", "Declined" },
            { "127", "Wrong PIN" },
            { "128", "Declined" },
            { "129", "Declined, Contact your bank" },
            { "182", "Declined" },
            { "183", "Declined" },
            { "184", "Declined" },
            { "185", "Declined" },
            { "188", "Declined" },
            { "190", "Declined" },
            { "195", "" },
            { "196", "" },
            { "200", "Declined" },
            { "201", "Expired Card, Contact your bank" },
            { "202", "Declined" },
            { "203", "Declined" },
            { "204", "Transaction not allowed" },
            { "205", "Declined" },
            { "206", "PIN Tries Exceeded" },
            { "207", "Declined" },
            { "208", "Declined, Contact your bank" },
            { "209", "Declined, Contact your bank" },
            { "210", "Declined, Contact your bank" },
            { "400", "Accepted" },
            { "480", "Unsuccessful" },
            { "481", "Unsuccessful" },
            { "500", "RECONCILIATION COMPLETED" },
            { "501", "RECONCILIATION UNSUCCESSFUL" },
            { "800", "Accepted" },
            { "888", "Error" },
            { "9xx", "System Error" }
        };


        public string GetErrorDescription(string responseCode)
        {
            return responseCodes.TryGetValue(responseCode, out string description) ? description : "Unknown Error Code";
        }
    }
}
