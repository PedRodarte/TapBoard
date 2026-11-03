// Configurações do TapBoard: o que fica salvo e a janela para editar.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;

public enum TipoAcao
{
    BrilhoAjustar,
    BrilhoDefinir,
    VolumeMenos,
    VolumeMais,
    Mudo,
    MidiaAnterior,
    MidiaPlayPause,
    MidiaProxima,
    Atalho,
    Abrir,
    BloquearPC,
    AlternarJanela,
    AlternarTema,
    Configuracoes,
    Digitar,
    Comando,
    MicrofoneMudo,
}

public enum AlvoDoBrilho
{
    Todos,
    Principal,
    Deck,
}

public enum EscolhaDeTema
{
    Windows,
    Claro,
    Escuro,
}

public class Botao
{
    public string Rotulo;
    public string Cor;
    public TipoAcao Acao;
    public string Parametro;
    public string Icone = "";
    public int Largura = Padrao.PassoDaGrade;
    public AlvoDoBrilho AlvoBrilho = AlvoDoBrilho.Todos;

    public Botao()
    {
    }

    public Botao(string rotulo, string cor, TipoAcao acao, string parametro = "")
    {
        Rotulo = rotulo;
        Cor = cor;
        Acao = acao;
        Parametro = parametro;
    }

    // Como no Bootstrap: múltiplos de 3 das 12 colunas, e 1 para meia casa.
    // Outro valor no arquivo sobe para o próximo.
    public int LarguraNaGrade()
    {
        if (Largura == 1)
            return 1;
        int limitada = Math.Max(1, Math.Min(Padrao.Colunas, Largura));
        return (limitada + Padrao.PassoDaGrade - 1) / Padrao.PassoDaGrade * Padrao.PassoDaGrade;
    }

    public int MeiasCasas()
    {
        int largura = LarguraNaGrade();
        return largura == 1 ? 1 : largura / Padrao.PassoDaGrade * 2;
    }

    public Color CorReal()
    {
        try
        {
            return ColorTranslator.FromHtml(Cor);
        }
        catch (Exception)
        {
            return Color.DimGray;
        }
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Rotulo) ? "(só ícone)" : Rotulo;
    }
}

public class Configuracoes
{
    public static readonly string Pasta = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TapBoard");
    static readonly string Arquivo = Path.Combine(Pasta, "configuracoes.xml");
    static readonly XmlSerializer Serializador = new XmlSerializer(typeof(Configuracoes));

    public static Configuracoes Atual;

    public bool AbrirEmJanela = true;
    public string TelaDoDeck = "";
    public EscolhaDeTema Tema = EscolhaDeTema.Windows;
    public string Fonte = Padrao.Fonte;
    public int EscalaFonte = 100;
    public int EspacoEntreBotoes = Padrao.Espaco;
    public bool GradeFluida = false;
    public int BotoesPorLinha = 4;
    public int LinhasDaGrade = 0;
    public int LarguraMinimaDoBotao = 300;
    public bool IconeAoLado = true;
    public bool MostrarContadores = true;
    public bool SempreVisivel = true;
    public bool BrilhoDeckPorSoftware = true;
    public int BrilhoDeck = 100;
    public int JanelaX;
    public int JanelaY;
    public int JanelaLargura;
    public int JanelaAltura;
    public List<Botao> Botoes = new List<Botao>();

    static Configuracoes ValoresPadrao()
    {
        var padrao = new Configuracoes();
        padrao.Botoes = Padrao.Botoes();
        return padrao;
    }

    // Retorna false se havia um arquivo, mas ele estava inválido.
    public static bool Carregar()
    {
        Atual = ValoresPadrao();
        if (!File.Exists(Arquivo))
            return true;

        try
        {
            using (FileStream entrada = File.OpenRead(Arquivo))
                Atual = (Configuracoes)Serializador.Deserialize(entrada);
            return true;
        }
        catch (Exception)
        {
            File.Copy(Arquivo, Arquivo + ".invalido", true);
            return false;
        }
    }

    public void Salvar()
    {
        try
        {
            Directory.CreateDirectory(Pasta);
            using (var memoria = new MemoryStream())
            {
                Serializador.Serialize(memoria, this);
                File.WriteAllBytes(Arquivo, memoria.ToArray());
            }
        }
        catch (Exception erro)
        {
            Painel.Avisar("Não consegui salvar as configurações: " + erro.Message);
        }
    }

