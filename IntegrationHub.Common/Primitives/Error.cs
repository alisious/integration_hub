namespace IntegrationHub.Common.Primitives;
public readonly record struct Error(
    string Code,
    string Message,
    int? HttpStatus = null,
    string? Details = null);