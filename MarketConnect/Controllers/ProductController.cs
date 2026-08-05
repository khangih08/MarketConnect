using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MarketConnect.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _products;
        private readonly ApplicationDbContext _db;

        public ProductController(IProductService products, ApplicationDbContext db)
        {
            _products = products;
            _db = db;
        }

        [HttpGet]
        [Route("Product/Category")]
        [Route("Products/Category")]
        [Route("Product/CategoryList")]
        [Route("Products/CategoryList")]
        public IActionResult CategoryList([FromQuery] string category)
        {
            ViewBag.SelectedCategory = string.IsNullOrWhiteSpace(category) ? "Rau củ & Trái cây tươi" : category;
            return View("~/Views/Product/CategoryList.cshtml");
        }

        [HttpGet]
        [Route("Product/ProductDetail")]
        [Route("Products/ProductDetail")]
        [Route("Product/Detail")]
        [Route("Products/Detail")]
        public IActionResult ProductDetail(string id, string group_key)
        {
            ViewData["ProductId"] = id;
            ViewData["GroupKey"] = group_key;
            return View("~/Views/Product/ProductDetail.cshtml");
        }

        [HttpGet]
        [Route("Product/Create")]
        [Route("Products/Create")]
        [Route("Product/PostListing")]
        public async Task<IActionResult> Create()
        {
            var categories = await _products.GetCategoriesAsync();
            ViewBag.Categories = categories;
            return View("~/Views/Product/Create.cshtml");
        }

        [HttpPost]
        [Route("Product/Create")]
        [Route("Products/Create")]
        [Route("Product/PostListing")]
        public async Task<IActionResult> Create([FromForm] ProductCreateDto dto, IFormFile? ImageFile, List<IFormFile>? MediaFiles)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                ModelState.AddModelError("Title", "Vui lòng nhập tiêu đề tin đăng");
                ViewBag.Categories = await _products.GetCategoriesAsync();
                return View("~/Views/Product/Create.cshtml", dto);
            }

            // Xử lý lưu ảnh đại diện chính từ máy tính
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                dto.ImageUrl = "/uploads/" + uniqueFileName;
            }

            // Xử lý lưu các ảnh/video phụ từ máy tính
            if (MediaFiles != null && MediaFiles.Count > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var savedUrls = new List<string>();
                foreach (var file in MediaFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        savedUrls.Add("/uploads/" + uniqueFileName);
                    }
                }
                if (savedUrls.Count > 0)
                {
                    dto.MediaUrls = string.Join(",", savedUrls);
                }
            }

            // Lấy User ID người dùng đang đăng nhập
            int? currentUserId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int uid))
                {
                    currentUserId = uid;
                }
                else
                {
                    var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity.Name;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail || u.Name == userEmail);
                        if (user != null) currentUserId = user.Id;
                    }
                }
            }

            var created = await _products.CreateListingAsync(dto, currentUserId);
            return RedirectToAction("ProductDetail", new { id = created.Id });
        }
    }
}
