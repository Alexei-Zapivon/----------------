using WarehouseApi.DTOs;
using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly DataStore _store;

    public InvoiceRepository(DataStore store) => _store = store;

    public PagedResponse<Invoice> GetAll(int page, int pageSize, string? status, int? supplierId, string? number)
    {
        var query = _store.Invoices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsedStatus))
            query = query.Where(i => i.Status == parsedStatus);

        if (supplierId.HasValue)
            query = query.Where(i => i.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(number))
            query = query.Where(i => i.Number.Contains(number, StringComparison.OrdinalIgnoreCase));

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<Invoice>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public IEnumerable<Invoice> GetFiltered(DateTime? dateFrom, DateTime? dateTo, string? status)
    {
        var query = _store.Invoices.AsEnumerable();

        if (dateFrom.HasValue)
            query = query.Where(i => i.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(i => i.Date <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsedStatus))
            query = query.Where(i => i.Status == parsedStatus);

        return query.ToList();
    }

    public Invoice GetById(int id) =>
        _store.Invoices.FirstOrDefault(i => i.Id == id)
        ?? throw new KeyNotFoundException($"Накладная с ID {id} не найдена");

    public Invoice Create(Invoice invoice)
    {
        invoice.Id = _store.NextInvoiceId();
        _store.Invoices.Add(invoice);
        return invoice;
    }

    public Invoice Update(int id, Invoice updated)
    {
        var existing = GetById(id);
        existing.Number = updated.Number;
        existing.Date = updated.Date;
        existing.SupplierId = updated.SupplierId;
        existing.TotalAmount = updated.TotalAmount;
        existing.Status = updated.Status;
        return existing;
    }

    public void Delete(int id)
    {
        var invoice = GetById(id);
        _store.Invoices.Remove(invoice);
    }
}
