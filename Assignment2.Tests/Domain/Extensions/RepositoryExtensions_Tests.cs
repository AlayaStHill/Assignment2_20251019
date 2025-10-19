using Domain.Interfaces;
using Domain.Results;
using Domain.Extensions;
using Moq;
// Jag har använt AI och promptteknik som stöd i arbetet med att skriva testerna.
namespace Assignment2.Tests.Domain.Extensions;

public class RepositoryExtensions_Tests
{
    private readonly Mock<IRepository<TestEntity>> _repoMock;
    public RepositoryExtensions_Tests()
    {
        // Ny mock för varje testmetod då xUnit automatisk skapar ny testklass-instans för varje test
        _repoMock = new Mock<IRepository<TestEntity>>();
    }

    // Minimal dummy-entitet för att kunna testa extension-metoden. Måste vara publik för att den exponeras i en publik metods signatur (GetOrCreateAsync), annars kompileringsfel "Inconsistent accessibility" och rött test
    public class TestEntity
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }

    // Happy path: test av match-funktion
    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnExistingEntity_WhenMatchIsFound()
    {
        // ARRANGE: en match ska finnas  
        TestEntity existing = new() { Id = "1", Name = "Banan" };

        // Simulera att ReadAsync returnerar en lista med en entitet som matchar
        _repoMock
            .Setup(mockRepo => mockRepo.ReadAsync(It.IsAny<CancellationToken>()))
            // När mocken anropas ska ett lyckat resultat som innehåller en lista med entiteten existing returneras. (OK() förväntar sig en IEnumerable<TestEntity> och List<TestEntity>[] implementerar det interfacet)
            .ReturnsAsync(RepositoryResult<IEnumerable<TestEntity>>.OK(new List<TestEntity> { existing }));

        // ACT: Försök hitta en entitet med Name = Banan. Om ingen match, skapa en ny entitet med namnet Banan. .Object = den fejkade IRepository<TestEntity> som mocken exponerar
        RepositoryResult<TestEntity> result = await _repoMock.Object.GetOrCreateAsync(
            entity => entity.Name == "Banan",
            () => new TestEntity { Id = "1", Name = "Banan" },
            CancellationToken.None);

        // ASSERT: match ska finnas
        Assert.True(result.Succeeded);
        Assert.Equal(existing, result.Data);

        // Verify kontrollerar att WriteAsync aldrig anropades på mockRepo (Times.Never), eftersom ingen ny entitet skulle skrivas till fil i detta testfall.
        _repoMock.Verify(mockRepo => mockRepo.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    /*
    Mock<IRepository<TestEntity>>: 
        - .Object är en property i Mock<T>-klassen. .Object implementerar IRepository<T> = agerar en låtsas-IRepository<T>
        - hur .Object ska agera i testet styrs av Mock<IRepository<TestEntity>>-instansen via metoder som.Setup(...), .Returns(...), .Verify(...)...
    */



    // Happy path: test av create-funktion
    [Fact]
    public async Task GetOrCreateAsync_ShouldCreateEntity_WhenNoMatchIsFound()
    {
        // ARRANGE: ReadAsync returnerar en tom lista. FirstOrDefault(isMatch) = ingen match hittas. Ger entity = null -> CreateEntity() anropas
        _repoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<TestEntity>>.OK(new List<TestEntity>()));

        // WriteAsync returnerar ett lyckat resultat
        _repoMock
            .Setup(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult.NoContent());

        // ACT: Ingen entitet med Name = Banan finns och en ny ska skapas
        RepositoryResult<TestEntity> result = await _repoMock.Object.GetOrCreateAsync(
            entity => entity.Name == "Banan",
            () => new TestEntity { Id = "1", Name = "Banan" },
            CancellationToken.None);

        // ASSERT: ny entitet ska vara skapad
        Assert.True(result.Succeeded);
        Assert.Equal("Banan", result.Data!.Name);

        // Kontrollera att WriteAsync anropades för att spara den nya entiteten
        _repoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Once);
    }



    // Negative case: GetOrCreateAsync ska misslyckas när ReadAsync misslyckas
    [Fact]
    public async Task GetOrCreateAsync_ShouldReturnError_WhenReadAsyncFails()
    {
        // ARRANGE: simulerar att ReadAsync returnerar ett InternalServerError med felmeddelandet "Läsfel"
        _repoMock
            .Setup(repoMock => repoMock.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(RepositoryResult<IEnumerable<TestEntity>>.InternalServerError("Läsfel"));

        // ACT: eftersom ReadAsync misslyckas ska GetOrCreateAsync misslyckas 
        RepositoryResult<TestEntity> result = await _repoMock.Object.GetOrCreateAsync(
            entity => entity.Name == "Banan",
            () => new TestEntity { Id = "1", Name = "Banan" },
            CancellationToken.None);

        // ASSERT: resultatet ska signalera fel och vidarebefordra samma felkod och felmeddelande
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal("Läsfel", result.ErrorMessage);

        // Kontrollera att WriteAsync aldrig anropades när ReadAsync misslyckades
        _repoMock.Verify(repoMock => repoMock.WriteAsync(It.IsAny<IEnumerable<TestEntity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
