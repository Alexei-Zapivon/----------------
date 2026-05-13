namespace WarehouseApi.DTOs;

public record CreateInvoiceRequest(
    string Number,
    DateTime Date,
    int SupplierId,
    decimal TotalAmount
);

public record UpdateInvoiceRequest(
    string Number,
    DateTime Date,
    int SupplierId,
    decimal TotalAmount,
    string Status
);

public record InvoiceResponse(
    int Id,
    string Number,
    DateTime Date,
    int SupplierId,
    string SupplierName,
    decimal TotalAmount,
    string Status
);
