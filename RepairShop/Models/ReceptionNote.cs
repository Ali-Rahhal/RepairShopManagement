using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RepairShop.Models
{
    public class ReceptionNote
    {
        public long Id { get; set; }

        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsPrinted { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Client
        public long ClientId { get; set; }

        [ValidateNever]
        public Client Client { get; set; }

        // Items
        [ValidateNever]
        [JsonIgnore]
        public ICollection<ReceptionNoteItem> Items { get; set; }
            = new List<ReceptionNoteItem>();
    }
}
