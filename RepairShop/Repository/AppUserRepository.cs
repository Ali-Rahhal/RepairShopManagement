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
    public class AppUserRepository : Repository<AppUser>, IAppUserRepository
    {
        private readonly AppDbContext _db;

        public AppUserRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsy(AppUser appUser)
        {
            _db.AppUsers.Update(appUser);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<AppUser> appUsers)
        {
            _db.AppUsers.UpdateRange(appUsers);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(AppUser appUser)
        {
            appUser.IsActive = false;
            await UpdateAsy(appUser);
            await Task.CompletedTask;
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<AppUser> appUsers)
        {
            foreach (var item in appUsers)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}