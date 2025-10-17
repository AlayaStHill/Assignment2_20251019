using ApplicationLayer.Results;
using ApplicationLayer.Services;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Results;
using Moq;


// Jag har använt AI och promptteknik som stöd i arbetet med att skriva testerna.

namespace Assignment2.Tests.ApplicationLayer.Services;

public class ProductService_Tests
{
    private readonly Mock<IRepository<Product>> _productRepoMock;
    private readonly Mock<IRepository<Category>> _categoryRepoMock;
    private readonly Mock<IRepository<Manufacturer>> _manufacturerRepoMock;
    // Testad klass. Skapas i konstruktorn för att slippa new:a upp i varje testmetod.
    private readonly ProductService _productService; 

    public ProductService_Tests()
    {
        // skapa nya mocks för varje test (xUnit gör en ny instans av testklassen för varje [Fact]).
        _productRepoMock = new Mock<IRepository<Product>>();
        _categoryRepoMock = new Mock<IRepository<Category>>();
        _manufacturerRepoMock = new Mock<IRepository<Manufacturer>>();

        // Injicera mockarna (fejkade repositories) i ProductService. .Object = själva "låtsas-implementationen" av IRepository<T>.
        _productService = new(
            _productRepoMock.Object,
            _categoryRepoMock.Object,
            _manufacturerRepoMock.Object
        );
    }

    // Happy path
    [Fact]
    public async Task EnsureLoadedAsync_ShouldLoadProducts_WhenRepositoryReturnsData()
    {
        // ARRANGE: simulera att ReadAsync returnerar en lista med en produkt
        List<Product> products = new() { new Product { Id = "1", Name = "Banan", Price = 6m } };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(products));

        // ACT: anropa EnsureLoadedAsync
        ServiceResult result = await _productService.EnsureLoadedAsync(CancellationToken.None); // Tydlighet, ingen avbrytning

        // ASSERT: resultatet ska lyckas och statuskoden vara 200
        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);

        // kontrollera att ReadAsync anropades en gång
        _productRepoMock.Verify(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }


    // Negative case: EnsureLoaded ska returnera ett ServiceResult med errormeddelande
    [Fact]
    public async Task EnsureLoadedAsync_ShouldReturnError_WhenReadAsyncFails()
    {
        // ARRANGE: simulera att ReadAsync misslyckas med "Läsfel"
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.InternalServerError("Läsfel"));

        // ACT: anropa EnsureLoadedAsync
        ServiceResult result = await _productService.EnsureLoadedAsync(CancellationToken.None);

        // ASSERT: resultatet ska vara misslyckat
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        // Säkerställ att Data inte är null innan nästa steg
        Assert.NotNull(result.ErrorMessage);               
        Assert.Equal("Ett okänt fel uppstod vid filhämtning", result.ErrorMessage);
    }



}
