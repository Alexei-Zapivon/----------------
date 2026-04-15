using Lab2_EF_Variant9.Models;

namespace Lab2_EF_Variant9.Repositories;

// Интерфейс Repository — CRUD + 2 доменных метода
public interface IRepository
{
    Task<List<Document>> GetAllAsync();
    Task<Document?> GetByIdAsync(int id);
    Task AddAsync(Document document);
    void Update(Document document);
    void Delete(Document document);

    // Доменные методы
    Task<List<Document>> GetByAmountGreaterThanAsync(decimal amount);
    Task<List<Document>> GetByTypeAsync(string type);
}
