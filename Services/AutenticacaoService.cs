using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ImportadorModelo2.Models;
using ImportadorModelo2.Repositories;
using Microsoft.Extensions.Configuration;

namespace ImportadorModelo2.Services
{
    public interface IAutenticacaoService
    {
        Task<LoginResponse> LoginAsync(string email, string senha, string hashMaquina);
        Task<string> ObterHashMaquinaAsync();
        Task SalvarCredenciaisAsync(string email, bool lembrarMe);
        Task<(string? email, bool lembrarMe)> CarregarCredenciaisAsync();
        Task LimparCredenciaisAsync();
    }

    public class AutenticacaoService : IAutenticacaoService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IConfiguration _configuration;
        private readonly ILocalStorageService _localStorage;

        public AutenticacaoService(
            IUsuarioRepository usuarioRepository,
            IConfiguration configuration,
            ILocalStorageService localStorage)
        {
            _usuarioRepository = usuarioRepository;
            _configuration = configuration;
            _localStorage = localStorage;
        }

        public async Task<LoginResponse> LoginAsync(string email, string senha, string hashMaquina)
        {
            try
            {
                // Buscar usuário no banco
                var usuario = await _usuarioRepository.GetByEmailAsync(email);
                if (usuario == null)
                {
                    return new LoginResponse(false, "Email ou senha incorretos");
                }

                // Validar senha (você pode usar BCrypt aqui)
                var senhaHash = GerarHashSenha(senha);
                if (usuario.SenhaHash != senhaHash)
                {
                    return new LoginResponse(false, "Email ou senha incorretos");
                }

                // Verificar se a máquina está autorizada
                var maquinaAutorizada = await _usuarioRepository.IsMachineAuthorizedAsync(email, hashMaquina);
                if (!maquinaAutorizada)
                {
                    return new LoginResponse(false, "Acesso negado. Esta máquina não está autorizada para este usuário.");
                }

                // Atualizar hash da máquina se necessário (primeiro acesso)
                if (string.IsNullOrEmpty(usuario.HashMaquina))
                {
                    await _usuarioRepository.UpdateMachineHashAsync(email, hashMaquina);
                    usuario.HashMaquina = hashMaquina;
                    usuario.DataAtivacao = DateTime.UtcNow;
                }

                // Atualizar último acesso
                await _usuarioRepository.UpdateLastAccessAsync(usuario.Id);

                return new LoginResponse(true, "Login realizado com sucesso", usuario);
            }
            catch (Exception ex)
            {
                // Log do erro
                System.Diagnostics.Debug.WriteLine($"Erro no login: {ex.Message}");
                return new LoginResponse(false, "Erro inesperado. Tente novamente.");
            }
        }

        public async Task<string> ObterHashMaquinaAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Combinar múltiplos identificadores únicos da máquina
                    var identificadores = new StringBuilder();
                    
                    // ID da máquina (nome do computador)
                    identificadores.Append(Environment.MachineName);
                    
                    // Nome do usuário
                    identificadores.Append(Environment.UserName);
                    
                    // Informações do sistema operacional
                    identificadores.Append(Environment.OSVersion.ToString());
                    
                    // Número de processadores
                    identificadores.Append(Environment.ProcessorCount);
                    
                    // TODO: Adicionar mais identificadores específicos se necessário
                    // Como MAC address, serial do HD, etc.
                    
                    // Gerar hash SHA256
                    using var sha256 = SHA256.Create();
                    var bytes = Encoding.UTF8.GetBytes(identificadores.ToString());
                    var hashBytes = sha256.ComputeHash(bytes);
                    
                    return Convert.ToBase64String(hashBytes);
                }
                catch
                {
                    // Fallback: usar um identificador básico
                    return Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{Environment.MachineName}_{Environment.UserName}")
                    );
                }
            });
        }

        public async Task SalvarCredenciaisAsync(string email, bool lembrarMe)
        {
            try
            {
                if (lembrarMe)
                {
                    await _localStorage.SetItemAsync("saved_email", email);
                    await _localStorage.SetItemAsync("remember_me", true);
                }
                else
                {
                    await _localStorage.RemoveItemAsync("saved_email");
                    await _localStorage.SetItemAsync("remember_me", false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao salvar credenciais: {ex.Message}");
            }
        }

        public async Task<(string? email, bool lembrarMe)> CarregarCredenciaisAsync()
        {
            try
            {
                var email = await _localStorage.GetItemAsync<string?>("saved_email");
                var lembrarMe = await _localStorage.GetItemAsync<bool>("remember_me");
                
                return (email, lembrarMe);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar credenciais: {ex.Message}");
                return (null, false);
            }
        }

        public async Task LimparCredenciaisAsync()
        {
            try
            {
                await _localStorage.RemoveItemAsync("saved_email");
                await _localStorage.RemoveItemAsync("remember_me");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao limpar credenciais: {ex.Message}");
            }
        }

        private string GerarHashSenha(string senha)
        {
            // Por simplicidade, usando SHA256. Em produção, use BCrypt
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(senha + "salt_secreto"); // Adicione um salt
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}