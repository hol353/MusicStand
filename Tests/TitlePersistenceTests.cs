using Avalonia;
using Avalonia.Media;
using DotNetCampus.Inking;
using DotNetCampus.Inking.Primitive;

namespace MusicStand.Tests;

public class XMLPersistTests : IClassFixture<AvaloniaFixture>
{
    /// <summary>
    /// Tests that the Write method does not create an XML file when the title has no links or strokes.
    /// </summary>
    [Fact]
    public void Write_DoesNotCreateFile_WhenTitleHasNoLinksOrStrokes()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var annotationsDirectory = Path.Combine(baseDirectory, "Annotations");
        Directory.CreateDirectory(annotationsDirectory);

        try
        {
            Settings settings = new()
            {
                HomeDirectory = baseDirectory,
                MusicLibraryBaseDirectory = baseDirectory
            };
            Title title = new(pdfPath: Path.Combine(baseDirectory, "NotFound.pdf")); 
            TitlePersistence.Write(title, settings);
            Assert.Empty(Directory.EnumerateFiles(annotationsDirectory));
        }

        finally
        {
            Directory.Delete(baseDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Tests that the Write method creates an XML file when the title has strokes and links.
    /// </summary>
     [Fact]
    public void Write_CreatesFile_WhenTitleHasStrokesAndLinks()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var annotationsDirectory = Path.Combine(tempDirectory, "Annotations");
        Directory.CreateDirectory(annotationsDirectory);

        try
        {

            // Write 2page.pdf to temporary directory
            var pdfFilePath = Path.Combine(tempDirectory, "2page.pdf");
            var assembly = typeof(XMLPersistTests).Assembly;
            using (var resourceStream = assembly.GetManifestResourceStream("Tests.Assets.2page.pdf"))
            {
                using (var fileStream = new FileStream(pdfFilePath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            } 
            
            // Create a settings instance
            Settings settings = new()
            {
                HomeDirectory = tempDirectory,
                MusicLibraryBaseDirectory = tempDirectory
            };

            // Create a test title.
            var stroke = new SkiaStroke(InkId.NewId());
            stroke.AddPoint(new InkStylusPoint(1, 1));
            Title title = new(pdfFilePath)
            { 
                Links = [new Link()
                {
                    Color = Colors.Red,
                    Point1PageNumber = 1,
                    Point1 = new Point(10, 20),
                    Point2PageNumber = 2,
                    Point2 = new Point(30, 40)
                }],
            };
            TitlePersistence.Initialise(title, settings);
            title.Pages.First().Strokes = [stroke];
            title.Pages.First().ViewPortBounds = new(0, 0, 100, 100);

            // Persist the test title to file.
            TitlePersistence.Write(title, settings);

            var files = Directory.EnumerateFiles(annotationsDirectory).ToList();
            Assert.Single(files);
            Assert.EndsWith(".xml", files[0]);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Tests that the Read method correctly initialises a Title instance when the XML file is valid.      
    /// </summary>
    [Fact]
    public void Read_InitialisesTitle_WhenXMLIsValid()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var annotationsDirectory = Path.Combine(tempDirectory, "Annotations");
        Directory.CreateDirectory(annotationsDirectory);        
        try
        {
            // Write 2page.pdf to temporary directory
            var pdfFilePath = Path.Combine(tempDirectory, "2page.pdf");
            var assembly = typeof(XMLPersistTests).Assembly;
            using (var resourceStream = assembly.GetManifestResourceStream("Tests.Assets.2page.pdf"))
            {
                using (var fileStream = new FileStream(pdfFilePath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            } 

            // Write a test xml file.
            string testXmlFilePath = Path.Combine(annotationsDirectory, "2page.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(testXmlFilePath));
            File.WriteAllText(testXmlFilePath,  
                "<title>" +
                "  <page>" +
                "    <number>1</number>" +
                "    <svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"100\" height=\"100\">" +
                "      <path fill=\"red\" d=\"M3.5 1Q3.5 1.12282 3.48796 1.24504Q3.47592 1.36727 3.45196 1.48773Q3.428 1.60818 3.39235 1.72571Q3.3567 1.84324 3.3097 1.95671Q3.2627 2.07018 3.2048 2.17849Q3.14691 2.28681 3.07867 2.38893Q3.01044 2.49104 2.93253 2.58598Q2.85461 2.68092 2.76777 2.76777Q2.68092 2.85461 2.58598 2.93253Q2.49104 3.01044 2.38893 3.07867Q2.28681 3.14691 2.17849 3.2048Q2.07018 3.2627 1.95671 3.3097Q1.84324 3.3567 1.72571 3.39235Q1.60818 3.428 1.48773 3.45196Q1.36727 3.47592 1.24504 3.48796Q1.12282 3.5 1 3.5Q0.877183 3.5 0.754957 3.48796Q0.632731 3.47592 0.512274 3.45196Q0.391817 3.428 0.274288 3.39235Q0.15676 3.3567 0.0432914 3.3097Q-0.0701768 3.2627 -0.178492 3.2048Q-0.286807 3.14691 -0.388926 3.07867Q-0.491044 3.01044 -0.585983 2.93253Q-0.680922 2.85461 -0.767767 2.76777Q-0.854612 2.68092 -0.932526 2.58598Q-1.01044 2.49104 -1.07867 2.38893Q-1.14691 2.28681 -1.2048 2.17849Q-1.2627 2.07018 -1.3097 1.95671Q-1.3567 1.84324 -1.39235 1.72571Q-1.428 1.60818 -1.45196 1.48773Q-1.47592 1.36727 -1.48796 1.24504Q-1.5 1.12282 -1.5 1Q-1.5 0.877183 -1.48796 0.754957Q-1.47592 0.632731 -1.45196 0.512274Q-1.428 0.391817 -1.39235 0.274288Q-1.3567 0.15676 -1.3097 0.0432914Q-1.2627 -0.0701768 -1.2048 -0.178492Q-1.14691 -0.286807 -1.07867 -0.388926Q-1.01044 -0.491044 -0.932526 -0.585983Q-0.854612 -0.680922 -0.767767 -0.767767Q-0.680922 -0.854612 -0.585983 -0.932526Q-0.491044 -1.01044 -0.388926 -1.07867Q-0.286807 -1.14691 -0.178492 -1.2048Q-0.0701768 -1.2627 0.0432914 -1.3097Q0.15676 -1.3567 0.274288 -1.39235Q0.391817 -1.428 0.512274 -1.45196Q0.632731 -1.47592 0.754957 -1.48796Q0.877183 -1.5 1 -1.5Q1.12282 -1.5 1.24504 -1.48796Q1.36727 -1.47592 1.48773 -1.45196Q1.60818 -1.428 1.72571 -1.39235Q1.84324 -1.3567 1.95671 -1.3097Q2.07018 -1.2627 2.17849 -1.2048Q2.28681 -1.14691 2.38893 -1.07867Q2.49104 -1.01044 2.58598 -0.932526Q2.68092 -0.854612 2.76777 -0.767767Q2.85461 -0.680922 2.93253 -0.585983Q3.01044 -0.491044 3.07867 -0.388926Q3.14691 -0.286807 3.2048 -0.178492Q3.2627 -0.0701768 3.3097 0.0432914Q3.3567 0.15676 3.39235 0.274288Q3.428 0.391817 3.45196 0.512274Q3.47592 0.632731 3.48796 0.754957Q3.5 0.877183 3.5 1Z\" />" +
                "    </svg>" +
                "  </page>" +
                "  <link>" +
                "    <color>Red</color>" +
                "    <point>" +
                "      <page>1</page>" +
                "      <x>10</x>" +
                "      <y>20</y>" +
                "    </point>" +
                "    <point>" +
                "      <page>2</page>" +
                "      <x>30</x>" +
                "      <y>40</y>" +
                "    </point>" +
                "  </link>" +
                "</title>");
            
            // Create a settings instance
            Settings settings = new()
            {
                HomeDirectory = tempDirectory,
                MusicLibraryBaseDirectory = tempDirectory
            };


            Title title = new(pdfFilePath);
            TitlePersistence.Initialise(title, settings);

            Assert.Single(title.Links);
            Assert.Single(title.Pages.First().Strokes);
            Assert.Empty(title.Pages.Last().Strokes);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}

/// <summary>
/// Initialises Avalonia for unit tests.
/// </summary>
public class AvaloniaFixture
{
    public AvaloniaFixture()
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
    }
}
