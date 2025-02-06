using System.Linq;
using System.Security.Claims;
using DocumentApp.Data;
using DocumentApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Controllers
{
    public class TopicController : Controller
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public TopicController(ApplicationDbContext dbContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> TopicList()
            {
            var topics = await _dbContext.Topic
                .OrderByDescending(t => t.ModifiedDate)
                .ToListAsync();

            return View(topics);
        }


        [Authorize]
        public IActionResult CreateTopic()
        {
            ViewBag.Unit = _dbContext.Unit.OrderBy(u => u.UnitName).ToList();
            return View();
        }

        [Authorize]
        public IActionResult UpdateTopic()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ViewTopic()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ViewTopic(int selectedTopicId)
        {
            var topic = await _dbContext.Topic
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.TopicId == selectedTopicId);

            if (topic == null)
            {
                return NotFound("Seçilen konu bulunamadı veya aktif değil.");
            }

            return View(topic);
        }

        [Authorize]
        public async Task<IActionResult> ListTopic()
        {
            var userId = _userManager.GetUserId(User); // Giriş yapan kullanıcının ID'sini al
            var userRole = User.FindFirstValue(ClaimTypes.Role); // Kullanıcının rollerini al

            IQueryable<DocumentApp.Models.Topic> query = _dbContext.Topic
                 .Include(t => t.Unit)
                 .OrderByDescending(t => t.ModifiedDate);

            // Kullanıcı Admin değilse sadece kendi eklediği dökümanları getir
            if (userRole != "Admin")
            {
                query = query.Where(t => t.UserId == userId);
            }

            var documents = await query.ToListAsync();

            return View(documents);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateTopic(Topic topic, List<IFormFile> files, int unitId)
        {
            var userId = _userManager.GetUserId(User);
            // Seçilen unit bilgisini topic'e atayalım (Topic modelinde UnitId property'si bulunduğunu varsayıyoruz)
            topic.UserId = userId;
            topic.UnitId = unitId;
            topic.CreatedDate = DateTime.Now;
            topic.ModifiedDate = DateTime.Now;
            _dbContext.Topic.Add(topic);
            // Konuyu veritabanına kaydet
            await _dbContext.SaveChangesAsync();

            // Dosyalar için wwwroot/Documents klasörünü kullan
            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Documents");
            Directory.CreateDirectory(wwwRootPath); // Klasör yoksa oluştur

            foreach (var file in files)
            {
                const int maxFileSize = 20 * 1024 * 1024; // 20 MB

                if (file.Length > maxFileSize)
                    return BadRequest("Dosya boyutu 20 MB'den büyük olamaz.");

                var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
                var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var extension = Path.GetExtension(file.FileName);
                var uniqueFileName = $"{sanitizedFileName}_{timestamp}{extension}";
                var filePath = Path.Combine(wwwRootPath, uniqueFileName);

                // Dosyayı wwwroot/Documents altına kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Dosya bilgilerini Documents modeline kaydet
                var document = new Documents
                {
                    TopicId = topic.TopicId, // İlgili konu ID'si ile ilişkilendir
                    FileName = sanitizedFileName,
                    FilePath = $"Documents/{uniqueFileName}", // Göreceli dosya yolu
                    Version = 1,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    UserId = userId,
                    UnitId = unitId
                };
            
                _dbContext.Documents.Add(document);
            }
            
            await _dbContext.SaveChangesAsync();
            

            return Json(new { success = true, message = "Konu başarıyla oluşturuldu." });
        }


        [Authorize]
        [HttpGet]
        public IActionResult UpdateTopic(int selectedTopicId)
        {
            // Giriş yapan kullanıcının ID'sini al
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kullanıcının rolünü al
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Seçilen konuyu veritabanından bul
            var existingTopic = _dbContext.Topic.FirstOrDefault(t => t.TopicId == selectedTopicId);

            if (existingTopic == null)
            {
                return NotFound("Konu bulunamadı.");
            }

            // Eğer kullanıcı Admin değilse ve başkasının konusunu düzenlemeye çalışıyorsa 403 Forbidden
            if (userRole != "Admin" && existingTopic.UserId != userId)
            {
                return Forbid(); // 403 Forbidden
            }

            // Admin veya konu sahibi düzenleyebilir, devam edelim
            ViewBag.Unit = _dbContext.Unit.OrderBy(u => u.UnitName).ToList();
            return View(existingTopic);
        }




        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateTopic(int id, Topic updatedTopic, List<IFormFile> files, int unitId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Güncellenecek konuyu bul
            var existingTopic = _dbContext.Topic.FirstOrDefault(t => t.TopicId == id);
            Console.WriteLine($"Gelen ID: {id}");

            if (existingTopic == null)
                return NotFound("Konu bulunamadı.");

            // Konu bilgilerini güncelle
            existingTopic.Title = updatedTopic.Title;
            existingTopic.Description = updatedTopic.Description;
            existingTopic.ModifiedDate = DateTime.Now;
            existingTopic.UserId = userId;
            existingTopic.UnitId = unitId;  // Seçilen birim bilgisi güncelleniyor

            // Dosya yükleme işlemi (varsa)
            if (files != null && files.Count > 0)
            {
                var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Documents");
                Directory.CreateDirectory(wwwRootPath); // Klasör yoksa oluştur

                foreach (var file in files)
                {
                    const int maxFileSize = 20 * 1024 * 1024; // 20 MB

                    if (file.Length > maxFileSize)
                        return BadRequest("Dosya boyutu 20 MB'den büyük olamaz.");

                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
                    var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName);
                    var extension = Path.GetExtension(file.FileName);
                    var uniqueFileName = $"{sanitizedFileName}_{timestamp}{extension}";
                    var filePath = Path.Combine(wwwRootPath, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Yeni dosyayı Documents tablosuna ekle
                    var document = new Documents
                    {
                        TopicId = existingTopic.TopicId,
                        FileName = sanitizedFileName,
                        FilePath = $"Documents/{uniqueFileName}",
                        Version = 1,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        UnitId = unitId,
                        UserId = userId
                    };

                    _dbContext.Documents.Add(document);
                }
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Konu başarıyla güncellendi." });
        }

        [Authorize]
        [HttpGet]
        public IActionResult DeleteTopic()
        {
            Console.WriteLine("basarili");
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            Console.WriteLine($"Silinecek Konu ID: {id}");

            var topic = await _dbContext.Topic.FirstOrDefaultAsync(t => t.TopicId == id);
            if (topic == null)
            {
                Console.WriteLine("Konu bulunamadı.");
                return Json(new { success = false, message = "Konu bulunamadı." });
            }

            // Önce Documents tablosundaki ilgili kayıtları sil
            var relatedDocuments = _dbContext.Documents.Where(d => d.TopicId == id);
            _dbContext.Documents.RemoveRange(relatedDocuments);

            // Şimdi Topic kaydını sil
            _dbContext.Topic.Remove(topic);

            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Konu başarıyla silindi." });
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeleteTopics(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                Console.WriteLine("Hiçbir konu seçilmedi.");
                return Json(new { success = false, message = "Hiçbir konu seçilmedi." });
            }

            // Silinecek konuları veritabanından alıyoruz.
            var topicsToDelete = _dbContext.Topic.Where(t => ids.Contains(t.TopicId)).ToList();

            // Bu konulara ait dökümanlarda TopicId değeri null yapılıyor.
            var documentsToUpdate = _dbContext.Documents
                .Where(d => d.TopicId != null && ids.Contains(d.TopicId.Value))
                .ToList();

            foreach (var document in documentsToUpdate)
            {
                document.TopicId = null; // Döküman ile ilişki kesiliyor.
                _dbContext.Documents.Update(document);
            }

            // Konuları topluca siliyoruz.
            _dbContext.Topic.RemoveRange(topicsToDelete);

            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Seçilen konular başarıyla silindi." });
        }



    }
}
