using RepairShop.Data;
using RepairShop.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepairShop.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;
        public ITransactionHeaderRepository TransactionHeader { get; private set; }
        public ITransactionBodyRepository TransactionBody { get; private set; }
        public IClientRepository Client { get; private set; }
        public IAppUserRepository AppUser { get; private set; }
        public IPartRepository Part { get; private set; }


        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            TransactionHeader = new TransactionHeaderRepository(_db);
            TransactionBody = new TransactionBodyRepository(_db);
            Client = new ClientRepository(_db);
            AppUser = new AppUserRepository(_db);
            Part = new PartRepository(_db);
        }

        public async Task SaveAsy()
        {
            await _db.SaveChangesAsync();
        }
    }
}