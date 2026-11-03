// TapBoard — painel de botões para monitores touch.
// Só usa o que já vem no Windows. Depois de editar, rode build.cmd.
// Compilador do .NET Framework: limitado ao C# 5.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

// ===== VALORES PADRÃO: usados até o usuário salvar as configurações =====
static class Padrao
{
    public const int Espaco = 24;
    public const int Colunas = 12;
    public const int PassoDaGrade = 3;
    public const int BrilhoMinimoDoDeck = 10;
    public const double GamaDoDeck = 2.2;
    public static readonly Size TamanhoJanela = new Size(960, 540);

    const string CaminhoVSCode = @"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe";

    public const string Fonte = "Segoe UI Semibold";

    const string Brilho = "#C27A34";
    const string Audio = "#3F7A80";
    const string Midia = "#8A5578";
    const string Sistema = "#6E8B47";
    const string Perigo = "#B4493B";
    const string Painel_ = "#6A6058";

    public static List<Botao> Botoes()
    {
        return new List<Botao>
        {
            new Botao("Brilho −10", Brilho, TipoAcao.BrilhoAjustar, "-10"),
            new Botao("Brilho +10", Brilho, TipoAcao.BrilhoAjustar, "10"),
            new Botao("Brilho 15%", Brilho, TipoAcao.BrilhoDefinir, "15"),
            new Botao("Brilho 50%", Brilho, TipoAcao.BrilhoDefinir, "50"),

            new Botao("Brilho 100%", Brilho, TipoAcao.BrilhoDefinir, "100"),
            new Botao("Volume −", Audio, TipoAcao.VolumeMenos),
            new Botao("Volume +", Audio, TipoAcao.VolumeMais),
            new Botao("Mudo", Audio, TipoAcao.Mudo),

            new Botao("Anterior", Midia, TipoAcao.MidiaAnterior),
            new Botao("Play / Pause", Midia, TipoAcao.MidiaPlayPause) { Largura = 6 },
            new Botao("Próxima", Midia, TipoAcao.MidiaProxima),

            new Botao("Recorte de tela", Sistema, TipoAcao.Atalho, "Win+Shift+S"),
            new Botao("Explorer", Sistema, TipoAcao.Abrir, "explorer.exe"),
            new Botao("VS Code", Sistema, TipoAcao.Abrir, CaminhoVSCode),
            new Botao("Tarefas", Sistema, TipoAcao.Abrir, "taskmgr.exe"),

            new Botao("Bloquear PC", Perigo, TipoAcao.BloquearPC),
            new Botao("Janela / Tela cheia", Painel_, TipoAcao.AlternarJanela),
            new Botao("Tema claro / escuro", Painel_, TipoAcao.AlternarTema),
            new Botao("Configurações", Painel_, TipoAcao.Configuracoes),
        };
    }
}

class Tema
{
    public Color Fundo, Campo, TextoBarra, TextoBotao, SombraTexto;
    public float ClarearBotao;
    public bool EscuroNoSistema;

    public static readonly Tema Escuro = new Tema
    {
        Fundo = Color.FromArgb(18, 18, 22),
        Campo = Color.FromArgb(38, 38, 46),
        TextoBarra = Color.Gainsboro,
        TextoBotao = Color.White,
        SombraTexto = Color.FromArgb(110, 0, 0, 0),
        ClarearBotao = 0f,
        EscuroNoSistema = true,
    };

    public static readonly Tema Claro = new Tema
    {
        Fundo = Color.FromArgb(232, 233, 238),
        Campo = Color.White,
        TextoBarra = Color.FromArgb(45, 45, 55),
        TextoBotao = Color.White,
        SombraTexto = Color.FromArgb(110, 0, 0, 0),
        ClarearBotao = 0.08f,
        EscuroNoSistema = false,
    };
}
// ===== FIM DOS VALORES PADRÃO =====

static class Vk
{
    public const byte Shift = 0x10;
    public const byte Ctrl = 0x11;
    public const byte Alt = 0x12;
    public const byte Esc = 0x1B;
    public const byte Espaco = 0x20;
    public const byte Delete = 0x2E;
    public const byte PageUp = 0x21;
    public const byte Windows = 0x5B;
    public const byte Mudo = 0xAD;
    public const byte VolumeMenos = 0xAE;
    public const byte VolumeMais = 0xAF;
    public const byte MidiaProxima = 0xB0;
    public const byte MidiaAnterior = 0xB1;
    public const byte MidiaPlayPause = 0xB3;
}

