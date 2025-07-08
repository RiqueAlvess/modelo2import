using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ImportadorModelo2.Models;
using ImportadorModelo2.Repositories;

namespace ImportadorModelo2.Utils
{
    public static class UsuarioSeeder
    {
        /// <summary>
        /// Cria usuário de teste se não existir
        /// </summary>
        public static async Task CriarUsuarioTesteAsync(IUsuarioRepository usuarioRepository)
        {
            try
            {
                var usuarioExistente = await usuarioRepository.GetByEmailAsync("teste@exemplo.com");
                if (usuarioExistente != null)
                {
                    Console.WriteLine("Usuário de teste já existe: teste@exemplo.com");
                    return;
                }

                var usuario = new Usuario
                {
                    Email = "teste@exemplo.com",
                    SenhaHash = GerarHashSenha("123456"),
                    Nome = "Usuário Teste",
                    Empresa = "Empresa Teste",
                    Ativo = true
                };

                await usuarioRepository.CreateAsync(usuario);
                Console.WriteLine("Usuário de teste criado:");
                Console.WriteLine("Email: teste@exemplo.com");
                Console.WriteLine("Senha: 123456");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar usuário de teste: {ex.Message}");
            }
        }

        /// <summary>
        /// Gera hash da senha usando SHA256
        /// </summary>
        private static string GerarHashSenha(string senha)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(senha + "salt_secreto");
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}