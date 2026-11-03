// TapBoard — painel de botões para monitores touch.
// Só usa o que já vem no Windows. Depois de editar, rode build.cmd.
// Compilador do .NET Framework: limitado ao C# 5.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
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
        { "Enter", (byte)Keys.Enter },
        { "Backspace", (byte)Keys.Back },
        { "Ins", (byte)Keys.Insert },
        { "PgUp", (byte)Keys.PageUp },
        { "PgDn", (byte)Keys.PageDown },
        { "PrtSc", (byte)Keys.PrintScreen },
        { "Cima", (byte)Keys.Up },
        { "Baixo", (byte)Keys.Down },
        { "Esquerda", (byte)Keys.Left },
        { "Direita", (byte)Keys.Right },
    };

    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_UNICODE = 0x4;

    [StructLayout(LayoutKind.Sequential)]
    struct EntradaDeTecla
    {
        public ushort Vk;
        public ushort Scan;
        public uint Flags;
        public uint Tempo;
        public IntPtr Extra;
    }

    // O INPUT do Windows é uma union; as folgas completam o tamanho do maior membro.
    [StructLayout(LayoutKind.Sequential)]
    struct Entrada
    {
        public uint Tipo;
        public EntradaDeTecla Tecla;
        uint folga1;
        uint folga2;
    }

    [DllImport("user32.dll")]
    static extern uint SendInput(uint quantidade, Entrada[] entradas, int tamanho);

    [DllImport("user32.dll")]
    static extern short VkKeyScan(char letra);

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

    // Várias combinações separadas por espaço viram uma sequência, como "Ctrl+K Ctrl+S".
    public static List<byte[]> InterpretarSequencia(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var sequencia = new List<byte[]>();
        string junto = Regex.Replace(texto.Trim(), @"\s*\+\s*", "+");
        foreach (string combinacao in Regex.Split(junto, @"\s+"))
        {
            byte[] teclas = Interpretar(combinacao);
            if (teclas == null)
                return null;
            sequencia.Add(teclas);
        }
        return sequencia;
    }

    public static void EnviarSequencia(List<byte[]> sequencia)
    {
        for (int i = 0; i < sequencia.Count; i++)
        {
            if (i > 0)
                Thread.Sleep(30);
            Enviar(sequencia[i]);
        }
    }

    // Digita o texto letra por letra, como se viesse do teclado, em qualquer layout.
    public static void Digitar(string texto)
    {
        var entradas = new List<Entrada>();
        foreach (char letra in texto.Replace("\r", ""))
        {
            bool enter = letra == '\n';
            foreach (bool soltar in new[] { false, true })
            {
                var entrada = new Entrada { Tipo = INPUT_KEYBOARD };
                entrada.Tecla.Vk = enter ? (ushort)Keys.Enter : (ushort)0;
                entrada.Tecla.Scan = enter ? (ushort)0 : letra;
                entrada.Tecla.Flags = (enter ? 0 : KEYEVENTF_UNICODE) | (soltar ? KEYEVENTF_KEYUP : 0);
                entradas.Add(entrada);
            }
        }
        if (entradas.Count > 0)
            SendInput((uint)entradas.Count, entradas.ToArray(), Marshal.SizeOf(typeof(Entrada)));
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

        // Pontuação, como o "." de Win+. (emojis), na tecla do layout atual.
        if (nome.Length == 1)
        {
            short codigo = VkKeyScan(nome[0]);
            if (codigo != -1 && (codigo >> 8) == 0)
            {
                vk = (byte)codigo;
                return true;
            }
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
                Audio.AtualizarEmSeguida();
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
                List<byte[]> sequencia = Teclado.InterpretarSequencia(botao.Parametro);
                if (sequencia == null)
                    throw new ArgumentException("atalho inválido.");
                Teclado.EnviarSequencia(sequencia);
                break;
            case TipoAcao.Digitar:
                Teclado.Digitar(botao.Parametro ?? "");
                break;
            case TipoAcao.Comando:
                Comando.Rodar(botao.Parametro);
                break;
            case TipoAcao.MicrofoneMudo:
                Audio.AlternarMicrofone();
                Painel.Redesenhar();
                break;
            case TipoAcao.Abrir:
                string alvo = Environment.ExpandEnvironmentVariables(botao.Parametro);
                IntPtr aberta = JanelaDoPrograma.Procurar(alvo);
                if (aberta != IntPtr.Zero)
                    JanelaDoPrograma.TrazerParaFrente(aberta);
                else
                    Process.Start(alvo);
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

// Roda uma linha de comando no cmd, sem janela. Programas que têm janela própria
// (wt, code, obs) aparecem normalmente. Se o comando falhar, avisa na bandeja.
static class Comando
{
    public static void Rodar(string linha)
    {
        var inicio = new ProcessStartInfo("cmd.exe", "/d /s /c \"" + linha + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };

        var processo = new Process { StartInfo = inicio, EnableRaisingEvents = true };
        string ultimaLinha = null;
        DataReceivedEventHandler guardar = (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                ultimaLinha = e.Data.Trim();
        };
        processo.OutputDataReceived += guardar;
        processo.ErrorDataReceived += guardar;
        // Exited, e não WaitForExit: um programa aberto pelo comando pode herdar a saída
        // e mantê-la aberta depois que o cmd termina.
        processo.Exited += (s, e) =>
        {
            int codigo = processo.ExitCode;
            processo.Dispose();
            if (codigo != 0)
                Painel.Avisar("O comando terminou com erro " + codigo
                    + (ultimaLinha != null ? ": " + ultimaLinha : "."));
        };
        processo.Start();
        processo.BeginOutputReadLine();
        processo.BeginErrorReadLine();
    }
}

// Mudo do microfone e do som pelo Core Audio, o mesmo controle do ícone de som do
// Windows. O do microfone vale para qualquer app de reunião.
static class Audio
{
    const int Saida = 0;
    const int Captura = 1;
    const int PapelConsole = 0;
    const int PapelComunicacao = 2;
    const int CLSCTX_ALL = 0x17;

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class Enumerador
    {
    }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints();
        void GetDefaultAudioEndpoint(int fluxo, int papel, out IMMDevice dispositivo);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        void Activate(ref Guid interfaceId, int contexto, IntPtr parametros, [MarshalAs(UnmanagedType.IUnknown)] out object objeto);
    }

    // Só os dois últimos métodos são usados; os anteriores ocupam o lugar na tabela.
    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify();
        void UnregisterControlChangeNotify();
        void GetChannelCount();
        void SetMasterVolumeLevel();
        void SetMasterVolumeLevelScalar();
        void GetMasterVolumeLevel();
        void GetMasterVolumeLevelScalar();
        void SetChannelVolumeLevel();
        void SetChannelVolumeLevelScalar();
        void GetChannelVolumeLevel();
        void GetChannelVolumeLevelScalar();
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool mudo, ref Guid contexto);
        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool mudo);
    }

    // Último estado lido, para desenhar os botões sem chamar o Core Audio a cada pintura.
    public static volatile bool MicrofoneMudo;
    public static volatile bool SomMudo;

    public static void AlternarMicrofone()
    {
        IAudioEndpointVolume principal = Volume(Captura, PapelConsole);
        if (principal == null)
            throw new InvalidOperationException("nenhum microfone encontrado.");

        bool mudo;
        principal.GetMute(out mudo);
        Guid contexto = Guid.Empty;
        principal.SetMute(!mudo, ref contexto);
        // O de comunicação (Teams, Zoom) pode ser outro microfone: segue o principal.
        IAudioEndpointVolume comunicacao = Volume(Captura, PapelComunicacao);
        if (comunicacao != null)
            comunicacao.SetMute(!mudo, ref contexto);
        MicrofoneMudo = !mudo;
    }

    // Lê o estado atual. Retorna true se algo mudou desde a última leitura.
    public static bool Atualizar()
    {
        bool microfone = LerMudo(Captura);
        bool som = LerMudo(Saida);
        bool mudou = microfone != MicrofoneMudo || som != SomMudo;
        MicrofoneMudo = microfone;
        SomMudo = som;
        return mudou;
    }

    // A tecla de mudo do som é tratada pelo Windows logo depois de enviada.
    public static void AtualizarEmSeguida()
    {
        ThreadPool.QueueUserWorkItem(estado =>
        {
            Thread.Sleep(150);
            try
            {
                if (Atualizar())
                    Painel.Redesenhar();
            }
            catch (Exception)
            {
                // A leitura periódica tenta de novo.
            }
        });
    }

    static bool LerMudo(int fluxo)
    {
        IAudioEndpointVolume volume = Volume(fluxo, PapelConsole);
        bool mudo = false;
        if (volume != null)
            volume.GetMute(out mudo);
        return mudo;
    }

    static IAudioEndpointVolume Volume(int fluxo, int papel)
    {
        try
        {
            var enumerador = (IMMDeviceEnumerator)new Enumerador();
            IMMDevice dispositivo;
            enumerador.GetDefaultAudioEndpoint(fluxo, papel, out dispositivo);
            Guid id = typeof(IAudioEndpointVolume).GUID;
            object volume;
            dispositivo.Activate(ref id, CLSCTX_ALL, IntPtr.Zero, out volume);
            return (IAudioEndpointVolume)volume;
        }
        catch (COMException)
        {
            // Sem esse dispositivo ligado.
            return null;
        }
    }
}

