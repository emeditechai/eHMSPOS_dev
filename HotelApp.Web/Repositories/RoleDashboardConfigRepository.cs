using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class RoleDashboardConfigRepository : IRoleDashboardConfigRepository
{
    private readonly string _connectionString;

    public RoleDashboardConfigRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IEnumerable<RoleDashboardConfig>> GetAllWithRoleNamesAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = """
            SELECT dc.Id, dc.RoleId, dc.DashboardController, dc.DashboardAction,
                   dc.DisplayName, dc.IsActive, dc.CreatedDate, dc.LastModifiedDate,
                   r.Name AS RoleName
            FROM   RoleDashboardConfig dc
            INNER  JOIN Roles r ON r.Id = dc.RoleId
            ORDER  BY r.Name
            """;
        return await connection.QueryAsync<RoleDashboardConfig>(sql);
    }

    public async Task<RoleDashboardConfig?> GetByRoleIdAsync(int roleId)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = """
            SELECT dc.Id, dc.RoleId, dc.DashboardController, dc.DashboardAction,
                   dc.DisplayName, dc.IsActive, dc.CreatedDate, dc.LastModifiedDate,
                   r.Name AS RoleName
            FROM   RoleDashboardConfig dc
            INNER  JOIN Roles r ON r.Id = dc.RoleId
            WHERE  dc.RoleId = @RoleId
            """;
        return await connection.QueryFirstOrDefaultAsync<RoleDashboardConfig>(sql, new { RoleId = roleId });
    }

    public async Task UpdateAsync(RoleDashboardConfig config)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = """
            UPDATE RoleDashboardConfig
            SET    DashboardController = @DashboardController,
                   DashboardAction     = @DashboardAction,
                   DisplayName         = @DisplayName,
                   IsActive            = @IsActive,
                   LastModifiedDate    = GETDATE()
            WHERE  RoleId = @RoleId
            """;
        await connection.ExecuteAsync(sql, new
        {
            config.DashboardController,
            config.DashboardAction,
            config.DisplayName,
            config.IsActive,
            config.RoleId
        });
    }
}
