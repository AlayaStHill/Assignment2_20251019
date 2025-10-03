namespace Domain.Results;
// beskriver tekniska operationer (mekaniken runtomkring affärslogiken - hur data transporteras eller lagras) - Infrastructure
public class RepositoryResult
{
    public bool Succeeded { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RepositoryResult<T> : RepositoryResult
{
    public T? Data { get; set; }
}