// Ícones dos botões: SVG desenhado aqui mesmo, imagens comuns e busca na Iconify.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

static class Icones
{
    public static readonly string Pasta = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TapBoard", "icones");

    // Ícones que vêm dentro do programa: "embutido:mdi-lock".
    public const string PREFIXO = "embutido:";

    static bool EhEmbutido(string caminho)
    {
        return caminho.StartsWith(PREFIXO, StringComparison.Ordinal);
    }

    public static bool Existe(string caminho)
    {
        return EhEmbutido(caminho)
            ? IconesEmbutidos.Svg.ContainsKey(caminho.Substring(PREFIXO.Length))
            : File.Exists(caminho);
    }

    // Variante("embutido:mdi-microphone", "-off") = "embutido:mdi-microphone-off".
    public static string Variante(string caminho, string sufixo)
    {
        if (EhEmbutido(caminho))
            return caminho + sufixo;
        return Path.Combine(Path.GetDirectoryName(caminho),
            Path.GetFileNameWithoutExtension(caminho) + sufixo + Path.GetExtension(caminho));
    }

    static readonly Dictionary<string, object> carregados = new Dictionary<string, object>();

    static object Obter(string caminho)
    {
        object icone;
        if (carregados.TryGetValue(caminho, out icone))
            return icone;

        try
        {
            string svg;
            if (EhEmbutido(caminho))
                icone = IconesEmbutidos.Svg.TryGetValue(caminho.Substring(PREFIXO.Length), out svg) ? IconeVetorial.Ler(svg) : null;
            else if (Path.GetExtension(caminho).Equals(".svg", StringComparison.OrdinalIgnoreCase))
                icone = IconeVetorial.Ler(File.ReadAllText(caminho));
            else
                using (var memoria = new MemoryStream(File.ReadAllBytes(caminho)))
                using (Image imagem = Image.FromStream(memoria))
                    icone = new Bitmap(imagem);
        }
        catch (Exception)
        {
            icone = null;
        }
        carregados[caminho] = icone;
        return icone;
    }

    public static void Desenhar(Graphics g, string caminho, Rectangle area, Color cor)
    {
        if (string.IsNullOrEmpty(caminho))
            return;

        object icone = Obter(caminho);
        var vetorial = icone as IconeVetorial;
        if (vetorial != null)
        {
            vetorial.Desenhar(g, area, cor);
            return;
        }

        var imagem = icone as Image;
        if (imagem != null)
        {
            float escala = Math.Min((float)area.Width / imagem.Width, (float)area.Height / imagem.Height);
            float largura = imagem.Width * escala;
            float altura = imagem.Height * escala;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imagem,
                area.X + (area.Width - largura) / 2,
                area.Y + (area.Height - altura) / 2,
                largura,
                altura);
        }
    }
}

// Desenha formas e caminhos SVG. O ícone assume a cor do texto do botão,
// por isso as cores do arquivo são ignoradas.
class IconeVetorial
{
    class Forma
    {
        public GraphicsPath Caminho;
        public bool Preencher;
        public bool Contornar;
        public float Espessura;
    }

    readonly List<Forma> formas = new List<Forma>();
    RectangleF caixa = new RectangleF(0, 0, 24, 24);

    public static IconeVetorial Ler(string svg)
    {
        // Sem DTD nem recursos externos: o arquivo pode vir da internet.
        var ajustes = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        var documento = new XmlDocument { XmlResolver = null };
        using (var texto = new StringReader(svg))
        using (XmlReader leitor = XmlReader.Create(texto, ajustes))
            documento.Load(leitor);

        var icone = new IconeVetorial();
        XmlElement raiz = documento.DocumentElement;
        string[] partes = raiz.GetAttribute("viewBox").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 4)
            icone.caixa = new RectangleF(Numero(partes[0]), Numero(partes[1]), Numero(partes[2]), Numero(partes[3]));