// Se o programa de um botão Abrir já está aberto, traz a janela dele para a frente.
// Abrir de novo não basta: o VS Code, por exemplo, só tenta focar a janela que já
// existe, e o Windows barra isso porque o TapBoard não está em foco.
static class JanelaDoPrograma
{
    delegate bool AoListar(IntPtr janela, IntPtr dado);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(AoListar aoListar, IntPtr dado);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr janela, out uint processo);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr janela);

    [DllImport("user32.dll")]
    static extern int GetWindowTextLength(IntPtr janela);

    [DllImport("user32.dll")]
    static extern IntPtr GetWindow(IntPtr janela, uint relacao);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr janela, int indice);

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr janela);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr janela, int comando);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool AttachThreadInput(uint deThread, uint paraThread, bool ligar);

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr janela);

    [DllImport("user32.dll")]
    static extern bool BringWindowToTop(IntPtr janela);

    const uint GW_OWNER = 4;
    const int GWL_EXSTYLE = -20;
    const int WS_EX_TOOLWINDOW = 0x80;
    const int SW_RESTORE = 9;

    // Só .exe; o Explorer fica de fora porque abrir uma janela nova é o esperado.
    public static IntPtr Procurar(string caminho)
    {
        if (!caminho.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || !File.Exists(caminho)
            || Path.GetFileName(caminho).Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
            return IntPtr.Zero;

        var processos = new HashSet<uint>();
        foreach (Process processo in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(caminho)))
        {
            try
            {
                if (string.Equals(processo.MainModule.FileName, Path.GetFullPath(caminho), StringComparison.OrdinalIgnoreCase))
                    processos.Add((uint)processo.Id);
            }
            catch (Exception)
            {
                // Processo de outro usuário ou já encerrado.
            }
        }
        if (processos.Count == 0)
            return IntPtr.Zero;

        // EnumWindows vai da janela mais à frente para a mais atrás: fica a usada por último.
        IntPtr achada = IntPtr.Zero;
        EnumWindows((janela, dado) =>
        {
            uint processo;
            GetWindowThreadProcessId(janela, out processo);
            if (processos.Contains(processo) && IsWindowVisible(janela) && GetWindowTextLength(janela) > 0
                && GetWindow(janela, GW_OWNER) == IntPtr.Zero
                && (GetWindowLong(janela, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) == 0)
            {
                achada = janela;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return achada;
    }

    // Ligar a fila de entrada à da janela em foco libera o SetForegroundWindow.
    public static void TrazerParaFrente(IntPtr janela)
    {
        if (IsIconic(janela))
            ShowWindow(janela, SW_RESTORE);

        uint processo;
        uint threadDaFrente = GetWindowThreadProcessId(GetForegroundWindow(), out processo);
        uint minhaThread = GetCurrentThreadId();
        bool ligada = threadDaFrente != 0 && threadDaFrente != minhaThread
            && AttachThreadInput(minhaThread, threadDaFrente, true);
        try
        {
            BringWindowToTop(janela);
            SetForegroundWindow(janela);
        }
        finally
        {
            if (ligada)
                AttachThreadInput(minhaThread, threadDaFrente, false);
        }
    }
}

public enum StatusDoApp
{
    Nenhum,
    Disponivel,
    Ocupado,
    NaoIncomodar,
    Ausente,
    Offline,
}

// Contador de notificações e status dos apps, lidos do botão de cada um na barra
// de tarefas. O Teams, por exemplo, escreve ali "Nenhum item, status Disponível" e,
// com mensagens novas, o número delas. É o mesmo texto que os leitores de tela leem.
static class Contadores
{
    const int Intervalo = 3000;
    const string Pacote = @"shell:AppsFolder\";
    const string Prefixo = "Appid: ";
    static readonly Regex Numero = new Regex(@"\d+");
    static readonly Regex Status = new Regex(@"status\s+(.+?)\.?\s*$", RegexOptions.IgnoreCase);

    // Texto de cada app na barra, pela chave do botão.
    static volatile Dictionary<string, string> atuais = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    static System.Threading.Timer relogio;

    public static event Action Mudaram;

    public static void Iniciar()
    {
        relogio = new System.Threading.Timer(estado => Ler(), null, 0, Timeout.Infinite);
    }

    public static void Parar()
    {
        if (relogio != null)
            relogio.Dispose();
        relogio = null;
    }

    public static int De(Botao botao)
    {
        Match numero = Numero.Match(TextoDe(botao));
        return numero.Success ? int.Parse(numero.Value) : 0;
    }

    // Os nomes são os do Teams em português; as cores, as do próprio Teams.
    public static StatusDoApp StatusDe(Botao botao)
    {
        if (botao.Acao == TipoAcao.MicrofoneMudo)
            return Audio.MicrofoneMudo ? StatusDoApp.NaoIncomodar : StatusDoApp.Nenhum;

        Match status = Status.Match(TextoDe(botao));
        if (!status.Success)
            return StatusDoApp.Nenhum;

        string nome = status.Groups[1].Value.ToLowerInvariant();
        if (nome.Contains("disponível") || nome.Contains("disponivel"))
            return StatusDoApp.Disponivel;
        if (nome.Contains("incomodar"))
            return StatusDoApp.NaoIncomodar;
        if (nome.Contains("ocupad") || nome.Contains("chamada") || nome.Contains("reuni")
            || nome.Contains("apresent") || nome.Contains("concentr"))
            return StatusDoApp.Ocupado;
        if (nome.Contains("ausente") || nome.Contains("volto"))
            return StatusDoApp.Ausente;
        if (nome.Contains("offline"))
            return StatusDoApp.Offline;
        return StatusDoApp.Nenhum;
    }

    static string TextoDe(Botao botao)
    {
        string chave = ChaveDo(botao);
        string texto;
        return chave != null && atuais.TryGetValue(chave, out texto) ? texto : "";
    }

    // A barra identifica cada app pelo AppID do pacote ou pelo caminho do .exe.
    static string ChaveDo(Botao botao)
    {
        if (botao.Acao != TipoAcao.Abrir || string.IsNullOrWhiteSpace(botao.Parametro))
            return null;
        string alvo = Environment.ExpandEnvironmentVariables(botao.Parametro.Trim());
        return alvo.StartsWith(Pacote, StringComparison.OrdinalIgnoreCase) ? alvo.Substring(Pacote.Length) : alvo;
    }

    static void Ler()
    {
        var novos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (Configuracoes.Atual.MostrarContadores)
            {
                AutomationElement barra = AutomationElement.RootElement.FindFirst(TreeScope.Children,
                    new PropertyCondition(AutomationElement.ClassNameProperty, "Shell_TrayWnd"));
                if (barra != null)
                {
                    AutomationElementCollection botoes = barra.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ClassNameProperty, "Taskbar.TaskListButtonAutomationPeer"));
                    foreach (AutomationElement botao in botoes)
                    {
                        string id = botao.Current.AutomationId ?? "";
                        string texto = botao.Current.HelpText ?? "";
                        if (id.StartsWith(Prefixo) && texto.Length > 0)
                            novos[id.Substring(Prefixo.Length)] = texto;
                    }
                }
            }
        }
        catch (Exception)
        {
            // A barra pode estar sendo recriada (Explorer reiniciando). Tenta de novo depois.
            novos = atuais;
        }

        bool mudou = false;
        try
        {
            mudou = Audio.Atualizar();
        }
        catch (Exception)
        {
            // Dispositivo de áudio sendo trocado. Tenta de novo depois.
        }

        if (!Iguais(novos, atuais))
        {
            atuais = novos;
            mudou = true;
        }
        if (mudou)
        {
            Action aviso = Mudaram;
            if (aviso != null)
                aviso();
        }

        System.Threading.Timer proximo = relogio;
        if (proximo != null)
        {
            try
            {
                proximo.Change(Intervalo, Timeout.Infinite);
            }
            catch (ObjectDisposedException)
            {
                // O painel fechou durante a leitura.
            }
        }
    }

    static bool Iguais(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count)
            return false;
        foreach (KeyValuePair<string, string> item in a)
        {
            string valor;
            if (!b.TryGetValue(item.Key, out valor) || valor != item.Value)
                return false;
        }
        return true;
    }
}

