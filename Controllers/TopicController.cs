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

        public async Task<IActionResult> TopicList(string searchTerm, int? unitId)
        {
            IQueryable<DocumentApp.Models.Topic> query = _dbContext.Topic
                .Include(t => t.Unit) // Birim bilgilerini de çekiyoruz.
                .OrderByDescending(t => t.ModifiedDate);

            // Arama: Konu başlığı veya açıklama içerisinde arama yap.
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm));
            }

            // Filtreleme: Seçilen birime göre filtreleme yap (unitId değeri varsa)
            if (unitId.HasValue && unitId.Value > 0)
            {
                query = query.Where(t => t.Unit != null && t.Unit.UnitId == unitId.Value);
            }

            var topics = await query.ToListAsync();

            // Konuyu oluşturan kullanıcıların bilgilerini almak için (Topic modelinizde UserId varsa)
            var topicUsers = new Dictionary<int, string>();  // Key: TopicId, Value: FullName
            foreach (var topic in topics)
            {
                if (!string.IsNullOrEmpty(topic.UserId))
                {
                    var user = await _userManager.FindByIdAsync(topic.UserId);
                    topicUsers[topic.TopicId] = user?.FullName ?? "Bilinmiyor";
                }
                else
                {
                    topicUsers[topic.TopicId] = "Bilinmiyor";
                }
            }

            // ViewBag'lere ekleyelim
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedUnitId = unitId;
            ViewBag.Units = await _dbContext.Unit.OrderBy(u => u.UnitName).ToListAsync();
            ViewBag.TopicUsers = topicUsers;

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
        public async Task<IActionResult> ListTopic(string searchTerm, int? unitId)
        {
            var userId = _userManager.GetUserId(User); // Giriş yapan kullanıcının ID'sini al
            var userRole = User.FindFirstValue(ClaimTypes.Role); // Kullanıcının rollerini al

            IQueryable<DocumentApp.Models.Topic> query = _dbContext.Topic
                 .Include(t => t.Unit)
                 .OrderByDescending(t => t.ModifiedDate);

            // Kullanıcı Admin değilse sadece kendi eklediği konuları getir
            if (userRole != "Admin")
            {
                query = query.Where(t => t.UserId == userId);
            }

            // Arama: Konu başlığı veya açıklama içinde arama yap
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(t => t.Title.Contains(searchTerm) || t.Description.Contains(searchTerm));
            }

            // Filtreleme: Seçilen birime göre filtrele (Varsayıyoruz ki birim bilgisi Unit üzerinden geliyor)
            if (unitId.HasValue && unitId.Value > 0)
            {
                query = query.Where(t => t.Unit != null && t.Unit.UnitId == unitId.Value);
            }

            var topics = await query.ToListAsync();

            // View'da form alanlarını doldurmak için arama ve filtre değerlerini, ayrıca mevcut birim listesini ViewBag üzerinden gönderiyoruz.
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedUnitId = unitId;
            ViewBag.Units = await _dbContext.Unit.OrderBy(u => u.UnitName).ToListAsync();

            return View(topics);
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

            // İlgili konuyla bağlantılı tüm belgeleri al
            var relatedDocuments = await _dbContext.Documents.Where(d => d.TopicId == id).ToListAsync();

            // Her belge için fiziksel dosyayı sil
            foreach (var doc in relatedDocuments)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Dosya fiziksel olarak silindi: {filePath}");
                }
                else
                {
                    Console.WriteLine($"Fiziksel dosya bulunamadı: {filePath}");
                }
            }

            // Belgeleri veritabanından kaldır
            _dbContext.Documents.RemoveRange(relatedDocuments);

            // Konu kaydını sil
            _dbContext.Topic.Remove(topic);

            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Konu başarıyla silindi." });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> BulkDeleteTopics(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                Console.WriteLine("Hiçbir konu seçilmedi.");
                return Json(new { success = false, message = "Hiçbir konu seçilmedi." });
            }

            Console.WriteLine($"Silinecek Konu ID'leri: {string.Join(", ", ids)}");

            // Seçilen konuları al
            var topicsToDelete = _dbContext.Topic.Where(t => ids.Contains(t.TopicId)).ToList();

            // Seçilen konulara ait belgeleri al
            var documentsToDelete = _dbContext.Documents
                .Where(d => d.TopicId != null && ids.Contains(d.TopicId.Value))
                .ToList();

            // Her belge için fiziksel dosyayı sil
            foreach (var doc in documentsToDelete)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Dosya fiziksel olarak silindi: {filePath}");
                }
                else
                {
                    Console.WriteLine($"Fiziksel dosya bulunamadı: {filePath}");
                }
            }

            // Belgeleri veritabanından kaldır
            _dbContext.Documents.RemoveRange(documentsToDelete);

            // Konuları veritabanından kaldır
            _dbContext.Topic.RemoveRange(topicsToDelete);

            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Seçilen konular başarıyla silindi." });
        }




    }
}
