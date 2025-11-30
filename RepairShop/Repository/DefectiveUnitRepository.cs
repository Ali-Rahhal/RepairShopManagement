using RepairShop.Data;
using RepairShop.Models;
using RepairShop.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace RepairShop.Repository
{
    public class DefectiveUnitRepository : Repository<DefectiveUnit>, IDefectiveUnitRepository
    {
        private readonly AppDbContext _db;

        public DefectiveUnitRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task AddAsy(DefectiveUnit defectiveUnit)
        {
            await _db.DefectiveUnits.AddAsync(defectiveUnit);

            await _db.SaveChangesAsync();

            defectiveUnit.Code = defectiveUnit.Id.ToString();

            await UpdateAsy(defectiveUnit);

            await Task.CompletedTask;
        }

        public async Task AddRangeAsy(IEnumerable<DefectiveUnit> defectiveUnits)
        {
            await _db.DefectiveUnits.AddRangeAsync(defectiveUnits);
            await _db.SaveChangesAsync();
            foreach (var defectiveUnit in defectiveUnits)
            {
                defectiveUnit.Code = defectiveUnit.Id.ToString();
            }

            await UpdateRangeAsy(defectiveUnits);

            await Task.CompletedTask;
        }

        public async Task UpdateAsy(DefectiveUnit defectiveUnit)
        {
            _db.DefectiveUnits.Update(defectiveUnit);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<DefectiveUnit> defectiveUnits)
        {
            _db.DefectiveUnits.UpdateRange(defectiveUnits);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(DefectiveUnit defectiveUnit)
        {
            defectiveUnit.IsActive = false;
            await UpdateAsy(defectiveUnit);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<DefectiveUnit> defectiveUnits)
        {
            foreach (var item in defectiveUnits)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}