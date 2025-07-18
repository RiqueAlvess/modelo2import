using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using Microsoft.Extensions.Logging;

namespace ImportadorModelo2.Services
{
    public interface IFileReaderService
    {
        Task<List<string>> LerCabecalhoAsync(string caminhoArquivo, int linhaHeader = 1);
        Task<List<List<string>>> LerDadosAsync(string caminhoArquivo, int linhaHeader = 1, int maxLinhas = 100);
        Task<FileInfoModel> ObterInformacoesArquivoAsync(string caminhoArquivo);
        bool ValidarArquivo(string caminhoArquivo);
        List<string> ObterFormatosSuportados();
    }

    public class FileReaderService : IFileReaderService
    {
        private readonly ILogger<FileReaderService>? _logger;
        private readonly List<string> _formatosSuportados = new() { ".csv", ".xlsx", ".xls" };

        public FileReaderService(ILogger<FileReaderService>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Lê o cabeçalho do arquivo na linha especificada
        /// </summary>
        public async Task<List<string>> LerCabecalhoAsync(string caminhoArquivo, int linhaHeader = 1)
        {
            try
            {
                _logger?.LogInformation("Lendo cabeçalho do arquivo: {Arquivo}, linha: {Linha}", caminhoArquivo, linhaHeader);

                var extensao = Path.GetExtension(caminhoArquivo).ToLower();
                
                return extensao switch
                {
                    ".csv" => await LerCabecalhoCSVAsync(caminhoArquivo, linhaHeader),
                    ".xlsx" or ".xls" => await LerCabecalhoExcelAsync(caminhoArquivo, linhaHeader),
                    _ => throw new NotSupportedException($"Formato de arquivo não suportado: {extensao}")
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao ler cabeçalho do arquivo: {Arquivo}", caminhoArquivo);
                throw;
            }
        }

        /// <summary>
        /// Lê dados do arquivo (limitado por maxLinhas)
        /// </summary>
        public async Task<List<List<string>>> LerDadosAsync(string caminhoArquivo, int linhaHeader = 1, int maxLinhas = 100)
        {
            try
            {
                _logger?.LogInformation("Lendo dados do arquivo: {Arquivo}, max linhas: {MaxLinhas}", caminhoArquivo, maxLinhas);

                var extensao = Path.GetExtension(caminhoArquivo).ToLower();
                
                return extensao switch
                {
                    ".csv" => await LerDadosCSVAsync(caminhoArquivo, linhaHeader, maxLinhas),
                    ".xlsx" or ".xls" => await LerDadosExcelAsync(caminhoArquivo, linhaHeader, maxLinhas),
                    _ => throw new NotSupportedException($"Formato de arquivo não suportado: {extensao}")
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao ler dados do arquivo: {Arquivo}", caminhoArquivo);
                throw;
            }
        }

        /// <summary>
        /// Obtém informações básicas do arquivo
        /// </summary>
        public async Task<FileInfoModel> ObterInformacoesArquivoAsync(string caminhoArquivo)
        {
            try
            {
                var fileInfo = new FileInfo(caminhoArquivo);
                var dados = await LerDadosAsync(caminhoArquivo, 1, 10); // Ler apenas algumas linhas para análise

                return new FileInfoModel
                {
                    Nome = fileInfo.Name,
                    Tamanho = fileInfo.Length,
                    Extensao = fileInfo.Extension.ToLower(),
                    DataModificacao = fileInfo.LastWriteTime,
                    LinhasEstimadas = EstimarTotalLinhas(caminhoArquivo),
                    ColunasDetectadas = dados.FirstOrDefault()?.Count ?? 0,
                    EncodingDetectado = DetectarEncoding(caminhoArquivo)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro ao obter informações do arquivo: {Arquivo}", caminhoArquivo);
                throw;
            }
        }

        /// <summary>
        /// Valida se o arquivo pode ser processado
        /// </summary>
        public bool ValidarArquivo(string caminhoArquivo)
        {
            try
            {
                if (!File.Exists(caminhoArquivo))
                {
                    _logger?.LogWarning("Arquivo não encontrado: {Arquivo}", caminhoArquivo);
                    return false;
                }

                var extensao = Path.GetExtension(caminhoArquivo).ToLower();
                if (!_formatosSuportados.Contains(extensao))
                {
                    _logger?.LogWarning("Formato não suportado: {Extensao}", extensao);
                    return false;
                }

                var fileInfo = new FileInfo(caminhoArquivo);
                if (fileInfo.Length == 0)
                {
                    _logger?.LogWarning("Arquivo vazio: {Arquivo}", caminhoArquivo);
                    return false;
                }

                // Validação adicional: tentar ler primeira linha
                var cabecalho = LerCabecalhoAsync(caminhoArquivo, 1).GetAwaiter().GetResult();
                if (!cabecalho.Any())
                {
                    _logger?.LogWarning("Não foi possível ler cabeçalho do arquivo: {Arquivo}", caminhoArquivo);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro na validação do arquivo: {Arquivo}", caminhoArquivo);
                return false;
            }
        }

        /// <summary>
        /// Retorna lista de formatos suportados
        /// </summary>
        public List<string> ObterFormatosSuportados()
        {
            return new List<string>(_formatosSuportados);
        }

        #region Métodos Privados

        /// <summary>
        /// Lê cabeçalho de arquivo CSV
        /// </summary>
        private async Task<List<string>> LerCabecalhoCSVAsync(string caminhoArquivo, int linhaHeader)
        {
            var encoding = DetectarEncoding(caminhoArquivo);
            using var reader = new StreamReader(caminhoArquivo, encoding);
            
            for (int i = 1; i < linhaHeader; i++)
            {
                await reader.ReadLineAsync();
            }

            var linha = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(linha))
            {
                return new List<string>();
            }

            return ParsearLinhaCSV(linha);
        }

        /// <summary>
        /// Lê dados de arquivo CSV
        /// </summary>
        private async Task<List<List<string>>> LerDadosCSVAsync(string caminhoArquivo, int linhaHeader, int maxLinhas)
        {
            var dados = new List<List<string>>();
            var encoding = DetectarEncoding(caminhoArquivo);
            
            using var reader = new StreamReader(caminhoArquivo, encoding);
            
            // Pular até a linha de dados (linha após o header)
            for (int i = 1; i <= linhaHeader; i++)
            {
                await reader.ReadLineAsync();
            }

            int linhasLidas = 0;
            string? linha;
            
            while ((linha = await reader.ReadLineAsync()) != null && linhasLidas < maxLinhas)
            {
                if (!string.IsNullOrEmpty(linha))
                {
                    dados.Add(ParsearLinhaCSV(linha));
                    linhasLidas++;
                }
            }

            return dados;
        }

        /// <summary>
        /// Lê cabeçalho de um arquivo Excel usando EPPlus
        /// </summary>
        private async Task<List<string>> LerCabecalhoExcelAsync(string caminhoArquivo, int linhaHeader)
        {
            return await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(caminhoArquivo));
                var worksheet = package.Workbook.Worksheets.First();

                int ultimaColuna = worksheet.Dimension.End.Column;
                var cabecalho = new List<string>();
                for (int col = 1; col <= ultimaColuna; col++)
                {
                    cabecalho.Add(worksheet.Cells[linhaHeader, col].Text.Trim());
                }

                return cabecalho;
            });
        }

        /// <summary>
        /// Lê dados de um arquivo Excel usando EPPlus
        /// </summary>
        private async Task<List<List<string>>> LerDadosExcelAsync(string caminhoArquivo, int linhaHeader, int maxLinhas)
        {
            return await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage(new FileInfo(caminhoArquivo));
                var worksheet = package.Workbook.Worksheets.First();

                int ultimaLinha = worksheet.Dimension.End.Row;
                int ultimaColuna = worksheet.Dimension.End.Column;

                var dados = new List<List<string>>();
                int linhaInicial = linhaHeader + 1;
                int linhaFinal = Math.Min(ultimaLinha, linhaInicial + maxLinhas - 1);

                for (int row = linhaInicial; row <= linhaFinal; row++)
                {
                    var linhaDados = new List<string>();
                    bool linhaVazia = true;
                    for (int col = 1; col <= ultimaColuna; col++)
                    {
                        var texto = worksheet.Cells[row, col].Text.Trim();
                        linhaDados.Add(texto);
                        if (!string.IsNullOrEmpty(texto)) linhaVazia = false;
                    }

                    if (!linhaVazia)
                    {
                        dados.Add(linhaDados);
                    }
                }

                return dados;
            });
        }

