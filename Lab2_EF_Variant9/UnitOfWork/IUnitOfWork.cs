using Lab2_EF_Variant9.Repositories;

namespace Lab2_EF_Variant9.UnitOfWork;

// Unit of Work — одна точка сохранения изменений
public interface IUnitOfWork : IAsyncDisposable
{
    IRepository Documents { get; }
    Task SaveAsync();
}
