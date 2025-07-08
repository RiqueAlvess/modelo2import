using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace ImportadorModelo2.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            SetupDragAndClose();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetupDragAndClose()
        {
            // Área para arrastar a janela
            var dragArea = this.FindControl<Border>("DragArea");
            if (dragArea != null)
            {
                dragArea.PointerPressed += (sender, e) =>
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    {
                        var window = this.FindAncestorOfType<Window>();
                        window?.BeginMoveDrag(e);
                    }
                };
            }

            // Botão para fechar a janela
            var closeButton = this.FindControl<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.Click += (sender, e) =>
                {
                    var window = this.FindAncestorOfType<Window>();
                    window?.Close();
                };
            }
        }
    }
}