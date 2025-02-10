using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
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
    [HttpGet]
    public async Task<IActionResult> EditUser(string id)  // Parametre adını "id" olarak güncelledik
    {
        // Giriş yapan kullanıcının ID'sini al
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // Giriş yapan kullanıcının rolünü al
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        // Güncellenecek kullanıcıyı UserManager üzerinden bul
        var userToUpdate = await _userManager.FindByIdAsync(id);
        if (userToUpdate == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        // Eğer kullanıcı Admin değilse ve başkasının bilgilerini düzenlemeye çalışıyorsa 403 Forbidden
        if (currentUserRole != "Admin" && userToUpdate.Id != currentUserId)
        {
            return Forbid();
        }

        // Rol seçimi için örnek rol listesini view'a gönderiyoruz
        ViewBag.Roles = new List<string> { "Admin", "User" };

        return View(userToUpdate);
    }



    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(ApplicationUser model)
    {
        // Formda "NewPassword" adlı input'tan gelen değeri alıyoruz
        string newPassword = Request.Form["NewPassword"];

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        // Güncellenecek kullanıcıyı tekrar buluyoruz
        var userToUpdate = await _userManager.FindByIdAsync(model.Id);
        if (userToUpdate == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        if (currentUserRole != "Admin" && userToUpdate.Id != currentUserId)
        {
            return Forbid();
        }

        // Güncellenen temel bilgileri ata
        userToUpdate.FullName = model.FullName;
        userToUpdate.Email = model.Email;
        userToUpdate.UserName = model.Email; // Kullanıcı adı olarak e-posta kullanılıyorsa

        // Eğer yeni şifre girilmişse, şifre sıfırlama işlemi yap
        if (!string.IsNullOrEmpty(newPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(userToUpdate);
            var passwordResult = await _userManager.ResetPasswordAsync(userToUpdate, token, newPassword);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = new List<string> { "Admin", "User" };
                return View(userToUpdate);
            }
        }

        // Rol güncelleme: Önce mevcut rolleri kaldır, ardından formdan gelen rolü ekle
        var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
        var removeResult = await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
        if (!removeResult.Succeeded)
        {
            ModelState.AddModelError("", "Mevcut roller kaldırılırken hata oluştu.");
            ViewBag.Roles = new List<string> { "Admin", "User" };
            return View(userToUpdate);
        }

        // Formda "Role" adlı select'ten gelen değeri alıyoruz
        string newRole = Request.Form["Role"];
        if (!string.IsNullOrEmpty(newRole))
        {
            var addResult = await _userManager.AddToRoleAsync(userToUpdate, newRole);
            if (!addResult.Succeeded)
            {
                foreach (var error in addResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                ViewBag.Roles = new List<string> { "Admin", "User" };
                return View(userToUpdate);
            }
        }

        // Kullanıcıyı güncelle
        var updateResult = await _userManager.UpdateAsync(userToUpdate);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            ViewBag.Roles = new List<string> { "Admin", "User" };
            return View(userToUpdate);
        }

        return RedirectToAction("UserList", "Admin");
    }

        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> UserList(string searchTerm, string roleFilter)
        {
            // Başlangıçta tüm kullanıcılar üzerinden sorgu oluşturuyoruz.
            var query = _userManager.Users.AsQueryable();

            // Arama: Kullanıcı adı veya email içeren kayıtlar
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            // Sorgudan listeyi alıyoruz
            var users = await query.ToListAsync();

            // Her kullanıcı için rol bilgisini alıp bir sözlüğe ekleyelim.
            var userRoles = new Dictionary<string, string>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Eğer rol bilgisi yoksa varsayılan "User" değeri veriyoruz.
                string roleValue = roles.Any() ? string.Join(", ", roles) : "User";
                userRoles[user.Id] = roleValue;
            }

            // Filtreleme: Eğer roleFilter parametresi gönderildiyse, yalnızca o role sahip kullanıcıları al.
            if (!string.IsNullOrEmpty(roleFilter))
            {
                users = users.Where(u => userRoles[u.Id].Contains(roleFilter)).ToList();
                // Sözlüğü de filtreleyelim
                userRoles = userRoles.Where(kvp => kvp.Value.Contains(roleFilter))
                                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            // Arama ve filtre değerlerini view'a gönderelim.
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedRole = roleFilter;
            ViewBag.UserRoles = userRoles;

            return View(users);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        
        public async Task<IActionResult> DeleteUser(string id)
        {
            // Silinecek kullanıcıyı UserManager üzerinden buluyoruz.
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Kullanıcıyı sil
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                // Hata durumunda oluşan hataları loglayabilir veya döndürebilirsiniz.
                return BadRequest("Kullanıcı silinemedi.");
            }

            return RedirectToAction("UserList", "Admin");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        
        public async Task<IActionResult> BulkDeleteUsers(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return BadRequest("Hiçbir kullanıcı seçilmedi.");
            }

            foreach (var id in ids)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        // Eğer bir kullanıcı silinemiyorsa, burada loglama yapabilir veya döngüyü devam ettirebilirsiniz.
                        Console.WriteLine($"Kullanıcı {id} silinemedi: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            return RedirectToAction("UserList", "Admin");
        }



}
