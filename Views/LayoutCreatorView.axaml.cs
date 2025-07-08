using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ImportadorModelo2.ViewModels;

namespace ImportadorModelo2.Views
{
    public partial class LayoutCreatorView : UserControl
    {
        public event Action? VoltarRequested;
        public event Action? FecharRequested;

        public LayoutCreatorView()
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
            SetupNavigationButtons();
            SetupFileUpload();
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
        /// Configura botões de navegação
        /// </summary>
        private void SetupNavigationButtons()
        {
            var voltarButton = this.FindControl<Button>("VoltarButton");
            if (voltarButton != null)
            {
                voltarButton.Click += (sender, e) =>
                {
                    VoltarRequested?.Invoke();
                };
            }

            var closeButton = this.FindControl<Button>("CloseButton");
            if (closeButton != null)
            {
                closeButton.Click += (sender, e) =>
                {
                    FecharRequested?.Invoke();
                };
            }
        }

        /// <summary>
        /// Configura upload de arquivos
        /// </summary>
        private void SetupFileUpload()
        {
            var uploadArea = this.FindControl<Border>("UploadArea");
            if (uploadArea != null)
            {
                // Click para selecionar arquivo
                uploadArea.PointerPressed += async (sender, e) =>
                {
                    await SelecionarArquivoAsync();
                };

                // Drag and drop
                uploadArea.DragOver += (sender, e) =>
                {
                    e.DragEffects = DragDropEffects.Copy;
                    e.Handled = true;
                };

                uploadArea.Drop += async (sender, e) =>
                {
                    if (e.Data.Contains(DataFormats.Files))
                    {
                        var files = e.Data.GetFiles();
                        var file = files?.FirstOrDefault();
                        
                        if (file != null)
                        {
                            await ProcessarArquivoSelecionado(file);
                        }
                    }
                    e.Handled = true;
                };

                // Visual feedback para drag
                uploadArea.DragEnter += (sender, e) =>
                {
                    if (uploadArea.Classes.Contains("upload-area"))
                    {
                        uploadArea.Classes.Add("drag-over");
                    }
                };

                uploadArea.DragLeave += (sender, e) =>
                {
                    uploadArea.Classes.Remove("drag-over");
                };
            }
        }

        /// <summary>
        /// Abre dialog para selecionar arquivo
        /// </summary>
        private async System.Threading.Tasks.Task SelecionarArquivoAsync()
        {
            try
            {
                var window = this.FindAncestorOfType<Window>();
                if (window == null) return;

                var storageProvider = window.StorageProvider;
                
                var fileTypes = new[]
                {
                    new FilePickerFileType("Planilhas")
                    {
                        Patterns = new[] { "*.csv", "*.xlsx", "*.xls" }
                    },
                    new FilePickerFileType("CSV")
                    {
                        Patterns = new[] { "*.csv" }
                    },
                    new FilePickerFileType("Excel")
                    {
                        Patterns = new[] { "*.xlsx", "*.xls" }
                    }
                };

                var options = new FilePickerOpenOptions
                {
                    Title = "Selecionar Planilha",
                    AllowMultiple = false,
                    FileTypeFilter = fileTypes
                };

                var files = await storageProvider.OpenFilePickerAsync(options);
                var file = files?.FirstOrDefault();
                
                if (file != null)
                {
                    await ProcessarArquivoSelecionado(file);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao selecionar arquivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa arquivo selecionado
        /// </summary>
        private async System.Threading.Tasks.Task ProcessarArquivoSelecionado(IStorageFile file)
        {
            try
            {
                var viewModel = DataContext as LayoutCreatorViewModel;
                if (viewModel == null) return;

                // Validar extensão
                var extensao = Path.GetExtension(file.Name).ToLower();
                if (!new[] { ".csv", ".xlsx", ".xls" }.Contains(extensao))
                {
                    // Mostrar erro de formato não suportado
                    return;
                }

                // Obter informações do arquivo
                var properties = await file.GetBasicPropertiesAsync();
                var tamanho = FormatarTamanhoArquivo(properties.Size);

                // Atualizar ViewModel
                viewModel.ArquivoSelecionado = true;
                viewModel.NomeArquivo = file.Name;
                viewModel.TamanhoArquivo = tamanho;

                // Aqui seria feita a leitura real do arquivo
                // Por enquanto apenas simula a seleção
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar arquivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Formata tamanho do arquivo para exibição
        /// </summary>
        private string FormatarTamanhoArquivo(ulong bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}