// Catálogo de botões prontos: quem instala o TapBoard escolhe numa lista, sem
// precisar saber nomes de teclas, caminhos de programas ou arquivos de ícone.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

class ItemDoCatalogo
{
    public string Grupo;
    public string Descricao;
    public Botao Modelo;

    public Botao Novo()
    {
        return new Botao(Modelo.Rotulo, Modelo.Cor, Modelo.Acao, Modelo.Parametro)
        {
            Icone = Modelo.Icone,
            Largura = Modelo.Largura,
        };
    }
}

static class Catalogo
{
    const int MEIA = 1;

    static string grupo;
    static List<ItemDoCatalogo> itens;

    static void Grupo(string nome)
    {
        grupo = nome;
    }

    static void Item(string rotulo, string cor, TipoAcao acao, string parametro, string icone, string descricao, int largura = Padrao.PassoDaGrade)
    {
        itens.Add(new ItemDoCatalogo
        {
            Grupo = grupo,
            Descricao = descricao,
            Modelo = new Botao(rotulo, cor, acao, parametro) { Icone = Icones.PREFIXO + "mdi-" + icone, Largura = largura },
        });
    }

    // Programas só entram se estiverem instalados.
    static void Programa(string rotulo, string parametro, string icone, bool instalado)
    {
        if (instalado)
            Item(rotulo, Padrao.Sistema, TipoAcao.Abrir, parametro, icone, "Abre o " + rotulo + ", ou traz a janela para frente se já estiver aberto.");
    }

