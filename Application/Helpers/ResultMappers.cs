using ApplicationLayer.Results;
using Domain.Results;

namespace ApplicationLayer.Helpers;
// Ger läsbarhet och mindre duplicering, men behåller specificitet i felmeddelanden.
public static class ResultMappers
{

    public static ServiceResult MapToServiceResult(
    // Extension-metod på RepositoryResult
    this RepositoryResult repoResult, 
    // Om servicelagret inte skickar med ett eget error-meddelande vid anropning, används det som finns i repoResult.ErrorMessage
    string? customErrorMessage = null,
    int? overrideStatusCode = null)
    {
        if (repoResult.Succeeded)
        {
            return new ServiceResult
            {
                Succeeded = true,
                // Ternary operator (condition ? true : false) Failsafe,det skickas alltid med statuskoder nu
                StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 200 : repoResult.StatusCode)
            };
        }

        return new ServiceResult
        {
            Succeeded = false,
            StatusCode = overrideStatusCode ?? (repoResult.StatusCode == 0 ? 500 : repoResult.StatusCode),
            ErrorMessage = customErrorMessage ?? repoResult.ErrorMessage ?? "Ett okänt fel uppstod vid filhantering."
        };
    }

}

//var saveResult = await _productRepository.WriteAsync(_products, _cts.Token);
//return saveResult.MapToServiceResult("Ett okänt fel inträffade vid filsparning");