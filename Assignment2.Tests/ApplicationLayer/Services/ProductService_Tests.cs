using ApplicationLayer.DTOs;
using ApplicationLayer.Results;
using ApplicationLayer.Services;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Results;
using Domain.Helpers;
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

        // Injicera mockarna (fejkade repositories) i ProductService. .Object = själva låtsas-implementationen av IRepository<T>.
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
        ServiceResult result = await _productService.EnsureLoadedAsync(CancellationToken.None); // Tydlighet - ingen avbrytning

        // ASSERT: result ska vara lyckad
        Assert.True(result.Succeeded);
        Assert.Equal(200, result.StatusCode);

        // kontrollera att ReadAsync anropades exakt en gång
        _productRepoMock.Verify(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: EnsureLoaded ska returnera ett ServiceResult med felmeddelande
    [Fact]
    public async Task EnsureLoadedAsync_ShouldReturnError_WhenReadAsyncFails()
    {
        // ARRANGE: simulera att ReadAsync misslyckas med "Läsfel"
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.InternalServerError("Läsfel"));

        // ACT: anropa EnsureLoadedAsync
        ServiceResult result = await _productService.EnsureLoadedAsync(CancellationToken.None);

        // ASSERT: result ska signalera fel
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

        // ASSERT: result ska vara lyckad och innehålla två produkter
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

        // ASSERT: result ska signalera fel
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

        // ASSERT: result ska vara lyckad och innehålla den nya produkten
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

        // ASSERT: result ska signalera valideringsfel
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

        // ASSERT: result ska signalera valideringsfel
        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        // produkten ska inte skapas
        Assert.Null(result.Data);                        
        Assert.NotNull(result.ErrorMessage);       
        Assert.Contains("Pris måste vara större än 0.", result.ErrorMessage);

        // Kontrollera att WriteAsync aldrig anropades
        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    // Happy path: när request är giltig och befintlig produkt hittas, uppdateras namn, pris, kategori och tillverkare.
        [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProduct_WhenRequestIsValid()
    {
        // ARRANGE: befintlig produkt i repo
        Product existingProduct = new()
        {
            Id = "1",
            Name = "Banan",
            Price = 6m,
            Category = new Category { Id = "10", Name = "Grönsaker" },
            Manufacturer = new Manufacturer { Id = "20", Name = "Bananträd" }
        };

        List<Product> productList = new() { existingProduct };

        // ReadAsync: returnera listan med den befintliga produkten
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        // WriteAsync: simulera att spara lyckas
        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // Simulera kategori-repo: returnera Frukt när ReadAsync anropas
        _categoryRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Category>>.OK(
                new List<Category> { new Category { Id = "11", Name = "Frukt" } }));

        // Simulera tillverkare-repo: returnera Äppelträd när ReadAsync anropas
        _manufacturerRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Manufacturer>>.OK(
                new List<Manufacturer> { new Manufacturer { Id = "21", Name = "Äppelträd" } }));

        // Uppdateringsrequest
        ProductUpdateRequest updateRequest = new()
        {
            Id = "1",
            // trimmning ska ske
            Name = "  Äpple  ",   
            Price = 8m,
            CategoryName = "  Frukt  ",
            ManufacturerName = "  Äppelträd  "
        };

        // ACT: försök uppdatera produkten
        ServiceResult result = await _productService.UpdateProductAsync(updateRequest, CancellationToken.None);

        // ASSERT: result ska vara lyckad 
        Assert.True(result.Succeeded);
        Assert.Equal(204, result.StatusCode);

        // Kontrollera att produkten uppdaterats korrekt
        Assert.Equal("Äpple", existingProduct.Name);  
        Assert.Equal(8m, existingProduct.Price);
        Assert.NotNull(existingProduct.Category);
        Assert.Equal("Frukt", existingProduct.Category.Name);
        Assert.NotNull(existingProduct.Manufacturer);
        Assert.Equal("Äppelträd", existingProduct.Manufacturer.Name);

        // Kontrollera att WriteAsync anropades
        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: när produkt-id inte finns ska UpdateProductAsync returnera 404 NotFound.
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // ARRANGE: lista med en annan produkt än den som updateRequest anger
        List<Product> productList = new()
        {
            new Product { Id = "1", Name = "Banan", Price = 6m }
        };

        // Repo returnerar listan
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        // updateRequest matchar ingen befintlig produkt
        ProductUpdateRequest updateRequest = new()
        {
            Id = "2",                
            Name = "Äpple",
            Price = 8m
        };

        // ACT:
        ServiceResult result = await _productService.UpdateProductAsync(updateRequest, CancellationToken.None);

        // ASSERT: result ska signalera fel
        Assert.False(result.Succeeded);            
        Assert.Equal(404, result.StatusCode);      
        Assert.Equal("Produkten med Id 2 kunde inte hittas", result.ErrorMessage);
    }

    // Negative case: ogiltigt request ska ge 400 Bad Request.
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // ARRANGE: ogiltig request (namn och pris saknas)
        ProductUpdateRequest updateRequest = new()
        {
            Id = "1",
            Name = "",
            Price = null
        };

        // ACT
        ServiceResult result = await _productService.UpdateProductAsync(updateRequest, CancellationToken.None);

        // ASSERT: result ska signalera fel
        Assert.False(result.Succeeded);                
        Assert.Equal(400, result.StatusCode);         
        Assert.NotNull(result.ErrorMessage);           
        Assert.Contains("Namn måste anges.", result.ErrorMessage);   
        Assert.Contains("Pris måste anges.", result.ErrorMessage);   
    }

    // Negative case: om en befintlig produkt redan har samma namn ska metoden, returnera 409 Conflict.
    [Fact]
    public async Task UpdateProductAsync_ShouldReturnConflict_WhenDuplicateNameExists()
    {
        // ARRANGE: Två produkter i listan: updateRequest försöker uppdatera produkt 1 till samma namn som produkt 2.  
        List<Product> productList = new()
        {
            new Product { Id = "1", Name = "Banan", Price = 6m },
            new Product { Id = "2", Name = "Äpple", Price = 8m }
        };

        // Repo returnerar listan
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(productList));

        // Request försöker uppdatera produkt 1 med samma namn som produkt 2
        ProductUpdateRequest request = new ProductUpdateRequest
        {
            Id = "1",
            // ska behandlas som Äpple
            Name = "Äpple",     
            Price = 8m
        };

        // ACT
        ServiceResult result = await _productService.UpdateProductAsync(request, CancellationToken.None);

        // ASSERT: result ska signalera fel
        Assert.False(result.Succeeded);                
        Assert.Equal(409, result.StatusCode);          
        Assert.NotNull(result.ErrorMessage);           
        Assert.Contains("En produkt med namnet Äpple finns redan.", result.ErrorMessage); 
    }



    // Happy path: produkten finns och ska tas bort
    [Fact]
    public async Task DeleteProductAsync_ShouldRemoveProduct_WhenProductExists()
    {
        // ARRANGE: skapa produkten som ska tas bort
        Product existingProduct = new() { Id = "1", Name = "Banan", Price = 6m };

        // Simulera att ReadAsync returnerar en lista med produkten
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product> { existingProduct }));

        // Simulera att WriteAsync lyckas när listan sparas efter borttagning
        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // ACT: anropa DeleteProductAsync och skicka in Id för produkten som ska tas bort
        ServiceResult result = await _productService.DeleteProductAsync("1", CancellationToken.None);

        // ASSERT: result ska vara lyckad
        // null-check innan nästa steg
        Assert.NotNull(result); 
        Assert.True(result.Succeeded);
        Assert.Equal(204, result.StatusCode); 
        Assert.Null(result.ErrorMessage);

        // Kontrollera att WriteAsync anropades
        _productRepoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Negative case: produkten finns inte i listan
    [Fact]
    public async Task DeleteProductAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // ARRANGE: simulera att ReadAsync returnerar en tom lista
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product>()));

        // ACT
        ServiceResult result = await _productService.DeleteProductAsync("3", CancellationToken.None);

        // ASSERT: result ska signalera fel med korrekt felmeddelande
        // null-check
        Assert.NotNull(result); 
        Assert.False(result.Succeeded); 
        Assert.Equal(404, result.StatusCode); 
        Assert.Equal("Produkten med Id 3 kunde inte hittas", result.ErrorMessage); 
    }

    // Negative case: WriteAsync-fel: misslyckas att spara ändringen efter borttagning
    [Fact]
    public async Task DeleteProductAsync_ShouldReturnError_WhenWriteAsyncFails()
    {
        // ARRANGE: skapa produkten som ska tas bort
        Product existingProduct = new Product { Id = "1", Name = "Banan", Price = 6m };

        // Simulera att ReadAsync returnerar listan med produkten
        _productRepoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<Product>>.OK(new List<Product> { existingProduct }));

        // Simulera att WriteAsync misslyckas att spara
        _productRepoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.InternalServerError("Ett okänt fel uppstod vid filsparning"));

        // ACT
        ServiceResult result = await _productService.DeleteProductAsync("1", CancellationToken.None);

        // ASSERT: result ska signalera fel med korrekt felmeddelande
        Assert.NotNull(result); 
        Assert.False(result.Succeeded); 
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("Ett okänt fel uppstod vid filsparning", result.ErrorMessage);
    }
}
