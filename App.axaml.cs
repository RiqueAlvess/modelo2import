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
            });

            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddSingleton<ILocalStorageService, LocalStorageService>();
            services.AddScoped<IAutenticacaoService, AutenticacaoService>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var dbService = _serviceProvider.GetRequiredService<IDatabaseService>();
                    await dbService.InitializeDatabaseAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao inicializar banco: {ex.Message}");
                }
            });
        }

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

                    
                    if (desktop.MainWindow is Window mainWindow)
                    {
                        mainWindow.Content = mainView;

                        mainWindow.Title = $"Importador Modelo 2 - {usuario.Nome ?? usuario.Email}";
                        mainWindow.Width = 1024;
                        mainWindow.Height = 768;
                        mainWindow.CanResize = true;
                        mainWindow.SystemDecorations = SystemDecorations.Full;
                    }
                }
            }
        }


        private void OnNavigationRequested(string destination)
        {
            switch (destination)
            {
                case "ForgotPassword":
                    System.Diagnostics.Debug.WriteLine("Navegar para: Recuperar Senha");
                    break;
                case "SignUp":
                    System.Diagnostics.Debug.WriteLine("Navegar para: Criar Conta");
                    break;
            }
        }
    }
}
