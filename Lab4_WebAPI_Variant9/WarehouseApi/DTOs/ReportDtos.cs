namespace WarehouseApi.DTOs;

public record GenerateInvoiceReportRequest(
    DateTime? DateFrom,
    DateTime? DateTo,
    string? Status
);
