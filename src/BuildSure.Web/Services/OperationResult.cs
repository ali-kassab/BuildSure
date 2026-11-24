namespace BuildSure.Web.Services;

/// <summary>Small result object so the service can report a business-rule failure without throwing.</summary>
public class OperationResult
{
    public bool Succeeded { get; private init; }
    public string? Error { get; private init; }

    public static OperationResult Success() => new() { Succeeded = true };
    public static OperationResult Failure(string error) => new() { Succeeded = false, Error = error };
}
