using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
namespace DocumentApp.Models
{
    public class Topic
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TopicId { get; set; }

        [Required, StringLength(255)]
        public string Title { get; set; } // Konu başlığı

        [Required]
        public string Description { get; set; } // CKEditor içeriği (HTML)

        public int? UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } // İlişkilendirilmiş birim
        public string UserId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        // İlişki: Bir konu birden fazla ek içerebilir
        public ICollection<Documents> Documents { get; set; }
    }
}
