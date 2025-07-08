using Avalonia;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ImportadorModelo2.Repositories;
using ImportadorModelo2.Utils;

namespace ImportadorModelo2;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Log crítico na inicialização
            System.Diagnostics.Debug.WriteLine($"Erro crítico na inicialização: {ex.Message}");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // Método para criar usuário de teste (pode ser chamado via menu debug)
    public static async Task CriarUsuarioTesteAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var usuarioRepository = serviceProvider.GetRequiredService<IUsuarioRepository>();
            await UsuarioSeeder.CriarUsuarioTesteAsync(usuarioRepository);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar usuário de teste: {ex.Message}");
        }
    }
}