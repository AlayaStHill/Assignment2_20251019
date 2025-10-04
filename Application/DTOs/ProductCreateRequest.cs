namespace ApplicationLayer.DTOs;
// Data Transfer Object (DTO). Används bara för att flytta data mellan lager (UI -> ProductService)
public class ProductCreateRequest
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}