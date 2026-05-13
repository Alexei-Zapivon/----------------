using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

public interface ISupplierRepository
{
    IEnumerable<Supplier> GetAll();
    Supplier? TryGetById(int id);
    Supplier Create(Supplier supplier);
}
