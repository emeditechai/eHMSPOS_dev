using System.Security.Claims;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class AssetManagementController : BaseController
    {
        private readonly IAssetManagementRepository _assets;

        public AssetManagementController(IAssetManagementRepository assets)
        {
            _assets = assets;
        }

        private int? CurrentRoleId
        {
            get
            {
                var raw = User.FindFirstValue(ClaimTypes.Role);
                return int.TryParse(raw, out var roleId) ? roleId : null;
            }
        }

        private bool IsAdmin => CurrentRoleId == 1;
        private bool IsManagerOrAdmin => CurrentRoleId is 1 or 2;

        public async Task<IActionResult> Index(DateTime? fromDate = null, DateTime? toDate = null)
        {
            ViewData["Title"] = "Asset Management";

            var from = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (toDate ?? DateTime.Today).Date;
            if (to < from)
            {
                (from, to) = (to, from);
            }

            var summary = await _assets.GetDashboardSummaryAsync(CurrentBranchID, from, to);
            var stock = (await _assets.GetStockReportAsync(CurrentBranchID)).ToList();

            var lowStock = stock
                .Where(x => x.IsLowStock)
                .OrderBy(x => x.OnHandQty)
                .ThenBy(x => x.Name)
                .Take(10)
                .ToList();

            var negativeStock = stock
                .Where(x => x.OnHandQty < 0)
                .OrderBy(x => x.OnHandQty)
                .ThenBy(x => x.Name)
                .Take(10)
                .ToList();

            var recentMovements = (await _assets.GetMovementListAsync(CurrentBranchID, from, to))
                .OrderByDescending(x => x.MovementDate)
                .ThenByDescending(x => x.Id)
                .Take(10)
                .ToList();

            var vm = new AssetDashboardViewModel
            {
                FromDate = from,
                ToDate = to,
                Summary = summary,
                LowStockItems = lowStock,
                NegativeStockItems = negativeStock,
                RecentMovements = recentMovements
            };

            return View(vm);
        }

        // -------------------- Masters: Departments --------------------
        public async Task<IActionResult> Departments()
        {
            ViewData["Title"] = "Asset Departments";
            var rows = await _assets.GetDepartmentsAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> DepartmentDetails(int id)
        {
            ViewData["Title"] = "Department Details";
            var row = (await _assets.GetDepartmentsAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        public IActionResult CreateDepartment()
        {
            ViewData["Title"] = "Create Department";
            return View(new AssetDepartment { BranchID = CurrentBranchID, IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(AssetDepartment model)
        {
            model.BranchID = CurrentBranchID;
            model.CreatedBy = CurrentUserId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create Department";
                return View(model);
            }

            await _assets.CreateDepartmentAsync(model);
            TempData["SuccessMessage"] = "Department created successfully.";
            return RedirectToAction(nameof(Departments));
        }

        public async Task<IActionResult> EditDepartment(int id)
        {
            ViewData["Title"] = "Edit Department";
            var row = (await _assets.GetDepartmentsAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(AssetDepartment model)
        {
            model.BranchID = CurrentBranchID;
            model.UpdatedBy = CurrentUserId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Department";
                return View(model);
            }

            await _assets.UpdateDepartmentAsync(model);
            TempData["SuccessMessage"] = "Department updated successfully.";
            return RedirectToAction(nameof(Departments));
        }

        // -------------------- Masters: Units --------------------
        public async Task<IActionResult> Units()
        {
            ViewData["Title"] = "Asset Units";
            var rows = await _assets.GetUnitsAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> UnitDetails(int id)
        {
            ViewData["Title"] = "Unit Details";
            var row = (await _assets.GetUnitsAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        public IActionResult CreateUnit()
        {
            ViewData["Title"] = "Create Unit";
            return View(new AssetUnit { BranchID = CurrentBranchID, IsActive = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUnit(AssetUnit model)
        {
            model.BranchID = CurrentBranchID;
            model.CreatedBy = CurrentUserId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create Unit";
                return View(model);
            }

            await _assets.CreateUnitAsync(model);
            TempData["SuccessMessage"] = "Unit created successfully.";
            return RedirectToAction(nameof(Units));
        }

        public async Task<IActionResult> EditUnit(int id)
        {
            ViewData["Title"] = "Edit Unit";
            var row = (await _assets.GetUnitsAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUnit(AssetUnit model)
        {
            model.BranchID = CurrentBranchID;
            model.UpdatedBy = CurrentUserId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Unit";
                return View(model);
            }

            await _assets.UpdateUnitAsync(model);
            TempData["SuccessMessage"] = "Unit updated successfully.";
            return RedirectToAction(nameof(Units));
        }

        // -------------------- Masters: Makers --------------------
        public async Task<IActionResult> Makers()
        {
            ViewData["Title"] = "Asset Makers";
            var rows = await _assets.GetMakersAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> MakerDetails(int id)
        {
            ViewData["Title"] = "Maker Details";
            var row = (await _assets.GetMakersAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        public IActionResult CreateMaker()
        {
            ViewData["Title"] = "Create Maker";
            var model = new AssetMaker { BranchID = CurrentBranchID, IsActive = true };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaker(AssetMaker model)
        {
            model.BranchID = CurrentBranchID;
            model.CreatedBy = CurrentUserId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create Maker";
                return View(model);
            }

            if (await _assets.MakerNameExistsAsync(CurrentBranchID, model.Name, null))
            {
                ModelState.AddModelError(nameof(model.Name), "Maker already exists.");
                ViewData["Title"] = "Create Maker";
                return View(model);
            }

            await _assets.CreateMakerAsync(model);
            TempData["SuccessMessage"] = "Maker created successfully.";
            return RedirectToAction(nameof(Makers));
        }

        public async Task<IActionResult> EditMaker(int id)
        {
            ViewData["Title"] = "Edit Maker";
            var row = (await _assets.GetMakersAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMaker(AssetMaker model)
        {
            model.BranchID = CurrentBranchID;
            model.UpdatedBy = CurrentUserId;

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Maker";
                return View(model);
            }

            if (await _assets.MakerNameExistsAsync(CurrentBranchID, model.Name, model.Id))
            {
                ModelState.AddModelError(nameof(model.Name), "Maker already exists.");
                ViewData["Title"] = "Edit Maker";
                return View(model);
            }

            await _assets.UpdateMakerAsync(model);
            TempData["SuccessMessage"] = "Maker updated successfully.";
            return RedirectToAction(nameof(Makers));
        }

        // -------------------- Masters: Items --------------------
        public async Task<IActionResult> Items()
        {
            ViewData["Title"] = "Asset Item Master";
            var items = await _assets.GetItemLookupAsync(CurrentBranchID);
            return View(items);
        }

        public async Task<IActionResult> ItemDetails(int id)
        {
            ViewData["Title"] = "Item Details";
            var item = await _assets.GetItemByIdAsync(id, CurrentBranchID);
            if (item == null)
            {
                return NotFound();
            }

            var departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList();
            ViewBag.DepartmentNames = departments
                .Where(d => item.EligibleDepartmentIds.Contains(d.Id))
                .Select(d => d.Name)
                .ToList();

            return View(item);
        }

        public async Task<IActionResult> CreateItem()
        {
            ViewData["Title"] = "Create Asset Item";

            var vm = new AssetItemEditViewModel
            {
                Item = new AssetItem { BranchID = CurrentBranchID, IsActive = true, RequiresCustodian = true },
                Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList(),
                Units = (await _assets.GetUnitsAsync(CurrentBranchID)).ToList(),
                Makers = (await _assets.GetMakersAsync(CurrentBranchID)).ToList(),
                SelectedDepartmentIds = new List<int>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateItem(AssetItemEditViewModel vm)
        {
            vm.Item.BranchID = CurrentBranchID;
            vm.Item.CreatedBy = CurrentUserId;

            // Reload lookups
            vm.Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList();
            vm.Units = (await _assets.GetUnitsAsync(CurrentBranchID)).ToList();
            vm.Makers = (await _assets.GetMakersAsync(CurrentBranchID)).ToList();

            if (vm.Item.MakerId.HasValue && !vm.Makers.Any(m => m.Id == vm.Item.MakerId.Value))
            {
                ModelState.AddModelError("Item.MakerId", "Invalid maker.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Create Asset Item";
                return View(vm);
            }

            if (await _assets.ItemCodeExistsAsync(CurrentBranchID, vm.Item.Code, null))
            {
                ModelState.AddModelError("Item.Code", "Code already exists.");
                ViewData["Title"] = "Create Asset Item";
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.Item.Barcode) && await _assets.ItemBarcodeExistsAsync(CurrentBranchID, vm.Item.Barcode, null))
            {
                ModelState.AddModelError("Item.Barcode", "Barcode already exists.");
                ViewData["Title"] = "Create Asset Item";
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.Item.AssetTag) && await _assets.ItemAssetTagExistsAsync(CurrentBranchID, vm.Item.AssetTag, null))
            {
                ModelState.AddModelError("Item.AssetTag", "Asset tag already exists.");
                ViewData["Title"] = "Create Asset Item";
                return View(vm);
            }

            var id = await _assets.CreateItemAsync(vm.Item);
            await _assets.SetItemDepartmentsAsync(id, vm.SelectedDepartmentIds ?? new List<int>(), CurrentUserId);

            TempData["SuccessMessage"] = "Item created successfully.";
            return RedirectToAction(nameof(Items));
        }

        public async Task<IActionResult> EditItem(int id)
        {
            ViewData["Title"] = "Edit Asset Item";

            var item = await _assets.GetItemByIdAsync(id, CurrentBranchID);
            if (item == null)
            {
                return NotFound();
            }

            var vm = new AssetItemEditViewModel
            {
                Item = item,
                Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList(),
                Units = (await _assets.GetUnitsAsync(CurrentBranchID)).ToList(),
                Makers = (await _assets.GetMakersAsync(CurrentBranchID)).ToList(),
                SelectedDepartmentIds = item.EligibleDepartmentIds ?? new List<int>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditItem(AssetItemEditViewModel vm)
        {
            vm.Item.BranchID = CurrentBranchID;
            vm.Item.UpdatedBy = CurrentUserId;

            vm.Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList();
            vm.Units = (await _assets.GetUnitsAsync(CurrentBranchID)).ToList();
            vm.Makers = (await _assets.GetMakersAsync(CurrentBranchID)).ToList();

            if (vm.Item.MakerId.HasValue && !vm.Makers.Any(m => m.Id == vm.Item.MakerId.Value))
            {
                ModelState.AddModelError("Item.MakerId", "Invalid maker.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Edit Asset Item";
                return View(vm);
            }

            if (await _assets.ItemCodeExistsAsync(CurrentBranchID, vm.Item.Code, vm.Item.Id))
            {
                ModelState.AddModelError("Item.Code", "Code already exists.");
                ViewData["Title"] = "Edit Asset Item";
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.Item.Barcode) && await _assets.ItemBarcodeExistsAsync(CurrentBranchID, vm.Item.Barcode, vm.Item.Id))
            {
                ModelState.AddModelError("Item.Barcode", "Barcode already exists.");
                ViewData["Title"] = "Edit Asset Item";
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.Item.AssetTag) && await _assets.ItemAssetTagExistsAsync(CurrentBranchID, vm.Item.AssetTag, vm.Item.Id))
            {
                ModelState.AddModelError("Item.AssetTag", "Asset tag already exists.");
                ViewData["Title"] = "Edit Asset Item";
                return View(vm);
            }

            await _assets.UpdateItemAsync(vm.Item);
            await _assets.SetItemDepartmentsAsync(vm.Item.Id, vm.SelectedDepartmentIds ?? new List<int>(), CurrentUserId);

            TempData["SuccessMessage"] = "Item updated successfully.";
            return RedirectToAction(nameof(Items));
        }

        // -------------------- Masters: Consumable standards --------------------
        public async Task<IActionResult> ConsumableStandards()
        {
            ViewData["Title"] = "Consumable Standards";
            var rows = await _assets.GetConsumableStandardsAsync(CurrentBranchID);
            return View(rows);
        }

        public async Task<IActionResult> ConsumableStandardDetails(int id)
        {
            ViewData["Title"] = "Consumable Standard Details";
            var row = (await _assets.GetConsumableStandardsAsync(CurrentBranchID)).FirstOrDefault(x => x.Id == id);
            if (row == null)
            {
                return NotFound();
            }

            return View(row);
        }

        public async Task<IActionResult> UpsertConsumableStandard()
        {
            ViewData["Title"] = "Upsert Consumable Standard";

            var vm = new AssetConsumableStandard
            {
                BranchID = CurrentBranchID,
                IsActive = true
            };

            ViewBag.Items = await _assets.GetItemLookupAsync(CurrentBranchID);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertConsumableStandard(AssetConsumableStandard model)
        {
            model.BranchID = CurrentBranchID;
            model.UpdatedBy = CurrentUserId;
            model.CreatedBy = CurrentUserId;

            ViewBag.Items = await _assets.GetItemLookupAsync(CurrentBranchID);

            if (model.ItemId <= 0)
            {
                ModelState.AddModelError(nameof(model.ItemId), "Item is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Upsert Consumable Standard";
                return View(model);
            }

            await _assets.UpsertConsumableStandardAsync(model);
            TempData["SuccessMessage"] = "Consumable standard saved.";
            return RedirectToAction(nameof(ConsumableStandards));
        }

        // -------------------- Movements --------------------
        public async Task<IActionResult> CreateMovement()
        {
            ViewData["Title"] = "Stock Movement";

            var vm = new AssetMovementCreateViewModel
            {
                MovementType = AssetMovementType.OpeningStockIn,
                Items = (await _assets.GetItemLookupAsync(CurrentBranchID)).ToList(),
                Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList(),
                Rooms = (await _assets.GetRoomsLookupAsync(CurrentBranchID)).ToList(),
                Lines = new List<AssetMovementCreateLineViewModel> { new() }
            };

            ViewBag.IsAdmin = IsAdmin;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMovement(AssetMovementCreateViewModel vm)
        {
            ViewBag.IsAdmin = IsAdmin;

            // Reload lookups
            vm.Items = (await _assets.GetItemLookupAsync(CurrentBranchID)).ToList();
            vm.Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList();
            vm.Rooms = (await _assets.GetRoomsLookupAsync(CurrentBranchID)).ToList();

            var validLineExists = vm.Lines != null && vm.Lines.Any(l => l.ItemId > 0 && l.Qty > 0);
            if (!validLineExists)
            {
                ModelState.AddModelError("Lines", "At least one line is required.");
            }

            if (vm.AllowNegativeOverride && !IsAdmin)
            {
                vm.AllowNegativeOverride = false;
            }

            // Custodian enforcement: if any selected item requires custodian
            var itemLookup = vm.Items.ToDictionary(x => x.Id, x => x);
            var requiresCustodian = (vm.Lines ?? new List<AssetMovementCreateLineViewModel>())
                .Where(l => l.ItemId > 0 && l.Qty > 0)
                .Any(l => itemLookup.TryGetValue(l.ItemId, out var it) && it.RequiresCustodian);
            if (requiresCustodian && string.IsNullOrWhiteSpace(vm.CustodianName))
            {
                ModelState.AddModelError(nameof(vm.CustodianName), "Custodian is required for selected items.");
            }

            // Restrict movement types to those implemented in the UI
            var allowed = new HashSet<AssetMovementType>
            {
                AssetMovementType.OpeningStockIn,
                AssetMovementType.ReturnIn,
                AssetMovementType.DamageRecoveryIn,
                AssetMovementType.DepartmentIssueOut,
                AssetMovementType.RoomAllocationOut,
                AssetMovementType.GuestIssueOut,
                AssetMovementType.ConsumableUsageOut,
                AssetMovementType.AutoCheckoutConsumableOut
            };

            if (!allowed.Contains(vm.MovementType))
            {
                ModelState.AddModelError(nameof(vm.MovementType), "Unsupported movement type.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Stock Movement";
                if (vm.Lines == null || vm.Lines.Count == 0)
                {
                    vm.Lines = new List<AssetMovementCreateLineViewModel> { new() };
                }
                return View(vm);
            }

            var movement = new AssetMovement
            {
                BranchID = CurrentBranchID,
                MovementType = vm.MovementType,
                BookingNumber = vm.BookingNumber,
                RoomId = vm.RoomId,
                FromDepartmentId = vm.FromDepartmentId,
                ToDepartmentId = vm.ToDepartmentId,
                GuestName = vm.GuestName,
                CustodianName = vm.CustodianName,
                Notes = vm.Notes,
                AllowNegativeOverride = vm.AllowNegativeOverride,
                CreatedBy = CurrentUserId,
                Lines = (vm.Lines ?? new List<AssetMovementCreateLineViewModel>()).Select(l => new AssetMovementLine
                {
                    ItemId = l.ItemId,
                    Qty = l.Qty,
                    SerialNumber = l.SerialNumber,
                    AssetTag = l.AssetTag,
                    LineNote = l.LineNote
                }).ToList()
            };

            var (ok, error, movementId) = await _assets.CreateMovementAsync(movement);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, error ?? "Unable to create movement.");
                ViewData["Title"] = "Stock Movement";
                return View(vm);
            }

            TempData["SuccessMessage"] = $"Movement created (ID {movementId}).";
            return RedirectToAction(nameof(MovementAudit));
        }

        public async Task<IActionResult> MovementAudit(DateTime? fromDate = null, DateTime? toDate = null)
        {
            ViewData["Title"] = "Movement Audit";
            var rows = await _assets.GetMovementListAsync(CurrentBranchID, fromDate, toDate);
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            return View(rows);
        }

        public async Task<IActionResult> MovementDetails(int id)
        {
            ViewData["Title"] = "Movement Details";
            var movement = await _assets.GetMovementByIdAsync(id, CurrentBranchID);
            if (movement == null)
            {
                return NotFound();
            }

            return View(movement);
        }

        public async Task<IActionResult> StockReport()
        {
            ViewData["Title"] = "Stock Report";
            var rows = await _assets.GetStockReportAsync(CurrentBranchID);
            return View(rows);
        }

        // -------------------- Damage/Loss --------------------
        public async Task<IActionResult> DamageLoss(int? status = null)
        {
            ViewData["Title"] = "Damage/Loss";
            AssetDamageLossStatus? st = null;
            if (status.HasValue && Enum.IsDefined(typeof(AssetDamageLossStatus), status.Value))
            {
                st = (AssetDamageLossStatus)status.Value;
            }

            ViewBag.CanApprove = IsManagerOrAdmin;
            var rows = await _assets.GetDamageLossListAsync(CurrentBranchID, st);
            return View(rows);
        }

        public async Task<IActionResult> CreateDamageLoss()
        {
            ViewData["Title"] = "Log Damage/Loss";

            var vm = new AssetDamageLossCreateViewModel
            {
                IssueType = AssetIssueType.Damage,
                Items = (await _assets.GetItemLookupAsync(CurrentBranchID)).ToList(),
                Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList(),
                Rooms = (await _assets.GetRoomsLookupAsync(CurrentBranchID)).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDamageLoss(AssetDamageLossCreateViewModel vm)
        {
            vm.Items = (await _assets.GetItemLookupAsync(CurrentBranchID)).ToList();
            vm.Departments = (await _assets.GetDepartmentsAsync(CurrentBranchID)).ToList();
            vm.Rooms = (await _assets.GetRoomsLookupAsync(CurrentBranchID)).ToList();

            if (vm.ItemId <= 0)
            {
                ModelState.AddModelError(nameof(vm.ItemId), "Item is required.");
            }
            if (vm.Qty <= 0)
            {
                ModelState.AddModelError(nameof(vm.Qty), "Qty must be > 0.");
            }
            if (string.IsNullOrWhiteSpace(vm.Reason))
            {
                ModelState.AddModelError(nameof(vm.Reason), "Reason is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Log Damage/Loss";
                return View(vm);
            }

            var record = new AssetDamageLossRecord
            {
                BranchID = CurrentBranchID,
                ItemId = vm.ItemId,
                Qty = vm.Qty,
                IssueType = vm.IssueType,
                Reason = vm.Reason,
                DepartmentId = vm.DepartmentId,
                RoomId = vm.RoomId,
                BookingNumber = vm.BookingNumber,
                GuestName = vm.GuestName,
                ReportedBy = CurrentUserId,
                CreatedBy = CurrentUserId
            };

            await _assets.CreateDamageLossAsync(record);
            TempData["SuccessMessage"] = "Damage/Loss recorded.";
            return RedirectToAction(nameof(DamageLoss));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDamageLoss(int id)
        {
            if (!IsManagerOrAdmin)
            {
                return Forbid();
            }

            await _assets.ApproveDamageLossAsync(id, CurrentBranchID, CurrentUserId ?? 0);
            TempData["SuccessMessage"] = "Damage/Loss approved.";
            return RedirectToAction(nameof(DamageLoss));
        }

        public async Task<IActionResult> DamageLossDetails(int id)
        {
            ViewData["Title"] = "Damage/Loss Details";
            ViewBag.CanApprove = IsManagerOrAdmin;
            var record = await _assets.GetDamageLossByIdAsync(id, CurrentBranchID);
            if (record == null)
            {
                return NotFound();
            }

            return View(record);
        }

        public IActionResult CreateRecovery(int id)
        {
            ViewData["Title"] = "Record Recovery";
            return View(new AssetDamageLossRecoveryCreateViewModel { DamageLossId = id, RecoveryType = AssetRecoveryType.Cash });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRecovery(AssetDamageLossRecoveryCreateViewModel vm)
        {
            if (vm.Amount < 0)
            {
                ModelState.AddModelError(nameof(vm.Amount), "Amount cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Record Recovery";
                return View(vm);
            }

            // Note: Bill posting integration will be wired in next step (booking other charges).
            var rec = new AssetDamageLossRecovery
            {
                DamageLossId = vm.DamageLossId,
                RecoveryType = vm.RecoveryType,
                Amount = vm.Amount,
                Notes = vm.Notes,
                CreatedBy = CurrentUserId
            };

            await _assets.CreateRecoveryAsync(rec);
            TempData["SuccessMessage"] = "Recovery recorded.";
            return RedirectToAction(nameof(DamageLossDetails), new { id = vm.DamageLossId });
        }
    }
}
