using System.ComponentModel.DataAnnotations;

namespace DocumentApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "İsim zorunludur.")]
        [Display (Name = "Ad Soyad")]
        public string Name { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress]
        [Display(Name = "E-posta")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "The {0} must be at {2} and at max {1} characters long.")]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Şifreler aynı değil.")]
        [Display(Name = "Şifre")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Şifreyi Onayla kısmı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifreyi Onayla")]
        public string ConfirmPassword { get; set; }
    }
}
