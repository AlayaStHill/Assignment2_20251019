namespace Infrastructure.Models;

public class ProductServiceResult // Lägga till public int StatusCode { get; set; } // vad menas??
{
    public bool Succeeded { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ProductServiceResult<T> : ProductServiceResult
{
    public T? Data { get; set; }
}