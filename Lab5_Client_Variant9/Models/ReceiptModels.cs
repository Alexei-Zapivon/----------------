namespace Lab5Client.Models;

public record CreateReceiptRequest(
    int InvoiceId,
    DateTime ReceivedDate,
    string ReceivedBy,
    decimal Amount,
    string? Notes
);

public record ReceiptResponse(
    int Id,
    int InvoiceId,
    DateTime ReceivedDate,
    string ReceivedBy,
    decimal Amount,
    string? Notes
);
