using WarehouseApi.DTOs;
using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

public class ReceiptRepository : IReceiptRepository
{
    private readonly DataStore _store;

    public ReceiptRepository(DataStore store) => _store = store;

    public PagedResponse<Receipt> GetAll(int page, int pageSize, int? invoiceId)
    {
        var query = _store.Receipts.AsEnumerable();

        if (invoiceId.HasValue)
            query = query.Where(r => r.InvoiceId == invoiceId.Value);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResponse<Receipt>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Receipt GetById(int id) =>
        _store.Receipts.FirstOrDefault(r => r.Id == id)
        ?? throw new KeyNotFoundException($"Квитанция с ID {id} не найдена");

    public Receipt Create(Receipt receipt)
    {
        receipt.Id = _store.NextReceiptId();
        _store.Receipts.Add(receipt);
        return receipt;
    }

    public void Delete(int id)
    {
        var receipt = GetById(id);
        _store.Receipts.Remove(receipt);
    }
}
