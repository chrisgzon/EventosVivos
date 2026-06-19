namespace EventosVivos.Domain.Exceptions;

/// <summary>
/// Se lanza cuando una entidad solicitada (evento, venue, reserva) no existe.
/// La capa API la traduce a HTTP 404.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} con identificador '{key}' no fue encontrado.")
    {
    }
}