static class Teclado
{
    const uint KEYEVENTF_EXTENDEDKEY = 0x1;
    const uint KEYEVENTF_KEYUP = 0x2;

    static readonly Dictionary<string, byte> Nomes = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase)
    {
        { "Ctrl", Vk.Ctrl },
        { "Shift", Vk.Shift },
        { "Alt", Vk.Alt },
        { "Win", Vk.Windows },
        { "Esc", Vk.Esc },
        { "Espaco", Vk.Espaco },
        { "Espaço", Vk.Espaco },
        { "Del", Vk.Delete },
    };

    [DllImport("user32.dll")]
    static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    // As teclas vão para o app em foco, porque o painel nunca é ativado.
    public static void Enviar(params byte[] teclas)
    {
        foreach (byte vk in teclas)
            keybd_event(vk, 0, Flags(vk), UIntPtr.Zero);
        for (int i = teclas.Length - 1; i >= 0; i--)
            keybd_event(teclas[i], 0, Flags(teclas[i]) | KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    static uint Flags(byte vk)
    {
        bool estendida = vk == Vk.Windows
            || (vk >= Vk.PageUp && vk <= Vk.Delete)
            || (vk >= Vk.Mudo && vk <= Vk.MidiaPlayPause);
        return estendida ? KEYEVENTF_EXTENDEDKEY : 0;
    }

    // Converte um texto como "Ctrl+Shift+S". Retorna null se for inválido.
    public static byte[] Interpretar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var teclas = new List<byte>();
        foreach (string parte in texto.Split('+'))
        {
            byte vk;
            if (!Converter(parte.Trim(), out vk))
                return null;
            teclas.Add(vk);
        }
        return teclas.ToArray();
    }

    static bool Converter(string nome, out byte vk)
    {
        if (Nomes.TryGetValue(nome, out vk))
            return true;

        if (nome.Length == 1 && char.IsLetterOrDigit(nome[0]) && nome[0] < 128)
        {
            vk = (byte)char.ToUpperInvariant(nome[0]);
            return true;
        }

        Keys tecla;
        if (nome.Length > 0 && char.IsLetter(nome[0]) && Enum.TryParse(nome, true, out tecla)
            && Enum.IsDefined(typeof(Keys), tecla) && (int)tecla < 256)
        {
            vk = (byte)tecla;
            return true;
        }
        return false;
    }
}

static class Acoes
{
    [DllImport("user32.dll")]
    static extern bool LockWorkStation();

    public static void Executar(Botao botao)
    {
        switch (botao.Acao)
        {
            case TipoAcao.BrilhoAjustar:
                Brilho.Enfileirar(botao.AlvoBrilho, true, int.Parse(botao.Parametro));
                break;
            case TipoAcao.BrilhoDefinir:
                Brilho.Enfileirar(botao.AlvoBrilho, false, int.Parse(botao.Parametro));
                break;
            case TipoAcao.VolumeMenos:
                Teclado.Enviar(Vk.VolumeMenos);
                break;
            case TipoAcao.VolumeMais:
                Teclado.Enviar(Vk.VolumeMais);
                break;
            case TipoAcao.Mudo:
                Teclado.Enviar(Vk.Mudo);
                break;
            case TipoAcao.MidiaAnterior:
                Teclado.Enviar(Vk.MidiaAnterior);
                break;
            case TipoAcao.MidiaPlayPause:
                Teclado.Enviar(Vk.MidiaPlayPause);
                break;
            case TipoAcao.MidiaProxima:
                Teclado.Enviar(Vk.MidiaProxima);
                break;
            case TipoAcao.Atalho:
                byte[] teclas = Teclado.Interpretar(botao.Parametro);
                if (teclas == null)
                    throw new ArgumentException("atalho inválido.");
                Teclado.Enviar(teclas);
                break;
            case TipoAcao.Abrir:
                Process.Start(Environment.ExpandEnvironmentVariables(botao.Parametro));
                break;
            case TipoAcao.BloquearPC:
                LockWorkStation();
                break;
            case TipoAcao.AlternarJanela:
                Painel.AlternarJanela();
                break;
            case TipoAcao.AlternarTema:
                Painel.AlternarTema();
                break;
            case TipoAcao.Configuracoes:
                Painel.AbrirConfiguracoes();
                break;
        }
    }
}

