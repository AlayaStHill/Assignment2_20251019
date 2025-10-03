namespace Infrastructure.Models;
// Data Transfer Object (DTO). Används bara för att flytta data mellan lager (UI -> ProductService)
public class ProductRequest
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}