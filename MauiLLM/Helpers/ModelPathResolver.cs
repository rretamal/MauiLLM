namespace MauiLLM.Helpers;

public static class ModelPathResolver
{
    public const string ModelVariant = "cpu-int4-rtn-block-32-acc-level-4";
    public const string FriendlyModelFolder = "phi3-mini";
    public const string SourceVariantPath = "cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";

    public static string GetExpectedModelDirectory()
    {
        return GetCandidateModelDirectories().First();
    }

    public static string GetPrimaryInstallDirectory()
    {
#if ANDROID
        var externalFilesDirectory = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(externalFilesDirectory))
        {
            return Path.Combine(externalFilesDirectory, "models", FriendlyModelFolder);
        }
#endif

        return Path.Combine(FileSystem.AppDataDirectory, "models", FriendlyModelFolder);
    }

    public static IReadOnlyList<string> GetCandidateModelDirectories()
    {
        var candidates = new List<string>();

#if ANDROID
        var sharedStorageRoot = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(sharedStorageRoot))
        {
            //var downloadsFolderName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var downloadsFolderName = Android.OS.Environment.DirectoryDownloads ?? "Download";
            candidates.Add(Path.Combine(sharedStorageRoot, downloadsFolderName, FriendlyModelFolder));
            candidates.Add(Path.Combine(sharedStorageRoot, downloadsFolderName, ModelVariant));
            candidates.Add(Path.Combine(sharedStorageRoot, FriendlyModelFolder));
        }

        var externalFilesDirectory = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(externalFilesDirectory))
        {
            candidates.Add(Path.Combine(externalFilesDirectory, "models", FriendlyModelFolder));
            candidates.Add(Path.Combine(externalFilesDirectory, "models", ModelVariant));
        }
#endif

        candidates.Add(GetPrimaryInstallDirectory());
        candidates.Add(Path.Combine(FileSystem.AppDataDirectory, "models", FriendlyModelFolder));
        candidates.Add(Path.Combine(FileSystem.AppDataDirectory, "models", ModelVariant));

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool TryResolveModelDirectory(out string modelDirectory, out string errorMessage)
    {
        string? firstExistingDirectoryError = null;

        foreach (var candidate in GetCandidateModelDirectories())
        {
            if (TryValidateModelDirectory(candidate, out var validationError))
            {
                modelDirectory = candidate;
                errorMessage = string.Empty;
                return true;
            }

            if (Directory.Exists(candidate) && firstExistingDirectoryError is null)
            {
                firstExistingDirectoryError = validationError;
            }
        }

        modelDirectory = GetExpectedModelDirectory();
        errorMessage = firstExistingDirectoryError ?? BuildMissingModelMessage();
        return false;
    }

    public static bool TryValidateModelDirectory(string modelDirectory, out string errorMessage)
    {
        if (!Directory.Exists(modelDirectory))
        {
            errorMessage = $"Model not found at:{Environment.NewLine}{modelDirectory}";
            return false;
        }

        try
        {
            var files = Directory.GetFiles(modelDirectory, "*", SearchOption.TopDirectoryOnly);
            var fileNames = files.Select(Path.GetFileName).Where(file => !string.IsNullOrWhiteSpace(file)).ToArray();

            if (IsSharedDownloadPath(modelDirectory) && fileNames.Length == 0)
            {
                errorMessage = BuildSharedDownloadAccessMessage(modelDirectory);
                return false;
            }

            if (!fileNames.Any(file => file!.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = BuildMissingFileMessage(modelDirectory, "*.onnx", fileNames);
                return false;
            }

            if (!fileNames.Any(file => file!.EndsWith(".onnx.data", StringComparison.OrdinalIgnoreCase)))
            {
                errorMessage = BuildMissingFileMessage(modelDirectory, "*.onnx.data", fileNames);
                return false;
            }

            string[] requiredFiles =
            {
                "genai_config.json",
                "tokenizer.model",
                "tokenizer.json",
                "special_tokens_map.json"
            };

            var missingFile = requiredFiles.FirstOrDefault(
                requiredFile => !fileNames.Contains(requiredFile, StringComparer.OrdinalIgnoreCase));
            if (missingFile is not null)
            {
                errorMessage = BuildMissingFileMessage(modelDirectory, missingFile, fileNames);
                return false;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            errorMessage =
                $"The app cannot read the model directory:{Environment.NewLine}{modelDirectory}{Environment.NewLine}{ex.Message}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static string BuildMissingFileMessage(string modelDirectory, string expectedFile, IReadOnlyCollection<string?> fileNames)
    {
        var visibleFiles = fileNames.Count == 0
            ? "(no files visible to the app)"
            : string.Join(Environment.NewLine, fileNames.Select(file => $"- {file}"));

        return
            $"Model directory is missing '{expectedFile}':{Environment.NewLine}{modelDirectory}{Environment.NewLine}Files visible to the app:{Environment.NewLine}{visibleFiles}";
    }

    private static bool IsSharedDownloadPath(string modelDirectory)
    {
        return modelDirectory.Contains("/Download/", StringComparison.OrdinalIgnoreCase)
            || modelDirectory.EndsWith("/Download", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSharedDownloadAccessMessage(string modelDirectory)
    {
        var appSpecificPath = GetCandidateModelDirectories()
            .FirstOrDefault(path => path.Contains("/Android/data/", StringComparison.OrdinalIgnoreCase))
            ?? GetExpectedModelDirectory();

        return
            $"Android did not expose any files from:{Environment.NewLine}{modelDirectory}{Environment.NewLine}" +
            $"Use the app-specific model folder instead:{Environment.NewLine}{appSpecificPath}";
    }

    public static string BuildMissingModelMessage()
    {
        var candidatePaths = string.Join(
            Environment.NewLine,
            GetCandidateModelDirectories().Select(path => $"- {path}"));

        return
            $"Model not found. Copy the contents of '{SourceVariantPath}' to one of these folders:{Environment.NewLine}{candidatePaths}";
    }
}
