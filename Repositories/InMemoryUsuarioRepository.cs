using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ImportadorModelo2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImportadorModelo2.Repositories
{
    /// <summary>
    /// Implementação em memória do repositório de usuários para desenvolvimento
    /// </summary>
    public class InMemoryUsuarioRepository : IUsuarioRepository
    {
        private readonly List<Usuario> _usuarios;
        private readonly ILogger<InMemoryUsuarioRepository>? _logger;
        private readonly IConfiguration _configuration;

        public InMemoryUsuarioRepository(IConfiguration configuration, ILogger<InMemoryUsuarioRepository>? logger = null)
        {
            _configuration = configuration;
            _logger = logger;
            _usuarios = new List<Usuario>();
            
            InicializarUsuariosTeste();
        }

        /// <summary>
        /// Inicializa usuários de teste em memória
        /// </summary>
        private void InicializarUsuariosTeste()
        {
            try
            {
                var testUser = _configuration.GetSection("TestUser");
                var email = testUser["Email"] ?? "teste@exemplo.com";
                var password = testUser["Password"] ?? "123456";
                var nome = testUser["Nome"] ?? "Usuário Teste";
                var empresa = testUser["Empresa"] ?? "Empresa Teste";

                var usuario = new Usuario
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    SenhaHash = GerarHashSenha(password),
                    Nome = nome,
                    Empresa = empresa,
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow
                };

                _usuarios.Add(usuario);
                
                _logger?.LogInformation("Usuário de teste criado em memória: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao inicializar usuários de teste");
            }
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            await Task.Delay(50); // Simular operação assíncrona
            return _usuarios.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.Ativo);
        }

        public async Task<Usuario?> GetByIdAsync(Guid id)
        {
            await Task.Delay(50);
            return _usuarios.FirstOrDefault(u => u.Id == id);
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            await Task.Delay(50);
            
            usuario.Id = Guid.NewGuid();
            usuario.CriadoEm = DateTime.UtcNow;
            usuario.AtualizadoEm = DateTime.UtcNow;
            
            _usuarios.Add(usuario);
            
            return usuario;
        }

        public async Task<Usuario> UpdateAsync(Usuario usuario)
        {
            await Task.Delay(50);
            
            var existingUser = _usuarios.FirstOrDefault(u => u.Id == usuario.Id);
            if (existingUser != null)
            {
                existingUser.Nome = usuario.Nome;
                existingUser.Empresa = usuario.Empresa;
                existingUser.HashMaquina = usuario.HashMaquina;
                existingUser.DataAtivacao = usuario.DataAtivacao;
                existingUser.Ativo = usuario.Ativo;
                existingUser.AtualizadoEm = DateTime.UtcNow;
                
                return existingUser;
            }
            
            throw new InvalidOperationException("Usuário não encontrado");
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await Task.Delay(50);
            
            var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario != null)
            {
                usuario.Ativo = false;
                usuario.AtualizadoEm = DateTime.UtcNow;
                return true;
            }
            
            return false;
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string senhaHash)
        {
            await Task.Delay(50);
            
            var usuario = await GetByEmailAsync(email);
            return usuario != null && usuario.SenhaHash == senhaHash;
        }

        public async Task<bool> IsMachineAuthorizedAsync(string email, string hashMaquina)
        {
            await Task.Delay(50);
            
            var usuario = await GetByEmailAsync(email);
            if (usuario == null) return false;
            
            // Se não há hash armazenado, é o primeiro acesso
            if (string.IsNullOrEmpty(usuario.HashMaquina))
            {
                return true;
            }
            
            // Verificar se o hash da máquina coincide
            return usuario.HashMaquina == hashMaquina;
        }

        public async Task UpdateMachineHashAsync(string email, string hashMaquina)
        {
            await Task.Delay(50);
            
            var usuario = await GetByEmailAsync(email);
            if (usuario != null)
            {
                usuario.HashMaquina = hashMaquina;
                usuario.DataAtivacao ??= DateTime.UtcNow;
                usuario.AtualizadoEm = DateTime.UtcNow;
            }
        }

        public async Task UpdateLastAccessAsync(Guid usuarioId)
        {
            await Task.Delay(50);
            
            var usuario = await GetByIdAsync(usuarioId);
            if (usuario != null)
            {
                usuario.AtualizadoEm = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gera hash da senha (mesmo método usado no AutenticacaoService)
        /// </summary>
        private string GerarHashSenha(string senha)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(senha + "salt_secreto");
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}