using Microsoft.AspNetCore.Identity;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;
using System.Drawing.Text;
using System.Security.Claims;

namespace RepairShop.Services
{
    public class AuditLogService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(IUnitOfWork uow, IHttpContextAccessor hca) {
            _unitOfWork = uow;
            _httpContextAccessor = hca;
        }

        private AuditLog? auditLogForInsert = null;

        public async Task AddLogAsy(string action, string entityType, long entityId)
        {
            auditLogForInsert = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = await GetDescription(entityType, entityId),
                UserId = GetCurrentUserId(),
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            await _unitOfWork.AuditLog.AddAsy(auditLogForInsert);
            await _unitOfWork.SaveAsy();
            await Task.CompletedTask;
        }

        private async Task<string> GetDescription(string entityType, long entityId)
        {
            switch (entityType)
            {
                case SD.Entity_Branch:
                    var branch = await _unitOfWork.Client.GetAsy(c => c.Id == entityId, includeProperties: "ParentClient");
                    return $"{branch.ParentClient?.Name} - {branch.Name}";
                case SD.Entity_Client:
                    var client = await _unitOfWork.Client.GetAsy(c => c.Id == entityId);
                    return client.Name;
                case SD.Entity_SerialNumber:
                    var serialNumber = await _unitOfWork.SerialNumber.GetAsy(s => s.Id == entityId);
                    return serialNumber.Value;
                case SD.Entity_Model:
                    var model = await _unitOfWork.Model.GetAsy(m => m.Id == entityId);
                    return model.Name;
                case SD.Entity_Part:
                    var part = await _unitOfWork.Part.GetAsy(p => p.Id == entityId);
                    return part.Name;
                case SD.Entity_MaintenanceContract:
                    var mc = await _unitOfWork.MaintenanceContract.GetAsy(m => m.Id == entityId);
                    return $"CONTRACT-{mc.Id:D4}";
                case SD.Entity_Warranty:
                    var warranty = await _unitOfWork.Warranty.GetAsy(w => w.Id == entityId);
                    return warranty.Code ?? $"WARRANTY-{warranty.Id:D4}";
                case SD.Entity_DefectiveUnit:
                    var du = await _unitOfWork.DefectiveUnit.GetAsy(d => d.Id == entityId, includeProperties:"SerialNumber");
                    return $"DU for SN:{du.SerialNumber.Value}";
                case SD.Entity_TransactionHeader:
                    var tr = await _unitOfWork.TransactionHeader.GetAsy(t => t.Id == entityId, includeProperties:"DefectiveUnit,DefectiveUnit.SerialNumber");
                    return $"TRAN for SN:{tr.DefectiveUnit.SerialNumber.Value}";
                default:
                    break;
            }
            return string.Empty;
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null;
        }

    }
}
