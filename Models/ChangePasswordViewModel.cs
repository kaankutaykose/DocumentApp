using System.ComponentModel.DataAnnotations;

namespace DocumentApp.Models
{
    public class ChangePasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress]
        [Display (Name = "E-posta")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "{0} en az {2} ve en fazla {1} karakter uzunluğunda olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        [Compare("ConfirmNewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Şifreyi tekrarla zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre (Tekrar)")]
        public string ConfirmNewPassword { get; set; }
    }
}
