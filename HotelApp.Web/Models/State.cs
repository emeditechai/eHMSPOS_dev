namespace HotelApp.Web.Models
{
    public class State
    {
        public int Id { get; set; }
        public int CountryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }
}
