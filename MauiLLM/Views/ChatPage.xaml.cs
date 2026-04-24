using System.Collections.Specialized;
using System.ComponentModel;
using MauiLLM.Models;
using MauiLLM.ViewModels;

namespace MauiLLM.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    private bool _hasInitialized;
    private int _scrollRequestId;

    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        if (_viewModel.InitializeCommand.CanExecute(null))
        {
            _viewModel.InitializeCommand.Execute(null);
        }
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null)
        {
            return;
        }

        foreach (var item in e.NewItems.OfType<ChatMessage>())
        {
            item.PropertyChanged += OnChatMessagePropertyChanged;
        }

        RequestScrollToBottom();
    }

    private void OnChatMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatMessage.Text))
        {
            RequestScrollToBottom();
        }
    }

    private void RequestScrollToBottom()
    {
        if (_viewModel.Messages.Count == 0)
        {
            return;
        }

        var requestId = ++_scrollRequestId;

        ChatMessagesView.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () =>
        {
            if (requestId != _scrollRequestId || _viewModel.Messages.Count == 0 || ChatMessagesView.Handler is null)
            {
                return;
            }

            ChatMessagesView.ScrollTo(
                _viewModel.Messages.Count - 1,
                position: ScrollToPosition.End,
                animate: false);
        });
    }
}
