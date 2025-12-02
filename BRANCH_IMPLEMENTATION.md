# Branch Master Implementation Guide

## Overview
This document explains the comprehensive Branch Master system implemented throughout the Hotel Management Application. The system enables multi-branch operations with proper data isolation and branch-based filtering.

## Database Changes

### 1. Branch Master Table (Script: 13_CreateBranchMaster.sql)
Created `BranchMaster` table with the following structure:
- **BranchID**: Primary key, auto-increment
- **BranchName**: Name of the branch (e.g., "Mumbai Branch")
- **BranchCode**: Unique code for the branch (e.g., "MUM01")
- **Country, State, City**: Location information
- **Address**: Full address
- **Pincode**: Postal code (min 5 characters)
- **IsHOBranch**: Flag to identify Head Office branch
- **IsActive**: Status flag
- **CreatedBy, CreatedDate, ModifiedBy, ModifiedDate**: Audit fields

### 2. BranchID Added to All Tables (Script: 14_AlterTablesAddBranchID.sql)
Added `BranchID` column to all major entities:
- Users
- Guests
- Floors
- RoomTypes
- Rooms
- RateMaster
- Bookings
- BookingAuditLog
- Roles (nullable for global roles)

**Note**: All existing data is defaulted to BranchID = 1 (HO Branch)

## Application Changes

### 1. Models
Created/Updated the following models:
- **Branch.cs**: New model for Branch Master
- Updated all existing models (User, Guest, Room, RoomType, Floor, RateMaster, Booking, BookingAuditLog, Role) to include `BranchID` property

### 2. Repositories

#### New Repository
- **IBranchRepository**: Interface for branch operations
- **BranchRepository**: Implementation with methods:
  - `GetAllBranchesAsync()`: Get all branches
  - `GetActiveBranchesAsync()`: Get active branches only
  - `GetBranchByIdAsync(id)`: Get specific branch
  - `GetBranchByCodeAsync(code)`: Get branch by code
  - `GetHOBranchAsync()`: Get Head Office branch
  - `CreateBranchAsync(branch)`: Create new branch
  - `UpdateBranchAsync(branch)`: Update branch
  - `DeleteBranchAsync(id)`: Soft delete (deactivate)
  - `BranchCodeExistsAsync(code)`: Check uniqueness
  - `BranchNameExistsAsync(name)`: Check uniqueness

#### Updated Repositories
All existing repositories updated to support branch filtering:
- **GuestRepository**: Added `GetByPhoneAndBranchAsync()`, updated Create/Update to include BranchID
- **RoomRepository**: Added `GetAllByBranchAsync()`, updated Create to include BranchID
- **BookingRepository**: Updated Create to include BranchID
- **UserRepository**: Updated to fetch BranchID with user data

### 3. Controllers

#### New Controller
- **BranchMasterController**: Complete CRUD operations for Branch Master
  - Index: List all branches
  - Create: Create new branch with validation
  - Edit: Update existing branch
  - Details: View branch details
  - Delete: Deactivate branch (soft delete)

#### Base Controller
Created **BaseController** that all other controllers inherit from:
- Automatically retrieves `CurrentBranchID` from session
- Provides `GetCurrentBranchID()` and `GetCurrentUserId()` methods
- Sets BranchID and UserId in ViewBag for views

#### Updated Controllers
All main controllers now extend `BaseController`:
- BookingController
- GuestController
- RoomMasterController
- RoomTypeMasterController
- FloorMasterController
- RateMasterController
- DashboardController
- RoomsController

All entity creation operations updated to set `BranchID = CurrentBranchID`

### 4. Authentication
Updated **AccountController** to:
- Store BranchID in claims during login
- Store BranchID in session for easy access
- Store UserId in session

### 5. Views
Created complete CRUD views for Branch Master:
- **Index.cshtml**: List all branches with status badges
- **Create.cshtml**: Form to create new branch
- **Edit.cshtml**: Form to edit existing branch
- **Details.cshtml**: View branch details
- **Delete.cshtml**: Confirmation page for deactivation

