namespace Application.Results;
// beskriver affärslogiska operationer (kunde systemet uppfylla användarens begäran) - Application
public class ServiceResult 
{
    public bool Succeeded { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }
}