    public static List<ItemDoCatalogo> Prontos()
    {
        itens = new List<ItemDoCatalogo>();

        Grupo("Som");
        Item("Volume −", Padrao.Audio, TipoAcao.VolumeMenos, "", "volume-minus", "Abaixa o volume. Meia casa: forma uma tecla partida com o Volume +.", MEIA);
        Item("Volume +", Padrao.Audio, TipoAcao.VolumeMais, "", "volume-plus", "Aumenta o volume.", MEIA);
        Item("Mudo", Padrao.Audio, TipoAcao.Mudo, "", "volume-off", "Liga e desliga o som do PC. Fica afundado enquanto estiver mudo.", MEIA);
        Item("Microfone", Padrao.Audio, TipoAcao.MicrofoneMudo, "", "microphone", "Silencia o microfone em qualquer programa: Teams, Zoom, Discord, Meet…", MEIA);
        Item("Ditado", Padrao.Audio, TipoAcao.Atalho, "Win+H", "microphone-message", "Digita o que você falar, no campo em que estiver o cursor.");

        Grupo("Brilho do monitor");
        Item("Brilho −10", Padrao.Brilho, TipoAcao.BrilhoAjustar, "-10", "brightness-5", "Diminui o brilho dos monitores em 10%.", MEIA);
        Item("Brilho +10", Padrao.Brilho, TipoAcao.BrilhoAjustar, "10", "brightness-7", "Aumenta o brilho dos monitores em 10%.", MEIA);
        Item("Brilho 15%", Padrao.Brilho, TipoAcao.BrilhoDefinir, "15", "brightness-4", "Brilho baixo, para a noite.");
        Item("Brilho 50%", Padrao.Brilho, TipoAcao.BrilhoDefinir, "50", "brightness-6", "Brilho médio.");
        Item("Brilho 100%", Padrao.Brilho, TipoAcao.BrilhoDefinir, "100", "white-balance-sunny", "Brilho máximo.");

        Grupo("Música e vídeo");
        Item("Anterior", Padrao.Midia, TipoAcao.MidiaAnterior, "", "skip-previous", "Volta para a faixa anterior, em qualquer player.", MEIA);
        Item("Próxima", Padrao.Midia, TipoAcao.MidiaProxima, "", "skip-next", "Pula para a próxima faixa.", MEIA);
        Item("Play / Pause", Padrao.Midia, TipoAcao.MidiaPlayPause, "", "play-pause", "Toca ou pausa o que estiver tocando: Spotify, YouTube, Netflix…");

        Grupo("Windows");
        Item("Recorte de tela", Padrao.Sistema, TipoAcao.Atalho, "Win+Shift+S", "monitor-screenshot", "Tira um print de uma parte da tela.");
        Item("Explorer", Padrao.Sistema, TipoAcao.Abrir, "explorer.exe", "folder", "Abre uma janela do Explorador de Arquivos.");
        Item("Área de trabalho", Padrao.Sistema, TipoAcao.Atalho, "Win+D", "monitor", "Minimiza tudo e mostra a área de trabalho. Toque de novo para voltar.");
        Item("Visão de tarefas", Padrao.Sistema, TipoAcao.Atalho, "Win+Tab", "view-dashboard-outline", "Mostra todas as janelas abertas para escolher uma.");
        Item("Emojis", Padrao.Sistema, TipoAcao.Atalho, "Win+.", "emoticon-outline", "Abre o painel de emojis no campo em que estiver o cursor.");
        Item("Área de transferência", Padrao.Sistema, TipoAcao.Atalho, "Win+V", "clipboard-text-clock-outline", "Mostra o histórico do que você copiou.");
        Item("Gerenciador de Tarefas", Padrao.Sistema, TipoAcao.Atalho, "Ctrl+Shift+Esc", "chart-areaspline", "Abre o Gerenciador de Tarefas.");
        Item("Configurações do Windows", Padrao.Sistema, TipoAcao.Atalho, "Win+I", "cog-outline", "Abre as Configurações do Windows.");
        Item("Calculadora", Padrao.Sistema, TipoAcao.Abrir, "calc.exe", "calculator", "Abre a Calculadora.");
        Item("Bloco de Notas", Padrao.Sistema, TipoAcao.Abrir, "notepad.exe", "note-text-outline", "Abre o Bloco de Notas.");
        if (File.Exists(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe")))
            Item("Terminal", Padrao.Sistema, TipoAcao.Abrir, "wt.exe", "console", "Abre o Terminal do Windows.");
        Item("Bloquear PC", Padrao.Perigo, TipoAcao.BloquearPC, "", "lock", "Bloqueia o computador na hora, como Win+L.");

        Grupo("Editar (no programa em uso)");
        Item("Copiar", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+C", "content-copy", "Copia o que estiver selecionado.");
        Item("Colar", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+V", "content-paste", "Cola o que foi copiado.");
        Item("Recortar", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+X", "content-cut", "Recorta o que estiver selecionado.");
        Item("Desfazer", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+Z", "undo", "Desfaz a última alteração.", MEIA);
        Item("Refazer", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+Y", "redo", "Refaz o que foi desfeito.", MEIA);
        Item("Salvar", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+S", "content-save", "Salva o arquivo aberto.");
        Item("Localizar", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+F", "magnify", "Procura um texto no programa em uso.");
        Item("Imprimir", Padrao.Edicao, TipoAcao.Atalho, "Ctrl+P", "printer", "Imprime o que estiver aberto.");
        Item("Fechar janela", Padrao.Perigo, TipoAcao.Atalho, "Alt+F4", "window-close", "Fecha a janela do programa em uso.");

        Grupo("Reuniões (com a janela da reunião na frente)");
        Item("Teams: câmera", Padrao.Reuniao, TipoAcao.Atalho, "Ctrl+Shift+O", "video", "Liga e desliga a câmera no Teams.");
        Item("Teams: levantar a mão", Padrao.Reuniao, TipoAcao.Atalho, "Ctrl+Shift+K", "hand-back-right", "Levanta ou abaixa a mão no Teams.");
        Item("Teams: sair da chamada", Padrao.Perigo, TipoAcao.Atalho, "Ctrl+Shift+H", "phone-hangup", "Sai da chamada no Teams.");
        Item("Zoom: câmera", Padrao.Reuniao, TipoAcao.Atalho, "Alt+V", "video", "Liga e desliga a câmera no Zoom.");
        Item("Meet: câmera", Padrao.Reuniao, TipoAcao.Atalho, "Ctrl+E", "video", "Liga e desliga a câmera no Google Meet.");
        Item("Discord: ensurdecer", Padrao.Reuniao, TipoAcao.Atalho, "Ctrl+Shift+D", "headphones-off", "Desliga o áudio e o microfone no Discord.");

        Grupo("Programas instalados");
        Programa("Chrome", "chrome.exe", "google-chrome", TemAppPath("chrome.exe"));
        Programa("Edge", "msedge.exe", "microsoft-edge", TemAppPath("msedge.exe"));
        Programa("Firefox", "firefox.exe", "firefox", TemAppPath("firefox.exe"));
        Programa("Outlook", "outlook.exe", "microsoft-outlook", TemAppPath("outlook.exe"));
        Programa("Word", "winword.exe", "microsoft-word", TemAppPath("winword.exe"));
        Programa("Excel", "excel.exe", "microsoft-excel", TemAppPath("excel.exe"));
        Programa("PowerPoint", "powerpnt.exe", "microsoft-powerpoint", TemAppPath("powerpnt.exe"));
        Programa("Teams", "ms-teams:", "microsoft-teams", TemProtocolo("ms-teams"));
        string vsCode = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe");
        Programa("VS Code", @"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe", "microsoft-visual-studio-code", File.Exists(vsCode));
        Programa("Spotify", "spotify:", "spotify", TemProtocolo("spotify"));
        Programa("Discord", "discord:", "discord", TemProtocolo("discord"));
        Programa("WhatsApp", "whatsapp:", "whatsapp", TemProtocolo("whatsapp"));
        Programa("Steam", "steam://open/main", "steam", TemProtocolo("steam"));

        return itens;
    }

    public static List<ItemDoCatalogo> Personalizados()
    {
        itens = new List<ItemDoCatalogo>();

        Grupo("Teclado");
        Item("Atalho", Padrao.Sistema, TipoAcao.Atalho, "Ctrl+Shift+N", "keyboard-outline",
            "Aperta uma combinação de teclas no programa em uso. Ex.: Ctrl+Shift+N.");
        Item("Sequência de teclas", Padrao.Sistema, TipoAcao.Atalho, "Ctrl+K Ctrl+C", "keyboard-outline",
            "Várias teclas, uma depois da outra, separadas por espaço. Ex.: Ctrl+K Ctrl+C.");
        Item("Digitar texto", Padrao.Edicao, TipoAcao.Digitar, "Bom dia!", "form-textbox",
            "Escreve um texto pronto: assinatura de e-mail, endereço, resposta padrão.");

        Grupo("Abrir");
        Item("Programa ou arquivo", Padrao.Sistema, TipoAcao.Abrir, "", "application-outline",
            "Abre um programa, arquivo ou pasta. Escolha qual em Procurar… depois de adicionar.");
        Item("Site", Padrao.Sistema, TipoAcao.Abrir, "https://", "web",
            "Abre um endereço no navegador.");

        Grupo("Avançado");
        Item("Comando", Padrao.Sistema, TipoAcao.Comando, "", "script-text-outline",
            "Roda uma linha de comando do Windows, sem abrir janela. Avisa se der erro.");
        Item("Brilho em…", Padrao.Brilho, TipoAcao.BrilhoDefinir, "70", "brightness-6",
            "Põe os monitores num brilho escolhido por você.");

        return itens;
    }

    static bool TemAppPath(string exe)
    {
        const string chave = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\";
        foreach (RegistryKey raiz in new[] { Registry.CurrentUser, Registry.LocalMachine })
            using (RegistryKey achada = raiz.OpenSubKey(chave + exe))
                if (achada != null)
                    return true;
        return false;
    }

    static bool TemProtocolo(string nome)
    {
        using (RegistryKey achada = Registry.ClassesRoot.OpenSubKey(nome))
            return achada != null && achada.GetValue("URL Protocol") != null;
    }
}

// Janela "Adicionar botões": toque nos que quiser e depois em Adicionar.
class JanelaCatalogo : Form
{
    const int LARGURA_DO_ITEM = 150;
    const int ALTURA_DO_ITEM = 104;

    readonly Tema tema;
    readonly FlowLayoutPanel lista = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    readonly Button abaProntos = new Button { Text = "Prontos", AutoSize = true };
    readonly Button abaPersonalizados = new Button { Text = "Macros e personalizados", AutoSize = true };
    readonly Button botaoAdicionar = new Button { Text = "Adicionar", AutoSize = true, Enabled = false };
    readonly Label descricao = new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
    readonly List<ItemDoCatalogo> escolhidos = new List<ItemDoCatalogo>();
    readonly List<ItemDoCatalogo> prontos = Catalogo.Prontos();
    readonly List<ItemDoCatalogo> personalizados = Catalogo.Personalizados();

    public List<Botao> Novos { get; private set; }

    public JanelaCatalogo(Tema tema)
    {
        this.tema = tema;

        Text = "Adicionar botões";
        Icon = IconeDoApp.Janela();
        Font = new Font("Segoe UI", 10f);
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 680);
        MinimumSize = new Size(560, 420);
        MinimizeBox = false;
        TopMost = true;

        abaProntos.Click += (s, e) => MostrarAba(true);
        abaPersonalizados.Click += (s, e) => MostrarAba(false);
        var abas = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
        abas.Controls.Add(abaProntos);
        abas.Controls.Add(abaPersonalizados);

        botaoAdicionar.Click += (s, e) => Concluir();
        var cancelar = new Button { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
        CancelButton = cancelar;
        var rodape = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, WrapContents = false };
        rodape.Controls.Add(cancelar);
        rodape.Controls.Add(botaoAdicionar);

        var grade = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10) };
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.Controls.Add(abas, 0, 0);
        grade.SetColumnSpan(abas, 2);
        grade.Controls.Add(lista, 0, 1);
        grade.SetColumnSpan(lista, 2);
        grade.Controls.Add(descricao, 0, 2);
        grade.Controls.Add(rodape, 1, 2);
        Controls.Add(grade);

        JanelaConfiguracoes.Pintar(this, tema);
        lista.Resize += (s, e) => AjustarLarguras();
        MostrarAba(true);
    }

    void MostrarAba(bool deProntos)
    {
        abaProntos.BackColor = deProntos ? Destaque() : tema.Campo;
        abaPersonalizados.BackColor = deProntos ? tema.Campo : Destaque();
        descricao.Text = deProntos
            ? "Toque nos botões que quiser e depois em Adicionar. Dá para mudar tudo depois."
            : "Modelos para montar o seu: depois de adicionar, ajuste as teclas, o texto ou o programa no editor.";

        // Cada grupo é um título e um painel com os ladrilhos, que quebra linha sozinho.
        lista.SuspendLayout();
        foreach (Control antigo in lista.Controls.Cast<Control>().ToList())
            antigo.Dispose();
        FlowLayoutPanel ladrilhos = null;
        string grupoAtual = null;
        foreach (ItemDoCatalogo item in deProntos ? prontos : personalizados)
        {
            if (item.Grupo != grupoAtual)
            {
                grupoAtual = item.Grupo;
                lista.Controls.Add(new Label
                {
                    Text = grupoAtual,
                    AutoSize = true,
                    Font = new Font("Segoe UI Semibold", 11f),
                    ForeColor = tema.TextoBarra,
                    Margin = new Padding(3, lista.Controls.Count == 0 ? 0 : 14, 3, 4),
                });
                ladrilhos = new FlowLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = true,
                    Margin = new Padding(0),
                };
                lista.Controls.Add(ladrilhos);
            }
            ladrilhos.Controls.Add(Ladrilho(item));
        }
        AjustarLarguras();
        lista.ResumeLayout();
    }

    void AjustarLarguras()
    {
        int largura = Math.Max(LARGURA_DO_ITEM + 8, lista.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 6);
        foreach (Control filho in lista.Controls)
            if (filho is FlowLayoutPanel)
                filho.MaximumSize = new Size(largura, 0);
    }

    Color Destaque()
    {
        return tema.EscuroNoSistema ? Color.FromArgb(62, 62, 74) : Color.FromArgb(214, 216, 224);
    }

    Control Ladrilho(ItemDoCatalogo item)
    {
        var ladrilho = new Button
        {
            Size = new Size(LARGURA_DO_ITEM, ALTURA_DO_ITEM),
            FlatStyle = FlatStyle.Flat,
            BackColor = tema.Campo,
            AccessibleName = item.Modelo.Rotulo,
            Margin = new Padding(4),
        };
        ladrilho.FlatAppearance.BorderSize = 2;
        AtualizarLadrilho(ladrilho, item);
        new ToolTip().SetToolTip(ladrilho, item.Descricao);

        Color cor = item.Modelo.CorReal();
        ladrilho.Paint += (s, e) =>
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle area = ladrilho.ClientRectangle;
            using (var faixa = new SolidBrush(cor))
                g.FillRectangle(faixa, area.X + 2, area.Y + 2, area.Width - 4, 5);
            Icones.Desenhar(g, item.Modelo.Icone, new Rectangle((area.Width - 34) / 2, 16, 34, 34), tema.TextoBarra);
            TextRenderer.DrawText(g, item.Modelo.Rotulo, ladrilho.Font,
                new Rectangle(6, 54, area.Width - 12, area.Height - 58), tema.TextoBarra,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak);
            if (escolhidos.Contains(item))
                TextRenderer.DrawText(g, "✓", ladrilho.Font, new Rectangle(area.Width - 26, 8, 20, 20), tema.TextoBarra,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        ladrilho.Click += (s, e) =>
        {
            if (!escolhidos.Remove(item))
                escolhidos.Add(item);
            AtualizarLadrilho(ladrilho, item);
            ladrilho.Invalidate();
            descricao.Text = item.Descricao;
            int quantos = escolhidos.Count;
            botaoAdicionar.Enabled = quantos > 0;
            botaoAdicionar.Text = quantos <= 1 ? "Adicionar" : "Adicionar " + quantos;
        };
        return ladrilho;
    }

    void AtualizarLadrilho(Button ladrilho, ItemDoCatalogo item)
    {
        bool escolhido = escolhidos.Contains(item);
        ladrilho.BackColor = escolhido ? Destaque() : tema.Campo;
        ladrilho.FlatAppearance.BorderColor = escolhido ? tema.TextoBarra : tema.Campo;
    }

    void Concluir()
    {
        if (escolhidos.Count == 0)
            return;
        Novos = escolhidos.Select(i => i.Novo()).ToList();
        DialogResult = DialogResult.OK;
        Close();
    }
}
