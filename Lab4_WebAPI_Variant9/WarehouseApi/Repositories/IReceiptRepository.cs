using WarehouseApi.DTOs;
using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

public interface IReceiptRepository
{
    PagedResponse<Receipt> GetAll(int page, int pageSize, int? invoiceId);
    Receipt GetById(int id);
    Receipt Create(Receipt receipt);
    void Delete(int id);
}
