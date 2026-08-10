using System.Linq;
using System.Threading.Tasks;
using MarketConnect.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketConnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public LocationController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /api/location/provinces
        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            var provinces = await _db.Provinces
                .Select(p => new { p.Id, p.Name, p.Code })
                .ToListAsync();
            return Ok(provinces);
        }

        // GET: /api/location/districts?provinceId=1
        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts(int provinceId)
        {
            var districts = await _db.Districts
                .Where(d => d.ProvinceId == provinceId)
                .Select(d => new { d.Id, d.Name, d.Code, d.ProvinceId })
                .ToListAsync();
            return Ok(districts);
        }

        // GET: /api/location/wards?districtId=1
        [HttpGet("wards")]
        public async Task<IActionResult> GetWards(int districtId)
        {
            var wards = await _db.Wards
                .Where(w => w.DistrictId == districtId)
                .Select(w => new { w.Id, w.Name, w.Code, w.DistrictId })
                .ToListAsync();
            return Ok(wards);
        }

        // GET: /api/location/markets?provinceId=1&districtId=1&wardId=1
        [HttpGet("markets")]
        public async Task<IActionResult> GetMarkets(int? provinceId, int? districtId, int? wardId)
        {
            var query = _db.Markets
                .Include(m => m.Ward)
                .Include(m => m.District)
                .Include(m => m.Province)
                .Where(m => m.IsActive);

            if (wardId.HasValue && wardId.Value > 0)
            {
                query = query.Where(m => m.WardId == wardId.Value);
            }
            else if (districtId.HasValue && districtId.Value > 0)
            {
                query = query.Where(m => m.DistrictId == districtId.Value);
            }
            else if (provinceId.HasValue && provinceId.Value > 0)
            {
                query = query.Where(m => m.ProvinceId == provinceId.Value);
            }

            var markets = await query
                .Select(m => new {
                    m.Id,
                    m.Name,
                    m.Slug,
                    m.Address,
                    m.Latitude,
                    m.Longitude,
                    m.OpeningHours,
                    m.ManagementContact,
                    m.PopularCategories,
                    WardName = m.Ward != null ? m.Ward.Name : "",
                    DistrictName = m.District != null ? m.District.Name : "",
                    ProvinceName = m.Province != null ? m.Province.Name : ""
                })
                .ToListAsync();

            return Ok(markets);
        }
    }
}
