using Azure.AI.Agents.Persistent;

namespace AgentKnowledgeTools.Utilities;

public static class FileDownloadUtility
{
    /// <summary>
    /// Downloads a file from the Agents service and saves it to the specified path.
    /// </summary>
    /// <param name="agentsClient">The PersistentAgentsClient instance</param>
    /// <param name="fileId">The ID of the file to download</param>
    /// <param name="outputPath">The local path where the file should be saved</param>
    /// <returns>True if download was successful, false otherwise</returns>
    public static async Task<bool> DownloadFileAsync(
        PersistentAgentsClient agentsClient,
        string fileId,
        string outputPath)
    {
        try
        {
            Console.WriteLine($"Downloading file {fileId}...");

            // Get the file content
            var fileContentResponse = await agentsClient.Files.GetFileContentAsync(fileId);
            var fileContent = fileContentResponse.Value;

            // Ensure the output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write the file to disk
            await using (var fileStream = File.Create(outputPath))
            {
                await fileContent.ToStream().CopyToAsync(fileStream);
            }

            Console.WriteLine($"File downloaded successfully to: {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Downloads a file from the Agents service with automatic file naming.
    /// </summary>
    /// <param name="agentsClient">The PersistentAgentsClient instance</param>
    /// <param name="fileId">The ID of the file to download</param>
    /// <param name="outputDirectory">The directory where the file should be saved</param>
    /// <param name="fileName">Optional custom filename. If not provided, uses the file ID</param>
    /// <returns>The full path to the downloaded file, or null if download failed</returns>
    public static async Task<string?> DownloadFileToDirectoryAsync(
        PersistentAgentsClient agentsClient,
        string fileId,
        string outputDirectory = "downloads",
        string? fileName = null)
    {
        try
        {
            // Get file information to determine the actual filename if available
            PersistentAgentFileInfo fileInfo = await agentsClient.Files.GetFileAsync(fileId);

            // Use provided filename, or fall back to the file's original name, or file ID
            string actualFileName = fileName ?? fileInfo.Filename ?? $"{fileId}.png";

            string outputPath = Path.Combine(outputDirectory, actualFileName);

            bool success = await DownloadFileAsync(agentsClient, fileId, outputPath);
            return success ? outputPath : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file to directory: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Downloads multiple files from the Agents service.
    /// </summary>
    /// <param name="agentsClient">The PersistentAgentsClient instance</param>
    /// <param name="fileIds">Collection of file IDs to download</param>
    /// <param name="outputDirectory">The directory where files should be saved</param>
    /// <returns>Dictionary mapping file IDs to their local paths (null if download failed)</returns>
    public static async Task<Dictionary<string, string?>> DownloadFilesAsync(
        PersistentAgentsClient agentsClient,
        IEnumerable<string> fileIds,
        string outputDirectory = "downloads")
    {
        var results = new Dictionary<string, string?>();

        foreach (var fileId in fileIds)
        {
            var filePath = await DownloadFileToDirectoryAsync(agentsClient, fileId, outputDirectory);
            results[fileId] = filePath;
        }

        return results;
    }

    /// <summary>
    /// Downloads all image files from a message's content.
    /// </summary>
    /// <param name="agentsClient">The PersistentAgentsClient instance</param>
    /// <param name="message">The message containing image content</param>
    /// <param name="outputDirectory">The directory where images should be saved</param>
    /// <returns>List of paths to downloaded images</returns>
    public static async Task<List<string>> DownloadMessageImagesAsync(
        PersistentAgentsClient agentsClient,
        PersistentThreadMessage message,
        string outputDirectory = "downloads")
    {
        var downloadedFiles = new List<string>();

        foreach (var content in message.ContentItems)
        {
            if (content is MessageImageFileContent imageContent)
            {
                var filePath = await DownloadFileToDirectoryAsync(
                    agentsClient,
                    imageContent.FileId,
                    outputDirectory
                );

                if (filePath != null)
                {
                    downloadedFiles.Add(filePath);
                }
            }
        }

        return downloadedFiles;
    }
}
