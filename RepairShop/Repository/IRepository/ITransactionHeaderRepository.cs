using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface ITransactionHeaderRepository : IRepository<TransactionHeader>
    {
        Task UpdateAsy(TransactionHeader transactionHeader);

        Task UpdateRangeAsy(IEnumerable<TransactionHeader> transactionHeaders);
    }
}