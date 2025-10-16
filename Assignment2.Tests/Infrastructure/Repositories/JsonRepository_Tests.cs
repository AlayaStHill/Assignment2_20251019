using Infrastructure.Repositories;

namespace Assignment2.Tests.Infrastructure.Repositories;
// Jag har använt AI och promptteknik som stöd i arbetet med att skriva testerna.

// IDisposable innehåller metoden Dispose. Implementation gör att xUnit automatiskt anropar Dispose() efter varje [Fact] = slipper upprepa städkod i varje test
public class JsonRepository_Tests : IDisposable 
{
    // I varje [Fact]-metod skapar xUnit ett nytt objekt av testklassen, dvs new JsonRepository_Tests(). Konstruktorn körs alltså för varje testmetod och testfälten är unika för just det testet. 
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
    private class TestEntity
    {
        // tom sträng för att slippa hantera null-checkar, -exceptions
        public string Id { get; set; } = ""; 
        public string Name { get; set; } = ""; 
    }

    // Städfunktion som anropas efter varje test-metod = inga kvarlämnade filer inför nästa test. Parametern recursive: true = ta bort katalogen och allt inuti
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