        icone.Percorrer(raiz, new Dictionary<string, string>());
        if (icone.formas.Count == 0)
            throw new FormatException("SVG sem formas.");
        return icone;
    }

    static float Numero(string texto)
    {
        return float.Parse(texto, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    static float Atributo(XmlElement e, string nome)
    {
        string valor = e.GetAttribute(nome);
        return valor == "" ? 0f : Numero(Regex.Replace(valor, "[a-z%]+$", ""));
    }

    void Percorrer(XmlElement elemento, Dictionary<string, string> herdado)
    {
        var estilo = new Dictionary<string, string>(herdado);
        foreach (string nome in new[] { "fill", "stroke", "stroke-width", "fill-rule" })
            if (elemento.HasAttribute(nome))
                estilo[nome] = elemento.GetAttribute(nome);

        GraphicsPath caminho = CriarCaminho(elemento);
        if (caminho != null)
            Adicionar(caminho, estilo);

        foreach (XmlNode filho in elemento.ChildNodes)
        {
            var filhoElemento = filho as XmlElement;
            if (filhoElemento != null && filhoElemento.LocalName != "defs" && filhoElemento.LocalName != "title")
                Percorrer(filhoElemento, estilo);
        }
    }

    void Adicionar(GraphicsPath caminho, Dictionary<string, string> estilo)
    {
        string preenchimento, traco, espessura, regra;
        estilo.TryGetValue("fill", out preenchimento);
        estilo.TryGetValue("stroke", out traco);
        estilo.TryGetValue("stroke-width", out espessura);
        estilo.TryGetValue("fill-rule", out regra);

        caminho.FillMode = regra == "evenodd" ? FillMode.Alternate : FillMode.Winding;
        formas.Add(new Forma
        {
            Caminho = caminho,
            Preencher = preenchimento != "none",
            Contornar = traco != null && traco != "none",
            Espessura = string.IsNullOrEmpty(espessura) ? 1f : Numero(espessura),
        });
    }

    static GraphicsPath CriarCaminho(XmlElement e)
    {
        var caminho = new GraphicsPath();
        switch (e.LocalName)
        {
            case "path":
                new LeitorDeCaminho(e.GetAttribute("d"), caminho).Ler();
                return caminho;
            case "circle":
                float r = Atributo(e, "r");
                caminho.AddEllipse(Atributo(e, "cx") - r, Atributo(e, "cy") - r, r * 2, r * 2);
                return caminho;
            case "ellipse":
                float rx = Atributo(e, "rx"), ry = Atributo(e, "ry");
                caminho.AddEllipse(Atributo(e, "cx") - rx, Atributo(e, "cy") - ry, rx * 2, ry * 2);
                return caminho;
            case "rect":
                AdicionarRetangulo(caminho, e);
                return caminho;
            case "line":
                caminho.AddLine(Atributo(e, "x1"), Atributo(e, "y1"), Atributo(e, "x2"), Atributo(e, "y2"));
                return caminho;
            case "polyline":
            case "polygon":
                caminho.AddLines(Pontos(e.GetAttribute("points")));
                if (e.LocalName == "polygon")
                    caminho.CloseFigure();
                return caminho;
        }
        caminho.Dispose();
        return null;
    }

    static void AdicionarRetangulo(GraphicsPath caminho, XmlElement e)
    {
        float x = Atributo(e, "x"), y = Atributo(e, "y");
        float largura = Atributo(e, "width"), altura = Atributo(e, "height");
        float raio = Math.Max(Atributo(e, "rx"), Atributo(e, "ry"));
        if (raio <= 0)
        {
            caminho.AddRectangle(new RectangleF(x, y, largura, altura));
            return;
        }

        float d = Math.Min(raio * 2, Math.Min(largura, altura));
        caminho.AddArc(x, y, d, d, 180, 90);
        caminho.AddArc(x + largura - d, y, d, d, 270, 90);
        caminho.AddArc(x + largura - d, y + altura - d, d, d, 0, 90);
        caminho.AddArc(x, y + altura - d, d, d, 90, 90);
        caminho.CloseFigure();
    }

    static PointF[] Pontos(string texto)
    {
        string[] partes = texto.Split(new[] { ' ', ',', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var pontos = new PointF[partes.Length / 2];
        for (int i = 0; i < pontos.Length; i++)
            pontos[i] = new PointF(Numero(partes[i * 2]), Numero(partes[i * 2 + 1]));
        return pontos;
    }

    public void Desenhar(Graphics g, Rectangle area, Color cor)
    {
        float escala = Math.Min(area.Width / caixa.Width, area.Height / caixa.Height);
        GraphicsState estado = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(
            area.X + (area.Width - caixa.Width * escala) / 2,
            area.Y + (area.Height - caixa.Height * escala) / 2);
        g.ScaleTransform(escala, escala);
        g.TranslateTransform(-caixa.X, -caixa.Y);

        using (var pincel = new SolidBrush(cor))
        {
            foreach (Forma forma in formas)
            {
                if (forma.Preencher && !forma.Contornar)
                    g.FillPath(pincel, forma.Caminho);
                if (forma.Contornar)
                    using (var caneta = new Pen(cor, forma.Espessura))
                    {
                        caneta.StartCap = caneta.EndCap = LineCap.Round;
                        caneta.LineJoin = LineJoin.Round;
                        g.DrawPath(caneta, forma.Caminho);
                    }
            }
        }
        g.Restore(estado);
    }
}

// Interpreta o atributo "d" de um <path>.
class LeitorDeCaminho
{
    readonly string d;
    readonly GraphicsPath caminho;
    int i;
    PointF atual, inicio, controle;
    char anterior;

    public LeitorDeCaminho(string d, GraphicsPath caminho)
    {
        this.d = d;
        this.caminho = caminho;
    }

    public void Ler()
    {
        char comando = ' ';
        while (true)
        {
            PularSeparadores();
            if (i >= d.Length)
                return;

            if (char.IsLetter(d[i]))
                comando = d[i++];
            else if (comando == 'M')
                comando = 'L';
            else if (comando == 'm')
                comando = 'l';

            Executar(comando);
            anterior = char.ToUpperInvariant(comando);
        }
    }

    void PularSeparadores()
    {
        while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ','))
            i++;
    }

    float Numero()
    {
        PularSeparadores();
        int comeco = i;
        if (i < d.Length && (d[i] == '-' || d[i] == '+'))
            i++;

        bool ponto = false;
        while (i < d.Length)
        {
            char c = d[i];
            if (char.IsDigit(c))
                i++;
            else if (c == '.' && !ponto)
            {
                ponto = true;
                i++;
            }
            else if ((c == 'e' || c == 'E') && i + 1 < d.Length && (char.IsDigit(d[i + 1]) || d[i + 1] == '-'))
                i += 2;
            else
                break;
        }

        if (i == comeco)
            throw new FormatException("Número esperado no caminho SVG.");
        return float.Parse(d.Substring(comeco, i - comeco), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    // Em arcos, as bandeiras podem vir coladas: "a1 1 0 01.5.5".
    bool Bandeira()
    {
        PularSeparadores();
        return d[i++] == '1';
    }

    PointF Ponto(bool relativo)
    {
        float x = Numero();
        float y = Numero();
        return relativo ? new PointF(atual.X + x, atual.Y + y) : new PointF(x, y);
    }

    void Executar(char comando)
    {
        bool relativo = char.IsLower(comando);
        PointF c1, c2, fim;

        switch (char.ToUpperInvariant(comando))
        {
            case 'M':
                atual = inicio = Ponto(relativo);
                caminho.StartFigure();
                break;
            case 'L':
                Linha(Ponto(relativo));
                break;
            case 'H':
                float x = Numero();
                Linha(new PointF(relativo ? atual.X + x : x, atual.Y));
                break;
            case 'V':
                float y = Numero();
                Linha(new PointF(atual.X, relativo ? atual.Y + y : y));
                break;
            case 'C':
                c1 = Ponto(relativo);
                c2 = Ponto(relativo);
                Curva(c1, c2, Ponto(relativo));
                break;
            case 'S':
                c1 = anterior == 'C' || anterior == 'S' ? Refletido() : atual;
                c2 = Ponto(relativo);
                Curva(c1, c2, Ponto(relativo));
                break;
            case 'Q':
                c1 = Ponto(relativo);
                Quadratica(c1, Ponto(relativo));
                break;
            case 'T':
                c1 = anterior == 'Q' || anterior == 'T' ? Refletido() : atual;
                Quadratica(c1, Ponto(relativo));
                break;
            case 'A':
                float rx = Numero(), ry = Numero(), rotacao = Numero();
                bool grande = Bandeira(), horario = Bandeira();
                fim = Ponto(relativo);
                Arco(rx, ry, rotacao, grande, horario, fim);
                break;
            case 'Z':
                caminho.CloseFigure();
                atual = inicio;
                break;
            default:
                throw new FormatException("Comando desconhecido no caminho SVG.");
        }
    }

    PointF Refletido()
    {
        return new PointF(2 * atual.X - controle.X, 2 * atual.Y - controle.Y);
    }

    void Linha(PointF fim)
    {
        caminho.AddLine(atual, fim);
        atual = fim;
    }

    void Curva(PointF c1, PointF c2, PointF fim)
    {
        caminho.AddBezier(atual, c1, c2, fim);
        controle = c2;
        atual = fim;
    }

    void Quadratica(PointF c, PointF fim)
    {
        var c1 = new PointF(atual.X + (c.X - atual.X) * 2 / 3, atual.Y + (c.Y - atual.Y) * 2 / 3);
        var c2 = new PointF(fim.X + (c.X - fim.X) * 2 / 3, fim.Y + (c.Y - fim.Y) * 2 / 3);
        caminho.AddBezier(atual, c1, c2, fim);
        controle = c;
        atual = fim;
    }

    // Converte o arco da forma SVG (extremidades) para centro e ângulos,
    // conforme o apêndice de implementação da especificação SVG, e o
    // aproxima por curvas de Bézier de no máximo 90 graus.
    void Arco(float rx, float ry, float rotacao, bool grande, bool horario, PointF fim)
    {
        if (atual == fim)
            return;
        if (rx == 0 || ry == 0)
        {
            Linha(fim);
            return;
        }

        double fi = rotacao * Math.PI / 180;
        double cos = Math.Cos(fi), sen = Math.Sin(fi);
        double dx = (atual.X - fim.X) / 2.0, dy = (atual.Y - fim.Y) / 2.0;
        double x1 = cos * dx + sen * dy, y1 = -sen * dx + cos * dy;

        double r1 = Math.Abs(rx), r2 = Math.Abs(ry);
        double excesso = x1 * x1 / (r1 * r1) + y1 * y1 / (r2 * r2);
        if (excesso > 1)
        {
            r1 *= Math.Sqrt(excesso);
            r2 *= Math.Sqrt(excesso);
        }

        double divisor = r1 * r1 * y1 * y1 + r2 * r2 * x1 * x1;
        double fator = Math.Sqrt(Math.Max(0, (r1 * r1 * r2 * r2 - divisor) / divisor));
        if (grande == horario)
            fator = -fator;

        double cx1 = fator * r1 * y1 / r2, cy1 = -fator * r2 * x1 / r1;
        double cx = cos * cx1 - sen * cy1 + (atual.X + fim.X) / 2.0;
        double cy = sen * cx1 + cos * cy1 + (atual.Y + fim.Y) / 2.0;

        double angulo = Math.Atan2((y1 - cy1) / r2, (x1 - cx1) / r1);
        double varredura = Math.Atan2((-y1 - cy1) / r2, (-x1 - cx1) / r1) - angulo;
        if (horario && varredura < 0)
            varredura += 2 * Math.PI;
        else if (!horario && varredura > 0)
            varredura -= 2 * Math.PI;

        int segmentos = Math.Max(1, (int)Math.Ceiling(Math.Abs(varredura) / (Math.PI / 2) - 1e-6));
        double passo = varredura / segmentos;
        double k = 4.0 / 3.0 * Math.Tan(passo / 4);

        Func<double, double, PointF> paraTela = (px, py) => new PointF(
            (float)(cx + r1 * px * cos - r2 * py * sen),
            (float)(cy + r1 * px * sen + r2 * py * cos));

        PointF ponto = atual;
        for (int s = 0; s < segmentos; s++)
        {
            double a = angulo + s * passo, b = a + passo;
            PointF c1 = paraTela(Math.Cos(a) - k * Math.Sin(a), Math.Sin(a) + k * Math.Cos(a));
            PointF c2 = paraTela(Math.Cos(b) + k * Math.Sin(b), Math.Sin(b) - k * Math.Cos(b));
            PointF proximo = s == segmentos - 1 ? fim : paraTela(Math.Cos(b), Math.Sin(b));
            caminho.AddBezier(ponto, c1, c2, proximo);
            ponto = proximo;
        }
        atual = fim;
    }
}

class JanelaIcones : Form
{
    const string Api = "https://api.iconify.design";
    const string Tradutor = "https://api.mymemory.translated.net/get?q={0}&langpair=pt-BR%7Cen";
    const int EsperaDoTradutor = 4000;

    // O tradutor erra palavras soltas ("bloquear" vira "dishonor"): os termos comuns ficam aqui, sem acento.
    static readonly Dictionary<string, string> TermosComuns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "abrir", "open" }, { "anterior", "previous" }, { "arquivo", "file" }, { "audio", "audio" },
        { "bloquear", "lock" }, { "brilho", "brightness" }, { "cadeado", "lock" }, { "camera", "camera" },
        { "casa", "home" }, { "configuracoes", "settings" }, { "desligar", "power" }, { "engrenagem", "gear" },
        { "escuro", "moon" }, { "fechar", "close" }, { "fone", "headphones" }, { "gravar", "record" },
        { "janela", "window" }, { "lua", "moon" }, { "microfone", "microphone" }, { "monitor", "monitor" },
        { "mudo", "mute" }, { "musica", "music" }, { "pasta", "folder" }, { "pausar", "pause" },
        { "pausa", "pause" }, { "proxima", "next" }, { "proximo", "next" }, { "recorte", "scissors" },
        { "reiniciar", "restart" }, { "relogio", "clock" }, { "sol", "sun" }, { "som", "sound" },
        { "tela", "screen" }, { "tarefas", "tasks" }, { "teclado", "keyboard" }, { "tocar", "play" },
    };

    // Só coleções de licença permissiva: Lucide (ISC), Tabler e Phosphor (MIT), MDI (Apache 2.0).
    const string Colecoes = "lucide,tabler,mdi,ph";
    const int Limite = 48;
    const int LadoDoResultado = 72;

    static readonly Regex NomeValido = new Regex("^[a-z0-9-]+:[a-z0-9-]+$");

    readonly Tema tema;
    readonly TextBox campoBusca = new TextBox { Dock = DockStyle.Fill };
    readonly Label estado = new Label { AutoSize = true, Text = "Busque em português ou inglês: música, pasta, play…" };
    readonly FlowLayoutPanel resultados = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
    int buscaAtual;

    public string Escolhido { get; private set; }

    public JanelaIcones(Tema tema)
    {
        this.tema = tema;

        Text = "Buscar ícone";
        Icon = IconeDoApp.Janela();
        Font = new Font("Segoe UI", 10f);
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(720, 560);
        MinimizeBox = false;
        MaximizeBox = false;
        TopMost = true;

        var botaoBuscar = new Button { Text = "Buscar", AutoSize = true };
        botaoBuscar.Click += (s, e) => Buscar();
        AcceptButton = botaoBuscar;

        var grade = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Padding = new Padding(10) };
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grade.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grade.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grade.Controls.Add(campoBusca, 0, 0);
        grade.Controls.Add(botaoBuscar, 1, 0);
        grade.Controls.Add(estado, 0, 1);
        grade.SetColumnSpan(estado, 2);
        grade.Controls.Add(resultados, 0, 2);
        grade.SetColumnSpan(resultados, 2);
        Controls.Add(grade);

        JanelaConfiguracoes.Pintar(this, tema);
    }

    static WebClient Cliente()
    {
        return new WebClient { Encoding = System.Text.Encoding.UTF8 };
    }

    async void Buscar()
    {
        string termo = campoBusca.Text.Trim();
        if (termo == "")
            return;

        int busca = ++buscaAtual;
        resultados.Controls.Clear();
        estado.Text = "Buscando…";

        try
        {
            string ingles = await TraduzirParaIngles(termo);
            bool traduziu = !ingles.Equals(termo, StringComparison.OrdinalIgnoreCase);

            // A tradução vem primeiro; o termo original cobre o que já estava em inglês.
            List<string> nomes = await BuscarNomes(ingles);
            if (traduziu)
                nomes = nomes.Union(await BuscarNomes(termo)).Take(Limite).ToList();

            string[] conteudos = await Task.WhenAll(nomes.Select(Baixar));
            if (IsDisposed || busca != buscaAtual)
                return;

            for (int i = 0; i < nomes.Count; i++)
                AdicionarResultado(nomes[i], conteudos[i]);
            string origem = traduziu ? " para \"" + ingles + "\"" : "";
            estado.Text = nomes.Count == 0
                ? "Nada encontrado" + origem + ". Tente outro termo."
                : nomes.Count + " ícones" + origem + ". Clique para escolher.";
        }
        catch (Exception erro)
        {
            if (!IsDisposed && busca == buscaAtual)
                estado.Text = "Não consegui buscar: " + erro.Message;
        }
    }

    static string SemAcentos(string texto)
    {
        var limpo = new StringBuilder();
        foreach (char letra in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
                limpo.Append(letra);
        return limpo.ToString();
    }

    // Sem tradução disponível, a busca segue com o termo original.
    static async Task<string> TraduzirParaIngles(string termo)
    {
        string comum;
        if (TermosComuns.TryGetValue(SemAcentos(termo), out comum))
            return comum;

        try
        {
            using (WebClient cliente = Cliente())
            {
                Task<string> pedido = cliente.DownloadStringTaskAsync(string.Format(Tradutor, Uri.EscapeDataString(termo)));
                if (await Task.WhenAny(pedido, Task.Delay(EsperaDoTradutor)) != pedido)
                {
                    cliente.CancelAsync();
                    return termo;
                }

                var dados = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(await pedido);
                if (Convert.ToString(dados["responseStatus"]) != "200")
                    return termo;

                var resposta = (Dictionary<string, object>)dados["responseData"];
                string texto = ((string)resposta["translatedText"]).Split(';', '(')[0].Trim().ToLowerInvariant();
                return texto == "" || texto.StartsWith("mymemory warning") ? termo : texto;
            }
        }
        catch (Exception)
        {
            return termo;
        }
    }

    static async Task<List<string>> BuscarNomes(string termo)
    {
        string url = string.Format("{0}/search?query={1}&limit={2}&prefixes={3}",
            Api, Uri.EscapeDataString(termo), Limite, Colecoes);

        using (WebClient cliente = Cliente())
        {
            string json = await cliente.DownloadStringTaskAsync(url);
            var dados = (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(json);
            return ((object[])dados["icons"]).Cast<string>().Where(n => NomeValido.IsMatch(n)).ToList();
        }
    }

    static async Task<string> Baixar(string nome)
    {
        string[] partes = nome.Split(':');
        using (WebClient cliente = Cliente())
            return await cliente.DownloadStringTaskAsync(
                string.Format("{0}/{1}/{2}.svg", Api, partes[0], partes[1]));
    }

    void AdicionarResultado(string nome, string svg)
    {
        IconeVetorial icone;
        try
        {
            icone = IconeVetorial.Ler(svg);
        }
        catch (Exception)
        {
            return;
        }

        var botao = new Button
        {
            Size = new Size(LadoDoResultado, LadoDoResultado),
            AccessibleName = nome,
            FlatStyle = FlatStyle.Flat,
            BackColor = tema.Campo,
        };
        botao.Paint += (s, e) => icone.Desenhar(e.Graphics, Rectangle.Inflate(botao.ClientRectangle, -14, -14), tema.TextoBarra);
        botao.Click += (s, e) => Escolher(nome, svg);
        resultados.Controls.Add(botao);
    }

    void Escolher(string nome, string svg)
    {
        Directory.CreateDirectory(Icones.Pasta);
        Escolhido = Path.Combine(Icones.Pasta, nome.Replace(':', '-') + ".svg");
        File.WriteAllText(Escolhido, svg);
        DialogResult = DialogResult.OK;
        Close();
    }
}
