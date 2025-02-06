using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

 // Sadece Admin erişebilir
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [Authorize(Roles = "Admin")]
    public IActionResult AdminPanel()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UserList()
    {
        var users = _userManager.Users.ToList();
        var userRoles = new Dictionary<string, string>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles.Any() ? string.Join(", ", roles) : "User";
        }

        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignAdminRole(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        // Kullanıcı zaten admin mi kontrol et
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            return BadRequest("Bu kullanıcı zaten admin.");
        }

        await _userManager.AddToRoleAsync(user, "Admin");
        return RedirectToAction("UserList");
    }
}
