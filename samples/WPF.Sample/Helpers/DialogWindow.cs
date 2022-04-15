using MahApps.Metro.Controls;
using Prism.Services.Dialogs;

namespace Trading.UI.Sample.Helpers
{
    public class DialogWindow : MetroWindow, IDialogWindow
    {
        public IDialogResult Result { get; set; }
    }
}