// Ícone do TapBoard, embutido no .exe pelo build.cmd (/resource:TapBoard.ico).
static class IconeDoApp
{
    static readonly byte[] Dados = Ler();

    static byte[] Ler()
    {
        using (Stream entrada = typeof(IconeDoApp).Assembly.GetManifestResourceStream("TapBoard.ico"))
        {
            if (entrada == null)
                return null;
            var memoria = new MemoryStream();
            entrada.CopyTo(memoria);
            return memoria.ToArray();
        }
    }

    // Janelas: o Windows escolhe o tamanho certo para a barra de título e de tarefas.
    public static Icon Janela()
    {
        return Dados == null ? SystemIcons.Application : new Icon(new MemoryStream(Dados));
    }

    public static Icon Bandeja()
    {
        return Dados == null ? SystemIcons.Application : new Icon(new MemoryStream(Dados), SystemInformation.SmallIconSize);
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
    const int TENTATIVAS = 5;

    const int PAUSA_ENTRE_TENTATIVAS = 80;
    const int BaseDoDeck = 100;

    static readonly object trava = new object();
    static readonly Queue<Action> fila = new Queue<Action>();
    static bool processando;

    // Pedido de brilho que ainda espera na fila. Toques seguidos no mesmo alvo se
    // juntam nele: cada comando DDC leva centenas de ms, e descer até o limite com
    // vários -10 deixava o 100% esperando segundos atrás deles.
    class Pedido
    {
        public AlvoDoBrilho Alvo;
        public bool Relativo;
        public int Valor;
    }

    static Pedido pendente;

    // Logo depois de um salto grande o monitor fica ocupado e não responde à leitura.
    // Para um valor fixo a leitura só serve para saber o máximo, que quase nunca muda.
    static int ultimoMaximo = 100;

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
        lock (trava)
        {
            if (pendente != null && pendente.Alvo == alvo)
            {
                if (relativo)
                {
                    pendente.Valor += valor;
                }
                else
                {
                    pendente.Relativo = false;
                    pendente.Valor = valor;
                }
                return;
            }
        }

        var pedido = new Pedido { Alvo = alvo, Relativo = relativo, Valor = valor };
        Pedir(() =>
        {
            lock (trava)
            {
                if (pendente == pedido)
                    pendente = null;
            }
            if (!Aplicar(pedido.Alvo, pedido.Relativo, pedido.Valor))
                Painel.Avisar("Não consegui ajustar o brilho de todos os monitores escolhidos.");
        }, pedido);
    }

