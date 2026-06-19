namespace EventosVivos.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se intenta hacer una transición de estado inválida
/// (p.ej. confirmar una reserva ya cancelada, o cancelar una ya cancelada).
/// La capa API la traduce a HTTP 409 Conflict.
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string entityName, string currentState, string attemptedAction)
        : base($"No es posible ejecutar '{attemptedAction}' sobre {entityName} que se encuentra en estado '{currentState}'.")
    {
    }
}
