using HotelApp.Web.Models;
using HotelApp.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApp.Web.Controllers;

[Authorize]
public class RefundController : BaseController
{
    private readonly IRefundRepository _refundRepository;
    private readonly IBankRepository _bankRepository;

    public RefundController(
        IRefundRepository refundRepository,
        IBankRepository bankRepository)
    {
        _refundRepository = refundRepository;
        _bankRepository = bankRepository;
    }

    // GET /Refund or /Refund/Index
    [HttpGet]
    public async Task<IActionResult> Index(string? fromDate, string? toDate)
    {
        var today = DateTime.Today;
        var from = DateTime.TryParse(fromDate, out var fd) ? fd.Date : today;
        var to   = DateTime.TryParse(toDate,   out var td) ? td.Date : today;
        if (to < from) to = from;

        var pendingRefunds   = await _refundRepository.GetPendingRefundsByDateAsync(CurrentBranchID, from, to);
        var completedStats   = await _refundRepository.GetCompletedRefundsTotalAsync(CurrentBranchID, from, to);
        var allPending       = await _refundRepository.GetPendingRefundsAsync(CurrentBranchID); // for sidebar showing all

        ViewBag.Banks              = await _bankRepository.GetAllActiveAsync();
        ViewBag.FromDate           = from.ToString("yyyy-MM-dd");
        ViewBag.ToDate             = to.ToString("yyyy-MM-dd");
        ViewBag.TotalRefunded      = completedStats.TotalRefunded;
        ViewBag.RefundedCount      = completedStats.RefundedCount;
        ViewBag.AllPendingList     = allPending;

        return View(pendingRefunds);
    }

    // GET /Refund/Detail?cancellationId=123
    [HttpGet]
    public async Task<IActionResult> Detail(int cancellationId)
    {
        var detail = await _refundRepository.GetRefundDetailAsync(cancellationId, CurrentBranchID);
        if (detail == null)
            return Json(new { success = false, message = "Refund record not found or already processed." });

        return Json(new
        {
            success = true,
            cancellationId = detail.CancellationId,
            bookingId = detail.BookingId,
            bookingNumber = detail.BookingNumber,
            guestName = detail.GuestName,
            guestPhone = detail.GuestPhone ?? "",
            guestEmail = detail.GuestEmail ?? "",
            roomType = detail.RoomType ?? "",
            roomNumber = detail.RoomNumber ?? "",
            checkInDate = detail.CheckInDate.ToString("dd MMM yyyy"),
            checkOutDate = detail.CheckOutDate.ToString("dd MMM yyyy"),
            nights = detail.Nights,
            bookingTotalAmount = detail.BookingTotalAmount,
            bookingBaseAmount = detail.BookingBaseAmount,
            bookingTaxAmount = detail.BookingTaxAmount,
            amountPaid = detail.AmountPaid,
            refundPercent = detail.RefundPercent,
            deductionAmount = detail.DeductionAmount,
            refundAmount = detail.RefundAmount,
            refundBaseAmount = detail.RefundBaseAmount,
            refundCGSTAmount = detail.RefundCGSTAmount,
            refundSGSTAmount = detail.RefundSGSTAmount,
            refundTaxAmount = detail.RefundTaxAmount,
            approvalStatus = detail.ApprovalStatus,
            reason = detail.Reason ?? "",
            isOverride = detail.IsOverride,
            overrideReason = detail.OverrideReason ?? "",
            cancelledOn = detail.CancelledOn.ToString("dd MMM yyyy, hh:mm tt"),
            hoursBeforeCheckIn = detail.HoursBeforeCheckIn
        });
    }

    // POST /Refund/Approve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve([FromBody] ApproveRefundRequest request)
    {
        if (request == null || request.CancellationId <= 0)
            return Json(new { success = false, message = "Invalid request." });

        if (CurrentUserId == null || CurrentUserId.Value <= 0)
            return Json(new { success = false, message = "Unable to identify user. Please refresh and try again." });

        var (success, message) = await _refundRepository.ApproveRefundAsync(
            request.CancellationId, CurrentBranchID, CurrentUserId.Value);

        return Json(new { success, message });
    }

    // POST /Refund/Process
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process([FromBody] ProcessRefundRequest request)
    {
        if (request == null)
            return Json(new { success = false, message = "Invalid request." });

        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return Json(new { success = false, message = "Please select a refund payment method." });

        if (CurrentUserId == null || CurrentUserId.Value <= 0)
            return Json(new { success = false, message = "Unable to identify user. Please refresh and try again." });

        var result = await _refundRepository.ProcessRefundAsync(request, CurrentBranchID, CurrentUserId.Value);
        return Json(new
        {
            success = result.Success,
            message = result.Message,
            refundAmount = result.RefundAmount,
            receiptNumber = result.ReceiptNumber
        });
    }
}
