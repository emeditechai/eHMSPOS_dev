using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers
{
    public class BranchMasterController : Controller
    {
        private readonly IBranchRepository _branchRepository;
        private readonly ILogger<BranchMasterController> _logger;

        public BranchMasterController(
            IBranchRepository branchRepository,
            ILogger<BranchMasterController> logger)
        {
            _branchRepository = branchRepository;
            _logger = logger;
        }

        // GET: BranchMaster
        public async Task<IActionResult> Index()
        {
            try
            {
                var branches = await _branchRepository.GetAllBranchesAsync();
                return View(branches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches");
                TempData["ErrorMessage"] = "Error loading branches. Please try again.";
                return View(new List<Branch>());
            }
        }

        // GET: BranchMaster/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var branch = await _branchRepository.GetBranchByIdAsync(id);
                if (branch == null)
                {
                    TempData["ErrorMessage"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch details");
                TempData["ErrorMessage"] = "Error loading branch details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: BranchMaster/Create
        public IActionResult Create()
        {
            return View(new Branch());
        }

        // POST: BranchMaster/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Branch branch)
        {
            try
            {
                // Validate branch code uniqueness
                if (await _branchRepository.BranchCodeExistsAsync(branch.BranchCode))
                {
                    ModelState.AddModelError("BranchCode", "Branch code already exists.");
                    return View(branch);
                }

                // Validate branch name uniqueness
                if (await _branchRepository.BranchNameExistsAsync(branch.BranchName))
                {
                    ModelState.AddModelError("BranchName", "Branch name already exists.");
                    return View(branch);
                }

                if (ModelState.IsValid)
                {
                    // Get current user ID from session
                    var userId = HttpContext.Session.GetInt32("UserId");
                    
                    branch.CreatedBy = userId;
                    branch.CreatedDate = DateTime.Now;
                    branch.IsActive = true;

                    var branchId = await _branchRepository.CreateBranchAsync(branch);
                    
                    TempData["SuccessMessage"] = "Branch created successfully.";
                    return RedirectToAction(nameof(Index));
                }

                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch");
                TempData["ErrorMessage"] = "Error creating branch. Please try again.";
                return View(branch);
            }
        }

        // GET: BranchMaster/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var branch = await _branchRepository.GetBranchByIdAsync(id);
                if (branch == null)
                {
                    TempData["ErrorMessage"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch for edit");
                TempData["ErrorMessage"] = "Error loading branch. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BranchMaster/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Branch branch)
        {
            if (id != branch.BranchID)
            {
                return NotFound();
            }

            try
            {
                // Validate branch code uniqueness (excluding current branch)
                if (await _branchRepository.BranchCodeExistsAsync(branch.BranchCode, branch.BranchID))
                {
                    ModelState.AddModelError("BranchCode", "Branch code already exists.");
                    return View(branch);
                }

                // Validate branch name uniqueness (excluding current branch)
                if (await _branchRepository.BranchNameExistsAsync(branch.BranchName, branch.BranchID))
                {
                    ModelState.AddModelError("BranchName", "Branch name already exists.");
                    return View(branch);
                }

                if (ModelState.IsValid)
                {
                    // Get current user ID from session
                    var userId = HttpContext.Session.GetInt32("UserId");
                    
                    branch.ModifiedBy = userId;
                    branch.ModifiedDate = DateTime.Now;

                    var result = await _branchRepository.UpdateBranchAsync(branch);
                    
                    if (result)
                    {
                        TempData["SuccessMessage"] = "Branch updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Error updating branch.";
                        return View(branch);
                    }
                }

                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating branch");
                TempData["ErrorMessage"] = "Error updating branch. Please try again.";
                return View(branch);
            }
        }

        // GET: BranchMaster/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var branch = await _branchRepository.GetBranchByIdAsync(id);
                if (branch == null)
                {
                    TempData["ErrorMessage"] = "Branch not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branch for delete");
                TempData["ErrorMessage"] = "Error loading branch. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BranchMaster/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _branchRepository.DeleteBranchAsync(id);
                
                if (result)
                {
                    TempData["SuccessMessage"] = "Branch deactivated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deactivating branch.";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting branch");
                TempData["ErrorMessage"] = "Error deactivating branch. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
