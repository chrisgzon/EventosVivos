namespace EventosVivos.Application.Interfaces;

/// <summary>
/// Abstracción del proveedor de fecha/hora.
/// Permite inyectar fechas fijas en pruebas unitarias sin acoplarse a DateTime.UtcNow.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
