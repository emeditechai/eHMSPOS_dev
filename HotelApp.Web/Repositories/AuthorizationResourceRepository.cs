using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public class AuthorizationResourceRepository : IAuthorizationResourceRepository
{
    private readonly string _connectionString;

    public AuthorizationResourceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    public async Task<IReadOnlyList<AuthorizationResource>> GetAllActiveAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT Id, ResourceType, ResourceKey, Title, Controller, Action, ParentResourceId, SortOrder, IsActive
            FROM AuthorizationResources
            WHERE IsActive = 1
            ORDER BY SortOrder, Title";

        return (await connection.QueryAsync<AuthorizationResource>(sql)).ToList();
    }

    public async Task<AuthorizationResource?> GetPageResourceAsync(string controller, string action)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT TOP 1 Id, ResourceType, ResourceKey, Title, Controller, Action, ParentResourceId, SortOrder, IsActive
            FROM AuthorizationResources
            WHERE IsActive = 1 AND ResourceType = 'Page'
              AND Controller = @Controller AND Action = @Action";

        return await connection.QueryFirstOrDefaultAsync<AuthorizationResource>(sql, new { Controller = controller, Action = action });
    }

    public async Task<AuthorizationResource?> GetByKeyAsync(string resourceKey)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT TOP 1 Id, ResourceType, ResourceKey, Title, Controller, Action, ParentResourceId, SortOrder, IsActive
            FROM AuthorizationResources
            WHERE IsActive = 1 AND ResourceKey = @ResourceKey";

        return await connection.QueryFirstOrDefaultAsync<AuthorizationResource>(sql, new { ResourceKey = resourceKey });
    }

    public async Task<int> CreateUiResourceAsync(string resourceKey, string title, int? parentResourceId, int sortOrder, int? createdBy)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO AuthorizationResources (ResourceType, ResourceKey, Title, ParentResourceId, SortOrder, IsActive, CreatedBy, CreatedDate)
            VALUES ('Ui', @ResourceKey, @Title, @ParentResourceId, @SortOrder, 1, @CreatedBy, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() as int)";

        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            ResourceKey = resourceKey,
            Title = title,
            ParentResourceId = parentResourceId,
            SortOrder = sortOrder,
            CreatedBy = createdBy
        });
    }
}
