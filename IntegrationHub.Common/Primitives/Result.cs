namespace IntegrationHub.Common.Primitives;
public readonly record struct Result<T, TError>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public TError? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    // Niejawne konwersje – pozwalają zwracać T albo TError bez „new Result<...>(...)”
    public static implicit operator Result<T, TError>(T value) => new(value);
    public static implicit operator Result<T, TError>(TError error) => new(error);

    // Wygodny pattern-matching
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<TError, TResult> onError)
        => IsSuccess ? onSuccess(Value!) : onError(Error!);

    // Deconstruction, jeśli lubisz tuple-style
    public void Deconstruct(out bool isSuccess, out T? value, out TError? error)
    {
        isSuccess = IsSuccess; value = Value; error = Error;
    }
}
