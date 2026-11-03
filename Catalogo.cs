// Catálogo de botões prontos: quem instala o TapBoard marca na lista o que quer no
// painel, sem precisar saber nomes de teclas, caminhos de programas ou arquivos de ícone.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

// Um item pode ser um botão ou um par de meia casa, que vira uma tecla partida.
class ItemDoCatalogo
{
    public string Nome;
    public string Grupo;
    public string Descricao;
    public List<Botao> Modelos = new List<Botao>();

    public string Icone
    {
        get { return Modelos[0].Icone; }
    }

    public List<Botao> Novos()
    {
        return Modelos.Select(m => new Botao(m.Rotulo, m.Cor, m.Acao, m.Parametro)
        {
            Icone = m.Icone,
            Largura = m.Largura,
        }).ToList();
    }
}

static class Catalogo
{
    const int MEIA = 1;

    static string grupo, cor;
    static List<ItemDoCatalogo> itens;

    static void Grupo(string nome, string corDoGrupo)
    {
        grupo = nome;
        cor = corDoGrupo;
    }

    static Botao Modelo(string rotulo, TipoAcao acao, string parametro, string icone, int largura)
    {
        return new Botao(rotulo, cor, acao, parametro) { Icone = Icones.PREFIXO + "mdi-" + icone, Largura = largura };
    }

    static void Item(string rotulo, TipoAcao acao, string parametro, string icone, string descricao)
    {
        var item = new ItemDoCatalogo { Nome = rotulo, Grupo = grupo, Descricao = descricao };
        item.Modelos.Add(Modelo(rotulo, acao, parametro, icone, Padrao.PassoDaGrade));
        itens.Add(item);
    }

    static void Atalho(string rotulo, string teclas, string icone, string descricao)
    {
        Item(rotulo, TipoAcao.Atalho, teclas, icone, descricao + " (" + teclas + ")");
    }

    // Duas metades que ocupam uma casa, como Volume − e Volume +.
    static void Par(string nome, string descricao,
        string rotuloA, TipoAcao acaoA, string parametroA, string iconeA,
        string rotuloB, TipoAcao acaoB, string parametroB, string iconeB)
    {
        var item = new ItemDoCatalogo { Nome = nome, Grupo = grupo, Descricao = descricao };
        item.Modelos.Add(Modelo(rotuloA, acaoA, parametroA, iconeA, MEIA));
        item.Modelos.Add(Modelo(rotuloB, acaoB, parametroB, iconeB, MEIA));
        itens.Add(item);
    }

    static void ParDeAtalhos(string nome, string descricao, string rotuloA, string teclasA, string iconeA, string rotuloB, string teclasB, string iconeB)
    {
        Par(nome, descricao + " (" + teclasA + " / " + teclasB + ")",
            rotuloA, TipoAcao.Atalho, teclasA, iconeA, rotuloB, TipoAcao.Atalho, teclasB, iconeB);
    }

    static void Abrir(string rotulo, string alvo, string icone, string descricao)
    {
        Item(rotulo, TipoAcao.Abrir, alvo, icone, descricao);
    }

    // Programas só entram se estiverem instalados.
    static void Programa(string rotulo, string alvo, string icone, bool instalado)
    {
        if (instalado)
            Abrir(rotulo, alvo, icone, "Abre o " + rotulo + ", ou traz a janela para a frente se já estiver aberto.");
    }

