using MauiLLM.Views;

namespace MauiLLM
{
    public partial class AppShell : Shell
    {
        public AppShell(ChatPage chatPage)
        {
            InitializeComponent();

            Items.Add(new ShellContent
            {
                Title = "Phi-3 Chat",
                Route = nameof(ChatPage),
                Content = chatPage
            });
        }
    }
}
