using System.Diagnostics;
using System.Net;
using DocumentApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace DocumentApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }


        public IActionResult ListDocuments()
        {
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Services()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> UploadCKEDITOR(IFormFile upload)
        {
            if (upload != null && upload.Length > 0)
            {
                var filename = DateTime.Now.ToString("yyyyMMddHHmmss") + upload.FileName;
                var path = Path.Combine(Directory.GetCurrentDirectory(), _hostingEnvironment.WebRootPath, "uploads", filename);

                using (var str = new FileStream(path, FileMode.Create))
                {
                    await upload.CopyToAsync(str);
                }

                var url = $"{"/uploads/"}{filename}";
                return Json(new { uploaded = true, url });
            }
            return Json(new { path = "/uploads/" });
        }
        [HttpGet]
        public async Task<IActionResult> FileBrowserCKEDITOR(IFormFile upload)
        {
            var dir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), _hostingEnvironment.WebRootPath, "uploads"));
            ViewBag.fileInfos = dir.GetFiles();
            return View("FileBrowserCKEDITOR");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
