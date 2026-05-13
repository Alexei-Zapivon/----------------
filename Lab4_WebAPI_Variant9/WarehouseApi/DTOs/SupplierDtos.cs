namespace WarehouseApi.DTOs;

public record CreateSupplierRequest(
    string Name,
    string ContactPerson,
    string Phone,
    string Address
);

public record SupplierResponse(
    int Id,
    string Name,
    string ContactPerson,
    string Phone,
    string Address
);
