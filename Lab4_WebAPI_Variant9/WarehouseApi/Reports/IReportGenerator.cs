using WarehouseApi.Models;

namespace WarehouseApi.Reports;

public interface IReportGenerator
{
    byte[] Generate(IEnumerable<Invoice> invoices, IEnumerable<Supplier> suppliers);
}
