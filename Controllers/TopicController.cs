using System.Linq;
using DocumentApp.Data;
using DocumentApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Controllers
{
    public class TopicController : Controller
    {

        private readonly ApplicationDbContext _dbContext;

        public TopicController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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
        public IActionResult CreateTopic()
        {
            ViewBag.Unit = _dbContext.Unit.OrderBy(u => u.UnitName).ToList();
            return View();
        }

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

        public async Task<IActionResult> ListTopic()
        {
            if (_dbContext == null)
            {
                throw new Exception("_dbContext is null. Check your database connection.");
            }

            
            var topics = await _dbContext.Topic
                .OrderByDescending(t => t.ModifiedDate)
                .ToListAsync();

            if (topics == null)
            {
                topics = new List<DocumentApp.Models.Topic>();
                Console.WriteLine("No active documents found. Returning empty list.");
            }

            return View(topics);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTopic(Topic topic, List<IFormFile> files, int unitId)
        {
            // Seçilen unit bilgisini topic'e atayalım (Topic modelinde UnitId property'si bulunduğunu varsayıyoruz)
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
                    UnitId = unitId
                };
            
                _dbContext.Documents.Add(document);
            }
            
            await _dbContext.SaveChangesAsync();
            

            return Json(new { success = true, message = "Konu başarıyla oluşturuldu." });
        }


        [HttpGet]
        public IActionResult UpdateTopic(int selectedFileId)
        {
            // Seçilen konuyu veritabanından bul
            var existingTopic = _dbContext.Topic.FirstOrDefault(t => t.TopicId == selectedFileId);

            if (existingTopic == null)
            {
                return NotFound("Konu bulunamadı.");
            }

            // Birim listesini ViewBag'e aktaralım (UnitName'e göre sıralayarak)
            ViewBag.Unit = _dbContext.Unit.OrderBy(u => u.UnitName).ToList();

            // Mevcut konu bilgilerini view'e gönder
            return View(existingTopic);
        }



        [HttpPost]
        public async Task<IActionResult> UpdateTopic(int id, Topic updatedTopic, List<IFormFile> files, int unitId)
        {
            // Tüm konuları listeleyelim (debug amaçlı)
            var allTopics = _dbContext.Topic.ToList();
            Console.WriteLine($"Toplam konu sayısı: {allTopics.Count}");

            // Güncellenecek konuyu bul
            var existingTopic = _dbContext.Topic.FirstOrDefault(t => t.TopicId == id);
            Console.WriteLine($"Gelen ID: {id}");

            if (existingTopic == null)
                return NotFound("Konu bulunamadı.");

            // Konu bilgilerini güncelle
            existingTopic.Title = updatedTopic.Title;
            existingTopic.Description = updatedTopic.Description;
            existingTopic.ModifiedDate = DateTime.Now;
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
                        UnitId = unitId
                    };

                    _dbContext.Documents.Add(document);
                }
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = "Konu başarıyla güncellendi." });
        }

        [HttpGet]
        public IActionResult DeleteTopic()
        {
            Console.WriteLine("basarili");
            return View();
        }

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
