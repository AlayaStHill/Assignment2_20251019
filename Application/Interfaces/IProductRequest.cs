namespace ApplicationLayer.Interfaces;
internal interface IProductRequest
{
    string Name { get; }
    // Gör nullable för att wpf ska kunna binda nullvärde och på så vis inte släppa igenom att pris inte är angett, genom att automatiskt sätta default-värde. Null-värde fångas upp i ProductService.Helpers ValidateRequest() och ProductAdd- och EditViewModel Save().
    decimal? Price { get; } 
}
