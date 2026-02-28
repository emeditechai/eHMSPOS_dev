using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotelApp.Web.Controllers;

[Authorize]
public class UserManagementController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserBranchRoleRepository _userBranchRoleRepository;
    private readonly IWebHostEnvironment _env;

    public UserManagementController(
        IUserRepository userRepository,
        IUserBranchRepository userBranchRepository,
        IBranchRepository branchRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUserBranchRoleRepository userBranchRoleRepository,
        IWebHostEnvironment env)
    {
        _userRepository = userRepository;
        _userBranchRepository = userBranchRepository;
        _branchRepository = branchRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userBranchRoleRepository = userBranchRoleRepository;
        _env = env;
    }

    public async Task<IActionResult> Index(int? filterBranchId = null)
    {
        // Determine which branch's users to show
        IEnumerable<User> users;
        int effectiveBranchId;

        if (IsHOBranchAdmin)
        {
            // HO + Administrator: can filter by branch
            effectiveBranchId = filterBranchId ?? CurrentBranchID;
            users = effectiveBranchId > 0
                ? await _userRepository.GetUsersByBranchAsync(effectiveBranchId)
                : await _userRepository.GetAllUsersAsync();

            var allBranches = await _branchRepository.GetActiveBranchesAsync();
            ViewBag.AllBranches      = allBranches.ToList();
            ViewBag.FilterBranchId   = effectiveBranchId;
        }
        else
        {
            // Non-HO or non-Admin: show own branch only
            effectiveBranchId = CurrentBranchID;
            users = await _userRepository.GetUsersByBranchAsync(effectiveBranchId);
        }

        // Hide the superuser "admin" account from everyone except admin themselves
        var currentUsername = User.Identity?.Name ?? "";
        if (!currentUsername.Equals("admin", StringComparison.OrdinalIgnoreCase))
            users = users.Where(u => !u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));

        var usersWithBranchesAndRoles = new List<(User user, List<Branch> branches, List<Role> roles)>();
        foreach (var user in users)
        {
            var branches = (await _userBranchRepository.GetBranchesByUserIdAsync(user.Id)).ToList();
            var roles    = (await _userRoleRepository.GetRolesByUserIdAsync(user.Id)).ToList();
            usersWithBranchesAndRoles.Add((user, branches, roles));
        }

        ViewBag.UsersWithBranchesAndRoles = usersWithBranchesAndRoles;
        ViewBag.IsHOBranchAdmin = IsHOBranchAdmin;
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> GetUsersByBranch(int branchId)
    {
        if (!IsHOBranchAdmin)
            return Json(new { success = false, message = "Access denied" });

        var users = branchId == 0
            ? await _userRepository.GetAllUsersAsync()
            : await _userRepository.GetUsersByBranchAsync(branchId);

        // Hide the superuser "admin" from everyone except admin themselves
        var currentUsername = User.Identity?.Name ?? "";
        if (!currentUsername.Equals("admin", StringComparison.OrdinalIgnoreCase))
            users = users.Where(u => !u.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));

        var result = new List<object>();
        foreach (var user in users)
        {
            var branches = (await _userBranchRepository.GetBranchesByUserIdAsync(user.Id)).ToList();
            var roles    = (await _userRoleRepository.GetRolesByUserIdAsync(user.Id)).ToList();
            result.Add(new
            {
                id        = user.Id,
                username  = user.Username,
                fullName  = user.FullName ?? $"{user.FirstName} {user.LastName}",
                email     = user.Email,
                isActive  = user.IsActive,
                lastLogin = user.LastLoginDate.HasValue ? user.LastLoginDate.Value.ToString("dd MMM yyyy") : "Never",
                roles     = roles.Select(r => r.Name).ToList(),
                branches  = branches.Select(b => b.BranchCode).ToList()
            });
        }
        return Json(new { success = true, users = result });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        IEnumerable<Branch> branches;
        if (IsHOBranchAdmin)
        {
            branches = await _branchRepository.GetActiveBranchesAsync();
        }
        else
        {
            // Restrict to own branch only
            var ownBranch = await _branchRepository.GetBranchByIdAsync(CurrentBranchID);
            branches = ownBranch != null ? new[] { ownBranch } : Enumerable.Empty<Branch>();
        }
        var roles = await _roleRepository.GetAllRolesAsync();
        var model = new UserCreateViewModel
        {
            AvailableBranches    = branches.ToList(),
            AvailableRoles       = roles.ToList(),
            RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID,
            IsMultiBranchAllowed = IsHOBranchAdmin
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        // Parse branch-wise role assignments from JSON
        var branchRoleAssignments = ParseBranchRolesJson(model.BranchRolesJson);
        if (!branchRoleAssignments.Any() || branchRoleAssignments.All(a => !a.RoleIds.Any()))
        {
            ModelState.AddModelError("BranchRolesJson", "Please assign at least one role to at least one branch");
        }

        // Derive flat lists for backward compat
        model.SelectedBranchIds = branchRoleAssignments.Select(a => a.BranchId).Distinct().ToList();
        model.SelectedRoleIds   = branchRoleAssignments.SelectMany(a => a.RoleIds).Distinct().ToList();

        // Enforce: non-HO-Admin can only create users for their own branch
        if (!IsHOBranchAdmin && model.SelectedBranchIds.Any(b => b != CurrentBranchID))
        {
            ModelState.AddModelError("BranchRolesJson", "You can only create users for your own branch.");
        }

        async Task<(IEnumerable<Branch>, IEnumerable<Role>)> GetFormDataAsync()
        {
            IEnumerable<Branch> b = IsHOBranchAdmin
                ? await _branchRepository.GetActiveBranchesAsync()
                : new[] { (await _branchRepository.GetBranchByIdAsync(CurrentBranchID))! }.Where(x => x != null);
            var r = await _roleRepository.GetAllRolesAsync();
            return (b, r);
        }
        if (!ModelState.IsValid)
        {
            var (bList, rList) = await GetFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        if (await _userRepository.UsernameExistsAsync(model.Username))
        {
            ModelState.AddModelError("Username", "Username already exists");
            var (bList, rList) = await GetFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        if (await _userRepository.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Email already exists");
            var (bList, rList) = await GetFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            var (bList, rList) = await GetFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        var salt         = BCrypt.Net.BCrypt.GenerateSalt(12);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, salt);

        var user = new User
        {
            Username     = model.Username,
            Email        = model.Email,
            PasswordHash = passwordHash,
            Salt         = salt,
            FirstName    = model.FirstName,
            LastName     = model.LastName,
            FullName     = model.FullName ?? $"{model.FirstName} {model.LastName}",
            Phone        = model.PhoneNumber ?? model.Username,
            PhoneNumber  = model.PhoneNumber,
            Role         = model.SelectedRoleIds.FirstOrDefault(),
            BranchID     = model.SelectedBranchIds.First(),
            IsActive     = true
        };

        var userId = await _userRepository.CreateUserAsync(user);

        // Save profile picture if provided
        if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
        {
            var ext = Path.GetExtension(model.ProfilePictureFile.FileName).ToLowerInvariant();
            var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.ProfilePictureFile.CopyToAsync(stream);
            user.Id = userId;
            user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
            await _userRepository.UpdateUserAsync(user);
        }

        await _userBranchRepository.AssignBranchesToUserAsync(userId, model.SelectedBranchIds, CurrentUserId);
        await _userRoleRepository.AssignRolesToUserAsync(userId, model.SelectedRoleIds, CurrentUserId ?? 1);
        await _userBranchRoleRepository.SaveUserBranchRolesAsync(userId, branchRoleAssignments, CurrentUserId);

        TempData["SuccessMessage"] = "User created successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        // Non-HO-Admin can only edit users belonging to their own branch
        if (!IsHOBranchAdmin)
        {
            var userBranchList = (await _userBranchRepository.GetBranchesByUserIdAsync(id)).ToList();
            if (!userBranchList.Any(b => b.BranchID == CurrentBranchID))
                return Forbid();
        }

        var userBranches   = await _userBranchRepository.GetBranchesByUserIdAsync(id);
        var userRoles      = await _userRoleRepository.GetRolesByUserIdAsync(id);
        var allBranchRoles = (await _userBranchRoleRepository.GetByUserIdAsync(id)).ToList();

        IEnumerable<Branch> branches;
        if (IsHOBranchAdmin)
            branches = await _branchRepository.GetActiveBranchesAsync();
        else
        {
            var ownBranch = await _branchRepository.GetBranchByIdAsync(CurrentBranchID);
            branches = ownBranch != null ? new[] { ownBranch } : Enumerable.Empty<Branch>();
        }
        var roles = await _roleRepository.GetAllRolesAsync();

        // Build existing branch-role assignments
        var existingAssignments = allBranchRoles
            .GroupBy(ubr => ubr.BranchID)
            .Select(g => new UserBranchRoleAssignment
            {
                BranchId = g.Key,
                RoleIds  = g.Select(x => x.RoleId).ToList()
            }).ToList();

        // Fall back to global roles x branches if no branch-wise data yet
        if (!existingAssignments.Any())
        {
            var globalRoleIds = userRoles.Select(r => r.Id).ToList();
            existingAssignments = userBranches.Select(b => new UserBranchRoleAssignment
            {
                BranchId = b.BranchID,
                RoleIds  = new List<int>(globalRoleIds)
            }).ToList();
        }

        var jsonDict     = existingAssignments.ToDictionary(a => a.BranchId.ToString(), a => a.RoleIds);
        var branchRolesJson = JsonSerializer.Serialize(jsonDict);

        var model = new UserEditViewModel
        {
            Id                = user.Id,
            Username          = user.Username,
            Email             = user.Email,
            FirstName         = user.FirstName ?? string.Empty,
            LastName          = user.LastName  ?? string.Empty,
            FullName          = user.FullName,
            PhoneNumber       = user.PhoneNumber,
            Role              = user.Role,
            IsActive          = user.IsActive,
            SelectedBranchIds = userBranches.Select(b => b.BranchID).ToList(),
            SelectedRoleIds   = userRoles.Select(r => r.Id).ToList(),
            ExistingBranchRoleAssignments = existingAssignments,
            BranchRolesJson   = branchRolesJson,
            ProfilePicturePath = user.ProfilePicturePath,
            AvailableBranches    = branches.ToList(),
            AvailableRoles       = roles.ToList(),
            RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID,
            IsMultiBranchAllowed = IsHOBranchAdmin
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        // Parse branch-wise role assignments
        var branchRoleAssignments = ParseBranchRolesJson(model.BranchRolesJson);
        if (!branchRoleAssignments.Any() || branchRoleAssignments.All(a => !a.RoleIds.Any()))
        {
            ModelState.AddModelError("BranchRolesJson", "Please assign at least one role to at least one branch");
        }

        model.SelectedBranchIds = branchRoleAssignments.Select(a => a.BranchId).Distinct().ToList();
        model.SelectedRoleIds   = branchRoleAssignments.SelectMany(a => a.RoleIds).Distinct().ToList();

        // Enforce: non-HO-Admin can only assign own branch
        if (!IsHOBranchAdmin && model.SelectedBranchIds.Any(b => b != CurrentBranchID))
        {
            ModelState.AddModelError("BranchRolesJson", "You can only assign your own branch to this user.");
        }

        async Task<(IEnumerable<Branch>, IEnumerable<Role>)> GetEditFormDataAsync()
        {
            IEnumerable<Branch> b = IsHOBranchAdmin
                ? await _branchRepository.GetActiveBranchesAsync()
                : new[] { (await _branchRepository.GetBranchByIdAsync(CurrentBranchID))! }.Where(x => x != null);
            var r = await _roleRepository.GetAllRolesAsync();
            return (b, r);
        }

        if (!ModelState.IsValid)
        {
            var (bList, rList) = await GetEditFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.ExistingBranchRoleAssignments = branchRoleAssignments;
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        var user = await _userRepository.GetByIdAsync(model.Id);
        if (user == null) return NotFound();

        if (await _userRepository.UsernameExistsAsync(model.Username, model.Id))
        {
            ModelState.AddModelError("Username", "Username already exists");
            var (bList, rList) = await GetEditFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.ExistingBranchRoleAssignments = branchRoleAssignments;
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        if (await _userRepository.EmailExistsAsync(model.Email, model.Id))
        {
            ModelState.AddModelError("Email", "Email already exists");
            var (bList, rList) = await GetEditFormDataAsync();
            model.AvailableBranches    = bList.ToList();
            model.AvailableRoles       = rList.ToList();
            model.ExistingBranchRoleAssignments = branchRoleAssignments;
            model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
            model.IsMultiBranchAllowed = IsHOBranchAdmin;
            return View(model);
        }

        if (!string.IsNullOrEmpty(model.Password))
        {
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                var (bList, rList) = await GetEditFormDataAsync();
                model.AvailableBranches    = bList.ToList();
                model.AvailableRoles       = rList.ToList();
                model.ExistingBranchRoleAssignments = branchRoleAssignments;
                model.RestrictToBranchId   = IsHOBranchAdmin ? (int?)null : CurrentBranchID;
                model.IsMultiBranchAllowed = IsHOBranchAdmin;
                return View(model);
            }
            var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, salt);
            user.Salt = salt;
        }

        user.Username    = model.Username;
        user.Email       = model.Email;
        user.FirstName   = model.FirstName;
        user.LastName    = model.LastName;
        user.FullName    = model.FullName ?? $"{model.FirstName} {model.LastName}";
        user.PhoneNumber = model.PhoneNumber;
        user.Role        = model.SelectedRoleIds.FirstOrDefault();
        user.IsActive    = model.IsActive;
        user.BranchID    = model.SelectedBranchIds.FirstOrDefault();

        // Handle profile picture upload
        if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
        {
            var ext = Path.GetExtension(model.ProfilePictureFile.FileName).ToLowerInvariant();
            var fileName = $"{user.Id}_{Guid.NewGuid():N}{ext}";
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await model.ProfilePictureFile.CopyToAsync(stream);
            user.ProfilePicturePath = $"/uploads/profiles/{fileName}";
        }
        // else keep existing ProfilePicturePath already on user object

        await _userRepository.UpdateUserAsync(user);
        await _userBranchRepository.AssignBranchesToUserAsync(user.Id, model.SelectedBranchIds, CurrentUserId ?? 1);
        await _userRoleRepository.AssignRolesToUserAsync(user.Id, model.SelectedRoleIds, CurrentUserId ?? 1);
        await _userBranchRoleRepository.SaveUserBranchRolesAsync(user.Id, branchRoleAssignments, CurrentUserId);

        TempData["SuccessMessage"] = "User updated successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        var branches        = (await _userBranchRepository.GetBranchesByUserIdAsync(id)).ToList();
        var roles           = (await _userRoleRepository.GetRolesByUserIdAsync(id)).ToList();
        var branchRoleItems = (await _userBranchRoleRepository.GetByUserIdAsync(id)).ToList();
        ViewBag.UserBranches    = branches;
        ViewBag.UserRoles       = roles;
        ViewBag.UserBranchRoles = branchRoleItems;
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        await _userRepository.DeleteUserAsync(id);
        TempData["SuccessMessage"] = "User deleted successfully";
        return RedirectToAction(nameof(Index));
    }

    private static List<UserBranchRoleAssignment> ParseBranchRolesJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}") return new();
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<int>>>(json);
            if (dict == null) return new();
            return dict
                .Where(kv => int.TryParse(kv.Key, out _) && kv.Value?.Any() == true)
                .Select(kv => new UserBranchRoleAssignment
                {
                    BranchId = int.Parse(kv.Key),
                    RoleIds  = kv.Value
                }).ToList();
        }
        catch { return new(); }
    }
}