    static void Pedir(Action comando, Pedido pedido = null)
    {
        lock (trava)
        {
            fila.Enqueue(comando);
            if (pedido != null)
                pendente = pedido;
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
        if (leu && maximo > 0)
            ultimoMaximo = (int)maximo;
        else if (relativo)
            return false;
        else
            maximo = (uint)ultimoMaximo;

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

// Menus da barra nas cores do tema, com o ✓ na cor do texto.
class DesenhoDoMenu : ToolStripProfessionalRenderer
{
    public DesenhoDoMenu(Tema tema)
        : base(new CoresDoMenu(tema))
    {
        RoundedEdges = false;
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        TextRenderer.DrawText(e.Graphics, "✓", e.Item.Font, Rectangle.Inflate(e.ImageRectangle, 4, 4), e.Item.ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.ForeColor;
        base.OnRenderItemText(e);
    }
}

class CoresDoMenu : ProfessionalColorTable
{
    readonly Color fundo, destaque, borda;

    public CoresDoMenu(Tema tema)
    {
        UseSystemColors = false;
        fundo = tema.Campo;
        destaque = tema.EscuroNoSistema ? Color.FromArgb(62, 62, 74) : Color.FromArgb(214, 216, 224);
        borda = tema.EscuroNoSistema ? Color.FromArgb(70, 70, 82) : Color.FromArgb(190, 192, 200);
    }

    public override Color ToolStripDropDownBackground { get { return fundo; } }
    public override Color ImageMarginGradientBegin { get { return fundo; } }
    public override Color ImageMarginGradientMiddle { get { return fundo; } }
    public override Color ImageMarginGradientEnd { get { return fundo; } }
    public override Color MenuBorder { get { return borda; } }
    public override Color MenuItemBorder { get { return destaque; } }
    public override Color MenuItemSelected { get { return destaque; } }
    public override Color MenuItemSelectedGradientBegin { get { return destaque; } }
    public override Color MenuItemSelectedGradientEnd { get { return destaque; } }
    public override Color MenuItemPressedGradientBegin { get { return destaque; } }
    public override Color MenuItemPressedGradientEnd { get { return destaque; } }
    public override Color SeparatorDark { get { return borda; } }
    public override Color SeparatorLight { get { return fundo; } }
    public override Color CheckBackground { get { return fundo; } }
    public override Color CheckSelectedBackground { get { return destaque; } }
    public override Color CheckPressedBackground { get { return destaque; } }
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
    const int CORTE_DA_TECLA = 2;
    const int ALTURA_TECLA = 9;
    const int CAMADAS_SOMBRA = 4;
    const float PROPORCAO_ICONE = 0.48f;
    const float PROPORCAO_ICONE_AO_LADO = 0.42f;
    static readonly Color COR_CONTADOR = Color.FromArgb(0xE5, 0x48, 0x4D);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr janela, int atributo, ref int valor, int tamanho);

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    static Painel instancia;
    static JanelaConfiguracoes janelaConfiguracoes;

    readonly NotifyIcon iconeBandeja = new NotifyIcon();
    readonly Dictionary<int, int> toquesAtivos = new Dictionary<int, int>();
    Font fonte;
    // Um tamanho só para os ícones de todos os botões, calculado com a fonte.
    int ladoDoIcone;
    Tema tema = TemaEscolhido();
    bool janelaNaTela;
    readonly VeuDeBrilho veu = new VeuDeBrilho();

    public Painel()
    {
        instancia = this;

        Text = "TapBoard";
        Icon = IconeDoApp.Janela();
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
        iconeBandeja.Icon = IconeDoApp.Bandeja();
        iconeBandeja.Text = "TapBoard";
        iconeBandeja.ContextMenuStrip = menu;
        iconeBandeja.Visible = true;

        SystemEvents.DisplaySettingsChanged += AoMudarMonitores;
        Contadores.Mudaram += AoMudarContadores;
        Contadores.Iniciar();
    }

    void AoMudarContadores()
    {
        if (IsHandleCreated)
            BeginInvoke((Action)Invalidate);
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

    public static void Redesenhar()
    {
        if (instancia != null && instancia.IsHandleCreated)
            instancia.BeginInvoke((Action)instancia.Invalidate);
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
        Point ponto = PointToClient(naTela);
        toquesAtivos[id] = ponto.Y < ALTURA_DA_BARRA ? NA_BARRA : BotaoEm(ponto);
        Invalidate();
    }

    void ToqueFinalizado(int id, Point naTela, bool executar)
    {
        int botao;
        if (!toquesAtivos.TryGetValue(id, out botao))
            return;

        toquesAtivos.Remove(id);
        Invalidate();
        if (botao == NA_BARRA)
        {
            if (executar)
                TocarNaBarra(PointToClient(naTela));
            return;
        }
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

    // ===== Barra de menus =====
    // Faixa no topo, como a do Bloco de Notas: menus à esquerda e a engrenagem das
    // configurações à direita. É desenhada aqui, e não com MenuStrip, para tocar nela
    // não ativar a janela, como nos botões; só abrir um menu ativa, e ao fechar o
    // foco volta para o app que estava em uso.

    const int ALTURA_DA_BARRA = 36;
    const int NA_BARRA = -2;
    static readonly string[] MenusDaBarra = { "Arquivo", "Exibir", "Ajuda" };
    static readonly int ENGRENAGEM = MenusDaBarra.Length;
    static readonly bool TemMdl2 = Array.Exists(FontFamily.Families, f => f.Name == "Segoe MDL2 Assets");

    readonly Font fonteDaBarra = new Font("Segoe UI", 14f, GraphicsUnit.Pixel);
    readonly Font fonteDaEngrenagem = new Font(TemMdl2 ? "Segoe MDL2 Assets" : "Segoe UI Symbol", 16f, GraphicsUnit.Pixel);
    readonly Icon iconeDaBarra = IconeDoApp.Bandeja();
    int menuAberto = NENHUM;
    IntPtr focoAnterior;

    Rectangle[] ItensDaBarra()
    {
        var itens = new Rectangle[MenusDaBarra.Length + 1];
        int x = 44;
        for (int i = 0; i < MenusDaBarra.Length; i++)
        {
            int largura = TextRenderer.MeasureText(MenusDaBarra[i], fonteDaBarra).Width + 20;
            itens[i] = new Rectangle(x, 0, largura, ALTURA_DA_BARRA);
            x += largura;
        }
        itens[ENGRENAGEM] = new Rectangle(ClientSize.Width - 48, 0, 48, ALTURA_DA_BARRA);
        return itens;
    }

    void DesenharBarra(Graphics g)
    {
        using (var linha = new Pen(tema.Campo))
            g.DrawLine(linha, 0, ALTURA_DA_BARRA - 1, ClientSize.Width, ALTURA_DA_BARRA - 1);
        g.DrawIcon(iconeDaBarra, new Rectangle(14, (ALTURA_DA_BARRA - 18) / 2, 18, 18));

        Rectangle[] itens = ItensDaBarra();
        const TextFormatFlags centro = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
        for (int i = 0; i < itens.Length; i++)
        {
            if (i == menuAberto)
                using (var fundo = new SolidBrush(tema.Campo))
                    g.FillRectangle(fundo, Rectangle.Inflate(itens[i], 0, -4));
            if (i == ENGRENAGEM)
                TextRenderer.DrawText(g, TemMdl2 ? "\uE713" : "\u2699", fonteDaEngrenagem, itens[i], tema.TextoBarra, centro);
            else
                TextRenderer.DrawText(g, MenusDaBarra[i], fonteDaBarra, itens[i], tema.TextoBarra, centro);
        }
    }

    void TocarNaBarra(Point ponto)
    {
        Rectangle[] itens = ItensDaBarra();
        for (int i = 0; i < itens.Length; i++)
        {
            if (!itens[i].Contains(ponto))
                continue;
            if (i == ENGRENAGEM)
                AbrirConfiguracoes();
            else
                AbrirMenu(i);
            return;
        }
    }

    void AbrirMenu(int indice)
    {
        focoAnterior = GetForegroundWindow();
        Activate();

        ContextMenuStrip menu = MontarMenu(indice);
        menuAberto = indice;
        Invalidate();
        menu.Closed += (s, e) =>
        {
            menuAberto = NENHUM;
            Invalidate();
            BeginInvoke((Action)menu.Dispose);
            // Clicar em outro app já levou o foco para ele; Esc ou toque fora, não.
            if (e.CloseReason == ToolStripDropDownCloseReason.AppClicked
                || e.CloseReason == ToolStripDropDownCloseReason.Keyboard)
                DevolverFoco();
        };
        menu.Show(this, new Point(ItensDaBarra()[indice].Left, ALTURA_DA_BARRA));
    }

    void DevolverFoco()
    {
        if (focoAnterior != IntPtr.Zero && focoAnterior != Handle)
            JanelaDoPrograma.TrazerParaFrente(focoAnterior);
        focoAnterior = IntPtr.Zero;
    }

    ContextMenuStrip MontarMenu(int indice)
    {
        Configuracoes c = Configuracoes.Atual;
        var menu = new ContextMenuStrip
        {
            Font = new Font("Segoe UI", 15f, GraphicsUnit.Pixel),
            ForeColor = tema.TextoBarra,
            Renderer = new DesenhoDoMenu(tema),
            ShowImageMargin = false,
            ShowCheckMargin = true,
        };
        switch (indice)
        {
            case 0:
                Item(menu, "Configurações…", false, false, AbrirConfiguracoes);
                menu.Items.Add(new ToolStripSeparator());
                Item(menu, "Sair do TapBoard", false, false, Application.Exit);
                break;
            case 1:
                Item(menu, "Tela cheia", !c.AbrirEmJanela, true, AlternarJanela);
                Item(menu, "Sempre visível", c.SempreVisivel, true, () =>
                {
                    c.SempreVisivel = !c.SempreVisivel;
                    c.Salvar();
                    AplicarConfiguracoes();
                });
                menu.Items.Add(new ToolStripSeparator());
                Item(menu, "Tema igual ao Windows", c.Tema == EscolhaDeTema.Windows, true, () => EscolherTema(EscolhaDeTema.Windows));
                Item(menu, "Tema claro", c.Tema == EscolhaDeTema.Claro, true, () => EscolherTema(EscolhaDeTema.Claro));
                Item(menu, "Tema escuro", c.Tema == EscolhaDeTema.Escuro, true, () => EscolherTema(EscolhaDeTema.Escuro));
                break;
            default:
                Item(menu, "Abrir pasta das configurações", false, false, () => Process.Start("explorer.exe", Configuracoes.Pasta));
                Item(menu, "Sobre o TapBoard", false, false, () => MessageBox.Show(this,
                    "TapBoard\n\nPainel de botões para monitor touch.\nConfigurações em " + Configuracoes.Pasta,
                    "Sobre o TapBoard"));
                break;
        }
        return menu;
    }

    // Itens que não abrem janela devolvem o foco ao app que estava em uso.
    void Item(ContextMenuStrip menu, string texto, bool marcado, bool devolverFoco, Action acao)
    {
        var item = new ToolStripMenuItem(texto) { Checked = marcado, Padding = new Padding(0, 6, 0, 6) };
        item.Click += (s, e) =>
        {
            acao();
            if (devolverFoco)
                DevolverFoco();
        };
        menu.Items.Add(item);
    }

    static void EscolherTema(EscolhaDeTema escolha)
    {
        Configuracoes.Atual.Tema = escolha;
        Configuracoes.Atual.Salvar();
        AplicarConfiguracoes();
    }

    // ===== Layout e desenho =====

    Rectangle AreaDosBotoes()
    {
        Rectangle tudo = ClientRectangle;
        return Rectangle.FromLTRB(tudo.Left, tudo.Top + ALTURA_DA_BARRA, tudo.Right, tudo.Bottom);
    }

    // A grade é contada em meias casas, para caber o botão de meia casa.
    // Na grade fluida, cada casa tem pelo menos a largura mínima do botão:
    // painel mais largo, mais botões por linha. Na fixa, o número escolhido de casas.
    int ColunasDaGrade()
    {
        Configuracoes c = Configuracoes.Atual;
        if (!c.GradeFluida)
            return Math.Max(1, Math.Min(10, c.BotoesPorLinha)) * 2;
        int espaco = c.EspacoEntreBotoes;
        int casas = (AreaDosBotoes().Width - espaco) / (Math.Max(40, c.LarguraMinimaDoBotao) + espaco);
        return Math.Max(1, casas) * 2;
    }

    // "Linha inteira" ocupa a linha toda; os outros, até o que couber na linha.
    static int Ocupadas(Botao botao, int colunas)
    {
        if (botao.LarguraNaGrade() >= Padrao.Colunas)
            return colunas;
        return Math.Min(botao.MeiasCasas(), colunas);
    }

    int BotaoEm(Point posicao)
    {
        for (int i = 0; i < Configuracoes.Atual.Botoes.Count; i++)
            if (RetanguloDoBotao(i).Contains(posicao))
                return i;
        return NENHUM;
    }

    // Cada botão ocupa de meia a quatro casas; se não couber, desce para a linha seguinte.
    static Point[] PosicoesNaGrade(int colunas)
    {
        List<Botao> botoes = Configuracoes.Atual.Botoes;
        var posicoes = new Point[botoes.Count];
        int coluna = 0, linha = 0;
        for (int i = 0; i < botoes.Count; i++)
        {
            int largura = Ocupadas(botoes[i], colunas);
            if (coluna + largura > colunas)
            {
                coluna = 0;
                linha++;
            }
            posicoes[i] = new Point(coluna, linha);
            coluna += largura;
        }
        return posicoes;
    }

    // As linhas escolhidas na grade, ou mais, se os botões não couberem nelas.
    enum Metade
    {
        Nenhuma,
        Esquerda,
        Direita,
    }

    // Dois botões de meia casa seguidos, na mesma linha, formam uma tecla partida ao meio.
    Metade[] Metades(int colunas)
    {
        List<Botao> botoes = Configuracoes.Atual.Botoes;
        Point[] posicoes = PosicoesNaGrade(colunas);
        var metades = new Metade[botoes.Count];
        for (int i = 0; i + 1 < botoes.Count; i++)
        {
            if (metades[i] != Metade.Nenhuma || Ocupadas(botoes[i], colunas) != 1 || Ocupadas(botoes[i + 1], colunas) != 1)
                continue;
            if (posicoes[i + 1].Y != posicoes[i].Y || posicoes[i + 1].X != posicoes[i].X + 1)
                continue;
            metades[i] = Metade.Esquerda;
            metades[i + 1] = Metade.Direita;
        }
        return metades;
    }

    int Linhas()
    {
        Point[] posicoes = PosicoesNaGrade(ColunasDaGrade());
        int usadas = posicoes.Length == 0 ? 1 : posicoes[posicoes.Length - 1].Y + 1;
        return Math.Max(usadas, Math.Min(10, Configuracoes.Atual.LinhasDaGrade));
    }

    Rectangle RetanguloDoBotao(int indice)
    {
        Rectangle area = AreaDosBotoes();
        int colunas = ColunasDaGrade();
        int linhas = Linhas();
        int espaco = Configuracoes.Atual.EspacoEntreBotoes;
        int altura = (area.Height - espaco * (linhas + 1)) / linhas;
        Point posicao = PosicoesNaGrade(colunas)[indice];
        int ocupadas = Ocupadas(Configuracoes.Atual.Botoes[indice], colunas);

        // Metade de tecla partida: as duas dividem a casa, separadas só pelo corte.
        Metade metade = Metades(colunas)[indice];
        if (metade == Metade.Direita)
            posicao.X--;
        if (metade != Metade.Nenhuma)
            ocupadas = 2;

        // Fração por coluna, para a sobra não se acumular na borda direita.
        double coluna = (area.Width - espaco) / (double)colunas;
        int esquerda = area.X + espaco + (int)Math.Round(posicao.X * coluna);
        int direita = area.X + (int)Math.Round((posicao.X + ocupadas) * coluna);
        int meio = (esquerda + direita) / 2;
        if (metade == Metade.Esquerda)
            direita = meio - CORTE_DA_TECLA / 2;
        else if (metade == Metade.Direita)
            esquerda = meio + (CORTE_DA_TECLA + 1) / 2;
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
        // Pela altura da linha, mas sem passar do que cabe na largura de uma casa:
        // na grade fluida larga as linhas ficam altas e o texto quebraria no meio das palavras.
        Rectangle area = AreaDosBotoes();
        int casas = Math.Max(1, ColunasDaGrade() / 2);
        float pelaAltura = area.Height / Linhas() * 0.16f;
        float pelaLargura = (area.Width / (float)casas - c.EspacoEntreBotoes) * 0.07f;
        float tamanho = Math.Max(14f, Math.Min(pelaAltura, pelaLargura) * c.EscalaFonte / 100f);
        fonte = new Font(c.Fonte, tamanho, GraphicsUnit.Pixel);

        // Pela face de uma casa inteira, para botões de larguras diferentes terem o mesmo ícone.
        int linhas = Linhas();
        int espaco = c.EspacoEntreBotoes;
        int alturaDaFace = (area.Height - espaco * (linhas + 1)) / linhas - ALTURA_TECLA;
        int larguraDaCasa = (int)((area.Width - espaco) / (float)casas) - espaco;
        float proporcao = c.IconeAoLado ? PROPORCAO_ICONE_AO_LADO : PROPORCAO_ICONE;
        ladoDoIcone = Math.Max(8, Math.Min((int)(alturaDaFace * proporcao), larguraDaCasa / 6));
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

            DesenharBarra(g);

            List<Botao> botoes = Configuracoes.Atual.Botoes;
            Metade[] metades = Metades(ColunasDaGrade());
            for (int i = 0; i < botoes.Count; i++)
                DesenharBotao(g, botoes[i], RetanguloDoBotao(i), metades[i], toquesAtivos.ContainsValue(i), alinhamento);
        }
    }

    // Botões de dois estados ficam afundados quando ligados, como tecla com trava.
    static bool Travado(Botao botao)
    {
        switch (botao.Acao)
        {
            case TipoAcao.MicrofoneMudo:
                return Audio.MicrofoneMudo;
            case TipoAcao.Mudo:
                return Audio.SomMudo;
            default:
                return false;
        }
    }

    // Tecla em relevo: sombra na mesa, lateral escura e face com gradiente.
    // Pressionada, a face desce sobre a lateral; travada, desce até a metade.
    void DesenharBotao(Graphics g, Botao botao, Rectangle area, Metade metade, bool pressionado, StringFormat alinhamento)
    {
        Color cor = Misturar(botao.CorReal(), Color.White, tema.ClarearBotao);
        bool travado = !pressionado && Travado(botao);

        // Na tecla partida, a sombra não avança sobre a outra metade.
        for (int i = CAMADAS_SOMBRA; i >= 1; i--)
        {
            int antes = metade == Metade.Direita ? 0 : i;
            int depois = metade == Metade.Esquerda ? 0 : i;
            Rectangle camada = new Rectangle(area.X - antes, area.Y + i, area.Width + antes + depois, area.Height + i);
            using (GraphicsPath forma = Arredondado(camada, RAIO_BOTAO + i, metade))
            using (var sombra = new SolidBrush(Color.FromArgb(pressionado ? 8 : travado ? 14 : 22, Color.Black)))
                g.FillPath(sombra, forma);
        }

        int altura = area.Height - ALTURA_TECLA;
        Rectangle lateral = new Rectangle(area.X, area.Bottom - altura, area.Width, altura);
        using (GraphicsPath forma = Arredondado(lateral, RAIO_BOTAO, metade))
        using (var fundo = new SolidBrush(Misturar(cor, Color.Black, 0.42f)))
            g.FillPath(fundo, forma);

        int descida = pressionado ? ALTURA_TECLA - 2 : travado ? ALTURA_TECLA / 2 : 0;
        Rectangle face = new Rectangle(area.X, area.Y + descida, area.Width, altura);
        Color topo = travado ? Misturar(cor, Color.Black, 0.22f) : Misturar(cor, Color.White, pressionado ? 0.02f : 0.12f);
        Color base_ = Misturar(cor, Color.Black, pressionado ? 0.14f : travado ? 0.32f : 0.05f);
        using (GraphicsPath forma = Arredondado(face, RAIO_BOTAO, metade))
        using (var fundo = new LinearGradientBrush(face, topo, base_, LinearGradientMode.Vertical))
            g.FillPath(fundo, forma);

        Rectangle aro = Rectangle.Inflate(face, -1, -1);
        using (GraphicsPath forma = Arredondado(aro, RAIO_BOTAO - 1, metade))
        using (var brilho = new LinearGradientBrush(
            aro, Color.FromArgb(120, Color.White), Color.FromArgb(0, Color.White), LinearGradientMode.Vertical))
        using (var caneta = new Pen(brilho, 2f))
            g.DrawPath(caneta, forma);

        string icone = IconeDoEstado(botao);
        bool temIcone = !string.IsNullOrEmpty(icone);
        // Botão de meia casa com ícone mostra só o ícone; o rótulo fica para a lista.
        bool temRotulo = !string.IsNullOrWhiteSpace(botao.Rotulo) && !(temIcone && botao.MeiasCasas() == 1);
        Rectangle areaDoTexto = face;
        if (temIcone && temRotulo && Configuracoes.Atual.IconeAoLado)
        {
            // Ícone e texto formam um bloco centralizado, com o ícone à esquerda.
            int lado = ladoDoIcone;
            int folga = lado / 3;
            int margem = Math.Min(face.Height / 5, face.Width / 12);
            int maximo = Math.Max(1, face.Width - lado - folga - margem * 2);
            int largura = Math.Min(maximo, (int)Math.Ceiling(g.MeasureString(botao.Rotulo, fonte).Width));
            int x = face.X + (face.Width - lado - folga - largura) / 2;
            Icones.Desenhar(g, icone, new Rectangle(x, face.Y + (face.Height - lado) / 2, lado, lado), tema.TextoBotao);
            areaDoTexto = new Rectangle(x + lado + folga, face.Y, largura, face.Height);
        }
        else if (temIcone)
        {
            int lado = ladoDoIcone;
            int y = temRotulo ? face.Y + face.Height / 10 : face.Y + (face.Height - lado) / 2;
            Icones.Desenhar(g, icone, new Rectangle(face.X, y, face.Width, lado), tema.TextoBotao);
            areaDoTexto = new Rectangle(face.X, y + lado, face.Width, face.Bottom - y - lado);
        }

        int quantidade = Contadores.De(botao);
        if (quantidade > 0)
            DesenharContador(g, quantidade, face, cor);
        else
            DesenharStatus(g, Contadores.StatusDe(botao), face, cor);

        if (!temRotulo)
            return;

        Rectangle sombreado = areaDoTexto;
        sombreado.Offset(0, 1);
        using (var sombraTexto = new SolidBrush(tema.SombraTexto))
            g.DrawString(botao.Rotulo, fonte, sombraTexto, sombreado, alinhamento);
        using (var corTexto = new SolidBrush(tema.TextoBotao))
            g.DrawString(botao.Rotulo, fonte, corTexto, areaDoTexto, alinhamento);
    }

    // Círculo vermelho no canto da face, como o contador dos apps no celular.
    void DesenharContador(Graphics g, int quantidade, Rectangle face, Color corDoBotao)
    {
        if (quantidade <= 0)
            return;

        string texto = quantidade > 99 ? "99+" : quantidade.ToString();
        int diametro = Math.Max(22, (int)(face.Height * 0.42f));
        using (var letra = new Font(fonte.FontFamily, diametro * 0.58f, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var formato = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            int largura = Math.Max(diametro, (int)g.MeasureString(texto, letra).Width + diametro / 3);
            int margem = diametro / 4;
            var bolha = new Rectangle(face.Right - largura - margem, face.Y + (face.Height - diametro) / 2, largura, diametro);
            using (GraphicsPath forma = Arredondado(bolha, diametro / 2))
            using (var fundo = new SolidBrush(COR_CONTADOR))
            using (var aro = new Pen(Misturar(corDoBotao, Color.Black, 0.35f), 2f))
            {
                g.FillPath(fundo, forma);
                g.DrawPath(aro, forma);
            }
            using (var branco = new SolidBrush(Color.White))
                g.DrawString(texto, letra, branco, bolha, formato);
        }
    }

    // Com o microfone mudo, usa a versão "-off" do ícone (mdi-microphone-off.svg), se existir.
    static string IconeDoEstado(Botao botao)
    {
        if (botao.Acao != TipoAcao.MicrofoneMudo || !Audio.MicrofoneMudo || string.IsNullOrEmpty(botao.Icone))
            return botao.Icone;
        string desligado = Path.Combine(Path.GetDirectoryName(botao.Icone),
            Path.GetFileNameWithoutExtension(botao.Icone) + "-off" + Path.GetExtension(botao.Icone));
        return File.Exists(desligado) ? desligado : botao.Icone;
    }

    // Bolinha de status no canto da face, nas cores e símbolos do Teams.
    void DesenharStatus(Graphics g, StatusDoApp status, Rectangle face, Color corDoBotao)
    {
        Color cor;
        switch (status)
        {
            case StatusDoApp.Disponivel: cor = Color.FromArgb(0x6B, 0xB7, 0x00); break;
            case StatusDoApp.Ocupado:
            case StatusDoApp.NaoIncomodar: cor = Color.FromArgb(0xC4, 0x31, 0x4B); break;
            case StatusDoApp.Ausente: cor = Color.FromArgb(0xFF, 0xAA, 0x44); break;
            case StatusDoApp.Offline: cor = Color.FromArgb(0x8A, 0x88, 0x86); break;
            default: return;
        }

        int diametro = Math.Max(16, (int)(face.Height * 0.3f));
        int margem = (int)(face.Height * 0.42f * 0.25f);
        var bolinha = new Rectangle(face.Right - diametro - margem, face.Y + (face.Height - diametro) / 2, diametro, diametro);
        using (var fundo = new SolidBrush(cor))
        using (var aro = new Pen(Misturar(corDoBotao, Color.Black, 0.35f), 2f))
        {
            g.FillEllipse(fundo, bolinha);
            g.DrawEllipse(aro, bolinha);
        }

        float d = diametro;
        using (var traco = new Pen(Color.White, Math.Max(2f, d * 0.13f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
        {
            if (status == StatusDoApp.Disponivel)
                g.DrawLines(traco, new[]
                {
                    new PointF(bolinha.X + d * 0.28f, bolinha.Y + d * 0.52f),
                    new PointF(bolinha.X + d * 0.44f, bolinha.Y + d * 0.67f),
                    new PointF(bolinha.X + d * 0.72f, bolinha.Y + d * 0.36f),
                });
            else if (status == StatusDoApp.NaoIncomodar)
                g.DrawLine(traco, bolinha.X + d * 0.28f, bolinha.Y + d * 0.5f, bolinha.X + d * 0.72f, bolinha.Y + d * 0.5f);
        }
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
        return Arredondado(r, raio, Metade.Nenhuma);
    }

    // Na metade de uma tecla partida, os cantos do lado do corte são retos.
    static GraphicsPath Arredondado(Rectangle r, int raio, Metade metade)
    {
        int d = raio * 2;
        bool esquerdaReta = metade == Metade.Direita;
        bool direitaReta = metade == Metade.Esquerda;
        var forma = new GraphicsPath();
        if (esquerdaReta)
            forma.AddLine(r.Left, r.Top, r.Left, r.Top);
        else
            forma.AddArc(r.Left, r.Top, d, d, 180, 90);
        if (direitaReta)
            forma.AddLine(r.Right, r.Top, r.Right, r.Bottom);
        else
        {
            forma.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            forma.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        }
        if (esquerdaReta)
            forma.AddLine(r.Left, r.Bottom, r.Left, r.Bottom);
        else
            forma.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        forma.CloseFigure();
        return forma;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= AoMudarMonitores;
            Contadores.Mudaram -= AoMudarContadores;
            Contadores.Parar();
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
