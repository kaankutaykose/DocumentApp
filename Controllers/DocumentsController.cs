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
    public class DocumentsController : Controller
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DocumentsController(ApplicationDbContext dbContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _signInManager = signInManager;
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

        [Authorize]
        public IActionResult AddFile()
        {
            ViewBag.Unit = _dbContext.Unit.OrderBy(u => u.UnitName).ToList();
            return View();
        }

        public async Task<IActionResult> DocumentsList()
        {
            var documents = await _dbContext.Documents
                .OrderByDescending(d => d.ModifiedDate)
                .ToListAsync();

            return View(documents);
        }

        [Authorize]
        public async Task<IActionResult> ListDocuments()
        {
            var userId = _userManager.GetUserId(User); // Giriş yapan kullanıcının ID'sini al
            var userRole = User.FindFirstValue(ClaimTypes.Role); // Kullanıcının rollerini al

            IQueryable<DocumentApp.Models.Documents> query = _dbContext.Documents
                .Include(d => d.Unit)
                .OrderByDescending(d => d.ModifiedDate);

            // Kullanıcı Admin değilse sadece kendi eklediği dökümanları getir
            if (userRole != "Admin")
            {
                query = query.Where(d => d.IsActive && d.UserId == userId);
            }

            var documents = await query.ToListAsync();

            return View(documents);
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

        [Authorize]
        [HttpGet]
        public IActionResult Delete()
        {
            Console.WriteLine("basarili");
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            Console.WriteLine($"Silinecek Dosya ID: {id}"); // Log ekle

            // Silinecek tüm versiyonları al
            var documents = await _dbContext.Documents.Where(d => d.Id == id).OrderByDescending(d => d.Version).ToListAsync();

            if (documents == null || documents.Count == 0)
            {
                Console.WriteLine("Dosya bulunamadı."); // Log ekle
                return NotFound("Dosya bulunamadı.");
            }

            // En son versiyonu al
            var latestDocument = documents.First();
            Console.WriteLine($"En son versiyon siliniyor: ID={latestDocument.Id}, Version={latestDocument.Version}");

            // Fiziksel dosyayı sil
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", latestDocument.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Console.WriteLine("Dosya fiziksel olarak silindi.");
            }
            else
            {
                Console.WriteLine("Fiziksel dosya bulunamadı.");
            }

            // En son versiyonu veritabanından kaldır
            _dbContext.Documents.Remove(latestDocument);
            await _dbContext.SaveChangesAsync();

            // Eğer daha önceki versiyonlar varsa, en güncel olanı aktif hale getir
            var previousVersion = documents.Skip(1).FirstOrDefault(); // En güncel olanı al (silinen hariç)

            if (previousVersion != null)
            {
                previousVersion.IsActive = true;
                _dbContext.Documents.Update(previousVersion);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"Bir önceki versiyon aktif yapıldı: ID={previousVersion.Id}, Version={previousVersion.Version}");
            }
            else
            {
                Console.WriteLine("Bir önceki versiyon bulunamadı.");
            }

            return RedirectToAction("ListDocuments");
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                Console.WriteLine("Silinecek dosya seçilmedi.");
                return BadRequest("Silinecek dosya seçilmedi.");
            }

            Console.WriteLine($"Silinecek Dosya ID'leri: {string.Join(", ", ids)}");

            // Veritabanından tüm seçili belgeleri al
            var documents = await _dbContext.Documents.Where(d => ids.Contains(d.Id)).OrderByDescending(d => d.Version).ToListAsync();

            if (documents == null || documents.Count == 0)
            {
                Console.WriteLine("Seçilen dosyalar bulunamadı.");
                return NotFound("Seçilen dosyalar bulunamadı.");
            }

            foreach (var document in documents)
            {
                // Dosya yolu belirleme ve fiziksel dosyayı silme
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    Console.WriteLine($"Dosya fiziksel olarak silindi: {filePath}");
                }
                else
                {
                    Console.WriteLine($"Fiziksel dosya bulunamadı: {filePath}");
                }

                // Seçili belgeyi veritabanından kaldır
                _dbContext.Documents.Remove(document);
            }

            await _dbContext.SaveChangesAsync();
            Console.WriteLine("Tüm seçili belgeler silindi.");

            return RedirectToAction("ListDocuments");
        }

        [Authorize]
        [HttpGet]
        public IActionResult UpdateDocument(Guid selectedFileId)
        {
            // Giriş yapan kullanıcının ID'sini al
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kullanıcının rolünü al
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            // Seçilen dosyayı veritabanından bul
            var existingDocument = _dbContext.Documents.FirstOrDefault(d => d.Id == selectedFileId && d.IsActive);
            ViewBag.Unit = _dbContext.Unit.OrderBy(u => u.UnitName).ToList();
            if (existingDocument == null)
            {
                return NotFound("Dosya bulunamadı.");
            }

            // Eğer kullanıcı Admin değilse ve başkasının konusunu düzenlemeye çalışıyorsa 403 Forbidden
            if (userRole != "Admin" && existingDocument.UserId != userId)
            {
                return Forbid(); // 403 Forbidden
            }

            // Mevcut dosya bilgilerini view'a gönder
            return View(existingDocument);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateDocument(Guid id, IFormFile? file, string fileName, string description, int unitId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existingDocument = _dbContext.Documents.FirstOrDefault(d => d.Id == id && d.IsActive);

            if (existingDocument == null)
            {
                return Json(new { success = false, message = "Dosya bulunamadı veya aktif değil." });
            }

            const int maxFileSize = 20 * 1024 * 1024; // 20 MB

            string newFilePath = existingDocument.FilePath; // Varsayılan olarak eski dosya yolu kalır
            int newVersion = existingDocument.Version;

            if (file != null && file.Length > 0)
            {
                if (file.Length > maxFileSize)
                    return Json(new { success = false, message = "Dosya boyutu 20 MB'den büyük olamaz." });

                var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsFolder = Path.Combine(wwwRootPath, "Documents");
                Directory.CreateDirectory(uploadsFolder);

                var sanitizedFileName = Path.GetFileNameWithoutExtension(fileName);
                var extension = Path.GetExtension(file.FileName);
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
                var uniqueFileName = $"{sanitizedFileName}_{timestamp}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                newFilePath = $"Documents/{uniqueFileName}"; // Yeni dosya yolu
                newVersion++;
            }

            var newDocument = new Documents
            {
                Id = existingDocument.Id,
                FileName = fileName,
                FilePath = newFilePath, // Eğer yeni dosya yüklenmediyse, eski dosya yolu kalır
                Description = description, // Açıklama güncellendi
                CreatedDate = existingDocument.CreatedDate,
                ModifiedDate = DateTime.Now,
                Version = newVersion,
                IsActive = true,
                UserId = userId,
                UnitId = unitId // Seçilen birim bilgisi kaydediliyor
            };

            existingDocument.IsActive = false;
            _dbContext.Documents.Update(existingDocument);
            _dbContext.Documents.Add(newDocument);

            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = "Döküman başarıyla güncellendi." });
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddFile(List <IFormFile> files, string description, int unitId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"); // Uygulamanın kök dizinini al
            var uploadsFolder = Path.Combine(wwwRootPath, "Documents"); // wwwroot/Documents klasörü
            Directory.CreateDirectory(uploadsFolder); // Klasör yoksa oluştur

            foreach (var file in files)
            {
                 // Dosya boyutu kontrolü (20 MB = 20 * 1024 * 1024 bytes)
                const int maxFileSize = 20 * 1024 * 1024; // 20 MB

                if (file == null || file.Length == 0)
                    return BadRequest("Lütfen bir dosya seçin.");

                if (file.Length > maxFileSize)
                    return BadRequest("Dosya boyutu 20 MB'den büyük olamaz.");

                var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName); // Geçersiz karakterlerden arındırma
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
                    FileName = sanitizedFileName,
                    FilePath = $"Documents/{uniqueFileName}",
                    Description = description,
                    UserId = userId,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Version = 1,
                    IsActive = true,
                    UnitId = unitId
                };

                // Veritabanına ekleme
                _dbContext.Documents.Add(document);
            }
            await _dbContext.SaveChangesAsync();

            return Ok("Dosya başarıyla yüklendi.");
        } 

    }
}
