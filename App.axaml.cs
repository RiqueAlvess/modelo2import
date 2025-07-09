using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ImportadorModelo2.Services;
using ImportadorModelo2.Repositories;
using ImportadorModelo2.ViewModels;
using ImportadorModelo2.Views;
using ImportadorModelo2.Utils;

namespace ImportadorModelo2
{
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            ConfigureServices();
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var loginViewModel = _serviceProvider?.GetRequiredService<LoginViewModel>();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                    Content = new LoginView { DataContext = loginViewModel },
                    Title = "Importador Modelo 2",
                    Width = 480,
                    Height = 680,
                    Background = Avalonia.Media.Brushes.White,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    CanResize = false,
                    ShowInTaskbar = true,
                    SystemDecorations = SystemDecorations.None,
                    MinWidth = 480,
                    MaxWidth = 480,
                    MinHeight = 680,
                    MaxHeight = 680
                };

                if (loginViewModel != null)
                {
                    loginViewModel.LoginSucceeded += OnLoginSucceeded;
                    loginViewModel.NavigationRequested += OnNavigationRequested;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>
        /// Configura a injeção de dependência e serviços da aplicação
        /// </summary>
        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<ILocalStorageService, LocalStorageService>();
            services.AddScoped<ILayoutService, LayoutService>();
            services.AddScoped<IAutenticacaoService, AutenticacaoService>();
            services.AddScoped<IFileReaderService, FileReaderService>();

            // Determinar qual repositório usar
            var enableDatabase = configuration.GetValue<bool>("DatabaseSettings:EnableDatabase", false);
            var useInMemoryFallback = configuration.GetValue<bool>("DatabaseSettings:UseInMemoryFallback", true);

            if (enableDatabase)
            {
                services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            }
            else if (useInMemoryFallback)
            {
                services.AddScoped<IUsuarioRepository, InMemoryUsuarioRepository>();
            }
            else
            {
                services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            }

            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<LayoutCreatorViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            // Inicializar sistema em background
            InitializeSystemAsync();
        }

        /// <summary>
        /// Inicializa o sistema em background
        /// </summary>
        private void InitializeSystemAsync()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var logger = _serviceProvider?.GetService<ILogger<App>>();
                    var configuration = _serviceProvider?.GetRequiredService<IConfiguration>();
                    var enableDatabase = configuration?.GetValue<bool>("DatabaseSettings:EnableDatabase", false) ?? false;

                    if (enableDatabase)
                    {
                        // Tentar inicializar PostgreSQL
                        var dbService = _serviceProvider?.GetRequiredService<IDatabaseService>();
                        if (dbService != null)
                        {
                            await dbService.InitializeDatabaseAsync();
                            
                            if (dbService.IsAvailable)
                            {
                                logger?.LogInformation("PostgreSQL inicializado com sucesso");
                                
                                // Criar usuário de teste no PostgreSQL
                                var usuarioRepository = _serviceProvider?.GetRequiredService<IUsuarioRepository>();
                                if (usuarioRepository != null)
                                {
                                    await UsuarioSeeder.CriarUsuarioTesteAsync(usuarioRepository);
                                }
                            }
                            else
                            {
                                logger?.LogWarning("PostgreSQL não disponível - usando repositório configurado");
                            }
                        }
                    }
                    else
                    {
                        logger?.LogInformation("Modo de desenvolvimento - usando repositório em memória");
                    }
                }
                catch (Exception ex)
                {
                    var logger = _serviceProvider?.GetService<ILogger<App>>();
                    logger?.LogError(ex, "Erro durante inicialização do sistema");
                }
            });
        }

        /// <summary>
        /// Processa login bem-sucedido e navega para a tela principal
        /// </summary>
        private void OnLoginSucceeded(Models.Usuario usuario)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainViewModel = _serviceProvider?.GetRequiredService<MainViewModel>();

                if (mainViewModel != null)
                {
                    mainViewModel.Usuario = usuario;

                    var mainView = new MainView
                    {
                        DataContext = mainViewModel
                    };

                    mainView.LogoutRequested += OnLogoutRequested;
                    mainView.NovaImportacaoRequested += OnNovaImportacaoRequested;
                    mainView.VisualizarLogsRequested += OnVisualizarLogsRequested;
                    mainView.NovoLayoutRequested += OnNovoLayoutRequested;

                    if (desktop.MainWindow is Window mainWindow)
                    {
                        mainWindow.Content = mainView;
                        mainWindow.Title = $"Importador Modelo 2 - {usuario.Nome ?? usuario.Email}";
                        mainWindow.Width = 1024;
                        mainWindow.Height = 768;
                        mainWindow.CanResize = true;
                        mainWindow.SystemDecorations = SystemDecorations.None;
                        mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        mainWindow.MinWidth = 800;
                        mainWindow.MinHeight = 600;
                        mainWindow.MaxWidth = double.PositiveInfinity;
                        mainWindow.MaxHeight = double.PositiveInfinity;
                    }
                }
            }
        }

        /// <summary>
        /// Processa logout e retorna para a tela de login
        /// </summary>
        private void OnLogoutRequested()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                try
                {
                    var loginViewModel = _serviceProvider?.GetRequiredService<LoginViewModel>();

                    if (loginViewModel != null && desktop.MainWindow is Window mainWindow)
                    {
                        loginViewModel.LoginSucceeded += OnLoginSucceeded;
                        loginViewModel.NavigationRequested += OnNavigationRequested;

                        var loginView = new LoginView { DataContext = loginViewModel };
                        
                        mainWindow.Content = loginView;
                        mainWindow.Title = "Importador Modelo 2";
                        mainWindow.Width = 480;
                        mainWindow.Height = 680;
                        mainWindow.CanResize = false;
                        mainWindow.SystemDecorations = SystemDecorations.None;
                        mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        mainWindow.MinWidth = 480;
                        mainWindow.MaxWidth = 480;
                        mainWindow.MinHeight = 680;
                        mainWindow.MaxHeight = 680;

                        loginViewModel.Email = string.Empty;
                        loginViewModel.Password = string.Empty;
                        loginViewModel.RememberMe = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro durante logout: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Processa navegações do login (esqueci senha, cadastro, etc.)
        /// </summary>
        private void OnNavigationRequested(string destination)
        {
            try
            {
                switch (destination)
                {
                    case "ForgotPassword":
                        System.Diagnostics.Debug.WriteLine("Navegar para: Recuperar Senha");
                        break;
                    case "SignUp":
                        System.Diagnostics.Debug.WriteLine("Navegar para: Criar Conta");
                        break;
                    case "GoogleLogin":
                        System.Diagnostics.Debug.WriteLine("Navegar para: Login Google");
                        break;
                    case "MicrosoftLogin":
                        System.Diagnostics.Debug.WriteLine("Navegar para: Login Microsoft");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na navegação: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa solicitação de nova importação
        /// </summary>
        private void OnNovaImportacaoRequested()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Nova Importação solicitada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar Nova Importação: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa solicitação de visualização de logs
        /// </summary>
        private void OnVisualizarLogsRequested()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Visualizar Logs solicitado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar Visualizar Logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa solicitação de criação/edição de layout
        /// </summary>
        private void OnNovoLayoutRequested()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Novo Layout solicitado");
                
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var layoutViewModel = _serviceProvider?.GetRequiredService<LayoutCreatorViewModel>();
                    if (layoutViewModel == null) return;
                    var layoutView = new LayoutCreatorView
                    {
                        DataContext = layoutViewModel
                    };

                    // Conectar eventos da LayoutCreatorView
                    layoutView.VoltarRequested += () => VoltarParaMain();
                    layoutView.FecharRequested += () => FecharAplicacao();
                    
                    // Conectar evento de layout salvo
                    layoutViewModel.LayoutSalvo += (layout) => 
                    {
                        System.Diagnostics.Debug.WriteLine($"Layout '{layout.Nome}' salvo com sucesso!");
                        VoltarParaMain();
                    };

                    if (desktop.MainWindow is Window mainWindow)
                    {
                        mainWindow.Content = layoutView;
                        mainWindow.Title = "Importador Modelo 2 - Criar Layout";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao processar Novo Layout: {ex.Message}");
            }
        }

        /// <summary>
        /// Retorna o provedor de serviços configurado
        /// </summary>
        public IServiceProvider? GetServiceProvider() => _serviceProvider;

        /// <summary>
        /// Volta para a tela principal
        /// </summary>
        private void VoltarParaMain()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainViewModel = _serviceProvider?.GetRequiredService<MainViewModel>();
                if (mainViewModel != null && desktop.MainWindow is Window mainWindow)
                {
                    var mainView = new MainView
                    {
                        DataContext = mainViewModel
                    };

                    mainView.LogoutRequested += OnLogoutRequested;
                    mainView.NovaImportacaoRequested += OnNovaImportacaoRequested;
                    mainView.VisualizarLogsRequested += OnVisualizarLogsRequested;
                    mainView.NovoLayoutRequested += OnNovoLayoutRequested;

                    mainWindow.Content = mainView;
                    mainWindow.Title = $"Importador Modelo 2 - {mainViewModel.UsuarioNome}";
                }
            }
        }

        /// <summary>
        /// Fecha a aplicação
        /// </summary>
        private void FecharAplicacao()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Close();
            }
        }
    }
}