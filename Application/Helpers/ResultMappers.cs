using ApplicationLayer.Results;
using Domain.Results;

namespace ApplicationLayer.Helpers;
// Översätter RepoResult till ServiceResult. Ger läsbarhet och mindre duplicering, men behåller specificitet i felmeddelanden. 

public static class ResultMappers
{
    public static ServiceResult MapToServiceResult(
    // Extension-metod på RepositoryResult
    this RepositoryResult repoResult, 
    // Om servicelagret inte skickar med ett eget error-meddelande vid anropning, används det som finns i repoResult.ErrorMessage
    string? customErrorMessage = null,
    int? overrideStatusCode = null)
    {
        if (!repoResult.Succeeded)
        {
            return new ServiceResult
            {
                Succeeded = false,
                // Ternary operator (condition ? true : false) Failsafe,det skickas alltid med statuskoder nu
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
                ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering."
            };
        }

        return new ServiceResult
        {
            Succeeded = true,
            StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 200 : repoResult.StatusCode)
        };
    }



    public static ServiceResult<T> MapToServiceResult<T>(
        this RepositoryResult<T> repoResult,
        string? customErrorMessage = null,
        int? overrideStatusCode = null)
    {
        if (!repoResult.Succeeded || repoResult.Data is null)
        {
            return new ServiceResult<T>
            {
                Succeeded = false,
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
                ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering.",
                Data = default
            };
        }

        return new ServiceResult<T>
        {
            Succeeded = true,
            StatusCode = repoResult.StatusCode,
            Data = repoResult.Data
        };

    }

    // Mappar icke-generiskt RepositoryResult till ServiceResult<T>.
    public static ServiceResult<T> MapToServiceResult<T>(
    this RepositoryResult repoResult,
    string? customErrorMessage = null,
    int? overrideStatusCode = null)
    {
        if (!repoResult.Succeeded)
        {
            return new ServiceResult<T>
            {
                Succeeded = false,
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
                ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering.",
                Data = default
            };
        }

        return new ServiceResult<T>
        {
            Succeeded = true,
            StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 200 : repoResult.StatusCode),
            Data = default
        };
    }
}

/*
Tydlighet - Scenarion:
Read (Get):
Success = explicit 200 + Data (även tom lista).
Fel = mapper (404/500).

Write (Create/Update/Delete):
Success = explicit (201 vid Create, 204 vid Update/Delete).
Fel = mapper (404/409/500).
*/
