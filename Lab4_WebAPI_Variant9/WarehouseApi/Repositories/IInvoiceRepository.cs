using WarehouseApi.DTOs;
using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

public interface IInvoiceRepository
{
    PagedResponse<Invoice> GetAll(int page, int pageSize, string? status, int? supplierId, string? number);
    IEnumerable<Invoice> GetFiltered(DateTime? dateFrom, DateTime? dateTo, string? status);
    Invoice GetById(int id);
    Invoice Create(Invoice invoice);
    Invoice Update(int id, Invoice updated);
    void Delete(int id);
}
