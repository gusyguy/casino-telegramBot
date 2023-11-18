namespace Presentation.Common.Extentions;
public class FileExtention
{
    public static string directory = Path.Combine(Directory.GetCurrentDirectory(), "data");
    public static async Task<string> WriteFileAsync(string path, string content)
    {
        path = Path.Combine(directory, path);

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(path, content);

        return path;
    }
}
