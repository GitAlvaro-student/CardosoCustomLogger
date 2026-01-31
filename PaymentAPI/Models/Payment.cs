using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentAPI.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Status { get; set; } // "Pendente", "Pago", "Cancelado"
        public string MetodoPagamento { get; set; } // "Cartão", "Boleto", "PIX"
        public string NumeroCartao { get; set; }
        public string NomeTitular { get; set; }
    }
}