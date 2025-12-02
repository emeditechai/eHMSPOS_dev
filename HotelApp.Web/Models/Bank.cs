namespace HotelApp.Web.Models
{
    public class Bank
    {
        public int Id { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    }
}
