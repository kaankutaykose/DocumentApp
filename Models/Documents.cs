using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentApp.Models
{
    public class Documents
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int uniqueId { get; set; }

        public int? TopicId { get; set; }

        [ForeignKey("TopicId")]
        public Topic Topic { get; set; } // İlişkilendirilmiş konu

        public int? UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } // İlişkilendirilmiş birim

        public string UserId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid(); // GUID otomatik atanır.

        [Required]
        public int Version { get; set; } = 1; // İlk ekleme için varsayılan değer 1.

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } // Kullanıcı tarafından girilir.

        public string? Description { get; set; }

        [Required]
        public string FilePath { get; set; } // Sunucudaki dosya yolu.

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Otomatik atanır.

        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow; // İlk başta CreatedDate ile aynı.

        [Required]
        public bool IsActive { get; set; } = true; // İlk kayıt için varsayılan olarak aktif.

    }
}
