using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepairShop.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ITransactionHeaderRepository TransactionHeader { get; }
        ITransactionBodyRepository TransactionBody { get; }
        IClientRepository Client { get; }
        IAppUserRepository AppUser { get; }
        IPartRepository Part { get; }
        IModelRepository Model { get; }
        ISerialNumberRepository SerialNumber { get; }
        IWarrantyRepository Warranty { get; }
        IMaintenanceContractRepository MaintenanceContract { get; }
        IDefectiveUnitRepository DefectiveUnit { get; }
        


        Task SaveAsy();
    }
}