### 6. Configuration
Updated **Program.cs**:
- Registered `IBranchRepository` and `BranchRepository`
- Added Session configuration with 8-hour timeout
- Enabled session middleware in request pipeline

## Usage

### 1. Running Database Scripts
Execute scripts in order:
```sql
-- Run these scripts on your database
13_CreateBranchMaster.sql
14_AlterTablesAddBranchID.sql
```

### 2. Accessing Branch Master
Navigate to: `/BranchMaster/Index`

### 3. Creating a New Branch
1. Go to Branch Master
2. Click "Add New Branch"
3. Fill in required fields:
   - Branch Name
   - Branch Code (unique, min 2 chars)
   - Country, State, City
   - Address
   - Pincode (min 5 chars)
   - Check "Is Head Office Branch" if applicable
4. Click "Create Branch"

### 4. User Branch Assignment
When creating/editing users, assign them to a specific branch. Users will only see and manage data for their assigned branch.

### 5. Data Isolation
- All data operations automatically filter by the logged-in user's branch
- Cross-branch data access is prevented at the repository level
- Dashboard and reports show only branch-specific data

## Branch-Based Data Flow

1. **User Login**:
   - User credentials validated
   - BranchID retrieved from Users table
   - BranchID stored in claims and session
   - User redirected to dashboard

2. **Data Creation**:
   - Controller gets BranchID from BaseController
   - BranchID automatically assigned to new entities
   - Data saved with branch association

3. **Data Retrieval**:
   - Repositories filter queries by BranchID
   - Users see only their branch's data
   - Cross-branch queries prevented

4. **Data Updates**:
   - BranchID verified during updates
   - Users can only update their branch's data

## API Endpoints

### Branch Master
- `GET /BranchMaster/Index` - List all branches
- `GET /BranchMaster/Create` - Show create form
- `POST /BranchMaster/Create` - Create new branch
- `GET /BranchMaster/Edit/{id}` - Show edit form
- `POST /BranchMaster/Edit/{id}` - Update branch
- `GET /BranchMaster/Details/{id}` - View branch details
- `GET /BranchMaster/Delete/{id}` - Show delete confirmation
- `POST /BranchMaster/Delete/{id}` - Deactivate branch

## Security Considerations

1. **Data Isolation**: All queries filtered by BranchID
2. **Session Management**: BranchID stored securely in session
3. **Validation**: Branch codes and names must be unique
4. **Audit Trail**: Created/Modified by tracking for all branch operations
5. **Soft Delete**: Branches are deactivated, not permanently deleted

## Future Enhancements

1. **Branch Transfer**: Ability to transfer data between branches
2. **Multi-Branch Access**: Support for users accessing multiple branches
3. **Branch Permissions**: Role-based permissions per branch
4. **Branch Analytics**: Comparative reports across branches
5. **Branch Hierarchy**: Support for sub-branches or regions

## Troubleshooting

### Issue: BranchID is null
**Solution**: Ensure user is logged in and session is active. Check that BranchID is set in session during login.

### Issue: Cannot see data after branch setup
**Solution**: Verify that existing data has BranchID = 1 (HO Branch) and user is assigned to correct branch.

### Issue: Branch code already exists
**Solution**: Each branch must have a unique branch code. Use a different code or update the existing branch.

## Testing Checklist

- [ ] Create new branch successfully
- [ ] Edit existing branch
- [ ] View branch details
- [ ] Deactivate branch
- [ ] Login with user assigned to specific branch
- [ ] Create booking - verify BranchID is set
- [ ] Create room - verify BranchID is set
- [ ] Create guest - verify BranchID is set
- [ ] Verify data isolation between branches
- [ ] Check dashboard shows only branch-specific data

## Support

For issues or questions regarding Branch Master implementation, please contact the development team.

---
**Last Updated**: December 2, 2025
**Version**: 1.0