    public static List<ItemDoCatalogo> Prontos()
    {
        itens = new List<ItemDoCatalogo>();

        bool teams = TemProtocolo("ms-teams") || TemProtocolo("msteams");
        bool zoom = TemProtocolo("zoommtg");
        bool discord = TemProtocolo("discord");
        string vsCode = @"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe";
        bool temVsCode = Existe(vsCode);
        bool excel = TemAppPath("excel.exe");
        bool powerPoint = TemAppPath("powerpnt.exe");

        Grupo("Som", Padrao.Audio);
        Par("Volume − / +", "Abaixa e aumenta o volume, numa tecla partida.",
            "Volume −", TipoAcao.VolumeMenos, "", "volume-minus", "Volume +", TipoAcao.VolumeMais, "", "volume-plus");
        Item("Mudo", TipoAcao.Mudo, "", "volume-off", "Liga e desliga o som do PC. Fica afundado enquanto estiver mudo.");
        Item("Microfone", TipoAcao.MicrofoneMudo, "", "microphone", "Silencia o microfone em qualquer programa: Teams, Zoom, Discord, Meet…");
        Atalho("Ditado", "Win+H", "microphone-message", "Digita o que você falar, no campo em que estiver o cursor");
        Abrir("Configurações de som", "ms-settings:sound", "volume-high", "Abre as configurações de som do Windows, para trocar fone e caixa.");

        Grupo("Brilho do monitor", Padrao.Brilho);
        Par("Brilho −10 / +10", "Diminui e aumenta o brilho dos monitores em 10%, numa tecla partida.",
            "Brilho −10", TipoAcao.BrilhoAjustar, "-10", "brightness-5", "Brilho +10", TipoAcao.BrilhoAjustar, "10", "brightness-7");
        Item("Brilho 0%", TipoAcao.BrilhoDefinir, "0", "brightness-2", "O mínimo que o monitor permite.");
        Item("Brilho 15%", TipoAcao.BrilhoDefinir, "15", "brightness-4", "Brilho baixo, para a noite.");
        Item("Brilho 25%", TipoAcao.BrilhoDefinir, "25", "brightness-4", "Brilho baixo.");
        Item("Brilho 50%", TipoAcao.BrilhoDefinir, "50", "brightness-6", "Brilho médio.");
        Item("Brilho 75%", TipoAcao.BrilhoDefinir, "75", "brightness-6", "Brilho alto.");
        Item("Brilho 100%", TipoAcao.BrilhoDefinir, "100", "white-balance-sunny", "Brilho máximo.");
        Abrir("Luz noturna", "ms-settings:nightlight", "weather-night", "Abre a luz noturna do Windows, que deixa a tela mais amarelada.");

        Grupo("Música e vídeo", Padrao.Midia);
        Par("Anterior / Próxima", "Volta e pula faixas em qualquer player, numa tecla partida.",
            "Anterior", TipoAcao.MidiaAnterior, "", "skip-previous", "Próxima", TipoAcao.MidiaProxima, "", "skip-next");
        Item("Play / Pause", TipoAcao.MidiaPlayPause, "", "play-pause", "Toca ou pausa o que estiver tocando: Spotify, YouTube, Netflix…");
        Atalho("Parar", "MediaStop", "stop", "Para o que estiver tocando");

        Grupo("Windows", Padrao.Sistema);
        Atalho("Recorte de tela", "Win+Shift+S", "monitor-screenshot", "Tira um print de uma parte da tela");
        Atalho("Print da tela inteira", "Win+PrtSc", "fullscreen", "Salva um print da tela inteira na pasta Imagens");
        Atalho("Gravar a tela", "Win+Alt+R", "record-rec", "Começa e para a gravação do jogo ou app em uso, pela Xbox Game Bar");
        Atalho("Xbox Game Bar", "Win+G", "microsoft-xbox", "Abre a Game Bar: gravação, desempenho, áudio");
        Abrir("Explorer", "explorer.exe", "folder", "Abre uma janela do Explorador de Arquivos.");
        Atalho("Pesquisar", "Win+S", "magnify", "Abre a pesquisa do Windows");
        Atalho("Executar", "Win+R", "play-box-outline", "Abre a janela Executar");
        Atalho("Área de trabalho", "Win+D", "monitor", "Mostra a área de trabalho. Toque de novo para voltar");
        Atalho("Minimizar tudo", "Win+M", "window-minimize", "Minimiza todas as janelas");
        Atalho("Visão de tarefas", "Win+Tab", "view-dashboard-outline", "Mostra todas as janelas abertas para escolher uma");
        Atalho("Janela anterior", "Alt+Tab", "swap-horizontal", "Volta para a janela usada antes");
        Atalho("Maximizar janela", "Win+Cima", "window-maximize", "Maximiza a janela em uso");
        Atalho("Restaurar janela", "Win+Baixo", "window-restore", "Restaura ou minimiza a janela em uso");
        ParDeAtalhos("Encostar janela ◀ / ▶", "Encosta a janela em uso na metade esquerda ou direita da tela",
            "Encostar à esquerda", "Win+Esquerda", "dock-left", "Encostar à direita", "Win+Direita", "dock-right");
        Atalho("Mover para o outro monitor", "Win+Shift+Direita", "monitor-arrow-down-variant", "Leva a janela em uso para o monitor seguinte");
        Atalho("Layouts de janela", "Win+Z", "view-grid-outline", "Mostra os layouts para organizar a janela em uso");
        Atalho("Nova área de trabalho", "Win+Ctrl+D", "plus-box-multiple-outline", "Cria uma área de trabalho virtual");
        ParDeAtalhos("Área de trabalho ◀ / ▶", "Troca de área de trabalho virtual",
            "Área anterior", "Win+Ctrl+Esquerda", "arrow-left-bold-box-outline", "Próxima área", "Win+Ctrl+Direita", "arrow-right-bold-box-outline");
        Atalho("Fechar área de trabalho", "Win+Ctrl+F4", "close-box-multiple-outline", "Fecha a área de trabalho virtual atual");
        Atalho("Configurações rápidas", "Win+A", "toggle-switch-outline", "Abre Wi-Fi, Bluetooth, volume e brilho rápidos");
        Atalho("Notificações", "Win+N", "bell-outline", "Abre a central de notificações e o calendário");
        Atalho("Widgets", "Win+W", "widgets-outline", "Abre o painel de widgets");
        Atalho("Emojis", "Win+.", "emoticon-outline", "Abre o painel de emojis no campo em que estiver o cursor");
        Atalho("Área de transferência", "Win+V", "clipboard-text-clock-outline", "Mostra o histórico do que você copiou");
        Atalho("Lupa", "Win+=", "magnify-plus-outline", "Liga a lupa e aproxima a tela");
        Atalho("Fechar lupa", "Win+Esc", "magnify-close", "Desliga a lupa");
        Atalho("Teclado virtual", "Win+Ctrl+O", "keyboard-variant", "Mostra o teclado na tela");
        Atalho("Projetar", "Win+P", "projector", "Escolhe como usar o segundo monitor ou projetor");
        Atalho("Transmitir", "Win+K", "cast", "Transmite a tela para uma TV ou monitor sem fio");
        Atalho("Gerenciador de Tarefas", "Ctrl+Shift+Esc", "chart-areaspline", "Abre o Gerenciador de Tarefas");
        Atalho("Configurações do Windows", "Win+I", "cog-outline", "Abre as Configurações do Windows");
        Atalho("Reiniciar o vídeo", "Win+Ctrl+Shift+B", "restart", "Reinicia o driver de vídeo quando a tela trava ou fica preta");
        Item("Bloquear PC", TipoAcao.BloquearPC, "", "lock", "Bloqueia o computador na hora, como Win+L.");

        Grupo("Pastas e configurações", Padrao.Sistema);
        Abrir("Downloads", "shell:Downloads", "download", "Abre a pasta Downloads.");
        Abrir("Documentos", "shell:Personal", "folder-text-outline", "Abre a pasta Documentos.");
        Abrir("Imagens", "shell:My Pictures", "folder-image", "Abre a pasta Imagens.");
        Abrir("Lixeira", "shell:RecycleBinFolder", "delete-outline", "Abre a Lixeira.");
        Abrir("Wi-Fi", "ms-settings:network-wifi", "wifi", "Abre as configurações de Wi-Fi.");
        Abrir("Bluetooth", "ms-settings:bluetooth", "bluetooth", "Abre os dispositivos Bluetooth, para conectar fone ou mouse.");
        Abrir("Tela", "ms-settings:display", "monitor-dashboard", "Abre as configurações de tela: resolução, escala, monitores.");
        Abrir("Energia", "ms-settings:powersleep", "power-plug-outline", "Abre as configurações de energia e suspensão.");
        Abrir("Windows Update", "ms-settings:windowsupdate", "update", "Abre o Windows Update.");
        Abrir("Aplicativos instalados", "ms-settings:appsfeatures", "apps", "Abre a lista de aplicativos, para desinstalar.");
        Abrir("Impressoras", "ms-settings:printers", "printer-outline", "Abre as impressoras e scanners.");
        Abrir("Mouse", "ms-settings:mousetouchpad", "mouse", "Abre as configurações do mouse.");
        Abrir("Plano de fundo", "ms-settings:personalization-background", "image-outline", "Abre a escolha do papel de parede.");
        Abrir("Painel de Controle", "control.exe", "tune", "Abre o Painel de Controle clássico.");
        Abrir("Gerenciador de Dispositivos", "devmgmt.msc", "chip", "Abre o Gerenciador de Dispositivos.");

        Grupo("Apps do Windows", Padrao.Sistema);
        Abrir("Calculadora", "calc.exe", "calculator", "Abre a Calculadora.");
        Abrir("Bloco de Notas", "notepad.exe", "note-text-outline", "Abre o Bloco de Notas.");
        Abrir("Paint", "mspaint.exe", "palette-outline", "Abre o Paint.");
        Abrir("Câmera", "microsoft.windows.camera:", "camera-outline", "Abre a Câmera do Windows.");
        Abrir("Fotos", "ms-photos:", "image-multiple-outline", "Abre o app Fotos.");
        Abrir("Relógio e alarmes", "ms-clock:", "alarm", "Abre o Relógio: alarme, timer e cronômetro.");
        Abrir("Microsoft Store", "ms-windows-store:", "storefront-outline", "Abre a Microsoft Store.");
        if (Existe(@"%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe"))
            Abrir("Terminal", "wt.exe", "console", "Abre o Terminal do Windows.");
        Abrir("Prompt de Comando", "cmd.exe", "console-line", "Abre o Prompt de Comando.");
        Abrir("PowerShell", "powershell.exe", "powershell", "Abre o PowerShell.");

        Grupo("Programas instalados", Padrao.Sistema);
        Programa("Chrome", "chrome.exe", "google-chrome", TemAppPath("chrome.exe"));
        Programa("Edge", "msedge.exe", "microsoft-edge", TemAppPath("msedge.exe"));
        Programa("Firefox", "firefox.exe", "firefox", TemAppPath("firefox.exe"));
        Programa("Brave", "brave.exe", "web", TemAppPath("brave.exe"));
        Programa("Opera", @"%LOCALAPPDATA%\Programs\Opera\launcher.exe", "opera", Existe(@"%LOCALAPPDATA%\Programs\Opera\launcher.exe"));
        Programa("Vivaldi", @"%LOCALAPPDATA%\Vivaldi\Application\vivaldi.exe", "web", Existe(@"%LOCALAPPDATA%\Vivaldi\Application\vivaldi.exe"));
        Programa("Outlook", "outlook.exe", "microsoft-outlook", TemAppPath("outlook.exe"));
        Programa("Word", "winword.exe", "microsoft-word", TemAppPath("winword.exe"));
        Programa("Excel", "excel.exe", "microsoft-excel", excel);
        Programa("PowerPoint", "powerpnt.exe", "microsoft-powerpoint", powerPoint);
        Programa("OneNote", "onenote.exe", "microsoft-onenote", TemAppPath("onenote.exe"));
        Programa("Access", "msaccess.exe", "microsoft-access", TemAppPath("msaccess.exe"));
        Programa("Teams", "ms-teams:", "microsoft-teams", teams);
        Programa("Zoom", "zoommtg://", "video-outline", zoom);
        Programa("Slack", "slack:", "slack", TemProtocolo("slack"));
        Programa("Discord", "discord:", "discord", discord);
        Programa("WhatsApp", "whatsapp:", "whatsapp", TemProtocolo("whatsapp"));
        Programa("Telegram", "tg:", "send-outline", TemProtocolo("tg"));
        Programa("Spotify", "spotify:", "spotify", TemProtocolo("spotify"));
        Programa("VLC", "vlc.exe", "vlc", TemAppPath("vlc.exe"));
        Programa("VS Code", vsCode, "microsoft-visual-studio-code", temVsCode);
        Programa("Visual Studio", "devenv.exe", "microsoft-visual-studio", TemAppPath("devenv.exe"));
        Programa("Notepad++", "notepad++.exe", "notebook-edit-outline", TemAppPath("notepad++.exe"));
        Programa("Notion", "notion:", "notebook-outline", TemProtocolo("notion"));
        Programa("Obsidian", "obsidian:", "notebook-outline", TemProtocolo("obsidian"));
        Programa("Figma", "figma:", "vector-square", TemProtocolo("figma"));
        Programa("GitHub Desktop", "x-github-client:", "github", TemProtocolo("x-github-client"));
        Programa("Photoshop", "photoshop.exe", "image-edit-outline", TemAppPath("photoshop.exe"));
        Programa("Acrobat", "acrobat.exe", "file-pdf-box", TemAppPath("acrobat.exe"));
        Programa("Acrobat Reader", "acrord32.exe", "file-pdf-box", TemAppPath("acrord32.exe"));
        Programa("LibreOffice", "soffice.exe", "file-document-outline", TemAppPath("soffice.exe"));
        // O OBS só abre direito a partir da própria pasta.
        string obs = @"%ProgramFiles%\obs-studio\bin\64bit";
        if (Existe(obs + @"\obs64.exe"))
            Item("OBS Studio", TipoAcao.Comando, "cd /d \"" + obs + "\" && start \"\" obs64.exe", "record-rec", "Abre o OBS Studio.");
        Programa("Steam", "steam://open/main", "steam", TemProtocolo("steam"));
        Programa("Epic Games", "com.epicgames.launcher:", "gamepad-variant-outline", TemProtocolo("com.epicgames.launcher"));
        Programa("Xbox", "xbox:", "microsoft-xbox", TemProtocolo("xbox"));

        Grupo("Editar (no programa em uso)", Padrao.Edicao);
        Atalho("Copiar", "Ctrl+C", "content-copy", "Copia o que estiver selecionado");
        Atalho("Colar", "Ctrl+V", "content-paste", "Cola o que foi copiado");
        Atalho("Recortar", "Ctrl+X", "content-cut", "Recorta o que estiver selecionado");
        Atalho("Colar sem formatação", "Ctrl+Shift+V", "clipboard-outline", "Cola só o texto, sem cores e fontes");
        ParDeAtalhos("Desfazer / Refazer", "Desfaz e refaz a última alteração",
            "Desfazer", "Ctrl+Z", "undo", "Refazer", "Ctrl+Y", "redo");
        Atalho("Selecionar tudo", "Ctrl+A", "select-all", "Seleciona tudo");
        Atalho("Salvar", "Ctrl+S", "content-save", "Salva o arquivo aberto");
        Atalho("Novo", "Ctrl+N", "file-plus-outline", "Cria um arquivo ou janela nova");
        Atalho("Abrir arquivo", "Ctrl+O", "folder-open-outline", "Abre um arquivo no programa em uso");
        Atalho("Localizar", "Ctrl+F", "magnify", "Procura um texto no programa em uso");
        Atalho("Substituir", "Ctrl+H", "find-replace", "Procura e troca um texto (Word, Bloco de Notas, VS Code)");
        Atalho("Imprimir", "Ctrl+P", "printer", "Imprime o que estiver aberto");
        ParDeAtalhos("Zoom − / +", "Diminui e aumenta o zoom", "Diminuir zoom", "Ctrl+-", "magnify-minus-outline", "Aumentar zoom", "Ctrl+=", "magnify-plus-outline");
        Atalho("Zoom 100%", "Ctrl+0", "magnify-scan", "Volta o zoom ao normal");
        Atalho("Enter", "Enter", "keyboard-return", "Aperta Enter");
        Atalho("Esc", "Esc", "keyboard-esc", "Aperta Esc: cancela ou fecha a caixa aberta");
        Atalho("Tab", "Tab", "keyboard-tab", "Vai para o próximo campo");
        Atalho("Apagar", "Del", "backspace-reverse-outline", "Apaga o que estiver selecionado");
        Item("Fechar janela", TipoAcao.Atalho, "Alt+F4", "window-close", "Fecha a janela do programa em uso (Alt+F4)");

        Grupo("Navegador", Padrao.Edicao);
        Atalho("Nova aba", "Ctrl+T", "tab-plus", "Abre uma aba nova");
        Atalho("Fechar aba", "Ctrl+W", "tab-remove", "Fecha a aba atual");
        Atalho("Reabrir aba fechada", "Ctrl+Shift+T", "tab-unselected", "Reabre a última aba fechada");
        ParDeAtalhos("Aba ◀ / ▶", "Vai para a aba anterior ou a próxima", "Aba anterior", "Ctrl+Shift+Tab", "chevron-left", "Próxima aba", "Ctrl+Tab", "chevron-right");
        ParDeAtalhos("Voltar / Avançar", "Volta e avança páginas", "Voltar", "Alt+Esquerda", "arrow-left", "Avançar", "Alt+Direita", "arrow-right");
        Atalho("Atualizar página", "F5", "refresh", "Recarrega a página");
        Atalho("Janela anônima", "Ctrl+Shift+N", "incognito", "Abre uma janela anônima no Chrome e no Edge");
        Atalho("Barra de endereço", "Ctrl+L", "link-variant", "Seleciona o endereço para digitar outro");
        Atalho("Favoritar", "Ctrl+D", "star-outline", "Adiciona a página aos favoritos");
        Atalho("Downloads do navegador", "Ctrl+J", "download-outline", "Mostra os downloads do navegador");
        Atalho("Histórico", "Ctrl+H", "history", "Mostra o histórico do navegador");
        Atalho("Tela cheia", "F11", "fullscreen", "Põe a página em tela cheia");
        Atalho("Ferramentas do desenvolvedor", "F12", "code-braces", "Abre as ferramentas do desenvolvedor");

        Grupo("Reuniões (com a janela da reunião na frente)", Padrao.Reuniao);
        Atalho("Microfone na chamada (Windows)", "Win+Alt+K", "microphone-off", "Silencia o microfone na chamada, nos apps compatíveis com o Windows 11");
        if (teams)
        {
            Atalho("Teams: microfone", "Ctrl+Shift+M", "microphone", "Liga e desliga o microfone no Teams");
            Atalho("Teams: câmera", "Ctrl+Shift+O", "video", "Liga e desliga a câmera no Teams");
            Atalho("Teams: levantar a mão", "Ctrl+Shift+K", "hand-back-right", "Levanta ou abaixa a mão no Teams");
            Atalho("Teams: compartilhar tela", "Ctrl+Shift+E", "monitor-share", "Compartilha a tela no Teams");
            Atalho("Teams: atender", "Ctrl+Shift+A", "phone", "Atende uma chamada no Teams");
            Item("Teams: sair da chamada", TipoAcao.Atalho, "Ctrl+Shift+H", "phone-hangup", "Sai da chamada no Teams (Ctrl+Shift+H)");
        }
        if (zoom)
        {
            Atalho("Zoom: microfone", "Alt+A", "microphone", "Liga e desliga o microfone no Zoom");
            Atalho("Zoom: câmera", "Alt+V", "video", "Liga e desliga a câmera no Zoom");
            Atalho("Zoom: compartilhar tela", "Alt+S", "monitor-share", "Compartilha a tela no Zoom");
            Atalho("Zoom: levantar a mão", "Alt+Y", "hand-back-right", "Levanta ou abaixa a mão no Zoom");
            Atalho("Zoom: sair da reunião", "Alt+Q", "phone-hangup", "Sai da reunião no Zoom");
        }
        Atalho("Meet: microfone", "Ctrl+D", "microphone", "Liga e desliga o microfone no Google Meet");
        Atalho("Meet: câmera", "Ctrl+E", "video", "Liga e desliga a câmera no Google Meet");
        Atalho("Meet: levantar a mão", "Ctrl+Alt+H", "hand-back-right", "Levanta ou abaixa a mão no Google Meet");
        if (discord)
        {
            Atalho("Discord: microfone", "Ctrl+Shift+M", "microphone", "Liga e desliga o microfone no Discord");
            Atalho("Discord: ensurdecer", "Ctrl+Shift+D", "headphones-off", "Desliga o áudio e o microfone no Discord");
        }

        if (temVsCode)
        {
            Grupo("VS Code (com ele na frente)", Padrao.Edicao);
            Atalho("Paleta de comandos", "Ctrl+Shift+P", "console", "Abre a paleta de comandos");
            Atalho("Abrir arquivo rápido", "Ctrl+P", "file-search-outline", "Procura um arquivo pelo nome");
            Atalho("Buscar em tudo", "Ctrl+Shift+F", "text-search", "Busca em todos os arquivos");
            Atalho("Explorador", "Ctrl+Shift+E", "file-tree", "Mostra os arquivos do projeto");
            Atalho("Git", "Ctrl+Shift+G", "source-branch", "Mostra as alterações do Git");
            Atalho("Formatar documento", "Shift+Alt+F", "format-align-left", "Formata o arquivo aberto");
            Atalho("Salvar tudo", "Ctrl+K S", "content-save-all", "Salva todos os arquivos abertos");
            Atalho("Depurar", "F5", "play", "Começa ou continua a depuração");
            Atalho("Parar depuração", "Shift+F5", "stop", "Para a depuração");
        }

        if (excel)
        {
            Grupo("Excel (com ele na frente)", Padrao.Edicao);
            Atalho("Soma automática", "Alt+=", "sigma", "Soma as células acima");
            Atalho("Filtro", "Ctrl+Shift+L", "filter-outline", "Liga e desliga o filtro da tabela");
            Atalho("Data de hoje", "Ctrl+;", "calendar-today", "Escreve a data de hoje na célula");
            Atalho("Nova planilha", "Shift+F11", "table-plus", "Cria uma planilha nova no arquivo");
            Atalho("Inserir gráfico", "Alt+F1", "chart-bar", "Cria um gráfico com os dados selecionados");
        }

        if (powerPoint)
        {
            Grupo("PowerPoint (com ele na frente)", Padrao.Edicao);
            Atalho("Apresentar do início", "F5", "presentation-play", "Começa a apresentação do primeiro slide");
            Atalho("Apresentar deste slide", "Shift+F5", "presentation", "Começa a apresentação do slide atual");
            ParDeAtalhos("Slide ◀ / ▶", "Volta e avança slides na apresentação", "Slide anterior", "Esquerda", "chevron-left", "Próximo slide", "Direita", "chevron-right");
        }

        Grupo("Reações (digita no campo em uso)", Padrao.Edicao);
        Item("Joinha", TipoAcao.Digitar, "👍", "thumb-up-outline", "Digita 👍");
        Item("Coração", TipoAcao.Digitar, "❤️", "heart-outline", "Digita ❤️");
        Item("Risada", TipoAcao.Digitar, "😂", "emoticon-lol-outline", "Digita 😂");
        Item("Obrigado", TipoAcao.Digitar, "🙏", "hands-pray", "Digita 🙏");
        Item("Feito", TipoAcao.Digitar, "✅", "check-circle-outline", "Digita ✅");

        Grupo("Energia", Padrao.Perigo);
        Item("Desligar em 1 minuto", TipoAcao.Comando, "shutdown /s /t 60", "power", "Desliga o PC em 1 minuto. Dá tempo de cancelar.");
        Item("Reiniciar em 1 minuto", TipoAcao.Comando, "shutdown /r /t 60", "restart", "Reinicia o PC em 1 minuto. Dá tempo de cancelar.");
        Item("Cancelar desligamento", TipoAcao.Comando, "shutdown /a", "cancel", "Cancela o desligamento ou a reinicialização marcados.");

        return itens;
    }