    public Configuracoes Copia()
    {
        using (var memoria = new MemoryStream())
        {
            Serializador.Serialize(memoria, this);
            memoria.Position = 0;
            return (Configuracoes)Serializador.Deserialize(memoria);
        }
    }
}

static class InicioComWindows
{
    const string Chave = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Nome = "TapBoard";

    public static bool Ativo
    {
        get
        {
            using (RegistryKey registro = Registry.CurrentUser.OpenSubKey(Chave))
                return registro != null && registro.GetValue(Nome) != null;
        }
    }

    public static void Definir(bool ativo)
    {
        using (RegistryKey registro = Registry.CurrentUser.CreateSubKey(Chave))
        {
            if (ativo)
                registro.SetValue(Nome, "\"" + Application.ExecutablePath + "\"");
            else
                registro.DeleteValue(Nome, false);
        }
    }
}

// Sem instalador, é o próprio programa que se coloca no menu Iniciar.
static class AtalhoNoIniciar
{
    public static void Atualizar()
    {
        try
        {
            string caminho = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "TapBoard.lnk");
            Type tipo = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(tipo);
            object atalho = tipo.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { caminho });
            atalho.GetType().InvokeMember("TargetPath", BindingFlags.SetProperty, null, atalho, new object[] { Application.ExecutablePath });
            // Explícito, senão o Windows pode manter o ícone antigo em cache.
            atalho.GetType().InvokeMember("IconLocation", BindingFlags.SetProperty, null, atalho, new object[] { Application.ExecutablePath + ",0" });
            atalho.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, atalho, null);
        }
        catch (Exception)
        {
            // Sem o atalho, o painel funciona do mesmo jeito.
        }
    }
}

class Opcao
{
    public readonly object Valor;
    public readonly string Texto;
    public readonly string TextoDoParametro;

    public Opcao(object valor, string texto, string textoDoParametro = null)
    {
        Valor = valor;
        Texto = texto;
        TextoDoParametro = textoDoParametro;
    }

    public override string ToString()
    {
        return Texto;
    }
}

class JanelaConfiguracoes : Form
{
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    static readonly Opcao[] OpcoesDeAcao =
    {
        new Opcao(TipoAcao.BrilhoAjustar, "Brilho: aumentar ou diminuir", "Quanto (10 ou -10)"),
        new Opcao(TipoAcao.BrilhoDefinir, "Brilho: definir valor", "Percentual (0 a 100)"),
        new Opcao(TipoAcao.VolumeMenos, "Volume: diminuir"),
        new Opcao(TipoAcao.VolumeMais, "Volume: aumentar"),
        new Opcao(TipoAcao.Mudo, "Volume: mudo"),
        new Opcao(TipoAcao.MicrofoneMudo, "Microfone: mudo"),
        new Opcao(TipoAcao.MidiaAnterior, "Mídia: anterior"),
        new Opcao(TipoAcao.MidiaPlayPause, "Mídia: play / pause"),
        new Opcao(TipoAcao.MidiaProxima, "Mídia: próxima"),
        new Opcao(TipoAcao.Atalho, "Atalho de teclado", "Teclas (Ctrl+Shift+M)"),
        new Opcao(TipoAcao.Digitar, "Digitar texto", "Texto"),
        new Opcao(TipoAcao.Abrir, "Abrir programa, arquivo ou site", "Caminho ou endereço"),
        new Opcao(TipoAcao.Comando, "Rodar comando", "Linha de comando"),
        new Opcao(TipoAcao.BloquearPC, "Bloquear PC"),
        new Opcao(TipoAcao.AlternarJanela, "Alternar janela / tela cheia"),
        new Opcao(TipoAcao.AlternarTema, "Alternar tema"),
        new Opcao(TipoAcao.Configuracoes, "Abrir configurações"),
    };

    static readonly Opcao[] OpcoesDeLargura =
    {
        new Opcao(1, "Meia casa"),
        new Opcao(3, "1 casa"),
        new Opcao(6, "2 casas"),
        new Opcao(9, "3 casas"),
        new Opcao(12, "Linha inteira"),
    };

