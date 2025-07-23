using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekPay
{
    public class RequestModel
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
}
