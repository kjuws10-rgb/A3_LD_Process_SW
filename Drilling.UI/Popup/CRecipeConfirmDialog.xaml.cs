using System.Windows;
using System.Windows.Input;

namespace Drilling.UI.Popup;

public partial class CRecipeConfirmDialog : Window
{
    public CRecipeConfirmDialog(
        string title,
        string message,
        string confirmText = "OK")
    {
        InitializeComponent();

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        ConfirmButton.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            DialogResult = false;
        }
    }

    private void OnConfirmClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

