using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Services;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserBranchRepository _userBranchRepository;

    public AccountController(IAuthService authService, IBranchRepository branchRepository, IUserBranchRepository userBranchRepository)
    {
        _authService = authService;
        _branchRepository = branchRepository;
        _userBranchRepository = userBranchRepository;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        
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

        var userId = (int)TempData["AuthenticatedUserId"];

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

        var userId = (int)TempData["AuthenticatedUserId"];
        var username = TempData["AuthenticatedUsername"]?.ToString() ?? "";
        var email = TempData["AuthenticatedUserEmail"]?.ToString() ?? "";
        var fullName = TempData["AuthenticatedUserFullName"]?.ToString() ?? "";
        var role = TempData["AuthenticatedUserRole"] as int?;
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

        // Create claims and complete authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim("displayName", fullName),
            new Claim("fullName", fullName),
            new Claim("BranchID", model.BranchID.ToString()),
            new Claim("BranchName", selectedBranch.BranchName)
        };
        
        if (role.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        
        // Store selected BranchID in session for easy access
        HttpContext.Session.SetInt32("BranchID", model.BranchID);
        HttpContext.Session.SetInt32("UserId", userId);
        HttpContext.Session.SetString("BranchName", selectedBranch.BranchName);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    public IActionResult Denied() => View();
}
