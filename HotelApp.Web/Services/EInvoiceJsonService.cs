using System.Text.Json;
using System.Text.Json.Serialization;
using HotelApp.Web.Models;
using HotelApp.Web.Repositories;

namespace HotelApp.Web.Services
{
    public class EInvoiceJsonService : IEInvoiceJsonService
    {
        private readonly IB2BEInvoiceLogRepository _logRepository;
        private readonly IGstSlabRepository _gstSlabRepository;

        public EInvoiceJsonService(
            IB2BEInvoiceLogRepository logRepository,
            IGstSlabRepository gstSlabRepository)
        {
            _logRepository = logRepository;
            _gstSlabRepository = gstSlabRepository;
        }

        public async Task<B2BEInvoiceLog?> GenerateAndSaveAsync(
            Booking booking,
            HotelSettings hotelSettings,
            int? createdBy)
        {
            // Only generate when EInvoiceMode = MANUAL
            if (!string.Equals(hotelSettings.EInvoiceMode, "MANUAL", StringComparison.OrdinalIgnoreCase))
                return null;

            // Only for B2B bookings
            if (!string.Equals(booking.CustomerType, "B2B", StringComparison.OrdinalIgnoreCase))
                return null;

            // Skip if a log already exists for this booking (idempotent guard)
            if (await _logRepository.ExistsForBookingAsync(booking.Id))
                return null;

            var version = await _logRepository.GetNextVersionAsync();

            var checkoutDate = booking.ActualCheckOutDate ?? booking.CheckOutDate;
            var invoiceDate = checkoutDate.ToString("dd/MM/yyyy");
            var invoiceNo = booking.InvoiceNumber ?? booking.BookingNumber;

            var itemList = await BuildItemListAsync(booking);

            var payload = new EInvoicePayload
            {
                Version = version,
                TranDtls = new TranDtls { TaxSch = "GST", SupTyp = "B2B" },
                DocDtls = new DocDtls
                {
                    Typ = "INV",
                    No = invoiceNo,
                    Dt = invoiceDate
                },
                SellerDtls = new PartyDtls
                {
                    Gstin = hotelSettings.GSTCode ?? string.Empty,
                    LglNm = hotelSettings.LglNm ?? hotelSettings.HotelName
                },
                BuyerDtls = new PartyDtls
                {
                    Gstin = booking.CompanyGstNo ?? string.Empty,
                    LglNm = booking.B2BClientName ?? string.Empty
                },
                ItemList = itemList
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };
            var json = JsonSerializer.Serialize(payload, jsonOptions);

            var log = new B2BEInvoiceLog
            {
                BookingId = booking.Id,
                BookingNo = booking.BookingNumber,
                InvoiceNumber = invoiceNo,
                Version = version,
                GenerationType = hotelSettings.EInvoiceMode,   // reflects the actual Hotel Settings value
                JsonPayload = json,
                BranchID = booking.BranchID,
                CreatedBy = createdBy
            };

            log.Id = await _logRepository.SaveAsync(log);
            return log;
        }

