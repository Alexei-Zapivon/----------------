using Lab2_EF_Variant9.Data;
using Lab2_EF_Variant9.Repositories;

namespace Lab2_EF_Variant9.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private DocumentRepository? _documents;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Lazy-инициализация: репозиторий создаётся только при первом обращении
    public IRepository Documents
        => _documents ??= new DocumentRepository(_context);

    public async Task SaveAsync()
        => await _context.SaveChangesAsync();

    public async ValueTask DisposeAsync()
        => await _context.DisposeAsync();
}
