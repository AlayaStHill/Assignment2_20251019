using ApplicationLayer.DTOs;
using ApplicationLayer.Interfaces;
using ApplicationLayer.Results;
using Domain.Entities;
using System.Xml.Linq;

namespace ApplicationLayer.Services;

public partial class ProductService
{
    private static ServiceResult ValidateRequest(IProductRequest request)
    {
        if (request is null)
            return new ServiceResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Ingen data skickades in." };

        if (string.IsNullOrWhiteSpace(request.Name) || request.Price < 0)
            return new ServiceResult { Succeeded = false, StatusCode = 400, ErrorMessage = "Fälten namn och pris är inte korrekt ifyllda." };

        return new ServiceResult { Succeeded = true, StatusCode = 200 };
    }

    private Product? FindExistingProduct(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return _products.FirstOrDefault(product => product.Id == id);
    }

    // Finns en produkt i _products, vars ID inte är = requestId men vars namn är = requestNamn. Använda på CreateRequest då id valbart
    private bool IsDuplicateName(string requestName, string? requestId = null)
    {
        return _products.Any(product => (requestId is null || product.Id != requestId && string.Equals(product.Name, requestName, StringComparison.OrdinalIgnoreCase)));
    }
}

