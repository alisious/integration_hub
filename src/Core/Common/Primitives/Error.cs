namespace IntegrationHub.Common.Primitives;
public readonly record struct Error(
    string Code,
    string Message,
    int? HttpStatus = null,
    string? Details = null,
    ErrorKindEnum ErrorKind = ErrorKindEnum.Unspecified
);

public static class ErrorFactory
{
    public static Error BusinessError(ErrorCodeEnum codeEnum, string message, int? httpStatus = null, string? details = null)
        => new(codeEnum.ToString(), message, httpStatus, details, ErrorKindEnum.Business);

    public static Error TechnicalError(ErrorCodeEnum codeEnum, string message, int? httpStatus = null, string? details = null)
        => new(codeEnum.ToString(), message, httpStatus, details, ErrorKindEnum.Technical);
}
