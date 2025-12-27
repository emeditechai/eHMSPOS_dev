using System.Data;
using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Repositories;

public sealed class BillingHeadRepository : IBillingHeadRepository
{
    private static readonly IReadOnlyList<BillingHead> DefaultHeads = new List<BillingHead>
    {
        new() { Id = 1, Code = "S", Name = "Stay Charges", IsActive = true },
        new() { Id = 2, Code = "R", Name = "Room Services", IsActive = true },
        new() { Id = 3, Code = "O", Name = "Other Charges", IsActive = true },
    };

    private readonly IDbConnection _dbConnection;

    public BillingHeadRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<IReadOnlyList<BillingHead>> GetActiveAsync()
    {
        try
        {
            const string sql = "SELECT * FROM BillingsHeads";
            var rows = (await _dbConnection.QueryAsync<dynamic>(sql)).ToList();

            var heads = new List<BillingHead>();
            foreach (var row in rows)
            {
                if (row is not IDictionary<string, object> dict)
                {
                    continue;
                }

                var code = GetString(dict, "BillingCode", "BillingHeadCode", "HeadCode", "Code");
                var name = GetString(dict, "BillingHeadName", "HeadName", "BillingHead", "Name");

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var isActive = GetBool(dict, true, "IsActive", "Active");
                if (!isActive)
                {
                    continue;
                }

                var id = GetInt(dict, 0, "Id", "BillingHeadId", "HeadId");

                heads.Add(new BillingHead
                {
                    Id = id,
                    Code = code.Trim(),
                    Name = name.Trim(),
                    IsActive = true
                });
            }

            if (heads.Count == 0)
            {
                return DefaultHeads;
            }

            return heads
                .OrderBy(h => h.Id)
                .ThenBy(h => h.Code)
                .ToList();
        }
        catch (SqlException)
        {
            // Best-effort only; keep UI functional even if table is missing.
            return DefaultHeads;
        }
        catch
        {
            return DefaultHeads;
        }
    }

    private static string? GetString(IDictionary<string, object> dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!TryGetValue(dict, key, out var value) || value == null)
            {
                continue;
            }

            var str = Convert.ToString(value);
            if (!string.IsNullOrWhiteSpace(str))
            {
                return str;
            }
        }

        return null;
    }

    private static int GetInt(IDictionary<string, object> dict, int fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!TryGetValue(dict, key, out var value) || value == null)
            {
                continue;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                // ignore
            }
        }

        return fallback;
    }

    private static bool GetBool(IDictionary<string, object> dict, bool fallback, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!TryGetValue(dict, key, out var value) || value == null)
            {
                continue;
            }

            try
            {
                if (value is bool b)
                {
                    return b;
                }

                if (value is byte bt)
                {
                    return bt != 0;
                }

                if (value is short s)
                {
                    return s != 0;
                }

                if (value is int i)
                {
                    return i != 0;
                }

                var str = Convert.ToString(value);
                if (bool.TryParse(str, out var parsedBool))
                {
                    return parsedBool;
                }

                if (decimal.TryParse(str, out var parsedDecimal))
                {
                    return parsedDecimal != 0m;
                }
            }
            catch
            {
                // ignore
            }
        }

        return fallback;
    }

    private static bool TryGetValue(IDictionary<string, object> dict, string key, out object? value)
    {
        foreach (var kvp in dict)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
