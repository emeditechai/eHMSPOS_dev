using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Services;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using System.Security.Cryptography;

namespace HotelApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserBranchRoleRepository _userBranchRoleRepository;

    public AccountController(IAuthService authService, IBranchRepository branchRepository, IUserBranchRepository userBranchRepository, IUserRoleRepository userRoleRepository, IUserRepository userRepository, IUserBranchRoleRepository userBranchRoleRepository)
    {
        _authService = authService;
        _branchRepository = branchRepository;
        _userBranchRepository = userBranchRepository;
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _userBranchRoleRepository = userBranchRoleRepository;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? reason = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (reason == "timeout")
            TempData["SessionMessage"] = "Your session expired due to inactivity. Please log in again.";
        else if (reason == "session")
            TempData["SessionMessage"] = "Your session is incomplete. Please log in and select a branch and role.";

        return View(new LoginViewModel 
        { 
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Validate credentials
        var user = await _authService.ValidateCredentialsAsync(model.Username, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return View(model);
        }

        // Store authenticated user info in TempData for branch selection
        TempData["AuthenticatedUserId"] = user.Id;
        TempData["AuthenticatedUsername"] = user.Username;
        TempData["AuthenticatedUserEmail"] = user.Email;
        TempData["AuthenticatedUserFullName"] = user.FullName ?? $"{user.FirstName} {user.LastName}".Trim();
        TempData["AuthenticatedUserRole"] = user.Role;
        TempData["AuthenticatedUserProfilePicturePath"] = user.ProfilePicturePath ?? "";
        TempData["ReturnUrl"] = model.ReturnUrl;

        // Redirect to branch selection
        return RedirectToAction(nameof(SelectBranch));
    }

    [HttpGet]
    public async Task<IActionResult> SelectBranch()
    {
        // Check if user is authenticated via TempData
        if (TempData["AuthenticatedUserId"] == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!int.TryParse(TempData["AuthenticatedUserId"]?.ToString(), out var userId))
        {
            return RedirectToAction(nameof(Login));
        }

        // Keep TempData for POST
        TempData.Keep();

        // Load only branches assigned to this user
        var userBranches = await _userBranchRepository.GetBranchesByUserIdAsync(userId);
        var model = new LoginViewModel
        {
            AvailableBranches = userBranches.ToList(),
            Username = TempData["AuthenticatedUsername"]?.ToString() ?? "",
            ReturnUrl = TempData["ReturnUrl"]?.ToString()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectBranch(LoginViewModel model)
    {
        // Retrieve authenticated user info from TempData
        if (TempData["AuthenticatedUserId"] == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!int.TryParse(TempData["AuthenticatedUserId"]?.ToString(), out var userId))
        {
            return RedirectToAction(nameof(Login));
        }
        var username = TempData["AuthenticatedUsername"]?.ToString() ?? "";
        var email = TempData["AuthenticatedUserEmail"]?.ToString() ?? "";
        var fullName = TempData["AuthenticatedUserFullName"]?.ToString() ?? "";
        var role = TempData["AuthenticatedUserRole"] as int?;
        var profilePicturePath = TempData["AuthenticatedUserProfilePicturePath"]?.ToString() ?? "";
        var returnUrl = TempData["ReturnUrl"]?.ToString();

        if (model.BranchID == 0)
        {
            ModelState.AddModelError("BranchID", "Please select a branch");
            var branches = await _branchRepository.GetActiveBranchesAsync();
            model.AvailableBranches = branches.ToList();
            return View(model);
        }

        // Verify the selected branch is active
        var selectedBranch = await _branchRepository.GetBranchByIdAsync(model.BranchID);
        if (selectedBranch == null || !selectedBranch.IsActive)
        {
            ModelState.AddModelError("BranchID", "Selected branch is not available");
            var branches = await _branchRepository.GetActiveBranchesAsync();
            model.AvailableBranches = branches.ToList();
            return View(model);
        }

        // Use branch-specific roles, fall back to global if none assigned
        var branchSpecificRoles = (await _userBranchRoleRepository.GetRolesByUserBranchAsync(userId, model.BranchID)).ToList();
        var roles = branchSpecificRoles.Any()
            ? branchSpecificRoles
            : (await _userRoleRepository.GetRolesByUserIdAsync(userId)).ToList();
        if (roles.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No roles are assigned to this user for the selected branch.");
            var branches = await _branchRepository.GetActiveBranchesAsync();
            model.AvailableBranches = branches.ToList();
            return View(model);
        }

        // Decide selected role: if multiple roles, force selection after sign-in
        var selectedRoleId = roles.Count == 1 ? roles[0].Id : (role ?? roles[0].Id);
        var selectedRoleName = roles.FirstOrDefault(r => r.Id == selectedRoleId)?.Name ?? roles[0].Name;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim("displayName", fullName),
            new Claim("fullName", fullName),
            new Claim("BranchID", model.BranchID.ToString()),
            new Claim("BranchName", selectedBranch.BranchName),
            new Claim("SelectedRoleId", selectedRoleId.ToString()),
            new Claim("SelectedRoleName", selectedRoleName),
            new Claim("ProfilePicturePath", profilePicturePath)
        };

        // Keep ClaimTypes.Role as the currently selected role (int id as string for backward compatibility)
        claims.Add(new Claim(ClaimTypes.Role, selectedRoleId.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        // Store selected BranchID in session for easy access
        HttpContext.Session.SetInt32("BranchID", model.BranchID);
        HttpContext.Session.SetInt32("UserId", userId);
        HttpContext.Session.SetString("BranchName", selectedBranch.BranchName);
        HttpContext.Session.SetInt32("SelectedRoleId", selectedRoleId);
        HttpContext.Session.SetString("SelectedRoleName", selectedRoleName);
        HttpContext.Session.SetString("IsHOBranch", selectedBranch.IsHOBranch ? "1" : "0");

        // If multiple roles, go to role selection screen first
        if (roles.Count > 1)
        {
            TempData["ReturnUrl"] = returnUrl;
            return RedirectToAction(nameof(SelectRole));
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public async Task<IActionResult> SelectRole()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 0;
        if (userId <= 0)
        {
            return RedirectToAction(nameof(Login));
        }

        var branchSpecificRoles = branchId > 0
            ? (await _userBranchRoleRepository.GetRolesByUserBranchAsync(userId, branchId)).ToList()
            : new List<Role>();
        var roles = branchSpecificRoles.Any()
            ? branchSpecificRoles
            : (await _userRoleRepository.GetRolesByUserIdAsync(userId)).ToList();

        if (roles.Count <= 1)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var displayName = User.FindFirst("displayName")?.Value ?? User.Identity?.Name ?? "User";
        return View(new HotelApp.Web.ViewModels.SelectRoleViewModel
        {
            DisplayName = displayName,
            AvailableRoles = roles,
            ReturnUrl = TempData["ReturnUrl"]?.ToString()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectRole(HotelApp.Web.ViewModels.SelectRoleViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 0;
        if (userId <= 0 || branchId <= 0)
        {
            return RedirectToAction(nameof(Login));
        }

        var branchSpecificRoles = (await _userBranchRoleRepository.GetRolesByUserBranchAsync(userId, branchId)).ToList();
        var roles = branchSpecificRoles.Any()
            ? branchSpecificRoles
            : (await _userRoleRepository.GetRolesByUserIdAsync(userId)).ToList();

        if (!roles.Any(r => r.Id == model.SelectedRoleId))
        {
            ModelState.AddModelError("SelectedRoleId", "Invalid role selection");
            model.AvailableRoles = roles;
            model.DisplayName = User.FindFirst("displayName")?.Value ?? User.Identity?.Name ?? "User";
            return View(model);
        }

        var selectedRoleName = roles.First(r => r.Id == model.SelectedRoleId).Name;
        await UpdateSelectedRoleAsync(model.SelectedRoleId, selectedRoleName);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchRole(int roleId)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var branchId = HttpContext.Session.GetInt32("BranchID") ?? 0;
        if (userId <= 0)
        {
            return RedirectToAction(nameof(Login));
        }

        var branchSpecificRoles = branchId > 0
            ? (await _userBranchRoleRepository.GetRolesByUserBranchAsync(userId, branchId)).ToList()
            : new List<Role>();
        var roles = branchSpecificRoles.Any()
            ? branchSpecificRoles
            : (await _userRoleRepository.GetRolesByUserIdAsync(userId)).ToList();

        if (!roles.Any(r => r.Id == roleId))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var selectedRoleName = roles.First(r => r.Id == roleId).Name;
        await UpdateSelectedRoleAsync(roleId, selectedRoleName);
        return RedirectToAction("Index", "Dashboard");
    }

    private async Task UpdateSelectedRoleAsync(int selectedRoleId, string selectedRoleName)
    {
        HttpContext.Session.SetInt32("SelectedRoleId", selectedRoleId);
        HttpContext.Session.SetString("SelectedRoleName", selectedRoleName);

        var currentIdentity = User.Identity as ClaimsIdentity;
        if (currentIdentity is null)
        {
            return;
        }

        // Rebuild identity with updated role claims
        var newClaims = currentIdentity.Claims
            .Where(c => c.Type != ClaimTypes.Role && c.Type != "SelectedRoleId" && c.Type != "SelectedRoleName")
            .ToList();

        newClaims.Add(new Claim(ClaimTypes.Role, selectedRoleId.ToString()));
        newClaims.Add(new Claim("SelectedRoleId", selectedRoleId.ToString()));
        newClaims.Add(new Claim("SelectedRoleName", selectedRoleName));

        var newIdentity = new ClaimsIdentity(newClaims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(newIdentity));
    }

    /// <summary>
    /// Called by the client-side idle timer to cleanly end the session.
    /// Accepts GET so the browser can redirect here without needing a CSRF token.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> TimeoutLogout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login), new { reason = "timeout" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        if (userId <= 0)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!VerifyPassword(model.CurrentPassword, user.PasswordHash, user.Salt))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect");
            return View(model);
        }

        if (model.NewPassword == model.CurrentPassword)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "New password must be different from current password");
            return View(model);
        }

        var newSalt = BCrypt.Net.BCrypt.GenerateSalt(12);
        var newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, newSalt);
        await _userRepository.UpdatePasswordAsync(user.Id, newHash, newSalt);

        TempData["SuccessMessage"] = "Password updated successfully";
        return RedirectToAction(nameof(ChangePassword));
    }

    private static bool VerifyPassword(string password, string passwordHash, string? salt)
    {
        if (!string.IsNullOrEmpty(passwordHash) && passwordHash.StartsWith("$2"))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(salt))
        {
            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(32);
                var computedHash = Convert.ToBase64String(hash);
                return computedHash == passwordHash;
            }
            catch
            {
                // fallthrough to plaintext
            }
        }

        return passwordHash == password;
    }

    public IActionResult Denied() => View();
}
