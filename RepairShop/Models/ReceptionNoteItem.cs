using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace RepairShop.Models
{
    public class ReceptionNoteItem
    {
        public long Id { get; set; }

        // Header
        public long ReceptionNoteId { get; set; }
        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public ReceptionNote ReceptionNote { get; set; }

        // Device
        public long SerialNumberId { get; set; }

        [ValidateNever]
        [DeleteBehavior(DeleteBehavior.Restrict)] // Add this to prevent cascade issues
        public SerialNumber SerialNumber { get; set; }
    }
}
