using RepairShop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository.IRepository
{
    public interface IDefectiveUnitRepository : IRepository<DefectiveUnit>
    {
        Task AddAsy(DefectiveUnit defectiveUnit);

        Task AddRangeAsy(IEnumerable<DefectiveUnit> defectiveUnits);

        Task UpdateAsy(DefectiveUnit defectiveUnit);

        Task UpdateRangeAsy(IEnumerable<DefectiveUnit> defectiveUnits);
    }
}