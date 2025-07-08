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
        public static async Task CriarUsuarioTesteAsync(IUsuarioRepository usuarioRepository)
        {
            try
            {
                // Verificar se já existe usuário de teste
                var usuarioExistente = await usuarioRepository.GetByEmailAsync("teste@exemplo.com");
                if (usuarioExistente != null)
                {
                    Console.WriteLine("Usuário de teste já existe: teste@exemplo.com");
                    return;
                }

                // Criar usuário de teste
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

        private static string GerarHashSenha(string senha)
        {
            // Mesmo método usado no AutenticacaoService
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(senha + "salt_secreto");
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}