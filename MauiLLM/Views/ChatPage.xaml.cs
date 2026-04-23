using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using MauiLLM.Models;
using MauiLLM.ViewModels;

namespace MauiLLM.Views;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;
    private bool _hasInitialized;

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

        ScrollToBottom();
    }

    private void OnChatMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatMessage.Text))
        {
            ScrollToBottom();
        }
    }

    private void ScrollToBottom()
    {
        if (_viewModel.Messages.Count == 0)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var lastItem = _viewModel.Messages[^1];
            ChatMessagesView.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
        });
    }
}
