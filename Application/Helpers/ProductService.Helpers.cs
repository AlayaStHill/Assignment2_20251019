using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;

namespace ApplicationLayer.Services;

public partial class ProductService
{
    private static ServiceResult ValidateRequest(IProductRequest request)
    {
        if (request is null)
            return new ServiceResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Ingen data skickades in." };

        if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
            return new ServiceResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Fälten namn och pris är inte korrekt ifyllda." };

        return new ServiceResult { Succeeded = true, StatusCode = 200 };
    }
}