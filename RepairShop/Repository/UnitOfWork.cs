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
        public IModelRepository Model { get; private set; }
        public ISerialNumberRepository SerialNumber { get; private set; }
        public IWarrantyRepository Warranty { get; private set; }
        public IMaintenanceContractRepository MaintenanceContract { get; private set; }
        public IDefectiveUnitRepository DefectiveUnit { get; private set; }
        public IPreventiveMaintenanceRecordRepository PreventiveMaintenanceRecord { get; private set; }
        public IAuditLogRepository AuditLog { get; private set; }



        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            TransactionHeader = new TransactionHeaderRepository(_db);
            TransactionBody = new TransactionBodyRepository(_db);
            Client = new ClientRepository(_db);
            AppUser = new AppUserRepository(_db);
            Part = new PartRepository(_db);
            Model = new ModelRepository(_db);
            SerialNumber = new SerialNumberRepository(_db);
            Warranty = new WarrantyRepository(_db);
            MaintenanceContract = new MaintenanceContractRepository(_db);
            DefectiveUnit = new DefectiveUnitRepository(_db);
            PreventiveMaintenanceRecord = new PreventiveMaintenanceRecordRepository(_db);
            AuditLog = new AuditLogRepository(_db);
        }

        public async Task SaveAsy()
        {
            await _db.SaveChangesAsync();
        }
    }
}