// Brilho do monitor principal por DDC/CI, direto pela API do Windows.
// Película preta sobre o deck: o brilho por DDC/CI do Antuvi não é linear.
// Deixa passar toque e clique e nunca recebe foco.
class VeuDeBrilho : Form
{
    const int WS_EX_TOPMOST = 0x00000008;
    const int WS_EX_TRANSPARENT = 0x00000020;
    const int WS_EX_TOOLWINDOW = 0x00000080;
    const int WS_EX_LAYERED = 0x00080000;
    const int WS_EX_NOACTIVATE = 0x08000000;
    const uint SWP_NOSIZE = 0x0001, SWP_NOMOVE = 0x0002, SWP_NOACTIVATE = 0x0010;
    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr janela, IntPtr depoisDe, int x, int y, int largura, int altura, uint flags);

    public VeuDeBrilho()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parametros = base.CreateParams;
            parametros.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOPMOST | WS_EX_TOOLWINDOW;
            return parametros;
        }
    }

    public void Aplicar(Rectangle area, int percentual)
    {
        Bounds = area;
        // A luz da tela cai com a potência do sinal: sem compensar, 50% viraria cerca de 22%.
        Opacity = 1 - Math.Pow(percentual / 100.0, 1 / Padrao.GamaDoDeck);
        if (!Visible)
            Show();
        // Acima do painel, que também escurece junto com o resto do deck.
        SetWindowPos(Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
    }
}

static class Brilho
{
    const byte VCP_BRILHO = 0x10;
    const uint MONITOR_DEFAULTTONEAREST = 2;
    const int TENTATIVAS = 3;

    const int PAUSA_ENTRE_TENTATIVAS = 50;
    const int BaseDoDeck = 100;

    static readonly object trava = new object();
    static readonly Queue<Action> fila = new Queue<Action>();
    static bool processando;

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PHYSICAL_MONITOR
    {
        public IntPtr Handle;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Descricao;
    }

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromPoint(POINT ponto, uint flags);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr monitor, out uint quantidade);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr monitor, uint quantidade, [Out] PHYSICAL_MONITOR[] monitores);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool DestroyPhysicalMonitors(uint quantidade, PHYSICAL_MONITOR[] monitores);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr monitor, byte codigo, IntPtr tipo, out uint atual, out uint maximo);

    [DllImport("dxva2.dll", SetLastError = true)]
    static extern bool SetVCPFeature(IntPtr monitor, byte codigo, uint valor);

    // O DDC/CI é lento: roda fora da interface, um comando por vez.
    public static void Enfileirar(AlvoDoBrilho alvo, bool relativo, int valor)
    {
        Pedir(() =>
        {
            if (!Aplicar(alvo, relativo, valor))
                Painel.Avisar("Não consegui ajustar o brilho de todos os monitores escolhidos.");
        });
    }

    static void Pedir(Action comando)
    {
        lock (trava)
        {
            fila.Enqueue(comando);
            if (processando)
                return;
            processando = true;
        }
        ThreadPool.QueueUserWorkItem(_ => ProcessarFila());
    }

    // Um único trabalhador mantém os toques na ordem em que aconteceram.
    static void ProcessarFila()
    {
        while (true)
        {
            Action comando;
            lock (trava)
            {
                if (fila.Count == 0)
                {
                    processando = false;
                    return;
                }
                comando = fila.Dequeue();
            }
            comando();
        }
    }

    static List<Screen> Telas(AlvoDoBrilho alvo)
    {
        if (alvo == AlvoDoBrilho.Todos)
            return new List<Screen>(Screen.AllScreens);

        Screen tela = alvo == AlvoDoBrilho.Deck ? Painel.EscolherTela() : Screen.PrimaryScreen;
        return tela == null ? new List<Screen>() : new List<Screen> { tela };
    }

    // Um monitor que falhe não impede o ajuste dos outros.
    static bool Aplicar(AlvoDoBrilho alvo, bool relativo, int valor)
    {
        List<Screen> telas = Telas(alvo);
        bool sucesso = telas.Count > 0;
        foreach (Screen tela in telas)
            sucesso &= AplicarNaTela(tela, relativo, valor);
        return sucesso;
    }

    static bool AplicarNaTela(Screen tela, bool relativo, int valor)
    {
        if (Painel.DeckPorSoftware(tela))
        {
            Painel.AjustarVeu(relativo, valor);
            return AplicarPorDdc(tela, false, BaseDoDeck);
        }
        return AplicarPorDdc(tela, relativo, valor);
    }

    // A película escurece a partir do brilho do próprio monitor, então a base precisa ser fixa.
    public static void AncorarDeck()
    {
        Pedir(() =>
        {
            Screen deck = Painel.EscolherTela();
            if (deck != null && Painel.DeckPorSoftware(deck))
                AplicarPorDdc(deck, false, BaseDoDeck);
        });
    }

    static bool AplicarPorDdc(Screen tela, bool relativo, int valor)
    {
        var centro = new POINT
        {
            X = tela.Bounds.X + tela.Bounds.Width / 2,
            Y = tela.Bounds.Y + tela.Bounds.Height / 2,
        };
        IntPtr monitor = MonitorFromPoint(centro, MONITOR_DEFAULTTONEAREST);

        uint quantidade;
        if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out quantidade) || quantidade == 0)
            return false;

        var fisicos = new PHYSICAL_MONITOR[quantidade];
        if (!GetPhysicalMonitorsFromHMONITOR(monitor, quantidade, fisicos))
            return false;

        try
        {
            bool sucesso = true;
            foreach (PHYSICAL_MONITOR fisico in fisicos)
                sucesso &= Ajustar(fisico.Handle, relativo, valor);
            return sucesso;
        }
        finally
        {
            DestroyPhysicalMonitors(quantidade, fisicos);
        }
    }

    static bool Ajustar(IntPtr monitor, bool relativo, int valor)
    {
        uint atual = 0, maximo = 0;
        bool leu = false;
        for (int i = 0; i < TENTATIVAS && !leu; i++)
        {
            leu = GetVCPFeatureAndVCPFeatureReply(monitor, VCP_BRILHO, IntPtr.Zero, out atual, out maximo);
            if (!leu)
                Thread.Sleep(PAUSA_ENTRE_TENTATIVAS);
        }
        if (!leu)
            return false;

        // O alvo é calculado uma vez só: repetir a leitura somaria o passo de novo.
        int passo = valor * (int)maximo / 100;
        int alvo = relativo ? (int)atual + passo : passo;
        alvo = Math.Max(0, Math.Min((int)maximo, alvo));

        for (int i = 0; i < TENTATIVAS; i++)
        {
            if (SetVCPFeature(monitor, VCP_BRILHO, (uint)alvo))
                return true;
            Thread.Sleep(PAUSA_ENTRE_TENTATIVAS);
        }
        return false;
    }
}

