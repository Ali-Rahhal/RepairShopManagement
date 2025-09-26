using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface IClientRepository : IRepository<Client>
    {
        Task UpdateAsy(Client client);

        Task UpdateRangeAsy(IEnumerable<Client> clients);
    }
}