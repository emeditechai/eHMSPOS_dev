using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class BanquetEventTypeMasterController : BaseController
    {
        private readonly IBanquetEventTypeRepository _repo;
        public BanquetEventTypeMasterController(IBanquetEventTypeRepository repo) => _repo = repo;

        public async Task<IActionResult> List()
        {
            ViewData["Title"] = "Event Types";
            return View(await _repo.GetByBranchAsync(CurrentBranchID, false));
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Event Type";
            return View(new BanquetEventType { IsActive = true, IconClass = "fas fa-calendar-star" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanquetEventType model)
        {
            if (!ModelState.IsValid) return View(model);
            if (await _repo.CodeExistsAsync(model.EventTypeCode, CurrentBranchID))
            {
                ModelState.AddModelError("EventTypeCode", "Code already exists.");
                return View(model);
            }
            model.BranchID  = CurrentBranchID;
            model.CreatedBy = CurrentUserId;
            await _repo.CreateAsync(model);
            TempData["SuccessMessage"] = "Event type created.";
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _repo.GetByIdAsync(id);
            if (m == null) return NotFound();
            ViewData["Title"] = "Edit Event Type";
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BanquetEventType model)
        {
            if (!ModelState.IsValid) return View(model);
            if (await _repo.CodeExistsAsync(model.EventTypeCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError("EventTypeCode", "Code already exists.");
                return View(model);
            }
            model.UpdatedBy = CurrentUserId;
            await _repo.UpdateAsync(model);
            TempData["SuccessMessage"] = "Event type updated.";
            return RedirectToAction(nameof(List));
        }
    }

    [Authorize]
    public class BanquetPackageMasterController : BaseController
    {
        private readonly IBanquetPackageRepository _repo;
        private readonly IGstSlabRepository _gstSlabRepo;

        public BanquetPackageMasterController(IBanquetPackageRepository repo, IGstSlabRepository gstSlabRepo)
        {
            _repo = repo;
            _gstSlabRepo = gstSlabRepo;
        }

        public async Task<IActionResult> List()
        {
            ViewData["Title"] = "Menu Packages";
            return View(await _repo.GetByBranchAsync(CurrentBranchID, false));
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Menu Package";
            ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
            return View(new BanquetPackage { IsActive = true, IncludesMainCourse = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanquetPackage model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            if (await _repo.CodeExistsAsync(model.PackageCode, CurrentBranchID))
            {
                ModelState.AddModelError("PackageCode", "Package code already exists.");
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            model.BranchID  = CurrentBranchID;
            model.CreatedBy = CurrentUserId;
            await _repo.CreateAsync(model);
            TempData["SuccessMessage"] = "Package created.";
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _repo.GetByIdAsync(id);
            if (m == null) return NotFound();
            ViewData["Title"] = "Edit Package";
            ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BanquetPackage model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            if (await _repo.CodeExistsAsync(model.PackageCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError("PackageCode", "Package code already exists.");
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            model.UpdatedBy = CurrentUserId;
            await _repo.UpdateAsync(model);
            TempData["SuccessMessage"] = "Package updated.";
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Details(int id)
        {
            var m = await _repo.GetByIdAsync(id);
            if (m == null) return NotFound();
            ViewData["Title"] = "Package Details";
            return View(m);
        }

        // AJAX endpoint - returns package JSON for booking form
        [HttpGet]
        public async Task<IActionResult> GetPackageJson(int id)
        {
            var pkg = await _repo.GetByIdAsync(id);
            if (pkg == null) return NotFound();
            return Json(new
            {
                pkg.Id, pkg.PackageName, pkg.PackageType, pkg.PricePerPax,
                pkg.MinimumGuaranteePax, pkg.GSTPercent, pkg.CGSTPercent,
                pkg.SGSTPercent, pkg.IGSTPercent, pkg.SACCode,
                pkg.MenuDescription,
                pkg.IncludesStarter, pkg.IncludesMainCourse, pkg.IncludesDessert,
                pkg.IncludesBeverages, pkg.IncludesLive
            });
        }
    }

    [Authorize]
    public class BanquetAddonServiceMasterController : BaseController
    {
        private readonly IBanquetAddonServiceRepository _repo;
        private readonly IGstSlabRepository _gstSlabRepo;

        public BanquetAddonServiceMasterController(IBanquetAddonServiceRepository repo, IGstSlabRepository gstSlabRepo)
        {
            _repo = repo;
            _gstSlabRepo = gstSlabRepo;
        }

        public async Task<IActionResult> List()
        {
            ViewData["Title"] = "Addon Services";
            return View(await _repo.GetByBranchAsync(CurrentBranchID, false));
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Addon Service";
            ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
            return View(new BanquetAddonService { IsActive = true, RateType = "PerEvent" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BanquetAddonService model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            if (await _repo.CodeExistsAsync(model.ServiceCode, CurrentBranchID))
            {
                ModelState.AddModelError("ServiceCode", "Service code already exists.");
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            model.BranchID  = CurrentBranchID;
            model.CreatedBy = CurrentUserId;
            await _repo.CreateAsync(model);
            TempData["SuccessMessage"] = "Addon service created.";
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var m = await _repo.GetByIdAsync(id);
            if (m == null) return NotFound();
            ViewData["Title"] = "Edit Addon Service";
            ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
            return View(m);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BanquetAddonService model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            if (await _repo.CodeExistsAsync(model.ServiceCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError("ServiceCode", "Service code already exists.");
                ViewBag.GstSlabs = await _gstSlabRepo.GetAllAsync(CurrentBranchID);
                return View(model);
            }
            model.UpdatedBy = CurrentUserId;
            await _repo.UpdateAsync(model);
            TempData["SuccessMessage"] = "Addon service updated.";
            return RedirectToAction(nameof(List));
        }
    }
}
