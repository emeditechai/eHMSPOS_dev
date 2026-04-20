using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class GstSlabMasterController : BaseController
    {
        private readonly IGstSlabRepository _gstSlabRepository;

        public GstSlabMasterController(IGstSlabRepository gstSlabRepository)
        {
            _gstSlabRepository = gstSlabRepository;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "GST Slab Master";
            var rows = await _gstSlabRepository.GetAllAsync(CurrentBranchID);
            return View(rows);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Create GST Slab";
            return View(new GstSlab
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true,
                TariffBands = new List<GstSlabBand>
                {
                    new() { IsActive = true, SortOrder = 1 }
                }
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GstSlab model)
        {
            Normalize(model);
            Validate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _gstSlabRepository.CodeExistsAsync(model.SlabCode, CurrentBranchID))
            {
                ModelState.AddModelError(nameof(model.SlabCode), "Slab code already exists.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.CreatedBy = GetCurrentUserId();
            await _gstSlabRepository.CreateAsync(model);
            TempData["SuccessMessage"] = "GST slab created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var row = await _gstSlabRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            if (!row.TariffBands.Any())
            {
                row.TariffBands.Add(new GstSlabBand { IsActive = true, SortOrder = 1 });
            }

            ViewData["Title"] = "Edit GST Slab";
            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GstSlab model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            Normalize(model);
            Validate(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _gstSlabRepository.CodeExistsAsync(model.SlabCode, CurrentBranchID, model.Id))
            {
                ModelState.AddModelError(nameof(model.SlabCode), "Slab code already exists.");
                return View(model);
            }

            model.BranchID = CurrentBranchID;
            model.UpdatedBy = GetCurrentUserId();
            await _gstSlabRepository.UpdateAsync(model);
            TempData["SuccessMessage"] = "GST slab updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var row = await _gstSlabRepository.GetByIdAsync(id);
            if (row == null || row.BranchID != CurrentBranchID)
            {
                return NotFound();
            }

            ViewData["Title"] = "View GST Slab";
            ViewBag.IsReadOnly = true;
            return View("Edit", row);
        }

        private static void Normalize(GstSlab model)
        {
            model.SlabCode = (model.SlabCode ?? string.Empty).Trim();
            model.SlabName = (model.SlabName ?? string.Empty).Trim();
            var rows = model.TariffBands ?? new List<GstSlabBand>();
            model.TariffBands = rows
                .Where(row => row.TariffFrom > 0
                    || row.TariffTo.HasValue
                    || row.GstPercent > 0
                    || row.CgstPercent > 0
                    || row.SgstPercent > 0
                    || row.IgstPercent > 0
                    || row.IsActive)
                .Select((row, index) =>
                {
                    var gst = Math.Max(0, row.GstPercent);
                    var half = Math.Round(gst / 2m, 2, MidpointRounding.AwayFromZero);
                    return new GstSlabBand
                    {
                        Id = row.Id,
                        GstSlabId = row.GstSlabId,
                        TariffFrom = row.TariffFrom,
                        TariffTo = row.TariffTo,
                        GstPercent = gst,
                        CgstPercent = half,
                        SgstPercent = half,
                        IgstPercent = gst,
                        SortOrder = index + 1,
                        IsActive = row.IsActive
                    };
                })
                .ToList();
        }

        private void Validate(GstSlab model)
        {
            if (!model.TariffBands.Any())
            {
                ModelState.AddModelError(nameof(model.TariffBands), "At least one GST tariff slab is required.");
            }

            if (model.EffectiveTo.HasValue && model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
            {
                ModelState.AddModelError(nameof(model.EffectiveTo), "Effective to date must be on or after effective from date.");
            }

            var orderedBands = model.TariffBands.OrderBy(row => row.TariffFrom).ThenBy(row => row.SortOrder).ToList();
            for (var index = 0; index < orderedBands.Count; index++)
            {
                var band = orderedBands[index];
                var prefix = $"TariffBands[{index}]";

                if (band.TariffTo.HasValue && band.TariffTo.Value < band.TariffFrom)
                {
                    ModelState.AddModelError($"{prefix}.TariffTo", "Tariff to must be greater than or equal to tariff from.");
                }

                if (index > 0)
                {
                    var previousBand = orderedBands[index - 1];
                    if (!previousBand.TariffTo.HasValue)
                    {
                        ModelState.AddModelError($"{prefix}.TariffFrom", "No tariff band can be added after an open-ended slab.");
                    }
                    else if (band.TariffFrom <= previousBand.TariffTo.Value)
                    {
                        ModelState.AddModelError($"{prefix}.TariffFrom", "Tariff bands must not overlap. Start the next band above the previous tariff to value.");
                    }
                }
            }
        }
    }
}