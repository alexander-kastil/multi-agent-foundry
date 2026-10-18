namespace AFWOrchestration;

public class LogService
{
    public static async Task CopyLogFilesAsync(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string file in Directory.GetFiles(sourceDirectory))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(destinationDirectory, fileName);
            File.Copy(file, destFile, true);
        }
        await Task.CompletedTask;
    }
}