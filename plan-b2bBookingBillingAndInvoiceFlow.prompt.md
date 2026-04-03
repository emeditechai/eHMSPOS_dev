## Plan: B2B Booking, Billing, and Invoice Flow

Extend the existing HotelApp booking flow with a first-class B2B path that reuses current booking, rate, payment, GST, receipt, and reporting patterns while leaving B2C behavior unchanged. The recommended design is to add three Utility masters (B2B Customer Master, Agreement Master, GST Slab Master), extend booking creation/details/payment flows with B2B-only sections and validations, and introduce an internal-ready GST invoice and credit-note workflow with pluggable e-invoice submission support.

**Steps**
1. Phase 1 - Schema foundation. Add SQL scripts for B2B master data and booking extensions: B2B customers, customer contacts/billing defaults, agreements/contracts, GST slab master, booking B2B header fields, booking pricing snapshot fields, booking guest compliance fields, invoice header/detail tables, invoice-booking link tables, IRN/QR/Ack storage fields, credit note tables, and audit/status-log support. Seed new Utility menu items and role mappings. This blocks all later phases.
2. Phase 1 - Reuse-first domain modeling. Add models and repository contracts for B2BCustomer, B2BAgreement, GstSlab, B2BInvoice, B2BInvoiceLine, CreditNote, and invoice status history. Extend existing Booking, BookingGuest, and BookingPayment models with only the new B2B/GST/compliance fields required by the BRD instead of duplicating booking structures. This depends on step 1.
3. Phase 1 - Repository integration. Extend BookingRepository and related repositories so they can load B2B masters, resolve agreement-linked defaults, calculate number of nights, determine GST slab by room tariff, validate company credit limit and outstanding balance, persist B2B booking fields, and expose invoice/credit-note queries. Build these on current Dapper patterns already used for booking, payment, GST, and reporting. This depends on steps 1-2.
4. Phase 2 - Utility masters. Create the following new masters under Utility using the existing master-page CRUD pattern:
   B2B Customer Master: corporate / travel-agent / OTA B2B account, GSTIN, billing address, contact person, contact number, email, credit terms, billing type default, billing-to default, credit limit, active status.
   Agreement Master: linked customer contract/agreement id, effective dates, room/rate-plan mapping, corporate/contract rate defaults, discount rules, payment terms, meal-plan applicability, and active status.
   GST Slab Master: tariff range, GST rate, intra-state CGST/SGST split, inter-state IGST rate, effective dates, and active flag.
   These depend on steps 1-3 and the three masters can be implemented in parallel once common CRUD patterns are set.
5. Phase 2 - Booking header integration. Extend the Create Booking flow so when Customer Type = B2B the header switches into a dedicated B2B booking section before stay pricing is finalized. Restrict Booking Source to Corporate / Travel Agent / OTA B2B, require B2B customer selection, auto-fill company GST/contact/billing defaults, link an Agreement, and display credit terms, billing type, rate plan, agreement id, company credit limit, and outstanding balance. Keep existing B2C defaults and source behavior unchanged. This depends on steps 3-4.
6. Phase 2 - Guest and stay compliance integration. Extend booking input and persistence to capture B2B-required guest compliance fields without breaking existing guest flow: guest name/mobile/email, ID proof type, ID number, nationality, room type, rooms, pax, meal plan, special requests, and remarks. Reuse current guest and booking sections, adding B2B-specific validation only when Customer Type = B2B. This depends on step 5.
7. Phase 2 - Rate and pricing engine extension. Reuse the existing room-rate lookup flow, but add agreement-driven corporate rate resolution, discount handling, extra pax charges, meal-plan effect, GST slab selection by tariff, and pricing snapshots saved to the booking/invoice context. The GST slab logic should support 0%, 12%, and 18% tariff-based rules while remaining master-driven for future changes. This depends on steps 3-6.
8. Phase 2 - Booking save rules. Update BookingController create validation and repository save logic so B2B bookings require customer, agreement, B2B source, GST/contact fields, billing fields, and company credit checks. Persist billing-to, invoice type, payment mode intent, advance paid, remarks, and status/audit metadata. Preserve current B2C create/save logic with no new mandatory fields for B2C. This depends on steps 5-7.
9. Phase 3 - Booking details and operations. Extend the booking details page and related operational actions to show B2B company metadata, billing settings, agreement snapshot, GST breakdown, invoice linkage, credit usage, room allocation tracking, and internal remarks. Reuse existing detail-card and status patterns and keep B2C details rendering unchanged unless a common improvement is harmless. This depends on step 8.
10. Phase 3 - Payment and billing integration. Extend the current AddPayment and billing logic so B2B bookings support advance paid, payment mode (Cash / Card / Credit using existing payment method structure where possible), billing-to Company/Guest behavior, outstanding balance updates, and company receivable tracking. Reuse current receipt generation and GST data paths instead of building a separate B2B payment subsystem. This depends on steps 3 and 8-9.
11. Phase 3 - Invoice workflow. Add B2B GST invoice generation after bill closure/checkout. The invoice flow should prepare the mandatory supplier/buyer/tax/item data, generate and store the e-invoice JSON payload, keep IRN/QR/Ack fields ready, lock invoice edits once IRN exists, and support print/email rendering. For this phase, external NIC/GSP submission stays pluggable: implement internal-ready JSON generation, status management, and storage, not live provider connectivity. This depends on steps 1-10.
12. Phase 3 - Credit note workflow. Add credit notes tied to original invoices for cancellation/refund/billing correction use cases. Capture original invoice reference, reason, taxable reversal, GST adjustment, net credit amount, and note status/audit history. Reuse invoice/tax structures so GST liability reduction can be reported consistently. This depends on step 11.
13. Phase 4 - Reporting and compliance. Add or extend reports for company outstanding balance, B2B invoice register, invoice aging/receivables, credit-note register, and invoice audit visibility. Reuse current GST and outstanding-balance reporting patterns as much as possible. This depends on steps 10-12.
14. Phase 4 - Professional UX and regression protection. Refine the B2B sections in Create and Details into clearly grouped cards/tabs so the flow feels professional, readable, and aligned with the existing app. Validate that B2C flow, room rates, cancellation policy, payment collection, and reports continue to behave exactly as before. This depends on all prior steps.

