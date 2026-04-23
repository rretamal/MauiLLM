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
    private string _userInput = string.Empty;
    private string _statusText = "Looking for the Phi-3 model on device storage...";
    private string _modelPathHelpText = string.Empty;
    private bool _isBusy;
    private bool _isReady;

    public ChatViewModel(LocalLlmService llmService)
    {
        _llmService = llmService;
        InitializeCommand = new AsyncCommand(InitializeAsync, () => !IsBusy);
        SendCommand = new AsyncCommand(SendAsync, CanSend);
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    public ICommand InitializeCommand { get; }

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

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
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
                NotifyCommandStateChanged();
            }
        }
    }

    public bool IsInputEnabled => IsReady && !IsBusy;

    public async Task InitializeAsync()
    {
        ModelPathHelpText =
            $"Accepted model folders:{Environment.NewLine}{string.Join(Environment.NewLine, ModelPathResolver.GetCandidateModelDirectories().Select(path => $"- {path}"))}";

        if (!ModelPathResolver.TryResolveModelDirectory(out var modelDirectory, out var validationError))
        {
            IsReady = false;
            StatusText = validationError;
            return;
        }

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
        return IsReady && !IsBusy && !string.IsNullOrWhiteSpace(UserInput);
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
    }

    private void NotifyCommandStateChanged()
    {
        if (InitializeCommand is AsyncCommand initializeCommand)
        {
            initializeCommand.NotifyCanExecuteChanged();
        }

        if (SendCommand is AsyncCommand sendCommand)
        {
            sendCommand.NotifyCanExecuteChanged();
        }
    }
}
