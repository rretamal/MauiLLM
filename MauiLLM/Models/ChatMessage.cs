using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiLLM.Models;

public sealed class ChatMessage : INotifyPropertyChanged
{
    private string _text;

    public ChatMessage(string text, bool isUser)
    {
        _text = text;
        IsUser = isUser;
    }

    public bool IsUser { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            OnPropertyChanged();
        }
    }

    public Color BackgroundColor => IsUser ? Color.FromArgb("#DDEEFF") : Color.FromArgb("#F3F4F6");

    public Color TextColor => IsUser ? Color.FromArgb("#0F172A") : Color.FromArgb("#1F2937");

    public LayoutOptions BubbleAlignment => IsUser ? LayoutOptions.End : LayoutOptions.Start;

    public void AppendText(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        Text += value;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
