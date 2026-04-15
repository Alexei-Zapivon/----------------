using Lab2_EF_Variant9.Data;
using Lab2_EF_Variant9.Models;
using Microsoft.EntityFrameworkCore;

namespace Lab2_EF_Variant9.Repositories;

public class DocumentRepository : IRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Document>> GetAllAsync()
        => await _context.Documents.ToListAsync();

    public async Task<Document?> GetByIdAsync(int id)
        => await _context.Documents.FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Document document)
        => await _context.Documents.AddAsync(document);

    public void Update(Document document)
        => _context.Documents.Update(document);

    public void Delete(Document document)
        => _context.Documents.Remove(document);

    // Доменный метод 1: документы с суммой больше X
    // Примечание: Amount хранится как double в SQLite, поэтому сравниваем через AsEnumerable()
    public async Task<List<Document>> GetByAmountGreaterThanAsync(decimal amount)
    {
        var all = await _context.Documents.ToListAsync();
        return all.Where(d => d.Amount > amount).OrderByDescending(d => d.Amount).ToList();
    }

    // Доменный метод 2: документы по типу
    public async Task<List<Document>> GetByTypeAsync(string type)
    {
        var all = await _context.Documents.ToListAsync();
        return type.ToLower() switch
        {
            "receipt"  or "квитанция" => all.OfType<Receipt>().Cast<Document>().ToList(),
            "invoice"  or "накладная" => all.OfType<Invoice>().Cast<Document>().ToList(),
            "bill"     or "счёт"      => all.OfType<Bill>().Cast<Document>().ToList(),
            _ => new List<Document>()
        };
    }
}
