namespace MauiLLM.Services;

public sealed record ModelDownloadProgress(
    string FileName,
    int FileIndex,
    int FileCount,
    long FileBytesDownloaded,
    long? FileBytesTotal,
    double OverallProgress);