    public static List<ItemDoCatalogo> Personalizados()
    {
        itens = new List<ItemDoCatalogo>();

        Grupo("Teclado", Padrao.Sistema);
        Item("Atalho", TipoAcao.Atalho, "Ctrl+Shift+N", "keyboard-outline",
            "Aperta uma combinação de teclas no programa em uso. Ex.: Ctrl+Shift+N.");
        Item("Sequência de teclas", TipoAcao.Atalho, "Ctrl+K Ctrl+C", "keyboard-outline",
            "Várias teclas, uma depois da outra, separadas por espaço. Ex.: Ctrl+K Ctrl+C.");
        Grupo("Teclado", Padrao.Edicao);
        Item("Digitar texto", TipoAcao.Digitar, "Bom dia!", "form-textbox",
            "Escreve um texto pronto: assinatura de e-mail, endereço, resposta padrão.");

        Grupo("Abrir", Padrao.Sistema);
        Item("Programa ou arquivo", TipoAcao.Abrir, "", "application-outline",
            "Abre um programa, arquivo ou pasta. Escolha qual em Procurar… depois de adicionar.");
        Item("Site", TipoAcao.Abrir, "https://", "web",
            "Abre um endereço no navegador.");

        Grupo("Avançado", Padrao.Sistema);
        Item("Comando", TipoAcao.Comando, "", "script-text-outline",
            "Roda uma linha de comando do Windows, sem abrir janela. Avisa se der erro.");
        Grupo("Avançado", Padrao.Brilho);
        Item("Brilho em…", TipoAcao.BrilhoDefinir, "70", "brightness-6",
            "Põe os monitores num brilho escolhido por você.");

        return itens;
    }

