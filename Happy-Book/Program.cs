// See https://aka.ms/new-console-template for more information
using Happy_Book;
using Happy_Book.Readers;

using (var client = new HttpClient())
{
    var bookName = "Worm Arc 1 - Gestation";
    var url = "https://parahumans.wordpress.com/category/stories-arcs-1-10/arc-1-gestation/1-01/";

    var chapterCount = 54;
    var reader = new WormReader(url, bookName);
    var writer = new WordWriter($"C:\\temp\\BookReader\\{bookName}.docx");

    int counter = 0;
    await foreach (var chapter in reader.GetChapters(chapterCount))
    {
        Console.Write($"Writing chapter {counter + 1}");
        writer.WriteChapter(chapter);
        Console.WriteLine("... Done");
        counter++;
    }

    Console.Write("Saving file...");
    writer.Save();
    Console.WriteLine(" Done.");
    Console.WriteLine($"Finished downloading [{counter}] chapters.");


}


