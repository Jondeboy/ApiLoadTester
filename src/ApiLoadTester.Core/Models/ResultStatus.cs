namespace ApiLoadTester.Core.Models;

public enum ResultStatus
{
    Success,
    HttpError,
    TlsError,
    Timeout,
    ConnectionError,
    Cancelled,
    OtherException
}