    // O botão do painel é deste item? Mesma ação e parâmetro; se outro item do
    // catálogo tiver a mesma ação e parâmetro (Ctrl+Shift+M no Teams e no Discord),
    // o rótulo desempata. Programas também se reconhecem pelo rótulo.
    public static bool Corresponde(Botao modelo, Botao botao, List<ItemDoCatalogo> todos)
    {
        if (modelo.Acao != botao.Acao)
            return false;
        bool mesmoRotulo = string.Equals(modelo.Rotulo, botao.Rotulo, StringComparison.OrdinalIgnoreCase);
        if (modelo.Acao == TipoAcao.Abrir && mesmoRotulo)
            return true;
        if (!string.Equals(modelo.Parametro ?? "", botao.Parametro ?? "", StringComparison.OrdinalIgnoreCase))
            return false;
        int iguais = todos.SelectMany(i => i.Modelos)
            .Count(m => m.Acao == modelo.Acao && string.Equals(m.Parametro ?? "", modelo.Parametro ?? "", StringComparison.OrdinalIgnoreCase));
        return iguais <= 1 || mesmoRotulo;
    }

    public static bool Existe(string caminho)
    {
        return File.Exists(Environment.ExpandEnvironmentVariables(caminho))
            || Directory.Exists(Environment.ExpandEnvironmentVariables(caminho));
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

// Janela "Escolher botões": cada item do catálogo tem uma caixa; marcado é estar
// no painel. Desmarcar tira o botão; marcar põe no fim.
class JanelaCatalogo : Form
{
    const int LARGURA_DO_ITEM = 268;
    const int ALTURA_DO_ITEM = 40;
    const int LADO_DO_ICONE = 22;

    readonly Tema tema;
    readonly List<Botao> botoes;
    readonly FlowLayoutPanel lista = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    readonly TextBox campoBusca = new TextBox { Dock = DockStyle.Fill, AccessibleName = "Buscar" };
    readonly Button abaProntos = new Button { Text = "Prontos", AutoSize = true };
    readonly Button abaPersonalizados = new Button { Text = "Macros e personalizados", AutoSize = true };
    readonly Button botaoAplicar = new Button { Text = "Aplicar", AutoSize = true };
    readonly Label descricao = new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
    readonly ToolTip dica = new ToolTip();
    readonly List<ItemDoCatalogo> prontos = Catalogo.Prontos();
    readonly List<ItemDoCatalogo> personalizados = Catalogo.Personalizados();
    readonly HashSet<ItemDoCatalogo> estavam = new HashSet<ItemDoCatalogo>();
    readonly HashSet<ItemDoCatalogo> marcados = new HashSet<ItemDoCatalogo>();
    readonly Dictionary<string, Bitmap> imagens = new Dictionary<string, Bitmap>();
    bool naAbaDeProntos = true;
    ItemDoCatalogo modeloEscolhido;

    // Índice do botão para abrir no editor depois (o primeiro novo), ou -1.
    public int ParaEditar { get; private set; }

    public JanelaCatalogo(Tema tema, List<Botao> botoes)
    {
        this.tema = tema;
        this.botoes = botoes;
        ParaEditar = -1;

        foreach (ItemDoCatalogo item in prontos)
            if (item.Modelos.Any(m => botoes.Any(b => Catalogo.Corresponde(m, b, prontos))))
            {
                estavam.Add(item);
                marcados.Add(item);
            }

        Text = "Escolher botões";
        Icon = IconeDoApp.Janela();
        Font = new Font("Segoe UI", 10f);
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(940, 720);
        MinimumSize = new Size(600, 440);
        MinimizeBox = false;
        TopMost = true;

        abaProntos.Click += (s, e) => MostrarAba(true);
        abaPersonalizados.Click += (s, e) => MostrarAba(false);
        var abas = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
        abas.Controls.Add(abaProntos);
        abas.Controls.Add(abaPersonalizados);
        abas.Controls.Add(new Label { Text = "Buscar:", AutoSize = true, Margin = new Padding(24, 8, 4, 0) });
        campoBusca.Width = 260;
        campoBusca.Dock = DockStyle.None;
        campoBusca.Margin = new Padding(0, 5, 0, 0);
        campoBusca.TextChanged += (s, e) => Montar();
        abas.Controls.Add(campoBusca);

        botaoAplicar.Click += (s, e) => Aplicar();
        AcceptButton = botaoAplicar;
        var cancelar = new Button { Text = "Cancelar", AutoSize = true, DialogResult = DialogResult.Cancel };
        CancelButton = cancelar;
        var rodape = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, WrapContents = false };
        rodape.Controls.Add(cancelar);
        rodape.Controls.Add(botaoAplicar);

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

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        campoBusca.Focus();
    }

    void MostrarAba(bool deProntos)
    {
        naAbaDeProntos = deProntos;
        abaProntos.BackColor = deProntos ? Destaque() : tema.Campo;
        abaPersonalizados.BackColor = deProntos ? tema.Campo : Destaque();
        Montar();
    }

    void Resumo()
    {
        descricao.Text = naAbaDeProntos
            ? "Marque o que quer no painel e desmarque o que não quer. " + marcados.Count + " marcados."
            : "Modelos para montar o seu: toque num deles e ajuste as teclas, o texto ou o programa no editor.";
    }

    static string SemAcentos(string texto)
    {
        var limpo = new StringBuilder();
        foreach (char letra in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                limpo.Append(letra);
        return limpo.ToString().ToLowerInvariant();
    }

    // Cada grupo é um título e um painel com os itens, que quebra linha sozinho.
    void Montar()
    {
        string busca = SemAcentos(campoBusca.Text.Trim());
        lista.SuspendLayout();
        foreach (Control antigo in lista.Controls.Cast<Control>().ToList())
            antigo.Dispose();
        FlowLayoutPanel doGrupo = null;
        string grupoAtual = null;
        foreach (ItemDoCatalogo item in naAbaDeProntos ? prontos : personalizados)
        {
            if (busca != "" && !SemAcentos(item.Nome + " " + item.Grupo + " " + item.Descricao).Contains(busca))
                continue;
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
                doGrupo = new FlowLayoutPanel
                {
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = true,
                    Margin = new Padding(0),
                };
                lista.Controls.Add(doGrupo);
            }
            doGrupo.Controls.Add(naAbaDeProntos ? Caixa(item) : (Control)Modelo(item));
        }
        if (lista.Controls.Count == 0)
            lista.Controls.Add(new Label { Text = "Nada encontrado.", AutoSize = true, ForeColor = tema.TextoBarra });
        AjustarLarguras();
        lista.ResumeLayout();
        Resumo();
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

    Bitmap Imagem(string icone)
    {
        Bitmap imagem;
        if (imagens.TryGetValue(icone, out imagem))
            return imagem;
        imagem = new Bitmap(LADO_DO_ICONE, LADO_DO_ICONE);
        using (Graphics g = Graphics.FromImage(imagem))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Icones.Desenhar(g, icone, new Rectangle(0, 0, LADO_DO_ICONE, LADO_DO_ICONE), tema.TextoBarra);
        }
        imagens[icone] = imagem;
        return imagem;
    }

    Control Caixa(ItemDoCatalogo item)
    {
        var caixa = new CheckBox
        {
            Text = "  " + item.Nome,
            Checked = marcados.Contains(item),
            Size = new Size(LARGURA_DO_ITEM, ALTURA_DO_ITEM),
            Image = Imagem(item.Icone),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = tema.TextoBarra,
            BackColor = tema.Fundo,
            Margin = new Padding(4, 2, 4, 2),
            AutoEllipsis = true,
        };
        dica.SetToolTip(caixa, item.Descricao);
        caixa.CheckedChanged += (s, e) =>
        {
            if (caixa.Checked)
                marcados.Add(item);
            else
                marcados.Remove(item);
            Resumo();
        };
        caixa.MouseEnter += (s, e) => descricao.Text = item.Descricao;
        caixa.MouseLeave += (s, e) => Resumo();
        return caixa;
    }

    Control Modelo(ItemDoCatalogo item)
    {
        var botao = new Button
        {
            Text = "  + " + item.Nome,
            Size = new Size(LARGURA_DO_ITEM, ALTURA_DO_ITEM + 4),
            Image = Imagem(item.Icone),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            BackColor = tema.Campo,
            ForeColor = tema.TextoBarra,
            Margin = new Padding(4),
            Padding = new Padding(8, 0, 0, 0),
        };
        dica.SetToolTip(botao, item.Descricao);
        botao.MouseEnter += (s, e) => descricao.Text = item.Descricao;
        botao.MouseLeave += (s, e) => Resumo();
        botao.Click += (s, e) =>
        {
            modeloEscolhido = item;
            Aplicar();
        };
        return botao;
    }

    void Aplicar()
    {
        foreach (ItemDoCatalogo item in estavam.Where(i => !marcados.Contains(i)))
            botoes.RemoveAll(b => item.Modelos.Any(m => Catalogo.Corresponde(m, b, prontos)));

        int primeiro = botoes.Count;
        foreach (ItemDoCatalogo item in prontos.Where(i => marcados.Contains(i) && !estavam.Contains(i)))
            botoes.AddRange(item.Novos());
        if (modeloEscolhido != null)
        {
            primeiro = botoes.Count;
            botoes.AddRange(modeloEscolhido.Novos());
        }
        ParaEditar = primeiro < botoes.Count ? primeiro : -1;

        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (Bitmap imagem in imagens.Values)
                imagem.Dispose();
            dica.Dispose();
        }
        base.Dispose(disposing);
    }
}
