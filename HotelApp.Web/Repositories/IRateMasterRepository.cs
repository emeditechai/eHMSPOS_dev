using HotelApp.Web.Models;

namespace HotelApp.Web.Repositories
{
    public interface IRateMasterRepository
    {
        Task<IEnumerable<RateMaster>> GetAllAsync();
        Task<IEnumerable<RateMaster>> GetByBranchAsync(int branchId);
        Task<RateMaster?> GetByIdAsync(int id);
        Task<int> CreateAsync(RateMaster rate);
        Task<bool> UpdateAsync(RateMaster rate);
        Task<IEnumerable<RoomType>> GetRoomTypesAsync();
        Task<IEnumerable<string>> GetCustomerTypesAsync();
        Task<IEnumerable<string>> GetSourcesAsync();
        
        // Weekend Rates
        Task<int> CreateWeekendRateAsync(WeekendRate weekendRate);
        Task<IEnumerable<WeekendRate>> GetWeekendRatesByRateMasterIdAsync(int rateMasterId);
        Task<bool> UpdateWeekendRateAsync(WeekendRate weekendRate);
        Task<bool> DeleteWeekendRateAsync(int id);
        
        // Special Day Rates
        Task<int> CreateSpecialDayRateAsync(SpecialDayRate specialDayRate);
        Task<IEnumerable<SpecialDayRate>> GetSpecialDayRatesByRateMasterIdAsync(int rateMasterId);
        Task<bool> UpdateSpecialDayRateAsync(SpecialDayRate specialDayRate);
        Task<bool> DeleteSpecialDayRateAsync(int id);
    }
}
