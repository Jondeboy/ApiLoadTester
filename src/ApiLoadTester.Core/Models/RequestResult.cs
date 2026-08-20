namespace ApiLoadTester.Core.Models;

public sealed class RequestResult
{
    public long SequenceNumber { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public double LatencyMs { get; init; }
    public int? StatusCode { get; init; }
    public required ResultStatus Status { get; init; }
    public string? ErrorType { get; init; }
    public string? ErrorMessage { get; init; }
    public long ResponseBytes { get; init; }

    public bool IsSuccess => Status == ResultStatus.Success;
}
