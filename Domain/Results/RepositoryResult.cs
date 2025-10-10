namespace Domain.Results;
// beskriver tekniska operationer (mekaniken runtomkring affärslogiken - hur data transporteras eller lagras) - Infrastructure
public class RepositoryResult
{
    public bool Succeeded { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static RepositoryResult NoContent()
    {
        return new RepositoryResult
        {
            Succeeded = true,
            StatusCode = 204,
        };
    }

    public static RepositoryResult InternalServerError(string errorMessage)
    {
        return new RepositoryResult
        {
            Succeeded = false,
            StatusCode = 500,
            ErrorMessage = errorMessage
        };
    }


}

public class RepositoryResult<T> : RepositoryResult
{
    public T? Data { get; set; }


    public static RepositoryResult<T> OK(T? data)
    {
        return new RepositoryResult<T>
        {
            Succeeded = true,
            StatusCode = 200,
            Data = data
        };
    }

    public static new RepositoryResult<T> InternalServerError(string errorMessage)
    {
        return new RepositoryResult<T>
        {
            Succeeded = false,
            StatusCode = 500,
            ErrorMessage = errorMessage
        };
    }

    public static RepositoryResult<T> Created(T? data)
    {
        return new RepositoryResult<T>
        {
            Succeeded = true,
            StatusCode = 201,
            Data = data 
        };
    }



    public static RepositoryResult<T> NotFound(string errorMessage)
    {
        return new RepositoryResult<T>
        {
            Succeeded = false,
            StatusCode = 404,
            ErrorMessage = errorMessage
        };
    }

    public static RepositoryResult<T> Conflict(string errorMessage)
    {
        return new RepositoryResult<T>
        {
            Succeeded = false,
            StatusCode = 409,
            ErrorMessage = errorMessage
        };
    }
}


