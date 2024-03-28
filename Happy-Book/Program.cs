// See https://aka.ms/new-console-template for more information

using Happy.Document.Word;
using Happy.Reader;

using (var client = new HttpClient())
{
    var bookName = "Worm Arc 1 - Gestation";
    var url = "https://parahumans.wordpress.com/category/stories-arcs-1-10/arc-1-gestation/1-01/";

    var chapterCount = 3;
    var reader = new WormReader(url, bookName);
    var writer = new Writer($"C:\\temp\\BookReader\\{bookName}.docx");

    int counter = 0;
    await foreach (var chapter in reader.GetChapters(chapterCount))
    {
        Console.Write($"Writing chapter {counter + 1}");
        writer.WriteChapterFromHtml(chapter.Title, chapter.Html);
        Console.WriteLine("... Done");
        counter++;
    }

    Console.Write("Saving file...");
    writer.Save();
    Console.WriteLine(" Done.");
    Console.WriteLine($"Finished downloading [{counter}] chapters.");


}


