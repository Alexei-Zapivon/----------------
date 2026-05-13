using WarehouseApi.Models;

namespace WarehouseApi.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly DataStore _store;

    public SupplierRepository(DataStore store) => _store = store;

    public IEnumerable<Supplier> GetAll() => _store.Suppliers.AsReadOnly();

    public Supplier? TryGetById(int id) =>
        _store.Suppliers.FirstOrDefault(s => s.Id == id);

    public Supplier Create(Supplier supplier)
    {
        supplier.Id = _store.NextSupplierId();
        _store.Suppliers.Add(supplier);
        return supplier;
    }
}
