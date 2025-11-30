using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface ISerialNumberRepository : IRepository<SerialNumber>
    {
        Task AddAsy(SerialNumber serialNumber);

        Task AddRangeAsy(IEnumerable<SerialNumber> serialNumbers);

        Task UpdateAsy(SerialNumber serialNumber);

        Task UpdateRangeAsy(IEnumerable<SerialNumber> serialNumbers);
    }
}