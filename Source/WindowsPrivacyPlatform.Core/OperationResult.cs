namespace WindowsPrivacyPlatform.Core;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}

public class OperationResult<T> : OperationResult
{
    public T? Data { get; set; }
}
