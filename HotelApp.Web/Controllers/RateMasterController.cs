using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using System.Security.Claims;

namespace HotelApp.Web.Controllers
{
    [Authorize]
    public class RateMasterController : BaseController
    {
        private readonly IRateMasterRepository _rateMasterRepository;
        private readonly IRoomRepository _roomRepository;

        public RateMasterController(IRateMasterRepository rateMasterRepository, IRoomRepository roomRepository)
        {
            _rateMasterRepository = rateMasterRepository;
            _roomRepository = roomRepository;
        }

        // GET: RateMaster/List
        public async Task<IActionResult> List()
        {
            var rates = await _rateMasterRepository.GetByBranchAsync(CurrentBranchID);
            return View(rates);
        }

        // GET: RateMaster/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            
            var model = new RateMaster
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                IsWeekdayRate = true,
                IsDynamicRate = false,
                IsActive = true
            };
            
            return View(model);
        }

        // POST: RateMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RateMaster rate, List<WeekendRate> weekendRates, List<SpecialDayRate> specialDayRates)
        {
            if (rate.EndDate < rate.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userId, out int userIdInt))
                {
                    rate.CreatedBy = userIdInt;
                }
                rate.BranchID = CurrentBranchID;
                
                // Create the main rate
                var createdRateId = await _rateMasterRepository.CreateAsync(rate);
                
                // Create weekend rates if any
                if (weekendRates != null && weekendRates.Any())
                {
                    foreach (var weekendRate in weekendRates)
                    {
                        weekendRate.RateMasterId = createdRateId;
                        weekendRate.CreatedBy = userIdInt;
                        weekendRate.CreatedDate = DateTime.Now;
                        weekendRate.LastModifiedDate = DateTime.Now;
                        await _rateMasterRepository.CreateWeekendRateAsync(weekendRate);
                    }
                }
                
                // Create special day rates if any
                if (specialDayRates != null && specialDayRates.Any())
                {
                    foreach (var specialDayRate in specialDayRates)
                    {
                        specialDayRate.RateMasterId = createdRateId;
                        specialDayRate.CreatedBy = userIdInt;
                        specialDayRate.CreatedDate = DateTime.Now;
                        specialDayRate.LastModifiedDate = DateTime.Now;
                        await _rateMasterRepository.CreateSpecialDayRateAsync(specialDayRate);
                    }
                }
                
                TempData["SuccessMessage"] = "Rate created successfully with weekend/special day rates!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(rate);
        }

        // GET: RateMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var rate = await _rateMasterRepository.GetByIdAsync(id);
            if (rate == null)
            {
                return NotFound();
            }

            // Load weekend and special day rates
            var weekendRates = await _rateMasterRepository.GetWeekendRatesByRateMasterIdAsync(id);
            var specialDayRates = await _rateMasterRepository.GetSpecialDayRatesByRateMasterIdAsync(id);

            // Create ViewModel
            var viewModel = new RateMasterViewModel
            {
                Id = rate.Id,
                RoomTypeId = rate.RoomTypeId,
                CustomerType = rate.CustomerType,
                Source = rate.Source,
                BaseRate = rate.BaseRate,
                ExtraPaxRate = rate.ExtraPaxRate,
                TaxPercentage = rate.TaxPercentage,
                ApplyDiscount = rate.ApplyDiscount,
                StartDate = rate.StartDate,
                EndDate = rate.EndDate,
                IsWeekdayRate = rate.IsWeekdayRate,
                IsDynamicRate = rate.IsDynamicRate,
                IsActive = rate.IsActive,
                BranchID = rate.BranchID,
                CreatedDate = rate.CreatedDate,
                CreatedBy = rate.CreatedBy,
                LastModifiedDate = rate.LastModifiedDate,
                HasWeekendRates = weekendRates.Any(),
                WeekendRates = new List<RateMasterViewModel.WeekendRateItem>
                {
                    new RateMasterViewModel.WeekendRateItem { DayOfWeek = "Friday", IsSelected = false, BaseRate = 0, ExtraPaxRate = 0 },
                    new RateMasterViewModel.WeekendRateItem { DayOfWeek = "Saturday", IsSelected = false, BaseRate = 0, ExtraPaxRate = 0 },
                    new RateMasterViewModel.WeekendRateItem { DayOfWeek = "Sunday", IsSelected = false, BaseRate = 0, ExtraPaxRate = 0 }
                },
                HasSpecialDayRates = specialDayRates.Any(),
                SpecialDayRates = specialDayRates.Select(s => new RateMasterViewModel.SpecialDayRateItem
                {
                    Id = s.Id,
                    FromDate = s.FromDate,
                    ToDate = s.ToDate,
                    EventName = s.EventName,
                    BaseRate = s.BaseRate,
                    ExtraPaxRate = s.ExtraPaxRate
                }).ToList()
            };

            // Update weekend rates with existing data
            foreach (var weekendRate in weekendRates)
            {
                var item = viewModel.WeekendRates.FirstOrDefault(w => w.DayOfWeek == weekendRate.DayOfWeek);
                if (item != null)
                {
                    item.Id = weekendRate.Id;
                    item.IsSelected = true;
                    item.BaseRate = weekendRate.BaseRate;
                    item.ExtraPaxRate = weekendRate.ExtraPaxRate;
                }
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(viewModel);
        }

        // GET: RateMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var rate = await _rateMasterRepository.GetByIdAsync(id);
            if (rate == null)
            {
                return NotFound();
            }

            // Load weekend and special day rates
            var weekendRates = await _rateMasterRepository.GetWeekendRatesByRateMasterIdAsync(id);
            var specialDayRates = await _rateMasterRepository.GetSpecialDayRatesByRateMasterIdAsync(id);

            // Create ViewModel
            var viewModel = new RateMasterViewModel
            {
                Id = rate.Id,
                RoomTypeId = rate.RoomTypeId,
                CustomerType = rate.CustomerType,
                Source = rate.Source,
                BaseRate = rate.BaseRate,
                ExtraPaxRate = rate.ExtraPaxRate,
                TaxPercentage = rate.TaxPercentage,
                ApplyDiscount = rate.ApplyDiscount,
                StartDate = rate.StartDate,
                EndDate = rate.EndDate,
                IsWeekdayRate = rate.IsWeekdayRate,
                IsDynamicRate = rate.IsDynamicRate,
                IsActive = rate.IsActive,
                BranchID = rate.BranchID,
                CreatedDate = rate.CreatedDate,
                CreatedBy = rate.CreatedBy,
                LastModifiedDate = rate.LastModifiedDate,
                HasWeekendRates = weekendRates.Any(),
                WeekendRates = new List<RateMasterViewModel.WeekendRateItem>
                {
                    new RateMasterViewModel.WeekendRateItem { DayOfWeek = "Friday", IsSelected = false, BaseRate = 0, ExtraPaxRate = 0 },
                    new RateMasterViewModel.WeekendRateItem { DayOfWeek = "Saturday", IsSelected = false, BaseRate = 0, ExtraPaxRate = 0 },
                    new RateMasterViewModel.WeekendRateItem { DayOfWeek = "Sunday", IsSelected = false, BaseRate = 0, ExtraPaxRate = 0 }
                },
                HasSpecialDayRates = specialDayRates.Any(),
                SpecialDayRates = specialDayRates.Select(s => new RateMasterViewModel.SpecialDayRateItem
                {
                    Id = s.Id,
                    FromDate = s.FromDate,
                    ToDate = s.ToDate,
                    EventName = s.EventName,
                    BaseRate = s.BaseRate,
                    ExtraPaxRate = s.ExtraPaxRate
                }).ToList()
            };

            // Update weekend rates with existing data
            foreach (var weekendRate in weekendRates)
            {
                var item = viewModel.WeekendRates.FirstOrDefault(w => w.DayOfWeek == weekendRate.DayOfWeek);
                if (item != null)
                {
                    item.Id = weekendRate.Id;
                    item.IsSelected = true;
                    item.BaseRate = weekendRate.BaseRate;
                    item.ExtraPaxRate = weekendRate.ExtraPaxRate;
                }
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            ViewBag.IsReadOnly = true;
            ViewData["Title"] = "View Rate";
            return View("Edit", viewModel);
        }

        // POST: RateMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RateMasterViewModel viewModel, List<WeekendRate> weekendRates, List<SpecialDayRate> specialDayRates)
        {
            if (id != viewModel.Id)
            {
                return BadRequest();
            }

            if (viewModel.EndDate < viewModel.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            if (ModelState.IsValid)
            {
                // Update main rate
                var rate = new RateMaster
                {
                    Id = viewModel.Id,
                    RoomTypeId = viewModel.RoomTypeId,
                    CustomerType = viewModel.CustomerType,
                    Source = viewModel.Source,
                    BaseRate = viewModel.BaseRate,
                    ExtraPaxRate = viewModel.ExtraPaxRate,
                    TaxPercentage = viewModel.TaxPercentage,
                    ApplyDiscount = viewModel.ApplyDiscount,
                    StartDate = viewModel.StartDate,
                    EndDate = viewModel.EndDate,
                    IsWeekdayRate = viewModel.IsWeekdayRate,
                    IsDynamicRate = viewModel.IsDynamicRate,
                    IsActive = viewModel.IsActive,
                    BranchID = viewModel.BranchID,
                    CreatedDate = viewModel.CreatedDate,
                    CreatedBy = viewModel.CreatedBy,
                    LastModifiedBy = GetCurrentUserId()
                };

                await _rateMasterRepository.UpdateAsync(rate);

                // Get existing weekend and special day rates
                var existingWeekendRates = await _rateMasterRepository.GetWeekendRatesByRateMasterIdAsync(id);
                var existingSpecialDayRates = await _rateMasterRepository.GetSpecialDayRatesByRateMasterIdAsync(id);

                // Handle weekend rates
                if (viewModel.HasWeekendRates && weekendRates != null && weekendRates.Any())
                {
                    foreach (var weekendRate in weekendRates.Where(w => w.BaseRate > 0))
                    {
                        var existing = existingWeekendRates.FirstOrDefault(e => e.DayOfWeek == weekendRate.DayOfWeek);
                        if (existing != null)
                        {
                            // Update existing
                            existing.BaseRate = weekendRate.BaseRate;
                            existing.ExtraPaxRate = weekendRate.ExtraPaxRate;
                            existing.LastModifiedBy = GetCurrentUserId();
                            await _rateMasterRepository.UpdateWeekendRateAsync(existing);
                        }
                        else
                        {
                            // Create new
                            weekendRate.RateMasterId = id;
                            weekendRate.IsActive = true;
                            weekendRate.CreatedBy = GetCurrentUserId();
                            await _rateMasterRepository.CreateWeekendRateAsync(weekendRate);
                        }
                    }

                    // Delete rates that are no longer selected
                    var selectedDays = weekendRates.Where(w => w.BaseRate > 0).Select(w => w.DayOfWeek).ToList();
                    foreach (var existing in existingWeekendRates.Where(e => !selectedDays.Contains(e.DayOfWeek)))
                    {
                        await _rateMasterRepository.DeleteWeekendRateAsync(existing.Id);
                    }
                }
                else
                {
                    // Delete all weekend rates if checkbox is unchecked
                    foreach (var existing in existingWeekendRates)
                    {
                        await _rateMasterRepository.DeleteWeekendRateAsync(existing.Id);
                    }
                }

                // Handle special day rates
                if (viewModel.HasSpecialDayRates && specialDayRates != null && specialDayRates.Any())
                {
                    foreach (var specialDayRate in specialDayRates.Where(s => s.FromDate != default && s.ToDate != default && s.BaseRate > 0))
                    {
                        if (specialDayRate.Id > 0)
                        {
                            // Update existing
                            var existing = existingSpecialDayRates.FirstOrDefault(e => e.Id == specialDayRate.Id);
                            if (existing != null)
                            {
                                existing.FromDate = specialDayRate.FromDate;
                                existing.ToDate = specialDayRate.ToDate;
                                existing.EventName = specialDayRate.EventName;
                                existing.BaseRate = specialDayRate.BaseRate;
                                existing.ExtraPaxRate = specialDayRate.ExtraPaxRate;
                                existing.LastModifiedBy = GetCurrentUserId();
                                await _rateMasterRepository.UpdateSpecialDayRateAsync(existing);
                            }
                        }
                        else
                        {
                            // Create new
                            specialDayRate.RateMasterId = id;
                            specialDayRate.IsActive = true;
                            specialDayRate.CreatedBy = GetCurrentUserId();
                            await _rateMasterRepository.CreateSpecialDayRateAsync(specialDayRate);
                        }
                    }

                    // Delete rates that are no longer in the list
                    var submittedIds = specialDayRates.Where(s => s.Id > 0).Select(s => s.Id).ToList();
                    foreach (var existing in existingSpecialDayRates.Where(e => !submittedIds.Contains(e.Id)))
                    {
                        await _rateMasterRepository.DeleteSpecialDayRateAsync(existing.Id);
                    }
                }
                else
                {
                    // Delete all special day rates if checkbox is unchecked
                    foreach (var existing in existingSpecialDayRates)
                    {
                        await _rateMasterRepository.DeleteSpecialDayRateAsync(existing.Id);
                    }
                }

                TempData["SuccessMessage"] = "Rate updated successfully!";
                return RedirectToAction(nameof(List));
            }

            ViewBag.RoomTypes = await _roomRepository.GetRoomTypesByBranchAsync(CurrentBranchID);
            ViewBag.CustomerTypes = await _rateMasterRepository.GetCustomerTypesAsync();
            ViewBag.Sources = await _rateMasterRepository.GetSourcesAsync();
            return View(viewModel);
        }

        // Delete functionality removed (business rule: no deletions)
    }
}
