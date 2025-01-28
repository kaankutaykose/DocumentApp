using DocumentApp.Data;
using DocumentApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentApp.Controllers
{
    public class DocumentsController : Controller
    {

        private readonly ApplicationDbContext _dbContext;

        public DocumentsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Dosya listesini getirir

        //public async Task<IActionResult> ListDocuments()
        //{
        //    var documents = await _dbContext.Documents
        //        .Where(d => d.IsActive)
        //        .OrderByDescending(d => d.CreatedDate)
        //        .ToListAsync();

        //    return View(documents);
        //}

        public IActionResult AddFile()
        {
            return View();
        }


        public async Task<IActionResult> ListDocuments()
        {
            if (_dbContext == null)
            {
                throw new Exception("_dbContext is null. Check your database connection.");
            }

            // Sadece aktif (IsActive = true) olan dokümanları getir
            var activeDocuments = await _dbContext.Documents
                .Where(d => d.IsActive)
                .ToListAsync();

            if (activeDocuments == null)
            {
                activeDocuments = new List<DocumentApp.Models.Documents>();
                Console.WriteLine("No active documents found. Returning empty list.");
            }

            return View(activeDocuments);
        }

        [HttpGet]
        public IActionResult ViewDocument()
        {
            Console.WriteLine("basarili");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ViewDocument(Guid selectedFileId)
        {
            // Aktif olan dosyayı bul
            var document = await _dbContext.Documents
                .FirstOrDefaultAsync(d => d.Id == selectedFileId && d.IsActive == true);

            if (document == null)
            {
                return NotFound("Seçilen dosya bulunamadı veya aktif değil.");
            }

            // URL'ye id ve version ekleyerek yönlendirme yapılıyor
            return RedirectToAction("ViewDocumentWithParams", "Documents", new { id = document.Id, version = document.Version });
        }

       


        // GET metodu ile çalışacak olan yeni bir Action
        [HttpGet]
        public IActionResult ViewDocumentWithParams(Guid id, int? version)
        {
            // Eğer version parametresi verilmişse, belirli bir versiyonu getir
            if (version.HasValue)
            {
                var specificVersionDocument = _dbContext.Documents
                    .FirstOrDefault(d => d.Id == id && d.Version == version.Value);

                if (specificVersionDocument == null)
                {
                    return NotFound("Belirtilen versiyon bulunamadı.");
                }

                ViewData["FilePath"] = specificVersionDocument.FilePath;
                ViewData["Version"] = specificVersionDocument.Version;
            }
            else
            {
                // Eğer version parametresi verilmemişse, aktif (IsActive = true) versiyonu getir
                var activeVersionDocument = _dbContext.Documents
                    .FirstOrDefault(d => d.Id == id && d.IsActive);

                if (activeVersionDocument == null)
                {
                    return NotFound("Aktif belge bulunamadı.");
                }

                ViewData["FilePath"] = activeVersionDocument.FilePath;
                ViewData["Version"] = activeVersionDocument.Version;
            }

            return View("ViewDocument");
        }

        [HttpGet]
        public IActionResult Delete()
        {
            Console.WriteLine("basarili");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id) // id parametresi Guid olarak tanımlandı
        {
            Console.WriteLine($"Silinecek Dosya ID: {id}"); // Log ekle

            var document = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == id);
            if (document == null)
            {
                Console.WriteLine("Dosya bulunamadı."); // Log ekle
                return NotFound("Dosya bulunamadı.");
            }

            var filePath = document.FilePath;
            Console.WriteLine($"Dosya Yolu: {filePath}"); // Log ekle

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Console.WriteLine("Dosya fiziksel olarak silindi."); // Log ekle
            }
            else
            {
                Console.WriteLine("Fiziksel dosya bulunamadı."); // Log ekle
            }

            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ListDocuments");
        }

        [HttpGet]
        public IActionResult UpdateDocument(Guid selectedFileId)
        {
            // Seçilen dosyayı veritabanından bul
            var existingDocument = _dbContext.Documents.FirstOrDefault(d => d.Id == selectedFileId);

            if (existingDocument == null)
            {
                return NotFound("Dosya bulunamadı.");
            }

            // Mevcut dosya bilgilerini view'a gönder
            return View(existingDocument);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDocument(Guid id, IFormFile file, string fileName)
        {
            // Aktif olan dosyayı bul
            var existingDocument = _dbContext.Documents.FirstOrDefault(d => d.Id == id && d.IsActive);

            if (existingDocument == null)
            {
                return NotFound("Dosya bulunamadı veya aktif değil.");
            }

            // Dosya boyutu kontrolü (20 MB = 20 * 1024 * 1024 bytes)
            const int maxFileSize = 20 * 1024 * 1024; // 20 MB

            if (file == null || file.Length == 0)
                return BadRequest("Lütfen bir dosya seçin.");

            if (file.Length > maxFileSize)
                return BadRequest("Dosya boyutu 20 MB'den büyük olamaz.");

            // Yeni dosya için benzersiz bir dosya adı oluştur
            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(wwwRootPath, "Documents");
            Directory.CreateDirectory(uploadsFolder);

            var sanitizedFileName = Path.GetFileNameWithoutExtension(fileName); // Geçersiz karakterlerden arındırma
            var extension = Path.GetExtension(file.FileName); // Orijinal dosya uzantısı
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmm"); // YılAyGünSaatDakika
            var uniqueFileName = $"{sanitizedFileName}_{timestamp}{extension}"; // Kullanıcı adı + zaman damgası + uzantı
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Yeni dosya kaydı oluştur
            var newDocument = new Documents
            {
                Id = existingDocument.Id, // Aynı GUID'i kullan
                FileName = fileName,
                FilePath = filePath, // Yeni dosya yolu
                CreatedDate = existingDocument.CreatedDate,
                ModifiedDate = DateTime.UtcNow,
                Version = existingDocument.Version + 1, // Versiyonu bir artır
                IsActive = true
            };

            // Eski dosyanın IsActive değerini false yap
            existingDocument.IsActive = false;
            _dbContext.Documents.Update(existingDocument);
            // Veritabanına yeni dosyayı ekle ve eski dosyayı güncelle
            _dbContext.Documents.Add(newDocument);
            
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ListDocuments"); // İşlem tamamlandıktan sonra ana sayfaya yönlendir
        }

        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFile file, string fileName, int userId)
        {
            // Dosya boyutu kontrolü (20 MB = 20 * 1024 * 1024 bytes)
            const int maxFileSize = 20 * 1024 * 1024; // 20 MB

            if (file == null || file.Length == 0)
                return BadRequest("Lütfen bir dosya seçin.");

            if (file.Length > maxFileSize)
                return BadRequest("Dosya boyutu 20 MB'den büyük olamaz.");

            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"); // Uygulamanın kök dizinini al
            var uploadsFolder = Path.Combine(wwwRootPath, "Documents"); // wwwroot/Documents klasörü
            Directory.CreateDirectory(uploadsFolder); // Klasör yoksa oluştur

            var sanitizedFileName = Path.GetFileNameWithoutExtension(fileName); // Geçersiz karakterlerden arındırma
            var extension = Path.GetExtension(file.FileName); // Orijinal dosya uzantısı

            // Benzersiz dosya adı oluşturma (örneğin, zaman damgası ekleniyor)
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmm"); // YılAyGünSaatDakika
            var uniqueFileName = $"{sanitizedFileName}_{timestamp}{extension}"; // Kullanıcı adı + zaman damgası + uzantı

            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Dosyayı kaydetme
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Yeni dosya kaydı oluşturma
            var document = new Documents
            {
                FileName = fileName,
                FilePath = filePath,
                //UserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                Version = 1,
                IsActive = true
            };

            // Veritabanına ekleme
            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync();

            return Ok("Dosya başarıyla yüklendi.");
        }

    }
}
