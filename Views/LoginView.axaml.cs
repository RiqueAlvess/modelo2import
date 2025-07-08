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
            SetupEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Configura eventos da interface de login
        /// </summary>
        private void SetupEvents()
        {
            SetupDragArea();
            SetupCloseButton();
        }

        /// <summary>
        /// Configura área para arrastar janela
        /// </summary>
        private void SetupDragArea()
        {
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
        }

        /// <summary>
        /// Configura botão de fechar
        /// </summary>
        private void SetupCloseButton()
        {
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