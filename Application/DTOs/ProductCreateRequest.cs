using System.ComponentModel.DataAnnotations;

namespace ApplicationLayer.DTOs;
// Data Transfer Object (DTO). Används bara för att flytta data mellan lager (UI -> ProductService)
public class ProductCreateRequest
{
    [Required(ErrorMessage = "Fyll i namn")]
    public string Name { get; set; } = null!;
    [Range(1, int.MaxValue, ErrorMessage = "Priset måste vara mer än 0")]
    public decimal Price { get; set; }
}

// Med binding i UI, kan lägga till ErrorMessage???