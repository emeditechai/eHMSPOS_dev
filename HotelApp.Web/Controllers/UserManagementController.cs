using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using HotelApp.Web.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace HotelApp.Web.Controllers;

[Authorize]
public class UserManagementController : BaseController
{
    private readonly IUserRepository _userRepository;
    private readonly IUserBranchRepository _userBranchRepository;
    private readonly IBranchRepository _branchRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public UserManagementController(
        IUserRepository userRepository,
        IUserBranchRepository userBranchRepository,
        IBranchRepository branchRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _userBranchRepository = userBranchRepository;
        _branchRepository = branchRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userRepository.GetAllUsersAsync();
        
        // Get branches and roles for each user
        var usersWithBranchesAndRoles = new List<(User user, List<Branch> branches, List<Role> roles)>();
        foreach (var user in users)
        {
            var branches = (await _userBranchRepository.GetBranchesByUserIdAsync(user.Id)).ToList();
            var roles = (await _userRoleRepository.GetRolesByUserIdAsync(user.Id)).ToList();
            usersWithBranchesAndRoles.Add((user, branches, roles));
        }
        
        ViewBag.UsersWithBranchesAndRoles = usersWithBranchesAndRoles;
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var branches = await _branchRepository.GetActiveBranchesAsync();
        var roles = await _roleRepository.GetAllRolesAsync();
        var model = new UserCreateViewModel
        {
            AvailableBranches = branches.ToList(),
            AvailableRoles = roles.ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate username uniqueness
        if (await _userRepository.UsernameExistsAsync(model.Username))
        {
            ModelState.AddModelError("Username", "Username already exists");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate email uniqueness
        if (await _userRepository.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Email already exists");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate password match
        if (model.Password != model.ConfirmPassword)
        {
            ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate at least one branch selected
        if (!model.SelectedBranchIds.Any())
        {
            ModelState.AddModelError("SelectedBranchIds", "Please select at least one branch");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate at least one role selected
        if (!model.SelectedRoleIds.Any())
        {
            ModelState.AddModelError("SelectedRoleIds", "Please select at least one role");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Generate salt and hash password using BCrypt
        var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, salt);

        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = passwordHash,
            Salt = salt,
            FirstName = model.FirstName,
            LastName = model.LastName,
            FullName = model.FullName ?? $"{model.FirstName} {model.LastName}",
            Phone = model.Username, // Default phone
            PhoneNumber = model.Username,
            Role = model.SelectedRoleIds.FirstOrDefault(), // Keep first role for backward compatibility
            BranchID = model.SelectedBranchIds.First(), // Set primary branch
            IsActive = true
        };

        var userId = await _userRepository.CreateUserAsync(user);
        
        // Assign branches
        await _userBranchRepository.AssignBranchesToUserAsync(userId, model.SelectedBranchIds, CurrentUserId);
        
        // Assign roles
        await _userRoleRepository.AssignRolesToUserAsync(userId, model.SelectedRoleIds, CurrentUserId ?? 1);

        TempData["SuccessMessage"] = "User created successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userBranches = await _userBranchRepository.GetBranchesByUserIdAsync(id);
        var userRoles = await _userRoleRepository.GetRolesByUserIdAsync(id);
        var branches = await _branchRepository.GetActiveBranchesAsync();
        var roles = await _roleRepository.GetAllRolesAsync();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive,
            SelectedBranchIds = userBranches.Select(b => b.BranchID).ToList(),
            SelectedRoleIds = userRoles.Select(r => r.Id).ToList(),
            AvailableBranches = branches.ToList(),
            AvailableRoles = roles.ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        var user = await _userRepository.GetByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        // Validate username uniqueness
        if (await _userRepository.UsernameExistsAsync(model.Username, model.Id))
        {
            ModelState.AddModelError("Username", "Username already exists");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate email uniqueness
        if (await _userRepository.EmailExistsAsync(model.Email, model.Id))
        {
            ModelState.AddModelError("Email", "Email already exists");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate password match if provided
        if (!string.IsNullOrEmpty(model.Password))
        {
            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
                model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
                return View(model);
            }

            // Update password using BCrypt
            var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, salt);
            user.Salt = salt;
        }

        // Validate at least one branch selected
        if (!model.SelectedBranchIds.Any())
        {
            ModelState.AddModelError("SelectedBranchIds", "Please select at least one branch");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        // Validate at least one role selected
        if (!model.SelectedRoleIds.Any())
        {
            ModelState.AddModelError("SelectedRoleIds", "Please select at least one role");
            model.AvailableBranches = (await _branchRepository.GetActiveBranchesAsync()).ToList();
            model.AvailableRoles = (await _roleRepository.GetAllRolesAsync()).ToList();
            return View(model);
        }

        user.Username = model.Username;
        user.Email = model.Email;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.FullName = model.FullName ?? $"{model.FirstName} {model.LastName}";
        user.Role = model.SelectedRoleIds.FirstOrDefault(); // Keep first role for backward compatibility
        user.IsActive = model.IsActive;
        user.BranchID = model.SelectedBranchIds.First(); // Update primary branch

        await _userRepository.UpdateUserAsync(user);
        
        // Update branch assignments
        await _userBranchRepository.AssignBranchesToUserAsync(user.Id, model.SelectedBranchIds, CurrentUserId ?? 1);
        
        // Update role assignments
        await _userRoleRepository.AssignRolesToUserAsync(user.Id, model.SelectedRoleIds, CurrentUserId ?? 1);

        TempData["SuccessMessage"] = "User updated successfully";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userBranches = await _userBranchRepository.GetBranchesByUserIdAsync(id);
        var userRoles = await _userRoleRepository.GetRolesByUserIdAsync(id);
        
        ViewBag.UserBranches = userBranches;
        ViewBag.UserRoles = userRoles;

        return View(user);
    }

    private string GetRoleName(int? roleId)
    {
        return roleId switch
        {
            1 => "Administrator",
            2 => "Manager",
            3 => "Receptionist",
            4 => "Staff",
            _ => "Unknown"
        };
    }
}
