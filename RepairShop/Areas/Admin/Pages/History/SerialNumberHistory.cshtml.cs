using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RepairShop.Models;
using RepairShop.Models.Helpers;
using RepairShop.Repository.IRepository;

namespace RepairShop.Areas.Admin.Pages.History
{
    [Authorize(Roles = SD.Role_Admin)]
    public class SerialNumberHistoryModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public SerialNumberHistoryModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public SerialNumberHistoryVM? SerialNumberHistory { get; set; }
        public string SearchTerm { get; set; }

        public async Task<IActionResult> OnGet(string? searchTerm = null)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                SearchTerm = searchTerm.Trim();
                await LoadSerialNumberHistory(SearchTerm);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                TempData["error"] = "Please enter a serial number to search";
                return Page();
            }

            SearchTerm = searchTerm.Trim();
            await LoadSerialNumberHistory(SearchTerm);

            return Page();
        }

        public async Task<JsonResult> OnGetSearchSerialNumbers(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return new JsonResult(new { results = new List<object>() });
            }

            var serialNumbers = (await _unitOfWork.SerialNumber.GetAllAsy(
                sn => sn.Value.ToLower().Contains(term.ToLower()) && sn.IsActive == true,
                includeProperties: "Model,Client"
            )).Take(10);

            var results = serialNumbers.Select(sn => new
            {
                id = sn.Value,
                text = $"{sn.Value} - {sn.Model.Name} ({sn.Client.Name})"
            }).ToList();

            return new JsonResult(new { results });
        }

        private async Task LoadSerialNumberHistory(string searchTerm)
        {
            // Find the serial number
            var serialNumber = await _unitOfWork.SerialNumber.GetAsy(
                sn => sn.Value.ToLower().Equals(searchTerm.ToLower()) && sn.IsActive == true,
                includeProperties: "Model,Client,Warranty,MaintenanceContract"
            );

            if (serialNumber == null)
            {
                TempData["error"] = $"Serial number '{searchTerm}' not found";
                SerialNumberHistory = null;
                return;
            }

            // Get all defective units for this serial number
            var defectiveUnits = (await _unitOfWork.DefectiveUnit.GetAllAsy(
                du => du.SerialNumberId == serialNumber.Id && du.IsActive == true,
                includeProperties: "Warranty,MaintenanceContract"
            )).ToList();

            // Get all transaction headers for these defective units
            var transactionHeaders = new List<TransactionHeader>();
            var transactionBodies = new List<TransactionBody>();

            foreach (var du in defectiveUnits)
            {
                var transactions = await _unitOfWork.TransactionHeader.GetAllAsy(
                    th => th.DefectiveUnitId == du.Id && th.IsActive == true,
                    includeProperties: "User,Client,BrokenParts"
                );

                foreach (var transaction in transactions)
                {
                    transactionHeaders.Add(transaction);

                    // Get all transaction bodies for this header
                    var bodies = await _unitOfWork.TransactionBody.GetAllAsy(
                        tb => tb.TransactionHeaderId == transaction.Id && tb.IsActive == true,
                        includeProperties: "Part"
                    );
                    transactionBodies.AddRange(bodies);
                }
            }

            // Create timeline events
            var timelineEvents = new List<TimelineEvent>();

            // Add serial number received event
            timelineEvents.Add(new TimelineEvent
            {
                EventType = "Serial Number Received",
                Date = serialNumber.ReceivedDate,
                Description = $"Serial number {serialNumber.Value} was received",
                Details = $"Model: {serialNumber.Model.Name}, Client: {serialNumber.Client.Name}",
                RelatedId = serialNumber.Id,
                EventTypeColor = "success"
            });

            // Add defective unit events
            foreach (var du in defectiveUnits)
            {
                timelineEvents.Add(new TimelineEvent
                {
                    EventType = "Defective Unit Reported",
                    Date = du.ReportedDate,
                    Description = "Defective unit reported",
                    Details = $"Issue: {du.Description}",
                    Status = SD.Status_DU_Reported,
                    RelatedId = du.Id,
                    EventTypeColor = "danger"
                });

                // Add resolved date if applicable
                if (du.ResolvedDate.HasValue)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Defective Unit Resolved",
                        Date = du.ResolvedDate.Value,
                        Description = "Defective unit resolved",
                        Details = $"Issue was resolved",
                        Status = "Resolved",
                        RelatedId = du.Id,
                        EventTypeColor = "success"
                    });
                }
            }

            // Add transaction header events
            foreach (var transaction in transactionHeaders)
            {
                timelineEvents.Add(new TimelineEvent
                {
                    EventType = "Repair Job Created",
                    Date = transaction.CreatedDate,
                    Description = "Repair job created",
                    Details = $"Job Status: New",
                    Status = SD.Status_Job_New,
                    RelatedId = transaction.Id,
                    EventTypeColor = "secondary"
                });

                if (transaction.InProgressDate.HasValue)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Repair In Progress",
                        Date = transaction.InProgressDate.Value,
                        Description = "Repair work started",
                        Details = $"Job moved to in progress",
                        Status = SD.Status_Job_InProgress,
                        RelatedId = transaction.Id,
                        EventTypeColor = "warning"
                    });
                }

                if (transaction.CompletedOrOutOfServiceDate.HasValue)
                {
                    var eventType = transaction.Status == SD.Status_Job_Completed ? "Repair Completed" : "Marked Out of Service";
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = eventType,
                        Date = transaction.CompletedOrOutOfServiceDate.Value,
                        Description = eventType,
                        Details = $"Job finalized with status: {transaction.Status}",
                        Status = transaction.Status,
                        RelatedId = transaction.Id,
                        EventTypeColor = transaction.Status == SD.Status_Job_Completed ? "success" : "dark"
                    });
                }
            }

            // Add transaction body (parts) events
            foreach (var body in transactionBodies)
            {
                // Part added event - show initial status
                timelineEvents.Add(new TimelineEvent
                {
                    EventType = $"Part Added to {(body.PartId == null ? "Repair" : "Replace")}",
                    Date = body.CreatedDate,
                    Description = $"Part identified for {(body.PartId == null ? "repair" : "replacement")}",
                    Details = $"Part: {body.BrokenPartName} - Initial Status: Needs {(body.PartId == null ? "Repair" : "Replacement")}",
                    Status = body.Status,
                    RelatedId = body.Id,
                    EventTypeColor = "info"
                });

                // Pending Repair status
                if (body.Status == SD.Status_Part_Pending_Repair)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Part Pending Repair",
                        Date = body.CreatedDate,
                        Description = "Part waiting for repair",
                        Details = $"Part: {body.BrokenPartName} - Awaiting repair work to begin",
                        Status = SD.Status_Part_Pending_Repair,
                        RelatedId = body.Id,
                        EventTypeColor = "warning"
                    });
                }

                // Pending Replacement status
                if (body.Status == SD.Status_Part_Pending_Replace)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Part Pending Replacement",
                        Date = body.CreatedDate,
                        Description = "Part scheduled for replacement",
                        Details = $"Part: {body.BrokenPartName} - Scheduled for replacement, waiting to begin",
                        Status = SD.Status_Part_Pending_Replace,
                        RelatedId = body.Id,
                        EventTypeColor = "warning"
                    });
                }

                // Waiting for Part (out of stock) - only when explicitly set
                if (body.WaitingPartDate.HasValue)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Waiting for Replacement Part",
                        Date = body.WaitingPartDate.Value,
                        Description = "Part replacement delayed - out of stock",
                        Details = $"Part: {body.BrokenPartName} - Replacement part is out of stock, waiting for inventory",
                        Status = SD.Status_Part_Waiting_Part,
                        RelatedId = body.Id,
                        EventTypeColor = "danger"
                    });
                }

                // Part Fixed event
                if (body.FixedDate.HasValue)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Part Fixed",
                        Date = body.FixedDate.Value,
                        Description = "Part successfully repaired",
                        Details = $"Part: {body.BrokenPartName} - Successfully repaired",
                        Status = SD.Status_Part_Fixed,
                        RelatedId = body.Id,
                        EventTypeColor = "success"
                    });
                }

                // Part Replaced event
                if (body.ReplacedDate.HasValue)
                {
                    var partName = body.Part != null ? body.Part.Name : body.BrokenPartName;
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Part Replaced",
                        Date = body.ReplacedDate.Value,
                        Description = "Part replaced",
                        Details = $"Part: {body.BrokenPartName} replaced with {partName}",
                        Status = SD.Status_Part_Replaced,
                        RelatedId = body.Id,
                        EventTypeColor = "success"
                    });
                }

                // Part Not Repairable
                if (body.NotRepairableDate.HasValue)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Part Not Repairable",
                        Date = body.NotRepairableDate.Value,
                        Description = "Part cannot be repaired",
                        Details = $"Part: {body.BrokenPartName} - Cannot be repaired, requires replacement",
                        Status = SD.Status_Part_NotRepairable,
                        RelatedId = body.Id,
                        EventTypeColor = "danger"
                    });
                }

                // Part Not Replaceable
                if (body.NotReplaceableDate.HasValue)
                {
                    timelineEvents.Add(new TimelineEvent
                    {
                        EventType = "Part Not Replaceable",
                        Date = body.NotReplaceableDate.Value,
                        Description = "Part cannot be replaced",
                        Details = $"Part: {body.BrokenPartName} - Cannot be replaced",
                        Status = SD.Status_Part_NotReplaceable,
                        RelatedId = body.Id,
                        EventTypeColor = "danger"
                    });
                }
            }

            // Sort all events by date and keep the 1st elm(serial number received) in place
            timelineEvents = timelineEvents.Take(1)
                .Concat(timelineEvents.Skip(1).OrderBy(e => e.Date))
                .ToList();

            SerialNumberHistory = new SerialNumberHistoryVM
            {
                SerialNumber = serialNumber,
                DefectiveUnits = defectiveUnits,
                TransactionHeaders = transactionHeaders,
                TransactionBodies = transactionBodies,
                TimelineEvents = timelineEvents,
                TotalDefectiveUnits = defectiveUnits.Count,
                TotalRepairJobs = transactionHeaders.Count,
                TotalPartsReplaced = transactionBodies.Count(tb => tb.Status == SD.Status_Part_Replaced),
                TotalPartsFixed = transactionBodies.Count(tb => tb.Status == SD.Status_Part_Fixed),
                CompletedJobs = transactionHeaders.Count(th => th.Status == SD.Status_Job_Completed),
                OutOfServiceJobs = transactionHeaders.Count(th => th.Status == SD.Status_Job_OutOfService)
            };
        }

        private string GetPartStatusDisplay(string status)
        {
            return status switch
            {
                SD.Status_Part_Pending_Repair => "Pending Repair",
                SD.Status_Part_Pending_Replace => "Pending Replacement",
                SD.Status_Part_Waiting_Part => "Waiting for Part",
                SD.Status_Part_Fixed => "Fixed",
                SD.Status_Part_Replaced => "Replaced",
                SD.Status_Part_NotRepairable => "Not Repairable",
                SD.Status_Part_NotReplaceable => "Not Replaceable",
                _ => status
            };
        }
    }

    public class SerialNumberHistoryVM
    {
        public SerialNumber SerialNumber { get; set; }
        public List<DefectiveUnit> DefectiveUnits { get; set; }
        public List<TransactionHeader> TransactionHeaders { get; set; }
        public List<TransactionBody> TransactionBodies { get; set; }
        public List<TimelineEvent> TimelineEvents { get; set; }
        public int TotalDefectiveUnits { get; set; }
        public int TotalRepairJobs { get; set; }
        public int TotalPartsReplaced { get; set; }
        public int TotalPartsFixed { get; set; }
        public int CompletedJobs { get; set; }
        public int OutOfServiceJobs { get; set; }
    }

    public class TimelineEvent
    {
        public string EventType { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public string Status { get; set; }
        public int RelatedId { get; set; }
        public string EventTypeColor { get; set; } // Bootstrap color class
    }
}