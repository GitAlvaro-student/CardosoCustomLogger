using PaymentAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PaymentAPI.Services
{
    public class PaymentService : IPaymentService
    {
        private static List<Payment> _pagamentos = new List<Payment>();
        private static int _proximoId = 1;

        static PaymentService()
        {
            // Dados iniciais para teste
            _pagamentos.Add(new Payment
            {
                Id = _proximoId++,
                Descricao = "Compra Online - Loja A",
                Valor = 150.50m,
                DataPagamento = DateTime.Now.AddDays(-5),
                Status = "Pago",
                MetodoPagamento = "Cartão",
                NumeroCartao = "**** **** **** 1234",
                NomeTitular = "João Silva"
            });

            _pagamentos.Add(new Payment
            {
                Id = _proximoId++,
                Descricao = "Assinatura Mensal",
                Valor = 29.90m,
                DataPagamento = DateTime.Now.AddDays(-2),
                Status = "Pendente",
                MetodoPagamento = "PIX",
                NumeroCartao = null,
                NomeTitular = "Maria Santos"
            });
        }

        public List<Payment> ObterTodos()
        {
            return _pagamentos.OrderByDescending(p => p.DataPagamento).ToList();
        }

        public Payment ObterPorId(int id)
        {
            return _pagamentos.FirstOrDefault(p => p.Id == id);
        }

        public Payment Criar(PaymentRequest request)
        {
            var pagamento = new Payment
            {
                Id = _proximoId++,
                Descricao = request.Descricao,
                Valor = request.Valor,
                DataPagamento = DateTime.Now,
                Status = "Pendente",
                MetodoPagamento = request.MetodoPagamento,
                NumeroCartao = MascararNumeroCartao(request.NumeroCartao),
                NomeTitular = request.NomeTitular
            };

            _pagamentos.Add(pagamento);
            return pagamento;
        }

        public Payment Atualizar(int id, PaymentRequest request)
        {
            var pagamento = ObterPorId(id);
            if (pagamento == null) return null;

            if (pagamento.Status != "Pendente")
            {
                throw new InvalidOperationException("Só é possível atualizar pagamentos pendentes");
            }

            pagamento.Descricao = request.Descricao;
            pagamento.Valor = request.Valor;
            pagamento.MetodoPagamento = request.MetodoPagamento;
            pagamento.NumeroCartao = MascararNumeroCartao(request.NumeroCartao);
            pagamento.NomeTitular = request.NomeTitular;

            return pagamento;
        }

        public bool Excluir(int id)
        {
            var pagamento = ObterPorId(id);
            if (pagamento == null) return false;

            if (pagamento.Status == "Pago")
            {
                throw new InvalidOperationException("Não é possível excluir um pagamento já processado");
            }

            return _pagamentos.Remove(pagamento);
        }

        public Payment ProcessarPagamento(int id)
        {
            var pagamento = ObterPorId(id);
            if (pagamento == null) return null;

            if (pagamento.Status != "Pendente")
            {
                throw new InvalidOperationException("Só é possível processar pagamentos pendentes");
            }

            pagamento.Status = "Pago";
            pagamento.DataPagamento = DateTime.Now;

            return pagamento;
        }

        public Payment CancelarPagamento(int id)
        {
            var pagamento = ObterPorId(id);
            if (pagamento == null) return null;

            pagamento.Status = "Cancelado";

            return pagamento;
        }

        private string MascararNumeroCartao(string numeroCartao)
        {
            if (string.IsNullOrEmpty(numeroCartao) || numeroCartao.Length < 4)
                return numeroCartao;

            return $"**** **** **** {numeroCartao.Substring(numeroCartao.Length - 4)}";
        }
    }
}