        private async Task<List<InvoiceItem>> BuildItemListAsync(Booking booking)
        {
            var items = new List<InvoiceItem>();

            var roomLines = booking.B2BRoomLines?
                .Where(l => !l.IsCancelled)
                .ToList();

            if (roomLines != null && roomLines.Count > 0)
            {
                // Multi room-type B2B booking — resolve GST rate from master per line
                for (int i = 0; i < roomLines.Count; i++)
                {
                    var line = roomLines[i];
                    var qty = line.RequiredRooms > 0 ? line.RequiredRooms : 1;
                    var baseAmt = line.BaseAmount;
                    var unitPrice = qty > 0 ? Math.Round(baseAmt / qty, 2) : baseAmt;

                    // Use the GST Slab master rate for the room's RatePerNight.
                    // ResolveBandAsync matches tariff → band → GstPercent (the authoritative configured rate).
                    // Fall back to amount-derived rate only if no master band is found.
                    var stayDate = line.CheckInDate ?? booking.CheckInDate;
                    var band = await _gstSlabRepository.ResolveBandAsync(
                        line.RatePerNight,
                        stayDate,
                        booking.GstSlabId);

                    var gstRt = band != null
                        ? band.GstPercent
                        : (line.BaseAmount > 0
                            ? Math.Round((line.TaxAmount / line.BaseAmount) * 100m, 2)
                            : 0m);

                    var taxAmt = Math.Round(baseAmt * gstRt / 100m, 2);
                    items.Add(new InvoiceItem
                    {
                        SlNo = (i + 1).ToString(),
                        PrdDesc = $"Room Charges - {line.RoomTypeName}",
                        Qty = qty,
                        UnitPrice = unitPrice,
                        GstRt = gstRt,
                        TotAmt = baseAmt + taxAmt
                    });
                }
            }
            else
            {
                // Single room-type fallback using booking header totals
                var qty = booking.RequiredRooms > 0 ? booking.RequiredRooms : 1;
                var baseAmt = booking.BaseAmount;
                var unitPrice = qty > 0 ? Math.Round(baseAmt / qty, 2) : baseAmt;

                var stayDate = booking.CheckInDate;
                var band = await _gstSlabRepository.ResolveBandAsync(
                    booking.BaseAmount > 0 && booking.Nights > 0 && qty > 0
                        ? booking.BaseAmount / booking.Nights / qty
                        : 0m,
                    stayDate,
                    booking.GstSlabId);

                var gstRt = band != null
                    ? band.GstPercent
                    : (booking.BaseAmount > 0
                        ? Math.Round((booking.TaxAmount / booking.BaseAmount) * 100m, 2)
                        : 0m);

                var taxAmt = Math.Round(baseAmt * gstRt / 100m, 2);
                items.Add(new InvoiceItem
                {
                    SlNo = "1",
                    PrdDesc = "Room Charges",
                    Qty = qty,
                    UnitPrice = unitPrice,
                    GstRt = gstRt,
                    TotAmt = baseAmt + taxAmt
                });
            }

            return items;
        }

        // ── Internal DTOs for JSON serialization ───────────────────────────────────

        private class EInvoicePayload
        {
            [JsonPropertyName("Version")]
            public string Version { get; set; } = string.Empty;

            [JsonPropertyName("TranDtls")]
            public TranDtls TranDtls { get; set; } = new();

            [JsonPropertyName("DocDtls")]
            public DocDtls DocDtls { get; set; } = new();

            [JsonPropertyName("SellerDtls")]
            public PartyDtls SellerDtls { get; set; } = new();

            [JsonPropertyName("BuyerDtls")]
            public PartyDtls BuyerDtls { get; set; } = new();

            [JsonPropertyName("ItemList")]
            public List<InvoiceItem> ItemList { get; set; } = new();
        }

        private class TranDtls
        {
            [JsonPropertyName("TaxSch")]
            public string TaxSch { get; set; } = "GST";

            [JsonPropertyName("SupTyp")]
            public string SupTyp { get; set; } = "B2B";
        }

        private class DocDtls
        {
            [JsonPropertyName("Typ")]
            public string Typ { get; set; } = "INV";

            [JsonPropertyName("No")]
            public string No { get; set; } = string.Empty;

            [JsonPropertyName("Dt")]
            public string Dt { get; set; } = string.Empty;
        }

        private class PartyDtls
        {
            [JsonPropertyName("Gstin")]
            public string Gstin { get; set; } = string.Empty;

            [JsonPropertyName("LglNm")]
            public string LglNm { get; set; } = string.Empty;
        }

        private class InvoiceItem
        {
            [JsonPropertyName("SlNo")]
            public string SlNo { get; set; } = string.Empty;

            [JsonPropertyName("PrdDesc")]
            public string PrdDesc { get; set; } = string.Empty;

            [JsonPropertyName("Qty")]
            public int Qty { get; set; }

            [JsonPropertyName("UnitPrice")]
            public decimal UnitPrice { get; set; }

            [JsonPropertyName("GstRt")]
            public decimal GstRt { get; set; }

            [JsonPropertyName("TotAmt")]
            public decimal TotAmt { get; set; }
        }
    }
}
