using ApplicationLayer.DTOs;
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
        List<Product> productList = new() { new Product { Id = "1", Name = "Banan", Price = 6m } };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

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



    // Happy path
    [Fact]
    public async Task GetProductsAsync_ShouldReturnProducts_WhenEnsureLoadedSucceeds()
    {
        // ARRANGE: simulera en lista av produkter från repository
        List<Product> productList = new()
        {
            new Product { Id = "1", Name = "Banan", Price = 6m },
            new Product { Id = "2", Name = "Äpple", Price = 8m }
        };

        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        // ACT: hämta produkter
        ServiceResult<IEnumerable<Product>> result = await _productService.GetProductsAsync(CancellationToken.None);

        // ASSERT: resultatet ska lyckas och innehålla två produkter
        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);
        // Säkerställ att Data inte är null innan nästa steg
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count());
        Assert.Contains(result.Data!, product => product.Name == "Banan");
        Assert.Contains(result.Data!, product => product.Name == "Äpple");
    }

    // Negative case: GetProductsAsync ska returnera fel med tom produktlista
    [Fact]
    public async Task GetProductsAsync_ShouldReturnError_WhenEnsureLoadedFails()
    {
        // ARRANGE: simulera att ReadAsync misslyckas
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.InternalServerError("Läsfel"));

        // ACT: anropa GetProductsAsync
        ServiceResult<IEnumerable<Product>> result = await _productService.GetProductsAsync(CancellationToken.None);

        // ASSERT: resultatet ska signalera fel
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.NotNull(result.Data);                        
        Assert.Empty(result.Data);                          
        Assert.NotNull(result.ErrorMessage);                
        Assert.Equal("Ett okänt fel uppstod vid filhämtning", result.ErrorMessage);
    }


    // Happy path: när request är giltig (validering lyckas) ska en ny produkt skapas och sparas.
    [Fact]
    public async Task SaveProductAsync_ShouldCreateAndSaveProduct_WhenValidRequest()
    {
        // ARRANGE: giltig request
        ProductCreateRequest createRequest = new()
        {
            // mellanslag som ska trimmas bort
            Name = "  Banan  ",    
            Price = 6m
        };

        // EnsureLoadedAsync: simulera tom lista från ReadAsync
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product>()));

        // WriteAsync: simulera lyckad sparning
        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // ACT: försök spara produkten
        ServiceResult<Product> result = await _productService.SaveProductAsync(createRequest, CancellationToken.None);

        // ASSERT: resultatet ska lyckas och innehålla den nya produkten
        Assert.True(result.Succeeded);
        Assert.Equal(201, result.StatusCode);
        Assert.NotNull(result.Data); 
        // namnet ska vara trimmat
        Assert.Equal("Banan", result.Data.Name);     
        Assert.Equal(6m, result.Data.Price);

        // Kontrollera att WriteAsync anropades en gång
        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: när request är ogiltig (valideringen misslyckas) ska sparning inte ske.
    [Fact]
    public async Task SaveProductAsync_ShouldReturnError_WhenRequestIsInvalid()
    {
        // ARRANGE: ogiltigt request utan namn och pris (requestet innehåller både tomt namn och null-pris för att utlösa flera valideringsfel i samma test)
        ProductCreateRequest createRequest = new()
        {
            Name = "",
            Price = null
        };

        // ACT: försök spara produkten
        ServiceResult<Product> result = await _productService.SaveProductAsync(createRequest, CancellationToken.None);

        // ASSERT: resultatet ska signalera valideringsfel
        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        // produkten ska inte finnas
        Assert.Null(result.Data);                         
        Assert.NotNull(result.ErrorMessage);              
        Assert.Contains("Namn måste anges.", result.ErrorMessage);
        Assert.Contains("Pris måste anges.", result.ErrorMessage);

        // Kontrollera att WriteAsync aldrig anropades (eftersom request var ogiltig)
        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Negative case: när pris är 0 eller negativt ska validering misslyckas.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task SaveProductAsync_ShouldReturnError_WhenPriceIsZeroOrNegative(decimal invalidPrice)
    {
        // ARRANGE: request med giltigt namn men ogiltigt pris
        ProductCreateRequest createRequest = new()
        {
            Name = "Banan",
            Price = invalidPrice
        };

        // ACT: försök spara produkten
        ServiceResult<Product> result = await _productService.SaveProductAsync(createRequest, CancellationToken.None);

        // ASSERT: resultatet ska signalera valideringsfel
        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        // produkten ska inte skapas
        Assert.Null(result.Data);                        
        Assert.NotNull(result.ErrorMessage);       
        Assert.Contains("Pris måste vara större än 0.", result.ErrorMessage);

        // Kontrollera att WriteAsync aldrig anropades
        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
