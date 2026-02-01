using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentAPI.Models
{
    public class PaymentRequest
    {
        public string Descricao { get; set; }
        public decimal Valor { get; set; }
        public string MetodoPagamento { get; set; }
        public string NumeroCartao { get; set; }
        public string NomeTitular { get; set; }
    }
}