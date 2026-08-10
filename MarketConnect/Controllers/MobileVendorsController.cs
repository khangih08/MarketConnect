using System.Threading.Tasks;
using MarketConnect.Data;
using MarketConnect.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketConnect.Controllers
{
    public class MobileVendorsController : Controller
    {
        private readonly IMobileVendorService _vendorService;

        public MobileVendorsController(IMobileVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        // GET: /MobileVendors
        public async Task<IActionResult> Index()
        {
            var profile = await _vendorService.GetProfileByUserIdAsync(4); // Vendor user ID
            return View(profile);
        }

        // GET: /MobileVendors/CallMap
        public IActionResult CallMap()
        {
            return View();
        }

        // GET: /MobileVendors/SearchNearby
        [HttpGet]
        public async Task<IActionResult> SearchNearby(string? targetItem, double latitude = 21.0365, double longitude = 105.7830, double radiusKm = 3.0)
        {
            var results = await _vendorService.FindNearbyVendorsAsync(targetItem ?? "", latitude, longitude, radiusKm);
            return Json(results);
        }

        // POST: /MobileVendors/CreateCallRequest
        [HttpPost]
        public async Task<IActionResult> CreateCallRequest([FromBody] SellerCallRequest reqDto)
        {
            var req = await _vendorService.CreateCallRequestAsync(
                buyerId: 1,
                targetItem: reqDto.TargetItem,
                latitude: reqDto.MeetupLatitude,
                longitude: reqDto.MeetupLongitude,
                meetupNote: reqDto.MeetupAddressNote,
                buyerNote: reqDto.BuyerNote,
                radiusKm: reqDto.RadiusKm
            );

            return Json(new { success = true, request = req });
        }

        // POST: /MobileVendors/ToggleOnline
        [HttpPost]
        public async Task<IActionResult> ToggleOnline(bool isOnline, double latitude = 21.0365, double longitude = 105.7830)
        {
            var avail = await _vendorService.ToggleOnlineStatusAsync(4, isOnline, latitude, longitude);
            return Json(new { success = true, isOnline = avail.IsOnline });
        }

        // POST: /MobileVendors/AcceptRequest
        [HttpPost]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            bool ok = await _vendorService.AcceptCallRequestAsync(requestId, sellerUserId: 4);
            return Json(new { success = ok });
        }
    }
}
