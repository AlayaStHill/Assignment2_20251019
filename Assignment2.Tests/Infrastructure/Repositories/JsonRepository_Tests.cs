using Domain.Results;
using Infrastructure.Repositories;
using System.Text.Json;
// Jag har använt AI och promptteknik som stöd i arbetet med att skriva testerna.

namespace Assignment2.Tests.Infrastructure.Repositories;

// IDisposable innehåller metoden Dispose. Implementation gör att xUnit automatiskt anropar Dispose() efter varje [Fact] = slipper upprepa städkod i varje test.
public class JsonRepository_Tests : IDisposable 
{
    // I varje [Fact]-metod skapar xUnit-ramverket en ny instans av testklassen, dvs new JsonRepository_Tests(). Konstruktorn körs alltså för varje testmetod och testfälten är unika för just det testet. Efter varje test släpper xUnit alla referenser till instansen -> garbage collector.
    private readonly string _testDirectory;
    private readonly string _testFilePath;

    public JsonRepository_Tests() 
    {
        // Sätter strängfälten. Path.Combine bygger filsökvägen till de angivna mapparna och ger ex: C:\...\Temp\JsonRepoTests\1f0c1b8b2b6a4fd4b6b5b3a6e31b4e07\ med ett temporärt och garanterat unikt testDirectory.
        _testDirectory = Path.Combine(Path.GetTempPath(), "JsonRepoTests", Guid.NewGuid().ToString("N")); // Förklaring sist i filen
        // Lägg filnamnet test.json inne i den här katalogen
        _testFilePath = Path.Combine(_testDirectory, "test.json");
    }

    // JsonRepository<T> kräver en klass att serialisera. Minimal dummy-entitet som får motsvara Product för att kunna skriva/läsa json i testet. Själva testdatan skapas i varje test.
    // Private: används bara internt i testprojektets kontext
    private class TestEntity
    {
        // tom sträng för att slippa hantera null-checkar, -exceptions
        public string Id { get; set; } = ""; 
        public string Name { get; set; } = ""; 
    }

