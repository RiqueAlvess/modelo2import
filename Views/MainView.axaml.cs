using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace ImportadorModelo2.Views
{
    public partial class MainView : UserControl
    {
        public event Action? LogoutRequested;
        public event Action? NovaImportacaoRequested;
        public event Action? VisualizarLogsRequested;
        public event Action? NovoLayoutRequested;

        public MainView()
        {
            InitializeComponent();
            SetupEvents();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Configura eventos da interface
        /// </summary>
        private void SetupEvents()
        {
            SetupDragArea();
            SetupCloseButton();
            SetupLogoutButton();
            SetupActionButtons();
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

        /// <summary>
        /// Configura botão de logout
        /// </summary>
        private void SetupLogoutButton()
        {
            var logoutButton = this.FindControl<Button>("LogoutButton");
            if (logoutButton != null)
            {
                logoutButton.Click += (sender, e) =>
                {
                    LogoutRequested?.Invoke();
                };
            }
        }

        /// <summary>
        /// Configura botões de ação principais
        /// </summary>
        private void SetupActionButtons()
        {
            var novaImportacaoButton = this.FindControl<Button>("NovaImportacaoButton");
            if (novaImportacaoButton != null)
            {
                novaImportacaoButton.Click += (sender, e) =>
                {
                    NovaImportacaoRequested?.Invoke();
                };
            }

            var visualizarLogsButton = this.FindControl<Button>("VisualizarLogsButton");
            if (visualizarLogsButton != null)
            {
                visualizarLogsButton.Click += (sender, e) =>
                {
                    VisualizarLogsRequested?.Invoke();
                };
            }

            var novoLayoutButton = this.FindControl<Button>("NovoLayoutButton");
            if (novoLayoutButton != null)
            {
                novoLayoutButton.Click += (sender, e) =>
                {
                    NovoLayoutRequested?.Invoke();
                };
            }
        }
    }
}