        /// <summary>
        /// Faz parsing de uma linha CSV considerando vírgulas dentro de aspas
        /// </summary>
        private List<string> ParsearLinhaCSV(string linha)
        {
            var colunas = new List<string>();
            var colunaAtual = new StringBuilder();
            bool dentroAspas = false;
            
            for (int i = 0; i < linha.Length; i++)
            {
                char c = linha[i];
                
                if (c == '"')
                {
                    dentroAspas = !dentroAspas;
                }
                else if (c == ',' && !dentroAspas)
                {
                    colunas.Add(colunaAtual.ToString().Trim());
                    colunaAtual.Clear();
                }
                else
                {
                    colunaAtual.Append(c);
                }
            }
            
            // Adicionar última coluna
            colunas.Add(colunaAtual.ToString().Trim());
            
            return colunas;
        }

        /// <summary>
        /// Detecta encoding do arquivo
        /// </summary>
        private Encoding DetectarEncoding(string caminhoArquivo)
        {
            try
            {
                using var reader = new StreamReader(caminhoArquivo, Encoding.Default, true);
                reader.ReadLine();
                return reader.CurrentEncoding;
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Estima total de linhas do arquivo
        /// </summary>
        private int EstimarTotalLinhas(string caminhoArquivo)
        {
            try
            {
                if (Path.GetExtension(caminhoArquivo).ToLower() == ".csv")
                {
                    return File.ReadAllLines(caminhoArquivo).Length;
                }
                
                // Para Excel, retornar estimativa
                return 1000;
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// Informações de um arquivo
    /// </summary>
    public class FileInfoModel
    {
        public string Nome { get; set; } = string.Empty;
        public long Tamanho { get; set; }
        public string Extensao { get; set; } = string.Empty;
        public DateTime DataModificacao { get; set; }
        public int LinhasEstimadas { get; set; }
        public int ColunasDetectadas { get; set; }
        public Encoding EncodingDetectado { get; set; } = Encoding.UTF8;
    }
}