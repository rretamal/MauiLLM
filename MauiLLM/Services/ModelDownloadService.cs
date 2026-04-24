using MauiLLM.Helpers;

namespace MauiLLM.Services;

public sealed class ModelDownloadService : IDisposable
{
    private const string HuggingFaceBaseUrl =
        "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx/resolve/main/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";

    private static readonly string[] ModelFiles =
    {
        "genai_config.json",
        "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx",
        "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data",
        "special_tokens_map.json",
        "tokenizer.json",
        "tokenizer.model",
        "tokenizer_config.json"
    };

    private readonly HttpClient _httpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    public async Task<string> DownloadModelAsync(IProgress<ModelDownloadProgress>? progress = null)
    {
        var modelDirectory = ModelPathResolver.GetPrimaryInstallDirectory();
        Directory.CreateDirectory(modelDirectory);

        for (var i = 0; i < ModelFiles.Length; i++)
        {
            var fileName = ModelFiles[i];
            var destinationPath = Path.Combine(modelDirectory, fileName);

            if (File.Exists(destinationPath))
            {
                var length = new FileInfo(destinationPath).Length;
                progress?.Report(new ModelDownloadProgress(
                    fileName,
                    i + 1,
                    ModelFiles.Length,
                    length,
                    length,
                    (double)(i + 1) / ModelFiles.Length));
                continue;
            }

            await DownloadFileAsync(fileName, destinationPath, i, progress);
        }

        return modelDirectory;
    }

    private async Task DownloadFileAsync(
        string fileName,
        string destinationPath,
        int fileIndex,
        IProgress<ModelDownloadProgress>? progress)
    {
        var tempPath = $"{destinationPath}.download";
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }

        var url = $"{HuggingFaceBaseUrl}/{Uri.EscapeDataString(fileName)}";
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;
        long downloadedBytes = 0;
        await using (var httpStream = await response.Content.ReadAsStreamAsync())
        await using (var fileStream = File.Create(tempPath))
        {
            var buffer = new byte[1024 * 128];
            int read;

            while ((read = await httpStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                downloadedBytes += read;

                progress?.Report(new ModelDownloadProgress(
                    fileName,
                    fileIndex + 1,
                    ModelFiles.Length,
                    downloadedBytes,
                    totalBytes,
                    CalculateOverallProgress(fileIndex, downloadedBytes, totalBytes)));
            }
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(tempPath, destinationPath);

        progress?.Report(new ModelDownloadProgress(
            fileName,
            fileIndex + 1,
            ModelFiles.Length,
            downloadedBytes,
            totalBytes,
            (double)(fileIndex + 1) / ModelFiles.Length));
    }

    private static double CalculateOverallProgress(int fileIndex, long downloadedBytes, long? totalBytes)
    {
        var completedFilesProgress = (double)fileIndex / ModelFiles.Length;
        var currentFileProgress = totalBytes is > 0
            ? Math.Clamp((double)downloadedBytes / totalBytes.Value, 0, 1)
            : 0;

        return Math.Clamp(completedFilesProgress + currentFileProgress / ModelFiles.Length, 0, 1);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
