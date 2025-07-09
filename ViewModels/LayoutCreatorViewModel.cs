using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ImportadorModelo2.Core.Models;
using ImportadorModelo2.Core.Utils;
using ImportadorModelo2.Services;

namespace ImportadorModelo2.ViewModels;

public class LayoutCreatorViewModel : ViewModelBase
{
    private readonly IFileReaderService _fileReader;
    private readonly ILayoutService _layoutService;

    public LayoutCreatorViewModel(IFileReaderService fileReader, ILayoutService layoutService)
    {
        _fileReader = fileReader;
        _layoutService = layoutService;

        LinhaHeader = 1;
        CategoriaFiltro = "Todos";

        ProcessarArquivoCommand = new AsyncRelayCommand(ProcessarArquivoAsync, () => !string.IsNullOrEmpty(CaminhoArquivo));
        SalvarLayoutCommand = new AsyncRelayCommand(SalvarLayoutAsync, () => ProntoParaSalvar);
        LimparCommand = new RelayCommand(Limpar);

        CarregarCamposApi();
    }

    #region Propriedades da Tela

    private string? _caminhoArquivo;
    public string CaminhoArquivo
    {
        get => _caminhoArquivo ?? string.Empty;
        set { _caminhoArquivo = value; OnPropertyChanged(nameof(CaminhoArquivo)); ((AsyncRelayCommand)ProcessarArquivoCommand).NotifyCanExecuteChanged(); }
    }

    private bool _arquivoSelecionado;
    public bool ArquivoSelecionado
    {
        get => _arquivoSelecionado;
        set { _arquivoSelecionado = value; OnPropertyChanged(nameof(ArquivoSelecionado)); }
    }

    private string _nomeArquivo = string.Empty;
    public string NomeArquivo
    {
        get => _nomeArquivo;
        set { _nomeArquivo = value; OnPropertyChanged(nameof(NomeArquivo)); }
    }

    private string _tamanhoArquivo = string.Empty;
    public string TamanhoArquivo
    {
        get => _tamanhoArquivo;
        set { _tamanhoArquivo = value; OnPropertyChanged(nameof(TamanhoArquivo)); }
    }

    private int _linhaHeader;
    public int LinhaHeader
    {
        get => _linhaHeader;
        set { _linhaHeader = value; OnPropertyChanged(nameof(LinhaHeader)); }
    }

    private bool _colunasIdentificadas;
    public bool ColunasIdentificadas
    {
        get => _colunasIdentificadas;
        set { _colunasIdentificadas = value; OnPropertyChanged(nameof(ColunasIdentificadas)); AtualizarProntoParaSalvar(); }
    }

    private string _descricaoColunas = string.Empty;
    public string DescricaoColunas
    {
        get => _descricaoColunas;
        set { _descricaoColunas = value; OnPropertyChanged(nameof(DescricaoColunas)); }
    }

    public ObservableCollection<ColunaPlanilha> Colunas { get; } = new();
    public ObservableCollection<ColunaPlanilha> ColunasDisponiveis { get; } = new();

    public ObservableCollection<CampoMapeamentoVM> CamposMapeamento { get; } = new();

    private string _categoriaFiltro = "Todos";
    public string CategoriaFiltro
    {
        get => _categoriaFiltro;
        set { _categoriaFiltro = value; OnPropertyChanged(nameof(CategoriaFiltro)); }
    }

    // Regras de negócio
    public bool CriarFuncionario { get; set; } = true;
    public bool CriarCargo { get; set; } = true;
    public bool CriarSetor { get; set; } = true;
    public bool CriarCentroCusto { get; set; } = true;
    public bool CriarUnidade { get; set; } = true;
    public bool CriarTurno { get; set; } = true;

    public bool AtualizarFuncionario { get; set; } = true;
    public bool AtualizarCargo { get; set; }
    public bool AtualizarSetor { get; set; }
    public bool AtualizarCentroCusto { get; set; }
    public bool AtualizarUnidade { get; set; }
    public bool AtualizarTurno { get; set; }

    public bool CriarHistorico { get; set; } = true;
    public bool DestravarBloqueado { get; set; }
    public bool NaoImportarSemHierarquia { get; set; } = true;

    private string _nomeLayout = string.Empty;
    public string NomeLayout
    {
        get => _nomeLayout;
        set { _nomeLayout = value; OnPropertyChanged(nameof(NomeLayout)); AtualizarProntoParaSalvar(); ((AsyncRelayCommand)SalvarLayoutCommand).NotifyCanExecuteChanged(); }
    }

    private string _descricaoLayout = string.Empty;
    public string DescricaoLayout
    {
        get => _descricaoLayout;
        set { _descricaoLayout = value; OnPropertyChanged(nameof(DescricaoLayout)); }
    }

    private bool _prontoParaSalvar;
    public bool ProntoParaSalvar
    {
        get => _prontoParaSalvar;
        private set { _prontoParaSalvar = value; OnPropertyChanged(nameof(ProntoParaSalvar)); ((AsyncRelayCommand)SalvarLayoutCommand).NotifyCanExecuteChanged(); }
    }

    private string _statusMensagem = string.Empty;
    public string StatusMensagem
    {
        get => _statusMensagem;
        set { _statusMensagem = value; OnPropertyChanged(nameof(StatusMensagem)); }
    }

    #endregion

    #region Comandos
    public ICommand ProcessarArquivoCommand { get; }
    public ICommand SalvarLayoutCommand { get; }
    public ICommand LimparCommand { get; }
    #endregion

    public event Action<LayoutModel>? LayoutSalvo;

