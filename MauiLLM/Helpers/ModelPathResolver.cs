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

    public static IReadOnlyList<string> GetCandidateModelDirectories()
    {
        var candidates = new List<string>();

#if ANDROID
        var sharedStorageRoot = Android.OS.Environment.ExternalStorageDirectory?.AbsolutePath;
        if (!string.IsNullOrWhiteSpace(sharedStorageRoot))
        {
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

        candidates.Add(Path.Combine(FileSystem.AppDataDirectory, "models", FriendlyModelFolder));
        candidates.Add(Path.Combine(FileSystem.AppDataDirectory, "models", ModelVariant));

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool TryResolveModelDirectory(out string modelDirectory, out string errorMessage)
    {
        foreach (var candidate in GetCandidateModelDirectories())
        {
            if (TryValidateModelDirectory(candidate, out _))
            {
                modelDirectory = candidate;
                errorMessage = string.Empty;
                return true;
            }
        }

        modelDirectory = GetExpectedModelDirectory();
        errorMessage = BuildMissingModelMessage();
        return false;
    }

    public static bool TryValidateModelDirectory(string modelDirectory, out string errorMessage)
    {
        if (!Directory.Exists(modelDirectory))
        {
            errorMessage = $"Model not found at:{Environment.NewLine}{modelDirectory}";
            return false;
        }

        if (!Directory.GetFiles(modelDirectory, "*.onnx", SearchOption.TopDirectoryOnly).Any())
        {
            errorMessage = $"Model directory is missing a '*.onnx' file:{Environment.NewLine}{modelDirectory}";
            return false;
        }

        if (!Directory.GetFiles(modelDirectory, "*.onnx.data", SearchOption.TopDirectoryOnly).Any())
        {
            errorMessage = $"Model directory is missing a '*.onnx.data' file:{Environment.NewLine}{modelDirectory}";
            return false;
        }

        string[] requiredFiles =
        {
            "genai_config.json",
            "tokenizer.model",
            "tokenizer.json",
            "tokenizer_config.json",
            "special_tokens_map.json"
        };

        var missingFile = requiredFiles.FirstOrDefault(file => !File.Exists(Path.Combine(modelDirectory, file)));
        if (missingFile is not null)
        {
            errorMessage = $"Model directory is missing '{missingFile}':{Environment.NewLine}{modelDirectory}";
            return false;
        }

        errorMessage = string.Empty;
        return true;
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
