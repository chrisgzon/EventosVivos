namespace EventosVivos.Domain.Exceptions;

/// <summary>
/// Se lanza cuando se viola una regla de negocio (RN01-RN07).
/// La capa API la traduce a HTTP 422 Unprocessable Entity.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public string RuleCode { get; }

    public BusinessRuleViolationException(string ruleCode, string message)
        : base(message)
    {
        RuleCode = ruleCode;
    }
}
