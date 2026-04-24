using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MauiLLM.Helpers;
using MauiLLM.Models;
using MauiLLM.Services;

namespace MauiLLM.ViewModels;

public sealed class ChatViewModel : INotifyPropertyChanged
{
    private const string ReadyStatus = "Ready. Phi-3 is running 100% on this device.";

    private readonly LocalLlmService _llmService;
    private readonly ModelDownloadService _modelDownloadService;
    private string _userInput = string.Empty;
    private string _statusText = "Looking for the Phi-3 model on device storage...";
    private string _modelPathHelpText = string.Empty;
    private string _downloadStatusText = string.Empty;
    private bool _isBusy;
    private bool _isReady;
    private bool _isDownloading;
    private bool _canDownloadModel;
    private double _downloadProgress;

    public ChatViewModel(LocalLlmService llmService, ModelDownloadService modelDownloadService)
    {
        _llmService = llmService;
        _modelDownloadService = modelDownloadService;
        InitializeCommand = new AsyncCommand(InitializeAsync, () => !IsBusy && !IsDownloading);
        DownloadModelCommand = new AsyncCommand(DownloadModelAsync, () => CanDownloadModel);
        SendCommand = new AsyncCommand(SendAsync, CanSend);
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ICommand InitializeCommand { get; }

    public ICommand DownloadModelCommand { get; }

    public ICommand SendCommand { get; }

    public string UserInput
    {
        get => _userInput;
        set
        {
            if (SetProperty(ref _userInput, value))
            {
                NotifyCommandStateChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ModelPathHelpText
    {
        get => _modelPathHelpText;
        private set => SetProperty(ref _modelPathHelpText, value);
    }

    public string DownloadStatusText
    {
        get => _downloadStatusText;
        private set => SetProperty(ref _downloadStatusText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsDownloadSectionVisible));
                NotifyCommandStateChanged();
            }
        }
    }

    public bool IsReady
    {
        get => _isReady;
        private set
        {
            if (SetProperty(ref _isReady, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
                OnPropertyChanged(nameof(IsDownloadSectionVisible));
                NotifyCommandStateChanged();
            }
        }
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(IsInputEnabled));
                OnPropertyChanged(nameof(IsDownloadSectionVisible));
                NotifyCommandStateChanged();
            }
        }
    }

    public bool CanDownloadModel
    {
        get => _canDownloadModel && !IsBusy && !IsDownloading && !IsReady;
        private set
        {
            if (SetProperty(ref _canDownloadModel, value))
            {
                OnPropertyChanged(nameof(IsDownloadSectionVisible));
                NotifyCommandStateChanged();
            }
        }
    }

    public double DownloadProgress
    {
        get => _downloadProgress;
        private set => SetProperty(ref _downloadProgress, value);
    }

    public bool IsDownloadSectionVisible => !IsReady && (CanDownloadModel || IsDownloading);

    public bool IsInputEnabled => IsReady && !IsBusy && !IsDownloading;

    public async Task InitializeAsync()
    {
        ModelPathHelpText =
            $"Install path: {ModelPathResolver.GetPrimaryInstallDirectory()}";

        if (!ModelPathResolver.TryResolveModelDirectory(out var modelDirectory, out var validationError))
        {
            IsReady = false;
            CanDownloadModel = true;
            DownloadStatusText = "Model not installed. Download it to continue.";
            StatusText = BuildCompactMissingModelStatus(validationError);
            return;
        }

        CanDownloadModel = false;
        IsBusy = true;
        try
        {
            StatusText = $"Loading Phi-3 from:{Environment.NewLine}{modelDirectory}";
            await _llmService.LoadModelAsync(modelDirectory);
            IsReady = true;
            StatusText = ReadyStatus;
        }
        catch (Exception ex)
        {
            IsReady = false;
            StatusText = $"Could not load the model: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DownloadModelAsync()
    {
        if (!CanDownloadModel)
        {
            return;
        }

        IsReady = false;
        CanDownloadModel = false;
        IsDownloading = true;
        DownloadProgress = 0;
        StatusText = "Downloading Phi-3 model files. Keep the app open.";

        try
        {
            var progress = new Progress<ModelDownloadProgress>(UpdateDownloadProgress);
            var modelDirectory = await _modelDownloadService.DownloadModelAsync(progress);

            StatusText = "Download complete. Loading Phi-3 from local storage.";
            DownloadProgress = 1;

            if (!ModelPathResolver.TryValidateModelDirectory(modelDirectory, out var validationError))
            {
                IsReady = false;
                CanDownloadModel = true;
                StatusText = $"Downloaded model is incomplete: {validationError}";
                return;
            }

            await _llmService.LoadModelAsync(modelDirectory);
            IsReady = true;
            DownloadStatusText = string.Empty;
            StatusText = ReadyStatus;
        }
        catch (Exception ex)
        {
            IsReady = false;
            CanDownloadModel = true;
            DownloadStatusText = "Download failed. Check your connection and try again.";
            StatusText = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    public async Task SendAsync()
    {
        if (!CanSend())
        {
            return;
        }

        var prompt = UserInput.Trim();
        UserInput = string.Empty;

        var userMessage = new ChatMessage(prompt, isUser: true);
        var assistantMessage = new ChatMessage(string.Empty, isUser: false);

        Messages.Add(userMessage);
        Messages.Add(assistantMessage);

        IsBusy = true;
        StatusText = "Generating a response locally on the device...";

        try
        {
            await foreach (var chunk in _llmService.GenerateAsync(prompt))
            {
                assistantMessage.AppendText(chunk);
            }

            if (string.IsNullOrWhiteSpace(assistantMessage.Text))
            {
                assistantMessage.Text = "(No text was returned.)";
            }

            StatusText = ReadyStatus;
        }
        catch (Exception ex)
        {
            assistantMessage.Text = $"Generation failed: {ex.Message}";
            StatusText = $"Generation failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanSend()
    {
        return IsReady && !IsBusy && !IsDownloading && !string.IsNullOrWhiteSpace(UserInput);
    }

    private void UpdateDownloadProgress(ModelDownloadProgress progress)
    {
        DownloadProgress = progress.OverallProgress;

        var filePercentText = progress.FileBytesTotal is > 0
            ? $" ({(double)progress.FileBytesDownloaded / progress.FileBytesTotal.Value:P0})"
            : string.Empty;

        DownloadStatusText =
            $"Downloading {progress.FileName}{filePercentText}{Environment.NewLine}" +
            $"File {progress.FileIndex} of {progress.FileCount}. Overall {progress.OverallProgress:P0}.";
    }

    private static string BuildCompactMissingModelStatus(string validationError)
    {
        if (validationError.Contains("Android did not expose any files", StringComparison.OrdinalIgnoreCase))
        {
            return "Model not installed. Android cannot read the files from Downloads, so use the app download option.";
        }

        if (validationError.Contains("missing", StringComparison.OrdinalIgnoreCase))
        {
            return "Model not installed or incomplete. Download it from the app to continue.";
        }

        return "Model not installed. Download it from the app to continue.";
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        if (propertyName == nameof(IsBusy))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsInputEnabled)));
        }

        if (propertyName == nameof(IsBusy) || propertyName == nameof(IsDownloading) || propertyName == nameof(IsReady))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanDownloadModel)));
        }
    }

    private void NotifyCommandStateChanged()
    {
        if (InitializeCommand is AsyncCommand initializeCommand)
        {
            initializeCommand.NotifyCanExecuteChanged();
        }

        if (DownloadModelCommand is AsyncCommand downloadModelCommand)
        {
            downloadModelCommand.NotifyCanExecuteChanged();
        }

        if (SendCommand is AsyncCommand sendCommand)
        {
            sendCommand.NotifyCanExecuteChanged();
        }
    }
}
