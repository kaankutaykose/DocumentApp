using System.ComponentModel.DataAnnotations;

namespace DocumentApp.Models
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email zorunludur.")]
        [EmailAddress]
        public string Email { get; set; }
    }
}
