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
    public class ModelRepository : Repository<Model>, IModelRepository
    {
        private readonly AppDbContext _db;

        public ModelRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsy(Model model)
        {
            _db.Models.Update(model);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<Model> models)
        {
            _db.Models.UpdateRange(models);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(Model model)
        {
            model.IsActive = false;
            await UpdateAsy(model);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<Model> models)
        {
            foreach (var item in models)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}