    static readonly Opcao[] OpcoesDeMonitor =
    {
        new Opcao(AlvoDoBrilho.Todos, "Todos os monitores"),
        new Opcao(AlvoDoBrilho.Principal, "Só o monitor principal"),
        new Opcao(AlvoDoBrilho.Deck, "Só o monitor do deck"),
    };

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr janela, int atributo, ref int valor, int tamanho);

    readonly Configuracoes copia = Configuracoes.Atual.Copia();
    readonly Tema tema;

    readonly ComboBox comboTela = Combo("comboTela");
    readonly ComboBox comboModo = Combo("comboModo");
    readonly ComboBox comboTema = Combo("comboTema");
    readonly ComboBox comboIcone = Combo("comboIcone");
    readonly ComboBox comboColunas = new ComboBox { Name = "comboColunas", AccessibleName = "Colunas da grade", DropDownStyle = ComboBoxStyle.DropDownList, Width = 62 };
    readonly ComboBox comboLinhas = new ComboBox { Name = "comboLinhas", AccessibleName = "Linhas da grade", DropDownStyle = ComboBoxStyle.DropDownList, Width = 62 };
    readonly ComboBox comboAcao = Combo("comboAcao");
    readonly ComboBox comboFonte = Combo("comboFonte");
    readonly ComboBox comboLargura = Combo("comboLargura");
    readonly ComboBox comboMonitor = Combo("comboMonitor");
    readonly NumericUpDown campoEscala = new NumericUpDown { Name = "campoEscala", Minimum = 50, Maximum = 200, Increment = 10, Width = 80 };
    readonly NumericUpDown campoEspaco = new NumericUpDown { Name = "campoEspaco", Minimum = 0, Maximum = 80, Increment = 4, Width = 80 };
    readonly NumericUpDown campoLarguraMinima = new NumericUpDown { Name = "campoLarguraMinima", Minimum = 120, Maximum = 800, Increment = 20, Width = 80 };
    readonly CheckBox caixaInicio = new CheckBox { Name = "caixaInicio", Text = "Iniciar com o Windows", AutoSize = true };
    readonly CheckBox caixaSempreVisivel = new CheckBox { Name = "caixaSempreVisivel", Text = "Sempre visível sobre as outras janelas", AutoSize = true };
    readonly CheckBox caixaContadores = new CheckBox { Name = "caixaContadores", Text = "Mostrar notificações e status dos apps", AutoSize = true };
    readonly CheckBox caixaBrilhoSoftware = new CheckBox { Name = "caixaBrilhoSoftware", Text = "Escurecer o deck por software", AutoSize = true };
    readonly ListBox lista = new ListBox { Name = "lista", Dock = DockStyle.Fill, IntegralHeight = false };
    readonly TextBox campoRotulo = new TextBox { Name = "campoRotulo", AccessibleName = "Rotulo do botao", Dock = DockStyle.Fill };
    readonly TextBox campoParametro = new TextBox { Name = "campoParametro", AccessibleName = "Parametro do botao", Dock = DockStyle.Fill };
    readonly Label legendaParametro = Legenda("Parâmetro");
    readonly Label legendaMonitor = Legenda("Monitor");
    readonly Button botaoCor = new Button { Name = "botaoCor", Text = "Escolher cor", AutoSize = true };
    readonly Button botaoProcurar = new Button { Name = "botaoProcurar", Text = "Procurar…", AutoSize = true };
    readonly Panel previaIcone = new Panel { Name = "previaIcone", Size = new Size(36, 36) };
    bool carregando;

    public JanelaConfiguracoes(Tema tema)
    {
        this.tema = tema;

        Text = "Configurações do TapBoard";
        Icon = IconeDoApp.Janela();
        Font = new Font("Segoe UI", 10f);
        StartPosition = FormStartPosition.Manual;
        Size = new Size(960, 680);
        MinimumSize = new Size(820, 560);
        MinimizeBox = false;
        MaximizeBox = false;
        TopMost = true;

        // Abre no monitor principal, onde estão o teclado e o foco.
        Rectangle util = Screen.PrimaryScreen.WorkingArea;
        Location = new Point(util.X + (util.Width - Width) / 2, util.Y + (util.Height - Height) / 2);

        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(10) };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.Controls.Add(MontarGeral(), 0, 0);
        raiz.Controls.Add(MontarBotoes(), 1, 0);
        var rodape = MontarRodape();
        raiz.Controls.Add(rodape, 0, 1);
        raiz.SetColumnSpan(rodape, 2);
        Controls.Add(raiz);

        CarregarGeral();
        AtualizarLista(0);
        Pintar(this, tema);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int escuro = tema.EscuroNoSistema ? 1 : 0;
        DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref escuro, sizeof(int));
    }

    static ComboBox Combo(string nome)
    {
        return new ComboBox { Name = nome, DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    }

    static Label Legenda(string texto)
    {
        return new Label { Text = texto, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 8, 3) };
    }

    static Button Acao(string nome, string texto, EventHandler aoClicar)
    {
        var botao = new Button { Name = nome, Text = texto, AutoSize = true };
        botao.Click += aoClicar;
        return botao;
    }

    Control MontarGeral()
    {
        var grade = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Colunas x linhas, como a grade de teclas de um deck físico.
        var tamanhoDaGrade = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        tamanhoDaGrade.Controls.Add(comboColunas);
        tamanhoDaGrade.Controls.Add(new Label { Text = "×", AutoSize = true, Margin = new Padding(4, 7, 4, 0) });
        tamanhoDaGrade.Controls.Add(comboLinhas);
        grade.Controls.Add(Legenda("Grade (colunas × linhas)"), 0, 0);
        grade.Controls.Add(tamanhoDaGrade, 1, 0);
        grade.Controls.Add(Legenda("Largura mínima (px)"), 0, 1);
        grade.Controls.Add(campoLarguraMinima, 1, 1);
        grade.Controls.Add(Legenda("Monitor do deck"), 0, 2);
        grade.Controls.Add(comboTela, 1, 2);
        grade.Controls.Add(Legenda("Ao abrir"), 0, 3);
        grade.Controls.Add(comboModo, 1, 3);
        grade.Controls.Add(Legenda("Tema"), 0, 4);
        grade.Controls.Add(comboTema, 1, 4);
        grade.Controls.Add(Legenda("Fonte"), 0, 5);
        grade.Controls.Add(comboFonte, 1, 5);
        grade.Controls.Add(Legenda("Tamanho do texto (%)"), 0, 6);
        grade.Controls.Add(campoEscala, 1, 6);
        grade.Controls.Add(Legenda("Espaço entre botões (px)"), 0, 7);
        grade.Controls.Add(campoEspaco, 1, 7);
        grade.Controls.Add(Legenda("Ícone com texto"), 0, 8);
        grade.Controls.Add(comboIcone, 1, 8);
        // As caixas ocupam as duas colunas para o texto não ser cortado.
        CheckBox[] caixas = { caixaInicio, caixaSempreVisivel, caixaBrilhoSoftware, caixaContadores };
        for (int i = 0; i < caixas.Length; i++)
        {
            grade.Controls.Add(caixas[i], 0, 9 + i);
            grade.SetColumnSpan(caixas[i], 2);
        }

        var grupo = new GroupBox { Text = "Geral", Dock = DockStyle.Fill };
        grupo.Controls.Add(grade);
        return grupo;
    }

    Control MontarBotoes()
    {
        var acoes = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        acoes.Controls.Add(Acao("botaoAdicionar", "Escolher…", (s, e) => EscolherDoCatalogo()));
        acoes.Controls.Add(Acao("botaoRemover", "Remover", (s, e) => Remover()));
        acoes.Controls.Add(Acao("botaoSubir", "Subir", (s, e) => Mover(-1)));
        acoes.Controls.Add(Acao("botaoDescer", "Descer", (s, e) => Mover(1)));

        var editor = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 3 };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editor.Controls.Add(Legenda("Rótulo"), 0, 0);
        editor.Controls.Add(campoRotulo, 1, 0);
        editor.SetColumnSpan(campoRotulo, 2);
        editor.Controls.Add(Legenda("Ação"), 0, 1);
        editor.Controls.Add(comboAcao, 1, 1);
        editor.SetColumnSpan(comboAcao, 2);
        editor.Controls.Add(legendaParametro, 0, 2);
        editor.Controls.Add(campoParametro, 1, 2);
        editor.Controls.Add(botaoProcurar, 2, 2);
        editor.Controls.Add(legendaMonitor, 0, 3);
        editor.Controls.Add(comboMonitor, 1, 3);
        editor.SetColumnSpan(comboMonitor, 2);
        editor.Controls.Add(Legenda("Cor"), 0, 4);
        editor.Controls.Add(botaoCor, 1, 4);

        var acoesDoIcone = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
        acoesDoIcone.Controls.Add(previaIcone);
        acoesDoIcone.Controls.Add(Acao("botaoBuscarIcone", "Buscar ícone…", (s, e) => BuscarIcone()));
        acoesDoIcone.Controls.Add(Acao("botaoArquivoIcone", "Usar arquivo…", (s, e) => EscolherArquivoDeIcone()));
        acoesDoIcone.Controls.Add(Acao("botaoSemIcone", "Sem ícone", (s, e) => DefinirIcone("")));
        editor.Controls.Add(Legenda("Ícone"), 0, 5);
        editor.Controls.Add(acoesDoIcone, 1, 5);
        editor.SetColumnSpan(acoesDoIcone, 2);
        previaIcone.Paint += (s, e) => DesenharPrevia(e.Graphics);
        editor.Controls.Add(Legenda("Largura"), 0, 6);
        editor.Controls.Add(comboLargura, 1, 6);
        editor.SetColumnSpan(comboLargura, 2);

        var grade = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.Controls.Add(lista, 0, 0);
        grade.Controls.Add(acoes, 1, 0);
        grade.Controls.Add(editor, 0, 1);
        grade.SetColumnSpan(editor, 2);

        comboAcao.Items.AddRange(OpcoesDeAcao);
        comboLargura.Items.AddRange(OpcoesDeLargura);
        comboMonitor.Items.AddRange(OpcoesDeMonitor);
        lista.SelectedIndexChanged += (s, e) => MostrarSelecionado();
        campoRotulo.TextChanged += (s, e) => RotuloAlterado();
        campoParametro.TextChanged += (s, e) => ParametroAlterado();
        comboAcao.SelectedIndexChanged += (s, e) => AcaoAlterada();
        comboLargura.SelectedIndexChanged += (s, e) => LarguraAlterada();
        comboMonitor.SelectedIndexChanged += (s, e) => MonitorAlterado();
        botaoCor.Click += (s, e) => EscolherCor();
        botaoProcurar.Click += (s, e) => Procurar();

        var grupo = new GroupBox { Text = "Botões", Dock = DockStyle.Fill };
        grupo.Controls.Add(grade);
        return grupo;
    }

    Control MontarRodape()
    {
        var rodape = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true };
        rodape.Controls.Add(Acao("botaoSalvar", "Salvar", (s, e) => Salvar()));
        rodape.Controls.Add(Acao("botaoCancelar", "Cancelar", (s, e) => Close()));
        return rodape;
    }

    public static void Pintar(Control controle, Tema tema)
    {
        bool campo = controle is ListBox || controle is TextBox || controle is ComboBox
            || controle is NumericUpDown || controle is Button;
        controle.BackColor = campo ? tema.Campo : tema.Fundo;
        controle.ForeColor = tema.TextoBarra;

        var botao = controle as Button;
        if (botao != null)
            botao.FlatStyle = FlatStyle.Flat;
        var combo = controle as ComboBox;
        if (combo != null)
            combo.FlatStyle = FlatStyle.Flat;

        foreach (Control filho in controle.Controls)
            Pintar(filho, tema);
    }

    void DesenharPrevia(Graphics g)
    {
        Botao botao = Selecionado();
        if (botao != null)
            Icones.Desenhar(g, botao.Icone, previaIcone.ClientRectangle, tema.TextoBarra);
    }

    void DefinirIcone(string caminho)
    {
        Botao botao = Selecionado();
        if (botao == null)
            return;
        botao.Icone = caminho;
        previaIcone.Invalidate();
    }

    void BuscarIcone()
    {
        if (Selecionado() == null)
            return;

        using (var janela = new JanelaIcones(tema))
            if (janela.ShowDialog(this) == DialogResult.OK)
                DefinirIcone(janela.Escolhido);
    }

    void EscolherArquivoDeIcone()
    {
        if (Selecionado() == null)
            return;

        using (var dialogo = new OpenFileDialog
        {
            Title = "Escolher ícone",
            Filter = "Imagens (*.svg;*.png;*.ico;*.jpg;*.jpeg;*.bmp)|*.svg;*.png;*.ico;*.jpg;*.jpeg;*.bmp",
        })
        {
            if (dialogo.ShowDialog(this) == DialogResult.OK)
                DefinirIcone(dialogo.FileName);
        }
    }

    void CarregarGeral()
    {
        comboTela.Items.Add(new Opcao("", "Automático"));
        foreach (Screen tela in Screen.AllScreens)
        {
            string nome = tela.DeviceName.Replace(@"\\.\", "");
            string texto = string.Format("{0}: {1}x{2}{3}", nome, tela.Bounds.Width, tela.Bounds.Height,
                tela.Primary ? " (principal)" : "");
            comboTela.Items.Add(new Opcao(tela.DeviceName, texto));
        }
        Selecionar(comboTela, copia.TelaDoDeck);

        comboModo.Items.Add(new Opcao(true, "Em janela"));
        comboModo.Items.Add(new Opcao(false, "Em tela cheia"));
        Selecionar(comboModo, copia.AbrirEmJanela);

        comboTema.Items.Add(new Opcao(EscolhaDeTema.Windows, "Igual ao Windows"));
        comboTema.Items.Add(new Opcao(EscolhaDeTema.Claro, "Claro"));
        comboTema.Items.Add(new Opcao(EscolhaDeTema.Escuro, "Escuro"));
        Selecionar(comboTema, copia.Tema);

        comboIcone.Items.Add(new Opcao(true, "À esquerda do texto"));
        comboIcone.Items.Add(new Opcao(false, "Acima do texto"));
        Selecionar(comboIcone, copia.IconeAoLado);

        // Auto (0): nas colunas, a grade fluida; nas linhas, só as que os botões precisam.
        foreach (ComboBox combo in new[] { comboColunas, comboLinhas })
        {
            combo.Items.Add(new Opcao(0, "Auto"));
            for (int n = 1; n <= 10; n++)
                combo.Items.Add(new Opcao(n, n.ToString()));
        }
        Selecionar(comboColunas, copia.GradeFluida ? 0 : Math.Max(1, Math.Min(10, copia.BotoesPorLinha)));
        Selecionar(comboLinhas, Math.Max(0, Math.Min(10, copia.LinhasDaGrade)));
        comboColunas.SelectedIndexChanged += (s, e) => campoLarguraMinima.Enabled = (int)Valor(comboColunas) == 0;
        campoLarguraMinima.Enabled = copia.GradeFluida;

        foreach (FontFamily familia in FontFamily.Families)
            comboFonte.Items.Add(new Opcao(familia.Name, familia.Name));
        Selecionar(comboFonte, copia.Fonte);

        campoEscala.Value = Math.Max(50, Math.Min(200, copia.EscalaFonte));
        campoEspaco.Value = Math.Max(0, Math.Min(80, copia.EspacoEntreBotoes));
        campoLarguraMinima.Value = Math.Max(120, Math.Min(800, copia.LarguraMinimaDoBotao));
        caixaInicio.Checked = InicioComWindows.Ativo;
        caixaSempreVisivel.Checked = copia.SempreVisivel;
        caixaBrilhoSoftware.Checked = copia.BrilhoDeckPorSoftware;
        caixaContadores.Checked = copia.MostrarContadores;
    }

    static void Selecionar(ComboBox combo, object valor)
    {
        combo.SelectedIndex = 0;
        foreach (Opcao opcao in combo.Items)
            if (opcao.Valor.Equals(valor))
                combo.SelectedItem = opcao;
    }

    static object Valor(ComboBox combo)
    {
        return ((Opcao)combo.SelectedItem).Valor;
    }

    Botao Selecionado()
    {
        return lista.SelectedItem as Botao;
    }

    void AtualizarLista(int indice)
    {
        carregando = true;
        lista.Items.Clear();
        lista.Items.AddRange(copia.Botoes.ToArray());
        carregando = false;

        if (copia.Botoes.Count == 0)
            MostrarSelecionado();
        else
            lista.SelectedIndex = Math.Max(0, Math.Min(copia.Botoes.Count - 1, indice));
    }

    void MostrarSelecionado()
    {
        if (carregando)
            return;

        Botao botao = Selecionado();
        bool haBotao = botao != null;
        campoRotulo.Enabled = comboAcao.Enabled = campoParametro.Enabled = botaoCor.Enabled = haBotao;
        comboLargura.Enabled = haBotao;

        carregando = true;
        campoRotulo.Text = haBotao ? botao.Rotulo : "";
        campoParametro.Text = haBotao ? botao.Parametro : "";
        if (haBotao)
        {
            Selecionar(comboAcao, botao.Acao);
            Selecionar(comboLargura, botao.LarguraNaGrade());
            Selecionar(comboMonitor, botao.AlvoBrilho);
        }
        carregando = false;

        previaIcone.Invalidate();
        AjustarParametro();
    }

    void AjustarParametro()
    {
        var opcao = comboAcao.SelectedItem as Opcao;
        bool usaParametro = Selecionado() != null && opcao != null && opcao.TextoDoParametro != null;
        legendaParametro.Visible = campoParametro.Visible = usaParametro;
        legendaParametro.Text = usaParametro ? opcao.TextoDoParametro : "";
        botaoProcurar.Visible = usaParametro && TipoAcao.Abrir.Equals(opcao.Valor);

        bool usaMonitor = usaParametro
            && (TipoAcao.BrilhoAjustar.Equals(opcao.Valor) || TipoAcao.BrilhoDefinir.Equals(opcao.Valor));
        legendaMonitor.Visible = comboMonitor.Visible = usaMonitor;
    }

    void RotuloAlterado()
    {
        Botao botao = Selecionado();
        if (carregando || botao == null)
            return;

        botao.Rotulo = campoRotulo.Text;
        carregando = true;
        lista.Items[lista.SelectedIndex] = botao;
        carregando = false;
    }

    void LarguraAlterada()
    {
        Botao botao = Selecionado();
        if (!carregando && botao != null)
            botao.Largura = (int)Valor(comboLargura);
    }

    void MonitorAlterado()
    {
        Botao botao = Selecionado();
        if (!carregando && botao != null)
            botao.AlvoBrilho = (AlvoDoBrilho)Valor(comboMonitor);
    }

    void ParametroAlterado()
    {
        Botao botao = Selecionado();
        if (!carregando && botao != null)
            botao.Parametro = campoParametro.Text;
    }

    void AcaoAlterada()
    {
        Botao botao = Selecionado();
        if (carregando || botao == null)
            return;

        botao.Acao = (TipoAcao)Valor(comboAcao);
        botao.Parametro = "";
        carregando = true;
        campoParametro.Text = "";
        carregando = false;
        AjustarParametro();
    }

    void EscolherCor()
    {
        Botao botao = Selecionado();
        if (botao == null)
            return;

        using (var dialogo = new ColorDialog { Color = botao.CorReal(), FullOpen = true })
        {
            if (dialogo.ShowDialog(this) != DialogResult.OK)
                return;
            Color cor = dialogo.Color;
            botao.Cor = string.Format("#{0:X2}{1:X2}{2:X2}", cor.R, cor.G, cor.B);
        }
    }

    void Procurar()
    {
        using (var dialogo = new OpenFileDialog { Title = "Escolher programa ou arquivo" })
            if (dialogo.ShowDialog(this) == DialogResult.OK)
                campoParametro.Text = dialogo.FileName;
    }

    // Os novos entram no fim da lista; o primeiro fica selecionado para ajustar.
    public void EscolherDoCatalogo()
    {
        using (var janela = new JanelaCatalogo(tema, copia.Botoes))
        {
            if (janela.ShowDialog(this) != DialogResult.OK)
                return;
            AtualizarLista(janela.ParaEditar >= 0 ? janela.ParaEditar : lista.SelectedIndex);
            if (janela.ParaEditar >= 0)
            {
                campoParametro.Focus();
                campoParametro.SelectAll();
            }
        }
    }

    public void Editar(int indice)
    {
        AtualizarLista(indice);
        campoParametro.Focus();
        campoParametro.SelectAll();
    }

    void Remover()
    {
        int indice = lista.SelectedIndex;
        if (indice < 0)
            return;
        copia.Botoes.RemoveAt(indice);
        AtualizarLista(indice);
    }

    void Mover(int direcao)
    {
        int indice = lista.SelectedIndex;
        int destino = indice + direcao;
        if (indice < 0 || destino < 0 || destino >= copia.Botoes.Count)
            return;

        Botao botao = copia.Botoes[indice];
        copia.Botoes.RemoveAt(indice);
        copia.Botoes.Insert(destino, botao);
        AtualizarLista(destino);
    }

    // Retorna o problema encontrado, ou null se o botão estiver certo.
    static string Problema(Botao botao)
    {
        if (string.IsNullOrWhiteSpace(botao.Rotulo) && string.IsNullOrEmpty(botao.Icone))
            return "O botão precisa de um rótulo ou de um ícone.";

        int numero;
        switch (botao.Acao)
        {
            case TipoAcao.BrilhoAjustar:
                if (!int.TryParse(botao.Parametro, out numero) || numero < -100 || numero > 100)
                    return "Informe quanto ajustar, de -100 a 100.";
                break;
            case TipoAcao.BrilhoDefinir:
                if (!int.TryParse(botao.Parametro, out numero) || numero < 0 || numero > 100)
                    return "Informe um percentual de 0 a 100.";
                break;
            case TipoAcao.Atalho:
                if (Teclado.InterpretarSequencia(botao.Parametro) == null)
                    return "Atalho inválido. Use algo como Ctrl+Shift+M, ou Ctrl+K Ctrl+S para uma sequência.";
                break;
            case TipoAcao.Digitar:
                if (string.IsNullOrEmpty(botao.Parametro))
                    return "Informe o texto a digitar.";
                break;
            case TipoAcao.Comando:
                if (string.IsNullOrWhiteSpace(botao.Parametro))
                    return "Informe o comando.";
                break;
            case TipoAcao.Abrir:
                if (string.IsNullOrWhiteSpace(botao.Parametro))
                    return "Informe o programa, arquivo ou site.";
                break;
        }
        return null;
    }

    void Salvar()
    {
        for (int i = 0; i < copia.Botoes.Count; i++)
        {
            string problema = Problema(copia.Botoes[i]);
            if (problema == null)
                continue;
            lista.SelectedIndex = i;
            MessageBox.Show(this, copia.Botoes[i] + ": " + problema, Text);
            return;
        }

        copia.TelaDoDeck = (string)Valor(comboTela);
        copia.AbrirEmJanela = (bool)Valor(comboModo);
        copia.Tema = (EscolhaDeTema)Valor(comboTema);
        copia.IconeAoLado = (bool)Valor(comboIcone);
        copia.Fonte = (string)Valor(comboFonte);
        copia.EscalaFonte = (int)campoEscala.Value;
        copia.EspacoEntreBotoes = (int)campoEspaco.Value;
        copia.LinhasDaGrade = (int)Valor(comboLinhas);
        int porLinha = (int)Valor(comboColunas);
        copia.GradeFluida = porLinha == 0;
        if (porLinha > 0)
            copia.BotoesPorLinha = porLinha;
        copia.LarguraMinimaDoBotao = (int)campoLarguraMinima.Value;
        copia.SempreVisivel = caixaSempreVisivel.Checked;
        copia.BrilhoDeckPorSoftware = caixaBrilhoSoftware.Checked;
        copia.MostrarContadores = caixaContadores.Checked;
        copia.BrilhoDeck = Configuracoes.Atual.BrilhoDeck;

        // O painel pode ter sido movido com esta janela aberta.
        Configuracoes atual = Configuracoes.Atual;
        copia.JanelaX = atual.JanelaX;
        copia.JanelaY = atual.JanelaY;
        copia.JanelaLargura = atual.JanelaLargura;
        copia.JanelaAltura = atual.JanelaAltura;

        InicioComWindows.Definir(caixaInicio.Checked);
        Configuracoes.Atual = copia;
        Painel.AplicarConfiguracoes();
        copia.Salvar();
        Close();
    }
}
