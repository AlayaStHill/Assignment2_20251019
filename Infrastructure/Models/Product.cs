namespace Infrastructure.Models;

public class Product
{
    public string ProductId { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public decimal Price { get; set; }
    public Category Category { get; set; } = null!;
    public Manufacturer Manufacturer { get; set; } = null!;
}
/*
ProductRequest (CreateProductRequest): har bara de fält som krävs för att skapa en produkt - minimalcreate-request (namn, pris)

UpdateProductRequest: kan tillåta fler fält (inklusive beskrivning).

Product (din entitet): innehåller allt som en produkt kan ha, både användarstyrt och systemgenererat.
*/