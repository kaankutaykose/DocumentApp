using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocumentApp.Models
{
    public class Documents
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int uniqueId { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid(); // GUID otomatik atanır.

        [Required]
        public int Version { get; set; } = 1; // İlk ekleme için varsayılan değer 1.

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } // Kullanıcı tarafından girilir.

        [Required]
        public string FilePath { get; set; } // Sunucudaki dosya yolu.

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Otomatik atanır.

        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow; // İlk başta CreatedDate ile aynı.

        [Required]
        public bool IsActive { get; set; } = true; // İlk kayıt için varsayılan olarak aktif.

    }
}
