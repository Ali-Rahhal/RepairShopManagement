using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface IWarrantyRepository : IRepository<Warranty>
    {
        Task AddAsy(Warranty warranty);

        Task AddRangeAsy(IEnumerable<Warranty> warranties);

        Task UpdateAsy(Warranty warranty);

        Task UpdateRangeAsy(IEnumerable<Warranty> warranties);
    }
}