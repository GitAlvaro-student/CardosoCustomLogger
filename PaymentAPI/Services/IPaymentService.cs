using PaymentAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentAPI.Services
{
    public interface IPaymentService
    {
        List<Payment> ObterTodos();
        Payment ObterPorId(int id);
        Payment Criar(PaymentRequest request);
        Payment Atualizar(int id, PaymentRequest request);
        bool Excluir(int id);
        Payment ProcessarPagamento(int id);
        Payment CancelarPagamento(int id);
    }
}