    // Städfunktion som anropas efter varje test-metod = inga kvarlämnade filer inför nästa test. Parametern recursive: true = ta bort katalogen och allt inuti.
    public void Dispose()
    {
        try 
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true); 
        }
        catch
        {
            // Ignorera städfel = anledningen till try-catch. Ett undantag pga städfel säger inget om den kod som faktiskt testas och ska inte få ge ett rött test (är testinfrastruktur).
            // Guid-sökvägar = ingen konflikt om allt inte städas bort. Best effort
        }
    }

    // Happy path
    [Fact]
    public void EnsureInitialized_ShouldCreateDirectoryAndFile_WhenTheyDoNotExist()
    {
        // ARRANGE (förberedelser/premiss): Katalog och fil ska inte finnas.
        Assert.False(Directory.Exists(_testDirectory));
        Assert.False(File.Exists(_testFilePath));

        // ACT (utför) EnsureInitialized ska skapa katalog och fil med innehållet []
        JsonRepository<TestEntity>.EnsureInitialized(_testDirectory, _testFilePath);

        // ASSERT (verifiera) Katalog och fil ska finnas
        Assert.True(Directory.Exists(_testDirectory));
        Assert.True(File.Exists(_testFilePath));
        Assert.Equal("[]", File.ReadAllText(_testFilePath)); // Filens innehåll är []
    }

    // Happy path
    [Fact]
    public void EnsureInitialized_ShouldOverwriteWithEmptyArray_WhenFileContainsOnlyWhitespace()
    {
        // ARRANGE: skapa katalogen. Skapa filen och skriv in whitespace-tecken. 
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFilePath, "   \r\n\t  ");

        // ACT: EnsureInitialized ska skriva in en tom array i filen
        JsonRepository<TestEntity>.EnsureInitialized(_testDirectory, _testFilePath);

        // ASSERT
        Assert.Equal("[]", File.ReadAllText(_testFilePath));
    }

    // Happy path
    [Fact]
    public void EnsureInitialized_ShouldOverwriteWithEmptyArray_WhenFileIsEmpty()
    {
        // ARRANGE: skapa katalogen och en tom fil
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFilePath, string.Empty);
        Assert.True(File.Exists(_testFilePath));

        // ACT: EnsureInitialized ska skriva in en tom array i filen
        JsonRepository<TestEntity>.EnsureInitialized(_testDirectory, _testFilePath);

        // ASSERT: filen ska innehålla en tom array
        Assert.Equal("[]", File.ReadAllText(_testFilePath));
    }


    // Happy-path
    [Fact]
    public async Task WriteAsync_ShouldWriteJsonAndReturnSucceeded_WhenEntitiesProvided()
    {
        // ARRANGE: skapa repository för TestEntity med katalog och fil och data som ska sparas
        JsonRepository<TestEntity> repo = new JsonRepository<TestEntity>(_testDirectory, "test.json");
        List<TestEntity> entities = new()
        {
            new() { Id = "1", Name = "Banan" },
            new() { Id = "2", Name = "Äpple" }
        };

        // ACT: skriv entiteterna till fil
        RepositoryResult result = await repo.WriteAsync(entities, CancellationToken.None);

        // ASSERT: metoden lyckas och filen har skapats
        Assert.True(result.Succeeded);
        Assert.True(File.Exists(_testFilePath));

        // Läs in innehållet i filen som en sträng
        string json = File.ReadAllText(_testFilePath);
        // deserialisera för att kunna asserta innehållet (? roundtrip blir null om deserialiseringen misslyckas)
        List<TestEntity>? roundtrip = JsonSerializer.Deserialize<List<TestEntity>>(json);
        // säkerställer att roundtrip inte är null inför nästa steg
        Assert.NotNull(roundtrip);
        // Kontrollera att listan har rätt antal och varje objekt har rätt värden
        Assert.Equal(2, roundtrip!.Count);
        Assert.Equal("1", roundtrip[0].Id);
        Assert.Equal("Banan", roundtrip[0].Name);
        Assert.Equal("2", roundtrip[1].Id);
        Assert.Equal("Äpple", roundtrip[1].Name);

        // roundtrip-test: (att skriva data till fil och sedan läsa tillbaka) för att verifiera att sparad data i filen motsvarar originaldatan
    }


    // Negative case test. Verifiera att WriteAsync mappar fil-I/O (läsa/skriva json)-problem till InternalServerError 
    [Fact]
    public async Task WriteAsync_ShouldReturnInternalServerError_WhenFileIsLocked()
    {
        // ARRANGE: skapa filen och lås den exklusivt med FileStream
        Directory.CreateDirectory(_testDirectory);
        File.WriteAllText(_testFilePath, "[]");

        JsonRepository<TestEntity> repo = new JsonRepository<TestEntity>(_testDirectory, "test.json");

        // Filen öppnas och låses (using = Dispose körs automatiskt när scopet tar slut)
        using FileStream lockStream = new(
            _testFilePath,
            FileMode.Open,
            FileAccess.Read,
            // Anger vilka andra processer som får använda filen samtidigt
            FileShare.None);

        List<TestEntity> entities = new() { new() { Id = "1", Name = "Banan" } };

        // ACT: försök skriva medan filen är låst
        RepositoryResult result = await repo.WriteAsync(entities, CancellationToken.None);

        // ASSERT: metoden ska ge statuskod 500 och ett felmeddelande
        Assert.False(result.Succeeded);
        Assert.Equal(500, result.StatusCode);
        Assert.Contains("Kunde inte spara till fil:", result.ErrorMessage);
    }
}

/*  
PathCombine: bygger en ny sökvägs-sträng som består av tre delar:
1. GetTempPath - sökvägen till operativsystemets tillfälliga lagrings-mapp (Temp), som alltså redan finns i systemet. 
2. JsonRepoTests - sökvägen till och namnet på en underkatalog - mapp inuti en mapp (Temp) 
3. Guid.NewGuid().ToString("N"): sökvägen till och det unika namnet på en underkatalog i JsonRepoTests. 
Unika filsökvägar eliminerar risken för filkrockar, då olika testklasser potentiellt kan köras parallellt i xUnit (metoderna i klasserna körs sekventiellt). 
N = 32 hexsiffror utan bindestreck (kortare/snyggare mappnamn, smaksak)

Själva katalog/fil-skapandet gör den metod som ska testas med EnsureInitialized (Directory.CreateDirectory()) eller i ARRANGE

*/