class Painel : Form
{
    const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    const int HTCLIENT = 1;

    const int WM_MOUSEACTIVATE = 0x0021;
    const int WM_POINTERUPDATE = 0x0245;
    const int WM_POINTERDOWN = 0x0246;
    const int WM_POINTERUP = 0x0247;
    const int WM_POINTERACTIVATE = 0x024B;
    const int WM_POINTERCAPTURECHANGED = 0x024C;

    const int MA_NOACTIVATE = 3;
    const int PA_NOACTIVATE = 3;
    const int POINTER_FLAG_CANCELADO = 0x8000;

    const int NENHUM = -1;
    const int ID_MOUSE = -1;
    const int RAIO_BOTAO = 18;
    const int ALTURA_TECLA = 9;
    const int CAMADAS_SOMBRA = 4;
    const float PROPORCAO_ICONE = 0.48f;
    const float PROPORCAO_SO_ICONE = 0.62f;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr janela, int atributo, ref int valor, int tamanho);

    static Painel instancia;
    static JanelaConfiguracoes janelaConfiguracoes;

    readonly NotifyIcon iconeBandeja = new NotifyIcon();
    readonly Dictionary<int, int> toquesAtivos = new Dictionary<int, int>();
    Font fonte;
    Tema tema = TemaEscolhido();
    bool janelaNaTela;
    readonly VeuDeBrilho veu = new VeuDeBrilho();

    public Painel()
    {
        instancia = this;

        Text = "TapBoard";
        StartPosition = FormStartPosition.Manual;
        BackColor = tema.Fundo;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        MinimumSize = new Size(480, 270);
        SetStyle(ControlStyles.Selectable, false);

        var menu = new ContextMenuStrip();
        menu.Items.Add("Configurações", null, (s, e) => AbrirConfiguracoes());
        menu.Items.Add("Janela / Tela cheia", null, (s, e) => AlternarJanela());
        menu.Items.Add("Tema claro / escuro", null, (s, e) => AlternarTema());
        menu.Items.Add("Sair do TapBoard", null, (s, e) => Application.Exit());
        iconeBandeja.Icon = SystemIcons.Application;
        iconeBandeja.Text = "TapBoard";
        iconeBandeja.ContextMenuStrip = menu;
        iconeBandeja.Visible = true;

        SystemEvents.DisplaySettingsChanged += AoMudarMonitores;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        PintarBarraDoWindows();
    }

    void PintarBarraDoWindows()
    {
        int escuro = tema.EscuroNoSistema ? 1 : 0;
        DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref escuro, sizeof(int));
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);
        GuardarJanela();
        Configuracoes.Atual.Salvar();
    }

    // Ao ativar, o painel sobe e ficaria acima da película do deck.
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        AtualizarVeu();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        GuardarJanela();
        Configuracoes.Atual.Salvar();
        base.OnFormClosing(e);
    }

    // A janela é a única do app: fechá-la encerra o processo.
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        Application.Exit();
    }

    public static bool DeckPorSoftware(Screen tela)
    {
        Screen deck = EscolherTela();
        return Configuracoes.Atual.BrilhoDeckPorSoftware && !tela.Primary
            && deck != null && deck.DeviceName == tela.DeviceName;
    }

    public static void AjustarVeu(bool relativo, int valor)
    {
        instancia.BeginInvoke((Action)(() =>
        {
            Configuracoes c = Configuracoes.Atual;
            int alvo = relativo ? c.BrilhoDeck + valor : valor;
            c.BrilhoDeck = Math.Max(Padrao.BrilhoMinimoDoDeck, Math.Min(100, alvo));
            instancia.AtualizarVeu();
            c.Salvar();
        }));
    }

    void AtualizarVeu()
    {
        Screen deck = EscolherTela();
        Configuracoes c = Configuracoes.Atual;
        if (deck != null && DeckPorSoftware(deck) && c.BrilhoDeck < 100)
            veu.Aplicar(deck.Bounds, c.BrilhoDeck);
        else
            veu.Hide();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        AtualizarVeu();
        Brilho.AncorarDeck();
    }

    public static void Avisar(string mensagem)
    {
        instancia.BeginInvoke((Action)(() =>
            instancia.iconeBandeja.ShowBalloonTip(4000, "TapBoard", mensagem, ToolTipIcon.Warning)));
    }

    static Tema TemaEscolhido()
    {
        switch (Configuracoes.Atual.Tema)
        {
            case EscolhaDeTema.Claro:
                return Tema.Claro;
            case EscolhaDeTema.Escuro:
                return Tema.Escuro;
        }

        object valor = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme",
            null);
        return valor is int && (int)valor == 0 ? Tema.Escuro : Tema.Claro;
    }

    public static void AlternarTema()
    {
        Configuracoes.Atual.Tema = instancia.tema == Tema.Escuro ? EscolhaDeTema.Claro : EscolhaDeTema.Escuro;
        Configuracoes.Atual.Salvar();
        AplicarConfiguracoes();
    }

    public static void AlternarJanela()
    {
        Configuracoes.Atual.AbrirEmJanela = !Configuracoes.Atual.AbrirEmJanela;
        AplicarConfiguracoes();
        Configuracoes.Atual.Salvar();
    }

    public static void AplicarConfiguracoes()
    {
        instancia.tema = TemaEscolhido();
        instancia.BackColor = instancia.tema.Fundo;
        instancia.PintarBarraDoWindows();
        instancia.toquesAtivos.Clear();
        instancia.Posicionar();
        instancia.AtualizarVeu();
        Brilho.AncorarDeck();
    }

    // Esta janela recebe foco de propósito: é onde se digita.
    public static void AbrirConfiguracoes()
    {
        if (janelaConfiguracoes != null)
        {
            janelaConfiguracoes.Activate();
            return;
        }

        janelaConfiguracoes = new JanelaConfiguracoes(instancia.tema);
        janelaConfiguracoes.FormClosed += (s, e) => janelaConfiguracoes = null;
        janelaConfiguracoes.Show();
        janelaConfiguracoes.Activate();
    }

    public void Posicionar()
    {
        GuardarJanela();
        TopMost = Configuracoes.Atual.SempreVisivel;

        // Sem o monitor do deck, usa janela para não cobrir o monitor principal.
        Screen tela = EscolherTela();
        if (Configuracoes.Atual.AbrirEmJanela || tela == null)
            MostrarEmJanela(tela ?? Screen.PrimaryScreen);
        else
            MostrarEmTelaCheia(tela);

        Show();
        AjustarFonte();
        Invalidate();
    }

    void GuardarJanela()
    {
        if (!janelaNaTela)
            return;

        // Maximizada ou minimizada, vale o tamanho que ela tem ao restaurar.
        Rectangle area = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        Configuracoes c = Configuracoes.Atual;
        c.JanelaX = area.X;
        c.JanelaY = area.Y;
        c.JanelaLargura = area.Width;
        c.JanelaAltura = area.Height;
    }

    void MostrarEmTelaCheia(Screen tela)
    {
        janelaNaTela = false;
        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.None;
        Bounds = tela.Bounds;
    }

    void MostrarEmJanela(Screen tela)
    {
        janelaNaTela = true;
        FormBorderStyle = FormBorderStyle.Sizable;
        if (WindowState != FormWindowState.Normal)
            return;

        Configuracoes c = Configuracoes.Atual;
        var area = new Rectangle(c.JanelaX, c.JanelaY, c.JanelaLargura, c.JanelaAltura);
        bool visivel = c.JanelaLargura > 0
            && Screen.FromRectangle(area).Bounds.IntersectsWith(area);
        if (!visivel)
        {
            Rectangle util = tela.WorkingArea;
            Size tamanho = Padrao.TamanhoJanela;
            area = new Rectangle(
                util.X + (util.Width - tamanho.Width) / 2,
                util.Y + (util.Height - tamanho.Height) / 2,
                tamanho.Width,
                tamanho.Height);
        }
        Bounds = area;
    }

    public static Screen EscolherTela()
    {
        string escolhida = Configuracoes.Atual.TelaDoDeck;
        foreach (Screen tela in Screen.AllScreens)
        {
            bool serve = string.IsNullOrEmpty(escolhida)
                ? !tela.Primary
                : tela.DeviceName.Equals(escolhida, StringComparison.OrdinalIgnoreCase);
            if (serve)
                return tela;
        }
        return null;
    }

    void AoMudarMonitores(object sender, EventArgs e)
    {
        Posicionar();
        AtualizarVeu();
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            // Tocar nos botões não ativa a janela: as teclas seguem para o app em foco.
            // Barra e bordas ativam, como em qualquer janela.
            case WM_MOUSEACTIVATE:
                if (BitsBaixos(m.LParam) == HTCLIENT)
                {
                    m.Result = (IntPtr)MA_NOACTIVATE;
                    return;
                }
                break;
            case WM_POINTERACTIVATE:
                if (BitsAltos(m.WParam) == HTCLIENT)
                {
                    m.Result = (IntPtr)PA_NOACTIVATE;
                    return;
                }
                break;
            case WM_POINTERDOWN:
                ToqueIniciado(IdDoToque(m), PosicaoDoToque(m));
                m.Result = IntPtr.Zero;
                return;
            case WM_POINTERUPDATE:
                m.Result = IntPtr.Zero;
                return;
            case WM_POINTERUP:
                bool cancelado = (BitsAltos(m.WParam) & POINTER_FLAG_CANCELADO) != 0;
                ToqueFinalizado(IdDoToque(m), PosicaoDoToque(m), !cancelado);
                m.Result = IntPtr.Zero;
                return;
            case WM_POINTERCAPTURECHANGED:
                ToqueFinalizado(IdDoToque(m), Point.Empty, false);
                m.Result = IntPtr.Zero;
                return;
        }
        base.WndProc(ref m);
    }

    static int BitsBaixos(IntPtr valor)
    {
        return (int)(valor.ToInt64() & 0xFFFF);
    }

    static int BitsAltos(IntPtr valor)
    {
        return (int)((valor.ToInt64() >> 16) & 0xFFFF);
    }

    static int IdDoToque(Message m)
    {
        return BitsBaixos(m.WParam);
    }

    static Point PosicaoDoToque(Message m)
    {
        // short: em monitores à esquerda do principal, a coordenada é negativa.
        return new Point((short)BitsBaixos(m.LParam), (short)BitsAltos(m.LParam));
    }

    void ToqueIniciado(int id, Point naTela)
    {
        toquesAtivos[id] = BotaoEm(PointToClient(naTela));
        Invalidate();
    }

    void ToqueFinalizado(int id, Point naTela, bool executar)
    {
        int botao;
        if (!toquesAtivos.TryGetValue(id, out botao))
            return;

        toquesAtivos.Remove(id);
        Invalidate();
        if (executar && botao != NENHUM && botao == BotaoEm(PointToClient(naTela)))
            Executar(botao);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
            ToqueIniciado(ID_MOUSE, MousePosition);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
            ToqueFinalizado(ID_MOUSE, MousePosition, true);
    }

    static void Executar(int indice)
    {
        try
        {
            Acoes.Executar(Configuracoes.Atual.Botoes[indice]);
        }
        catch (Exception erro)
        {
            Avisar(erro.Message);
        }
    }

    // ===== Layout e desenho =====

    Rectangle AreaDosBotoes()
    {
        return ClientRectangle;
    }

    int BotaoEm(Point posicao)
    {
        for (int i = 0; i < Configuracoes.Atual.Botoes.Count; i++)
            if (RetanguloDoBotao(i).Contains(posicao))
                return i;
        return NENHUM;
    }

    // Cada botão ocupa 3, 6, 9 ou 12 colunas; se não couber, desce para a linha seguinte.
    static Point[] PosicoesNaGrade()
    {
        List<Botao> botoes = Configuracoes.Atual.Botoes;
        var posicoes = new Point[botoes.Count];
        int coluna = 0, linha = 0;
        for (int i = 0; i < botoes.Count; i++)
        {
            int largura = botoes[i].LarguraNaGrade();
            if (coluna + largura > Padrao.Colunas)
            {
                coluna = 0;
                linha++;
            }
            posicoes[i] = new Point(coluna, linha);
            coluna += largura;
        }
        return posicoes;
    }

    static int Linhas()
    {
        Point[] posicoes = PosicoesNaGrade();
        return posicoes.Length == 0 ? 1 : posicoes[posicoes.Length - 1].Y + 1;
    }

    Rectangle RetanguloDoBotao(int indice)
    {
        Rectangle area = AreaDosBotoes();
        int linhas = Linhas();
        int espaco = Configuracoes.Atual.EspacoEntreBotoes;
        int altura = (area.Height - espaco * (linhas + 1)) / linhas;
        Point posicao = PosicoesNaGrade()[indice];
        int ocupadas = Configuracoes.Atual.Botoes[indice].LarguraNaGrade();

        // Fração por coluna, para a sobra não se acumular na borda direita.
        double coluna = (area.Width - espaco) / (double)Padrao.Colunas;
        int esquerda = area.X + espaco + (int)Math.Round(posicao.X * coluna);
        int direita = area.X + (int)Math.Round((posicao.X + ocupadas) * coluna);
        return new Rectangle(
            esquerda,
            area.Y + espaco + posicao.Y * (altura + espaco),
            direita - esquerda,
            altura);
    }

    void AjustarFonte()
    {
        if (fonte != null)
            fonte.Dispose();
        Configuracoes c = Configuracoes.Atual;
        float tamanho = Math.Max(14f, AreaDosBotoes().Height / Linhas() * 0.16f * c.EscalaFonte / 100f);
        fonte = new Font(c.Fonte, tamanho, GraphicsUnit.Pixel);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        AjustarFonte();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using (var alinhamento = new StringFormat())
        {
            alinhamento.Alignment = StringAlignment.Center;
            alinhamento.LineAlignment = StringAlignment.Center;

            List<Botao> botoes = Configuracoes.Atual.Botoes;
            for (int i = 0; i < botoes.Count; i++)
                DesenharBotao(g, botoes[i], RetanguloDoBotao(i), toquesAtivos.ContainsValue(i), alinhamento);
        }
    }

    // Tecla em relevo: sombra na mesa, lateral escura e face com gradiente.
    // Pressionada, a face desce sobre a lateral.
    void DesenharBotao(Graphics g, Botao botao, Rectangle area, bool pressionado, StringFormat alinhamento)
    {
        Color cor = Misturar(botao.CorReal(), Color.White, tema.ClarearBotao);

        for (int i = CAMADAS_SOMBRA; i >= 1; i--)
        {
            Rectangle camada = new Rectangle(area.X - i, area.Y + i, area.Width + i * 2, area.Height + i);
            using (GraphicsPath forma = Arredondado(camada, RAIO_BOTAO + i))
            using (var sombra = new SolidBrush(Color.FromArgb(pressionado ? 8 : 22, Color.Black)))
                g.FillPath(sombra, forma);
        }

        int altura = area.Height - ALTURA_TECLA;
        Rectangle lateral = new Rectangle(area.X, area.Bottom - altura, area.Width, altura);
        using (GraphicsPath forma = Arredondado(lateral, RAIO_BOTAO))
        using (var fundo = new SolidBrush(Misturar(cor, Color.Black, 0.42f)))
            g.FillPath(fundo, forma);

        int descida = pressionado ? ALTURA_TECLA - 2 : 0;
        Rectangle face = new Rectangle(area.X, area.Y + descida, area.Width, altura);
        Color topo = Misturar(cor, Color.White, pressionado ? 0.02f : 0.12f);
        Color base_ = Misturar(cor, Color.Black, pressionado ? 0.14f : 0.05f);
        using (GraphicsPath forma = Arredondado(face, RAIO_BOTAO))
        using (var fundo = new LinearGradientBrush(face, topo, base_, LinearGradientMode.Vertical))
            g.FillPath(fundo, forma);

        Rectangle aro = Rectangle.Inflate(face, -1, -1);
        using (GraphicsPath forma = Arredondado(aro, RAIO_BOTAO - 1))
        using (var brilho = new LinearGradientBrush(
            aro, Color.FromArgb(120, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical))
        using (var caneta = new Pen(brilho, 2f))
            g.DrawPath(caneta, forma);

        bool temIcone = !string.IsNullOrEmpty(botao.Icone);
        bool temRotulo = !string.IsNullOrWhiteSpace(botao.Rotulo);
        Rectangle areaDoTexto = face;
        if (temIcone)
        {
            int lado = (int)(face.Height * (temRotulo ? PROPORCAO_ICONE : PROPORCAO_SO_ICONE));
            int y = temRotulo ? face.Y + face.Height / 10 : face.Y + (face.Height - lado) / 2;
            Icones.Desenhar(g, botao.Icone, new Rectangle(face.X, y, face.Width, lado), tema.TextoBotao);
            areaDoTexto = new Rectangle(face.X, y + lado, face.Width, face.Bottom - y - lado);
        }

        if (!temRotulo)
            return;

        Rectangle sombreado = areaDoTexto;
        sombreado.Offset(0, 1);
        using (var sombraTexto = new SolidBrush(tema.SombraTexto))
            g.DrawString(botao.Rotulo, fonte, sombraTexto, sombreado, alinhamento);
        using (var corTexto = new SolidBrush(tema.TextoBotao))
            g.DrawString(botao.Rotulo, fonte, corTexto, areaDoTexto, alinhamento);
    }

    static Color Misturar(Color a, Color b, float quantidade)
    {
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * quantidade),
            (int)(a.G + (b.G - a.G) * quantidade),
            (int)(a.B + (b.B - a.B) * quantidade));
    }

    static GraphicsPath Arredondado(Rectangle r, int raio)
    {
        int d = raio * 2;
        var forma = new GraphicsPath();
        forma.AddArc(r.Left, r.Top, d, d, 180, 90);
        forma.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        forma.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        forma.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        forma.CloseFigure();
        return forma;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= AoMudarMonitores;
            iconeBandeja.Dispose();
            if (fonte != null)
                fonte.Dispose();
        }
        base.Dispose(disposing);
    }
}

static class Programa
{
    // Monitores com escalas diferentes: trabalha sempre em pixels físicos.
    static readonly IntPtr DPI_POR_MONITOR_V2 = new IntPtr(-4);

    [DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(IntPtr contexto);

    [STAThread]
    static void Main()
    {
        bool primeiraInstancia;
        using (new Mutex(true, "TapBoard.InstanciaUnica", out primeiraInstancia))
        {
            if (!primeiraInstancia)
                return;

            SetProcessDpiAwarenessContext(DPI_POR_MONITOR_V2);
            Application.EnableVisualStyles();

            bool arquivoValido = Configuracoes.Carregar();
            if (InicioComWindows.Ativo)
                InicioComWindows.Definir(true);
            AtalhoNoIniciar.Atualizar();

            using (var painel = new Painel())
            {
                painel.Posicionar();
                if (!arquivoValido)
                    MessageBox.Show(
                        "O arquivo de configurações estava inválido. Guardei uma cópia dele com a extensão .invalido e voltei aos valores padrão.",
                        "TapBoard");
                Application.Run();
            }
        }
    }
}
