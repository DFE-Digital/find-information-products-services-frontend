using System.Diagnostics;

namespace FipsFrontend.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public Exception? Exception { get; set; }
    public string? ExceptionDetails { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool ShowException => Exception != null;
    public bool ShowExceptionDetails => !string.IsNullOrEmpty(ExceptionDetails);
}
