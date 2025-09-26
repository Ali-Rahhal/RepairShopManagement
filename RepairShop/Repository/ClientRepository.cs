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
    public class ClientRepository : Repository<Client>, IClientRepository
    {
        private readonly AppDbContext _db;

        public ClientRepository(AppDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task UpdateAsy(Client client)
        {
            _db.Clients.Update(client);
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsy(IEnumerable<Client> clients)
        {
            _db.Clients.UpdateRange(clients);
            await Task.CompletedTask;
        }

        // Soft delete implementation
        public override async Task RemoveAsy(Client client)
        {
            client.IsActive = false;
            await UpdateAsy(client);
            await Task.CompletedTask;   
        }

        // Soft delete implementation for multiple entities
        public override async Task RemoveRangeAsy(IEnumerable<Client> clients)
        {
            foreach (var item in clients)
            {
                item.IsActive = false;
                await UpdateAsy(item);
                await Task.CompletedTask;
            }
        }
    }
}