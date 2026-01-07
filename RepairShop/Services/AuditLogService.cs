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
        private readonly ILogger _logger;

        public AuditLogService(IUnitOfWork uow, IHttpContextAccessor hca, ILogger<AuditLogService> logger) {
            _unitOfWork = uow;
            _httpContextAccessor = hca;
            _logger = logger;
        }

        // Method 1: Without old entity
        public async Task AddLogAsy(string action, string entityType, long entityId)
        {
            var description = await GetDescription(entityType, entityId, action);
            await CreateAuditLog(action, entityType, entityId, description);
        }

        // Method 2: With old entity
        public async Task AddLogAsy<T>(string action, string entityType, long entityId, T oldEntity) where T : class
        {
            var description = await GetDescription(entityType, entityId, action, oldEntity);
            await CreateAuditLog(action, entityType, entityId, description);
        }

        // SINGLE GetDescription method that handles both cases
        private async Task CreateAuditLog(string action, string entityType, long entityId, string description)
        {
            // First, we need to check and update the database column length
            // For now, truncate to 450 characters to be safe (leaving room for future expansion)
            var truncatedDescription = description.Length > 450
                ? description.Substring(0, 447) + "..."
                : description;

            var auditLog = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = truncatedDescription,
                UserId = GetCurrentUserId(),
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.AuditLog.AddAsy(auditLog);
            await _unitOfWork.SaveAsy();
        }

        // SINGLE GetDescription method that handles both cases
        private async Task<string> GetDescription(string entityType, long entityId, string action, object oldEntity = null)
        {
            try
            {
                switch (entityType)
                {
                    case SD.Entity_Client:
                        var client = await _unitOfWork.Client.GetAsy(
                            c => c.Id == entityId,
                            includeProperties: "SerialNumbers,Branches,ParentClient");
                        if (client == null) return $"[Client {entityId} - Not Found]";

                        var isBranch = client.ParentClientId.HasValue;
                        var clientType = isBranch ? "Branch" : "Client";
                        var deviceCount = client.SerialNumbers?.Count(sn => sn.IsActive) ?? 0;
                        var branchCount = client.Branches?.Count(b => b.IsActive) ?? 0;
                        var parentInfo = isBranch ? $" of {client.ParentClient?.Name ?? "Unknown"}" : "";

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is Client oldClient)
                        {
                            var changes = new List<string>();

                            if (oldClient.Name != client.Name)
                                changes.Add($"Name changed from '{oldClient.Name}' to '{client.Name}'");

                            if (oldClient.Phone != client.Phone)
                                changes.Add($"Phone changed from '{oldClient.Phone ?? "N/A"}' to '{client.Phone ?? "N/A"}'");

                            if (oldClient.Email != client.Email)
                                changes.Add($"Email changed from '{oldClient.Email ?? "N/A"}' to '{client.Email ?? "N/A"}'");

                            if (oldClient.Address != client.Address)
                                changes.Add($"Address changed");

                            if (oldClient.ParentClientId != client.ParentClientId)
                            {
                                var oldParent = oldClient.ParentClient?.Name ?? "None";
                                var newParent = client.ParentClient?.Name ?? "None";
                                changes.Add($"Parent changed from {oldParent} to {newParent}");
                            }

                            if (oldClient.IsActive != client.IsActive)
                                changes.Add($"Active status changed from {(oldClient.IsActive ? "Yes" : "No")} to {(client.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"{clientType} Updated: '{client.Name}' | {changesText}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"{clientType}{parentInfo} Created: '{client.Name}'",
                            SD.Action_Update => $"{clientType} Updated: '{client.Name}'{parentInfo} | Devices: {deviceCount} | Branches: {branchCount}",
                            SD.Action_Delete => $"{clientType} Deleted: '{client.Name}'{parentInfo} | Had {deviceCount} devices and {branchCount} branches",
                            _ => $"{client.Name} ({clientType})"
                        };

                    case SD.Entity_Branch:
                        // Branch is handled in Client case above
                        return await GetDescription(SD.Entity_Client, entityId, action, oldEntity);

                    case SD.Entity_SerialNumber:
                        var serialNumber = await _unitOfWork.SerialNumber.GetAsy(
                            s => s.Id == entityId,
                            includeProperties: "Model,Client,Client.ParentClient,Warranty,MaintenanceContract");
                        if (serialNumber == null) return $"[Serial Number {entityId} - Not Found]";

                        var clientDisplay = serialNumber.Client.ParentClient != null
                            ? $"{serialNumber.Client.ParentClient.Name} ({serialNumber.Client.Name})"
                            : serialNumber.Client.Name;

                        var coverage = serialNumber.WarrantyId.HasValue ? $"WARRANTY-{serialNumber.WarrantyId:D4}"
                            : serialNumber.MaintenanceContractId.HasValue ? $"CONTRACT-{serialNumber.MaintenanceContractId:D4}"
                            : "None";

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is SerialNumber oldSerialNumber)
                        {
                            var changes = new List<string>();

                            if (oldSerialNumber.Value != serialNumber.Value)
                                changes.Add($"Serial Number changed from '{oldSerialNumber.Value}' to '{serialNumber.Value}'");

                            if (oldSerialNumber.ModelId != serialNumber.ModelId)
                            {
                                var oldModel = oldSerialNumber.Model?.Name ?? "Unknown";
                                var newModel = serialNumber.Model?.Name ?? "Unknown";
                                changes.Add($"Model changed from {oldModel} to {newModel}");
                            }

                            if (oldSerialNumber.ClientId != serialNumber.ClientId)
                            {
                                var oldClientName = oldSerialNumber.Client?.Name ?? "Unknown";
                                var newClientName = serialNumber.Client?.Name ?? "Unknown";
                                changes.Add($"Client changed from {oldClientName} to {newClientName}");
                            }

                            if (oldSerialNumber.WarrantyId != serialNumber.WarrantyId)
                                changes.Add($"Warranty coverage changed");

                            if (oldSerialNumber.MaintenanceContractId != serialNumber.MaintenanceContractId)
                                changes.Add($"Maintenance contract changed");

                            if (oldSerialNumber.IsActive != serialNumber.IsActive)
                                changes.Add($"Active status changed from {(oldSerialNumber.IsActive ? "Yes" : "No")} to {(serialNumber.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"Serial Number Updated: '{serialNumber.Value}' | {changesText}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Serial Number Created: '{serialNumber.Value}' ({serialNumber.Model?.Name ?? "Unknown"}) for {clientDisplay}",
                            SD.Action_Update => $"Serial Number Updated: '{serialNumber.Value}' | Model: {serialNumber.Model?.Name ?? "Unknown"} | Client: {clientDisplay} | Coverage: {coverage}",
                            SD.Action_Delete => $"Serial Number Deleted: '{serialNumber.Value}' | Model: {serialNumber.Model?.Name ?? "Unknown"} | Client: {clientDisplay} | Reason: {serialNumber.InactiveReason ?? "Not specified"}",
                            _ => $"{serialNumber.Value} ({serialNumber.Model?.Name ?? "Unknown"})"
                        };

                    case SD.Entity_Model:
                        var model = await _unitOfWork.Model.GetAsy(
                            m => m.Id == entityId,
                            includeProperties: "SerialNumbers");
                        if (model == null) return $"[Model {entityId} - Not Found]";

                        var activeDevices = model.SerialNumbers?.Count(sn => sn.IsActive) ?? 0;

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is Model oldModelEntity)
                        {
                            var changes = new List<string>();

                            if (oldModelEntity.Name != model.Name)
                                changes.Add($"Name changed from '{oldModelEntity.Name}' to '{model.Name}'");

                            if (oldModelEntity.Category != model.Category)
                                changes.Add($"Category changed from '{oldModelEntity.Category}' to '{model.Category}'");

                            if (oldModelEntity.IsActive != model.IsActive)
                                changes.Add($"Active status changed from {(oldModelEntity.IsActive ? "Yes" : "No")} to {(model.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"Model Updated: '{model.Name}' | {changesText} | Active devices: {activeDevices}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Model Created: '{model.Name}' in category '{model.Category}'",
                            SD.Action_Update => $"Model Updated: '{model.Name}' | Category: {model.Category} | Active devices: {activeDevices}",
                            SD.Action_Delete => $"Model Deleted: '{model.Name}' | Category: {model.Category} | Had {activeDevices} active devices",
                            _ => $"{model.Name} ({model.Category})"
                        };

                    case SD.Entity_Part:
                        var part = await _unitOfWork.Part.GetAsy(p => p.Id == entityId);
                        if (part == null) return $"[Part {entityId} - Not Found]";

                        var priceInfo = part.Price.HasValue ? $"${part.Price:F2}" : "No price";

                        if (action == SD.Action_Update && oldEntity is Part oldPart)
                        {
                            var changes = new List<string>();

                            if (oldPart.Name != part.Name)
                                changes.Add($"Name changed from '{oldPart.Name}' to '{part.Name}'");

                            if (oldPart.Category != part.Category)
                                changes.Add($"Category changed from '{oldPart.Category}' to '{part.Category}'");

                            if (oldPart.Quantity != part.Quantity)
                            {
                                var diff = part.Quantity - oldPart.Quantity;
                                var changeType = diff > 0 ? "increased" : "decreased";
                                changes.Add($"Quantity {changeType} from {oldPart.Quantity} to {part.Quantity} (change of {Math.Abs(diff)})");
                            }

                            if (oldPart.Price != part.Price)
                            {
                                var oldPrice = oldPart.Price.HasValue ? $"${oldPart.Price:F2}" : "No price";
                                var newPrice = part.Price.HasValue ? $"${part.Price:F2}" : "No price";
                                changes.Add($"Price changed from {oldPrice} to {newPrice}");
                            }

                            if (oldPart.IsActive != part.IsActive)
                                changes.Add($"Active status changed from {(oldPart.IsActive ? "Yes" : "No")} to {(part.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"Part Updated: '{part.Name}' | {changesText}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Part Created: '{part.Name}' | Category: {part.Category} | Initial Stock: {part.Quantity} | Price: {priceInfo}",
                            SD.Action_Update => $"Part Updated: '{part.Name}' | Category: {part.Category} | Stock: {part.Quantity} | Price: {priceInfo}",
                            SD.Action_Delete => $"Part Deleted: '{part.Name}' | Category: {part.Category} | Final Stock: {part.Quantity} | Price: {priceInfo}",
                            _ => $"Part: '{part.Name}' | Category: {part.Category}"
                        };

                    case SD.Entity_MaintenanceContract:
                        var mc = await _unitOfWork.MaintenanceContract.GetAsy(
                            m => m.Id == entityId,
                            includeProperties: "Client,Client.ParentClient");
                        if (mc == null) return $"[Maintenance Contract {entityId} - Not Found]";

                        var contractClient = mc.Client.ParentClient != null
                            ? $"{mc.Client.ParentClient.Name} ({mc.Client.Name})"
                            : mc.Client.Name;
                        var duration = (mc.EndDate - mc.StartDate).Days;
                        var status = mc.EndDate < DateTime.Now ? "Expired" : "Active";
                        var remainingDays = Math.Max(0, (mc.EndDate - DateTime.Now).Days);

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is MaintenanceContract oldContract)
                        {
                            var changes = new List<string>();

                            if (oldContract.ClientId != mc.ClientId)
                            {
                                var mcOldClient = oldContract.Client?.Name ?? "Unknown";
                                var newClient = mc.Client?.Name ?? "Unknown";
                                changes.Add($"Client changed from {mcOldClient} to {newClient}");
                            }

                            if (oldContract.StartDate != mc.StartDate)
                                changes.Add($"Start date changed from {oldContract.StartDate:dd-MMM-yyyy} to {mc.StartDate:dd-MMM-yyyy}");

                            if (oldContract.EndDate != mc.EndDate)
                                changes.Add($"End date changed from {oldContract.EndDate:dd-MMM-yyyy} to {mc.EndDate:dd-MMM-yyyy}");

                            if (oldContract.IsActive != mc.IsActive)
                                changes.Add($"Active status changed from {(oldContract.IsActive ? "Yes" : "No")} to {(mc.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"Maintenance Contract Updated: CONTRACT-{mc.Id:D4} | {changesText}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Maintenance Contract Created: CONTRACT-{mc.Id:D4} for {contractClient}",
                            SD.Action_Update => $"Maintenance Contract Updated: CONTRACT-{mc.Id:D4} | Client: {contractClient} | {mc.StartDate:dd-MMM-yyyy} to {mc.EndDate:dd-MMM-yyyy} | Status: {status} ({remainingDays} days left)",
                            SD.Action_Delete => $"Maintenance Contract Deleted: CONTRACT-{mc.Id:D4} | Was for {contractClient} | Period: {mc.StartDate:dd-MMM-yyyy} to {mc.EndDate:dd-MMM-yyyy}",
                            _ => $"CONTRACT-{mc.Id:D4} for {contractClient}"
                        };

                    case SD.Entity_Warranty:
                        var warranty = await _unitOfWork.Warranty.GetAsy(
                            w => w.Id == entityId,
                            includeProperties: "SerialNumbers");
                        if (warranty == null) return $"[Warranty {entityId} - Not Found]";

                        var coveredDevices = warranty.SerialNumbers?.Count(sn => sn.IsActive) ?? 0;
                        var warrantyStatus = warranty.EndDate < DateTime.Now ? "Expired" : "Active";
                        var warrantyRemainingDays = Math.Max(0, (warranty.EndDate - DateTime.Now).Days);

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is Warranty oldWarranty)
                        {
                            var changes = new List<string>();

                            if (oldWarranty.StartDate != warranty.StartDate)
                                changes.Add($"Start date changed from {oldWarranty.StartDate:dd-MMM-yyyy} to {warranty.StartDate:dd-MMM-yyyy}");

                            if (oldWarranty.EndDate != warranty.EndDate)
                                changes.Add($"End date changed from {oldWarranty.EndDate:dd-MMM-yyyy} to {warranty.EndDate:dd-MMM-yyyy}");

                            if (oldWarranty.IsActive != warranty.IsActive)
                                changes.Add($"Active status changed from {(oldWarranty.IsActive ? "Yes" : "No")} to {(warranty.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"Warranty Updated: WARRANTY-{warranty.Id:D4} | {changesText} | Covers {coveredDevices} devices";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Warranty Created: WARRANTY-{warranty.Id:D4}",
                            SD.Action_Update => $"Warranty Updated: WARRANTY-{warranty.Id:D4} | {warranty.StartDate:dd-MMM-yyyy} to {warranty.EndDate:dd-MMM-yyyy} | Status: {warrantyStatus} ({warrantyRemainingDays} days left) | Covers {coveredDevices} devices",
                            SD.Action_Delete => $"Warranty Deleted: WARRANTY-{warranty.Id:D4} | Covered {coveredDevices} devices | Period: {warranty.StartDate:dd-MMM-yyyy} to {warranty.EndDate:dd-MMM-yyyy}",
                            _ => $"WARRANTY-{warranty.Id:D4}"
                        };

                    case SD.Entity_DefectiveUnit:
                        var du = await _unitOfWork.DefectiveUnit.GetAsy(
                            d => d.Id == entityId,
                            includeProperties: "SerialNumber,SerialNumber.Model,SerialNumber.Client,SerialNumber.Client.ParentClient");
                        if (du == null) return $"[Defective Unit {entityId} - Not Found]";

                        var duClient = du.SerialNumber.Client.ParentClient != null
                            ? $"{du.SerialNumber.Client.ParentClient.Name} ({du.SerialNumber.Client.Name})"
                            : du.SerialNumber.Client.Name;
                        var duDescription = du.Description.Length > 100
                            ? du.Description.Substring(0, 100) + "..."
                            : du.Description;

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is DefectiveUnit oldDefectiveUnit)
                        {
                            var changes = new List<string>();

                            if (oldDefectiveUnit.Description != du.Description)
                                changes.Add($"Issue description changed");

                            if (oldDefectiveUnit.HasAccessories != du.HasAccessories)
                                changes.Add($"Accessories status changed from {(oldDefectiveUnit.HasAccessories ? "Yes" : "No")} to {(du.HasAccessories ? "Yes" : "No")}");

                            if (oldDefectiveUnit.Status != du.Status)
                                changes.Add($"Status changed from {oldDefectiveUnit.Status} to {du.Status}");

                            if (oldDefectiveUnit.ResolvedDate != du.ResolvedDate)
                                changes.Add($"Resolution date changed");

                            if (oldDefectiveUnit.IsActive != du.IsActive)
                                changes.Add($"Active status changed from {(oldDefectiveUnit.IsActive ? "Yes" : "No")} to {(du.IsActive ? "Yes" : "No")}");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            return $"Defective Unit Updated: Device '{du.SerialNumber.Value}' | {changesText}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Defective Unit Reported: Device '{du.SerialNumber.Value}' ({du.SerialNumber.Model.Name}) | Issue: {duDescription} | Client: {duClient}",
                            SD.Action_Update => $"Defective Unit Updated: Device '{du.SerialNumber.Value}' | Status: {du.Status} | Issue: {duDescription}",
                            SD.Action_Delete => $"Defective Unit Deleted: Was for device '{du.SerialNumber.Value}' | Issue: {duDescription} | Client: {duClient}",
                            _ => $"Defect for {du.SerialNumber.Value}"
                        };

                    case SD.Entity_TransactionHeader:
                        var tr = await _unitOfWork.TransactionHeader.GetAsy(
                            t => t.Id == entityId,
                            includeProperties: "DefectiveUnit,DefectiveUnit.SerialNumber,DefectiveUnit.SerialNumber.Model,DefectiveUnit.SerialNumber.Client,DefectiveUnit.SerialNumber.Client.ParentClient,User");
                        if (tr == null) return $"[Transaction {entityId} - Not Found]";

                        var trClient = tr.DefectiveUnit.SerialNumber.Client.ParentClient != null
                            ? $"{tr.DefectiveUnit.SerialNumber.Client.ParentClient.Name} ({tr.DefectiveUnit.SerialNumber.Client.Name})"
                            : tr.DefectiveUnit.SerialNumber.Client.Name;
                        var technician = tr.User?.UserName ?? "Unknown";
                        var laborInfo = tr.LaborFees.HasValue ? $" | Labor: ${tr.LaborFees:F2}" : "";
                        var dateInfo = "";

                        // Add relevant date based on status
                        if (tr.Status == SD.Status_Job_Completed || tr.Status == SD.Status_Job_OutOfService)
                            dateInfo = $" | {tr.Status}: {tr.CompletedOrOutOfServiceDate:dd-MMM-yyyy HH:mm}";
                        else if (tr.Status == SD.Status_Job_Delivered)
                            dateInfo = $" | Delivered: {tr.DeliveredDate:dd-MMM-yyyy HH:mm}";
                        else if (tr.Status == SD.Status_Job_InProgress && tr.InProgressDate.HasValue)
                            dateInfo = $" | Started: {tr.InProgressDate:dd-MMM-yyyy HH:mm}";

                        // Handle UPDATE with old entity for change tracking
                        if (action == SD.Action_Update && oldEntity is TransactionHeader oldTransaction)
                        {
                            var changes = new List<string>();

                            if (oldTransaction.Status != tr.Status)
                                changes.Add($"Status changed from {oldTransaction.Status} to {tr.Status}");

                            if (oldTransaction.LaborFees != tr.LaborFees)
                            {
                                var oldFee = oldTransaction.LaborFees.HasValue ? $"${oldTransaction.LaborFees:F2}" : "None";
                                var newFee = tr.LaborFees.HasValue ? $"${tr.LaborFees:F2}" : "None";
                                changes.Add($"Labor Fees changed from {oldFee} to {newFee}");
                            }

                            if (oldTransaction.Comment != tr.Comment)
                                changes.Add($"Comment updated");

                            var changesText = changes.Any()
                                ? string.Join(" | ", changes)
                                : "No changes detected";

                            var deviceInfo = tr.DefectiveUnit?.SerialNumber?.Value ?? "Unknown Device";
                            return $"Transaction Updated: TRAN-{tr.Id:D4} for '{deviceInfo}' | {changesText}";
                        }

                        return action switch
                        {
                            SD.Action_Create => $"Repair Transaction Created: Device '{tr.DefectiveUnit.SerialNumber.Value}' ({tr.DefectiveUnit.SerialNumber.Model.Name}) | Technician: {technician}",
                            SD.Action_Update => $"Repair Transaction Updated: TRAN-{tr.Id:D4} | Device: {tr.DefectiveUnit.SerialNumber.Value} | Status: {tr.Status}{laborInfo}{dateInfo}",
                            SD.Action_Delete => $"Repair Transaction Deleted: TRAN-{tr.Id:D4} | Was for device '{tr.DefectiveUnit.SerialNumber.Value}' | Status was: {tr.Status} | Technician: {technician}",
                            _ => $"TRAN-{tr.Id:D4} for {tr.DefectiveUnit.SerialNumber.Value}"
                        };

                    default:
                        return action switch
                        {
                            SD.Action_Create => $"{entityType} #{entityId} was created",
                            SD.Action_Update => $"{entityType} #{entityId} was updated",
                            SD.Action_Delete => $"{entityType} #{entityId} was deleted",
                            _ => $"{entityType} #{entityId}"
                        };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating description for {entityType} ID {entityId}, Action: {action}");
                return $"[Error retrieving {entityType} #{entityId}]";
            }
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? null;
        }
    }
}
