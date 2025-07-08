using System;
using System.Threading.Tasks;
using Npgsql;
using ImportadorModelo2.Models;
using ImportadorModelo2.Services;
using Microsoft.Extensions.Logging;

namespace ImportadorModelo2.Repositories
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> GetByEmailAsync(string email);
        Task<Usuario?> GetByIdAsync(Guid id);
        Task<Usuario> CreateAsync(Usuario usuario);
        Task<Usuario> UpdateAsync(Usuario usuario);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ValidateCredentialsAsync(string email, string senhaHash);
        Task<bool> IsMachineAuthorizedAsync(string email, string hashMaquina);
        Task UpdateMachineHashAsync(string email, string hashMaquina);
        Task UpdateLastAccessAsync(Guid usuarioId);
    }

    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<UsuarioRepository>? _logger;

        public UsuarioRepository(IDatabaseService databaseService, ILogger<UsuarioRepository>? logger = null)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    SELECT id, email, senha_hash, nome, empresa, hash_maquina, 
                           data_ativacao, ativo, criado_em, atualizado_em
                    FROM usuarios 
                    WHERE email = @email AND ativo = true";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@email", email);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return MapUsuario(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao buscar usuário por email: {Email}", email);
                throw;
            }
        }

        public async Task<Usuario?> GetByIdAsync(Guid id)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    SELECT id, email, senha_hash, nome, empresa, hash_maquina, 
                           data_ativacao, ativo, criado_em, atualizado_em
                    FROM usuarios 
                    WHERE id = @id";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    return MapUsuario(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao buscar usuário por ID: {Id}", id);
                throw;
            }
        }

        public async Task<Usuario> CreateAsync(Usuario usuario)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    INSERT INTO usuarios (email, senha_hash, nome, empresa, hash_maquina, data_ativacao, ativo)
                    VALUES (@email, @senha_hash, @nome, @empresa, @hash_maquina, @data_ativacao, @ativo)
                    RETURNING id, criado_em, atualizado_em";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@email", usuario.Email);
                command.Parameters.AddWithValue("@senha_hash", usuario.SenhaHash);
                command.Parameters.AddWithValue("@nome", (object?)usuario.Nome ?? DBNull.Value);
                command.Parameters.AddWithValue("@empresa", (object?)usuario.Empresa ?? DBNull.Value);
                command.Parameters.AddWithValue("@hash_maquina", (object?)usuario.HashMaquina ?? DBNull.Value);
                command.Parameters.AddWithValue("@data_ativacao", (object?)usuario.DataAtivacao ?? DBNull.Value);
                command.Parameters.AddWithValue("@ativo", usuario.Ativo);

                using var reader = await command.ExecuteReaderAsync();
                
                if (await reader.ReadAsync())
                {
                    usuario.Id = reader.GetGuid(0); // id
                    usuario.CriadoEm = reader.GetDateTime(1); // criado_em
                    usuario.AtualizadoEm = reader.GetDateTime(2); // atualizado_em
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao criar usuário: {Email}", usuario.Email);
                throw;
            }
        }

        public async Task<Usuario> UpdateAsync(Usuario usuario)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    UPDATE usuarios 
                    SET nome = @nome, empresa = @empresa, hash_maquina = @hash_maquina,
                        data_ativacao = @data_ativacao, ativo = @ativo, atualizado_em = now()
                    WHERE id = @id
                    RETURNING atualizado_em";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", usuario.Id);
                command.Parameters.AddWithValue("@nome", (object?)usuario.Nome ?? DBNull.Value);
                command.Parameters.AddWithValue("@empresa", (object?)usuario.Empresa ?? DBNull.Value);
                command.Parameters.AddWithValue("@hash_maquina", (object?)usuario.HashMaquina ?? DBNull.Value);
                command.Parameters.AddWithValue("@data_ativacao", (object?)usuario.DataAtivacao ?? DBNull.Value);
                command.Parameters.AddWithValue("@ativo", usuario.Ativo);

                var result = await command.ExecuteScalarAsync();
                if (result != null)
                {
                    usuario.AtualizadoEm = (DateTime)result;
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao atualizar usuário: {Id}", usuario.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = "UPDATE usuarios SET ativo = false, atualizado_em = now() WHERE id = @id";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao deletar usuário: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string senhaHash)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    SELECT COUNT(*) 
                    FROM usuarios 
                    WHERE email = @email AND senha_hash = @senha_hash AND ativo = true";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@senha_hash", senhaHash);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao validar credenciais para: {Email}", email);
                throw;
            }
        }

        public async Task<bool> IsMachineAuthorizedAsync(string email, string hashMaquina)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    SELECT hash_maquina 
                    FROM usuarios 
                    WHERE email = @email AND ativo = true";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@email", email);

                var storedHash = await command.ExecuteScalarAsync() as string;
                
                // Se não há hash armazenado, é o primeiro acesso
                if (string.IsNullOrEmpty(storedHash))
                {
                    return true;
                }

                // Verificar se o hash da máquina coincide
                return storedHash == hashMaquina;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao verificar autorização da máquina para: {Email}", email);
                throw;
            }
        }

        public async Task UpdateMachineHashAsync(string email, string hashMaquina)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = @"
                    UPDATE usuarios 
                    SET hash_maquina = @hash_maquina, 
                        data_ativacao = COALESCE(data_ativacao, now()),
                        atualizado_em = now()
                    WHERE email = @email";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@email", email);
                command.Parameters.AddWithValue("@hash_maquina", hashMaquina);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao atualizar hash da máquina para: {Email}", email);
                throw;
            }
        }

        public async Task UpdateLastAccessAsync(Guid usuarioId)
        {
            try
            {
                using var connection = await _databaseService.GetConnectionAsync();
                
                var sql = "UPDATE usuarios SET atualizado_em = now() WHERE id = @id";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@id", usuarioId);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao atualizar último acesso: {Id}", usuarioId);
                throw;
            }
        }

        private static Usuario MapUsuario(NpgsqlDataReader reader)
        {
            return new Usuario
            {
                Id = reader.GetGuid(0), // id
                Email = reader.GetString(1), // email
                SenhaHash = reader.GetString(2), // senha_hash
                Nome = reader.IsDBNull(3) ? null : reader.GetString(3), // nome
                Empresa = reader.IsDBNull(4) ? null : reader.GetString(4), // empresa
                HashMaquina = reader.IsDBNull(5) ? null : reader.GetString(5), // hash_maquina
                DataAtivacao = reader.IsDBNull(6) ? null : reader.GetDateTime(6), // data_ativacao
                Ativo = reader.GetBoolean(7), // ativo
                CriadoEm = reader.GetDateTime(8), // criado_em
                AtualizadoEm = reader.GetDateTime(9) // atualizado_em
            };
        }
    }
}