**Relevant files**
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Controllers/BookingController.cs — current booking creation, room-rate lookup, and payment flow to extend conditionally for B2B.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Repositories/BookingRepository.cs — primary Dapper persistence layer for booking, pricing, status, and payment logic.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/ViewModels/BookingCreateViewModel.cs — add B2B booking header, guest compliance, pricing, billing, and invoice inputs.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Models/Booking.cs — extend Booking, BookingGuest, and BookingPayment with B2B/compliance/invoice snapshot fields.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Views/Booking/Create.cshtml — add B2B-only sections and professional grouped UI while keeping B2C path intact.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Views/Booking/Details.cshtml — surface B2B company, agreement, invoice, billing, and audit information.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Controllers/OtherChargesMasterController.cs — CRUD/controller pattern to mirror for new Utility masters.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Repositories/IOtherChargeRepository.cs — repository interface pattern to mirror for B2B Customer, Agreement, and GST Slab masters.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Repositories/OtherChargeRepository.cs — Dapper CRUD implementation pattern to mirror.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Views/OtherChargesMaster/List.cshtml — reusable list-page pattern for Utility masters.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Views/OtherChargesMaster/Create.cshtml — reusable create/edit/details form pattern.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Views/Shared/Components/NavBarMenu/Default.cshtml — Utility menu rendering behavior to reuse.
- /Users/abhikporel/dev/Hotelapp/HotelApp.Web/Program.cs — DI registration for new repositories and services.
- /Users/abhikporel/dev/Hotelapp/Database/Scripts/50_CreateGSTReportStoredProcedure.sql — GST reporting pattern to extend for B2B invoice and credit-note reporting.
- /Users/abhikporel/dev/Hotelapp/Database/Scripts/82_CreateBusinessAnalyticsReports.sql — existing outstanding-balance reporting base to extend toward company receivables.
- /Users/abhikporel/dev/Hotelapp/Database/Scripts/87_CreateNavMenuAuthorizationTables.sql — authorization/menu schema.
- /Users/abhikporel/dev/Hotelapp/Database/Scripts/88_SeedNavMenuItems_FromExistingLayout.sql — navigation seed pattern.
- /Users/abhikporel/dev/Hotelapp/Database/Scripts/112_SeedRoleNavMenuItems_Refund.sql — role-menu authorization seed pattern.

**Verification**
1. Run the new migration scripts and verify all new B2B, GST slab, invoice, credit-note, and audit tables plus FKs/indexes/menu rows are created correctly.
2. Validate Utility navigation shows B2B Customer Master, Agreement Master, and GST Slab Master only for authorized roles.
3. Create and edit master data for each new master and confirm branch-wise behavior, active filtering, and agreement-to-customer linking.
4. Create a standard B2C booking and verify there is no regression in fields, required validations, rate lookup, cancellation policy, payment, or booking creation.
5. Create B2B bookings for Corporate, Travel Agent, and OTA B2B sources and verify customer/agreement autofill, mandatory validations, credit checks, outstanding-balance display, and successful save.
6. Verify nights, meal plan, discount, extra pax charges, tariff-based GST slab selection, and total room charges are computed and stored correctly.
7. Add B2B payments using cash/card/credit paths and verify advance paid, billing-to behavior, receipts, and outstanding company balance update correctly.
8. Close a B2B stay and generate an invoice; verify supplier/buyer GST data, item lines, tax values, invoice totals, JSON payload generation, IRN placeholder fields, print/email rendering, and invoice locking behavior when IRN is populated.
9. Issue a credit note against an invoice and verify original-invoice linkage, taxable reversal, GST adjustment, status change, and reporting visibility.
10. Run application build and targeted manual checks on Booking Create, Booking Details, payment flow, Utility masters, invoice views, and reports.

**Decisions**
- Included scope: B2B booking end-to-end, B2B Customer Master, Agreement Master, GST Slab Master, invoice workflow, and credit-note workflow.
- Excluded scope for this phase: live NIC/GSP connectivity and debit-note UI/workflow; however, the schema and service boundaries should remain ready for later debit-note and provider integration.
- No-regression rule: B2C flow must remain functionally unchanged, with all new mandatory behavior gated behind Customer Type = B2B.
- Reuse strategy: prefer extending current booking, payment, GST, reporting, and master-page patterns rather than introducing a separate module.
- GST strategy: tariff-based slab resolution must be master-driven so future slab changes do not require code changes.
- Invoice strategy: invoice becomes non-editable after IRN generation fields are filled; until then, the system supports internal invoice preparation and controlled corrections.
- Credit control: enforce server-side validation against customer credit limit and current outstanding balance, regardless of UI behavior.

**Further Considerations**
1. Rate-plan modeling: start by mapping Agreement Master to existing room/rate-plan structures and add custom contract-rate rows only if the current rate engine cannot express negotiated B2B pricing cleanly.
2. Place-of-supply and IGST: confirm whether hotel branch state and buyer state are already stored in a way that can reliably determine CGST/SGST versus IGST at invoice time; if not, add that to the schema in phase 1.
3. Audit depth: prefer using the existing booking status/audit patterns for booking changes, but keep dedicated invoice and credit-note status logs because compliance actions require tighter traceability.
