namespace HotelApp.Web.ViewModels
{
    public class BookingReceiptTemplateConfigurationViewModel
    {
        public string CurrentTemplateKey { get; set; } = "classic";
        public IReadOnlyList<ReceiptTemplateOption> Templates { get; set; } = Array.Empty<ReceiptTemplateOption>();

        public class ReceiptTemplateOption
        {
            public string Key { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