    private void AtualizarProntoParaSalvar()
    {
        ProntoParaSalvar = ColunasIdentificadas && !string.IsNullOrWhiteSpace(NomeLayout);
    }

    private void CarregarCamposApi()
    {
        var campos = new[]
        {
            new ApiField("nomeFuncionario", "string", true),
            new ApiField("cpf", "string", false),
            new ApiField("matricula", "string", false),
            new ApiField("dataAdmissao", "string", true),
            new ApiField("dataNascimento", "string", true),
            new ApiField("sexo", "string", true),
            new ApiField("estadoCivil", "string", true),
            new ApiField("codigoEmpresa", "string", true),
            new ApiField("tipoContratacao", "string", true),
            new ApiField("regimeTrabalho", "string", true),
            new ApiField("cargo.nome", "string", false),
            new ApiField("cargo.codigo", "string", false),
            new ApiField("setor.nome", "string", false),
            new ApiField("unidade.nome", "string", false)
        };

        foreach (var campo in campos)
        {
            CamposMapeamento.Add(new CampoMapeamentoVM
            {
                NomeCampo = campo.Nome,
                Categoria = ObterCategoria(campo.Nome),
                Tipo = campo.Tipo,
                Obrigatorio = campo.Obrigatorio
            });
        }
    }

    private string ObterCategoria(string campo)
    {
        if (campo.StartsWith("cargo.")) return "Cargo";
        if (campo.StartsWith("setor.")) return "Setor";
        if (campo.StartsWith("centroCusto.")) return "Centro de Custo";
        if (campo.StartsWith("unidade.")) return "Unidade";
        if (campo.StartsWith("turno.")) return "Turno";
        if (new[] { "codigoEmpresa", "tipoContratacao", "regimeTrabalho" }.Contains(campo)) return "Identificação";
        return "Funcionário";
    }

    private async Task ProcessarArquivoAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(CaminhoArquivo)) return;
            var cabecalho = await _fileReader.LerCabecalhoAsync(CaminhoArquivo, LinhaHeader);
            Colunas.Clear();
            ColunasDisponiveis.Clear();
            int index = 0;
            foreach (var col in cabecalho)
            {
                var coluna = new ColunaPlanilha { Nome = col, Indice = index++ };
                Colunas.Add(coluna);
                ColunasDisponiveis.Add(coluna);
            }
            DescricaoColunas = $"{cabecalho.Count} colunas identificadas.";
            ColunasIdentificadas = true;
        }
        catch (Exception ex)
        {
            StatusMensagem = $"Erro ao processar arquivo: {ex.Message}";
        }
    }

    private async Task SalvarLayoutAsync()
    {
        try
        {
            var layout = new LayoutModel
            {
                Nome = NomeLayout,
                Descricao = DescricaoLayout,
                LinhaHeader = LinhaHeader,
                NomeArquivoOrigem = NomeArquivo,
                RegrasNegocio = new RegrasNegocioModel
                {
                    CriarFuncionario = CriarFuncionario,
                    CriarCargo = CriarCargo,
                    CriarSetor = CriarSetor,
                    CriarCentroCusto = CriarCentroCusto,
                    CriarUnidade = CriarUnidade,
                    CriarTurno = CriarTurno,
                    AtualizarFuncionario = AtualizarFuncionario,
                    AtualizarCargo = AtualizarCargo,
                    AtualizarSetor = AtualizarSetor,
                    AtualizarCentroCusto = AtualizarCentroCusto,
                    AtualizarUnidade = AtualizarUnidade,
                    AtualizarTurno = AtualizarTurno,
                    CriarHistorico = CriarHistorico,
                    DestravarBloqueado = DestravarBloqueado,
                    NaoImportarSemHierarquia = NaoImportarSemHierarquia
                },
                Mapeamentos = CamposMapeamento.Select(m => new MapeamentoCampoModel
                {
                    CampoAPI = m.NomeCampo,
                    ColunaPlanilha = m.ColunaSelecionada?.Nome ?? string.Empty,
                    IndiceColuna = m.ColunaSelecionada?.Indice ?? -1,
                    ValorPadrao = m.ValorPadrao,
                    Obrigatorio = m.Obrigatorio,
                    TipoDado = m.Tipo
                }).ToList()
            };

            var ok = await _layoutService.SalvarLayoutAsync(layout);
            StatusMensagem = ok ? "Layout salvo com sucesso" : "Erro ao salvar layout";
            if (ok)
            {
                LayoutSalvo?.Invoke(layout);
            }
        }
        catch (Exception ex)
        {
            StatusMensagem = $"Erro ao salvar layout: {ex.Message}";
        }
    }

    private void Limpar()
    {
        CaminhoArquivo = string.Empty;
        ArquivoSelecionado = false;
        NomeArquivo = string.Empty;
        TamanhoArquivo = string.Empty;
        ColunasIdentificadas = false;
        DescricaoColunas = string.Empty;
        Colunas.Clear();
        ColunasDisponiveis.Clear();
        foreach (var campo in CamposMapeamento)
        {
            campo.ColunaSelecionada = null;
            campo.ValorPadrao = string.Empty;
        }
        NomeLayout = string.Empty;
        DescricaoLayout = string.Empty;
        StatusMensagem = string.Empty;
    }

    // Modelos auxiliares
    public class ColunaPlanilha
    {
        public string Nome { get; set; } = string.Empty;
        public int Indice { get; set; }
    }

    public class CampoMapeamentoVM : ViewModelBase
    {
        public string NomeCampo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Obrigatorio { get; set; }
        public ColunaPlanilha? ColunaSelecionada { get; set; }
        public string? ValorPadrao { get; set; }
    }

    private record ApiField(string Nome, string Tipo, bool Obrigatorio);
}

