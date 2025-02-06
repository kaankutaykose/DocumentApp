using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    // Ekstra alanlar ekleyebilirsiniz (Opsiyonel)
    public string FullName { get; set; }
}
