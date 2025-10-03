namespace Application.DTOs;
// Data Transfer Object (DTO). Används bara för att flytta data mellan lager (UI -> ProductService). Id = en pekare till en befintlig Product-entitey.
public class ProductUpdateRequest
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = null!;
    public string ManufacturerName { get; set; } = null!;
}