using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public class CreditNoteController : BaseController
{
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly IHotelSettingsRepository _hotelSettingsRepository;

    public CreditNoteController(
        ICreditNoteRepository creditNoteRepository,
        IHotelSettingsRepository hotelSettingsRepository)
    {
        _creditNoteRepository = creditNoteRepository;
        _hotelSettingsRepository = hotelSettingsRepository;
    }

    // GET /CreditNote/Print?id=5
    [HttpGet]
    public async Task<IActionResult> Print(int id)
    {
        if (id <= 0)
            return NotFound();

        var cn = await _creditNoteRepository.GetByIdAsync(id);
        if (cn == null || cn.BranchID != CurrentBranchID)
            return NotFound();

        var hotel = await _hotelSettingsRepository.GetByBranchAsync(CurrentBranchID);

        var vm = new CreditNoteViewModel
        {
            Id                    = cn.Id,
            CreditNoteNumber      = cn.CreditNoteNumber,
            GeneratedAt           = cn.GeneratedAt,

            HotelName             = hotel?.HotelName   ?? string.Empty,
            HotelAddress          = hotel?.Address      ?? string.Empty,
            HotelPhone            = hotel?.ContactNumber1 ?? string.Empty,
            HotelEmail            = hotel?.EmailAddress  ?? string.Empty,
            HotelGstNo            = hotel?.GSTCode,
            HotelLogoPath         = hotel?.LogoPath,

            BookingNumber         = cn.BookingNumber,
            OriginalInvoiceNumber = cn.OriginalInvoiceNumber,
            CustomerType          = cn.CustomerType,
            GuestName             = cn.GuestName,
            GuestEmail            = cn.GuestEmail,
            GuestPhone            = cn.GuestPhone,
            CompanyName           = cn.CompanyName,
            CompanyGstNo          = cn.CompanyGstNo,
            BillingAddress        = cn.BillingAddress,

            CheckInDate           = cn.CheckInDate,
            CheckOutDate          = cn.CheckOutDate,
            Nights                = cn.Nights,
            RoomType              = cn.RoomType,

            OriginalTotalAmount   = cn.OriginalTotalAmount,
            RefundBaseAmount      = cn.RefundBaseAmount,
            RefundCGSTAmount      = cn.RefundCGSTAmount,
            RefundSGSTAmount      = cn.RefundSGSTAmount,
            RefundTotalAmount     = cn.RefundTotalAmount,

            CancellationReason    = cn.CancellationReason,
            CancellationDate      = cn.CancellationDate,
            RefundPaymentMethod   = cn.RefundPaymentMethod,
            RefundPaymentReference = cn.RefundPaymentReference
        };

        return View(vm);
    }

    // GET /CreditNote/GetByCancellation?cancellationId=12
    [HttpGet]
    public async Task<IActionResult> GetByCancellation(int cancellationId)
    {
        var cn = await _creditNoteRepository.GetByCancellationIdAsync(cancellationId);
        if (cn == null || cn.BranchID != CurrentBranchID)
            return Json(new { success = false });

        return Json(new
        {
            success          = true,
            creditNoteId     = cn.Id,
            creditNoteNumber = cn.CreditNoteNumber,
            printUrl         = Url.Action("Print", "CreditNote", new { id = cn.Id })
        });
    }

    // GET /CreditNote/List?fromDate=2026-05-01&toDate=2026-05-09
    [HttpGet]
    public async Task<IActionResult> List(string? fromDate, string? toDate)
    {
        var today = DateTime.Today;
        var from  = DateTime.TryParse(fromDate, out var fd) ? fd.Date : today;
        var to    = DateTime.TryParse(toDate,   out var td) ? td.Date : today;
        if (to < from) to = from;

        var notes = await _creditNoteRepository.GetByDateRangeAsync(CurrentBranchID, from, to);

        var result = notes.Select(cn => new
        {
            id                    = cn.Id,
            creditNoteNumber      = cn.CreditNoteNumber,
            bookingNumber         = cn.BookingNumber,
            guestName             = cn.GuestName,
            companyName           = cn.CompanyName,
            customerType          = cn.CustomerType,
            refundTotalAmount     = cn.RefundTotalAmount,
            refundPaymentMethod   = cn.RefundPaymentMethod,
            cancellationDate      = cn.CancellationDate.ToString("dd MMM yyyy"),
            generatedAt           = cn.GeneratedAt.ToString("dd MMM yyyy, hh:mm tt"),
            printUrl              = Url.Action("Print", "CreditNote", new { id = cn.Id })
        });

        return Json(new { success = true, data = result });
    }
}
