using System.Text.Json;
using System.Diagnostics;

public class JsonUtils
{
    public static void Export(object data, string fileName)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

        string filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"{fileName}.json");

        File.WriteAllText(filePath, json);

        //Open file
        Process.Start(new ProcessStartInfo
        {
            FileName = filePath,
            UseShellExecute = true
        });
    }
}