namespace EventosVivos.Domain.Exceptions;

/// <summary>
/// Excepción base para todos los errores de dominio/negocio de EventosVivos.
/// Permite que la capa API los distinga de errores técnicos no controlados.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
