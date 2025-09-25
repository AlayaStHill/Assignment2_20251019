namespace Infrastructure.Models;

public class FileRepositoryResult
{
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}

public class FileRepositoryResult<T> : FileRepositoryResult
{
    public T? Data { get; set; }
}