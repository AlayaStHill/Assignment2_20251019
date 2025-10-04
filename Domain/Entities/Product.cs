namespace Domain.Entities;

public class Product
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public Category Category { get; set; } = null!;
    public Manufacturer Manufacturer { get; set; } = null!;
}
/*
ProductRequest (CreateProductRequest): har bara de fält som krävs för att skapa en produkt - minimalcreate-request (namn, pris)

UpdateProductRequest: kan tillåta fler fält (inklusive beskrivning).

Product (entitet): innehåller allt som en produkt kan ha, både användarstyrt och systemgenererat.
*/