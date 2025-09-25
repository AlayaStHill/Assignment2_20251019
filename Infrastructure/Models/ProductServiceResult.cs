namespace Infrastructure.Models;

public class ProductServiceResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}

public class ProductServiceResult<T> : ProductServiceResult
{
    public T? Data { get; set; }
}