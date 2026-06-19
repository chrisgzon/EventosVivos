namespace EventosVivos.Application.Interfaces;

/// <summary>
/// Abstracción de la unidad de trabajo para controlar transacciones
/// desde la capa Application sin acoplarla a EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
