# Gera o TapBoard.ico a partir do desenho abaixo. Só é preciso rodar de novo
# se o desenho mudar; o build.cmd usa o .ico que já está no repositório.
$codigo = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class GeradorDeIcone
{
    // Medidas do desenho original, numa tela de 1092 x 1092.
    const float Tela = 1092f;
    static readonly Color Fundo = Color.FromArgb(0x1E, 0x1F, 0x23);
    static readonly Color Laranja = Color.FromArgb(0xF0, 0x8A, 0x28);
    static readonly Color LaranjaLateral = Color.FromArgb(0x9C, 0x52, 0x10);

    public static void Gerar(string destino)
    {
        int[] tamanhos = { 16, 20, 24, 32, 40, 48, 64, 256 };
        var imagens = new List<byte[]>();
        foreach (int tamanho in tamanhos)
            imagens.Add(tamanho >= 256 ? Png(Desenhar(tamanho)) : Dib(Desenhar(tamanho)));

        using (var saida = new BinaryWriter(File.Create(destino)))
        {
            saida.Write((short)0);
            saida.Write((short)1);
            saida.Write((short)tamanhos.Length);
            int deslocamento = 6 + 16 * tamanhos.Length;
            for (int i = 0; i < tamanhos.Length; i++)
            {
                saida.Write((byte)(tamanhos[i] >= 256 ? 0 : tamanhos[i]));
                saida.Write((byte)(tamanhos[i] >= 256 ? 0 : tamanhos[i]));
                saida.Write((byte)0);
                saida.Write((byte)0);
                saida.Write((short)1);
                saida.Write((short)32);
                saida.Write(imagens[i].Length);
                saida.Write(deslocamento);
                deslocamento += imagens[i].Length;
            }
            foreach (byte[] imagem in imagens)
                saida.Write(imagem);
        }
    }

    static Bitmap Desenhar(int tamanho)
    {
        // Em tamanho pequeno o cinza some no fundo escuro: clareia as teclas.
        bool pequeno = tamanho <= 32;
        Color face = pequeno ? Color.FromArgb(0x78, 0x7E, 0x88) : Color.FromArgb(0x4A, 0x4F, 0x57);
        Color lateral = pequeno ? Color.FromArgb(0x4C, 0x50, 0x59) : Color.FromArgb(0x33, 0x36, 0x3E);

        var imagem = new Bitmap(tamanho, tamanho, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(imagem))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.ScaleTransform(tamanho / Tela, tamanho / Tela);

            Preencher(g, Fundo, 0, 0, 1092, 1092, 240);

            Tecla(g, face, lateral, 153, 153, 419, 475);
            Tecla(g, face, lateral, 588, 153, 419, 475);
            Tecla(g, face, lateral, 153, 561, 826, 882);
            Tecla(g, Laranja, LaranjaLateral, 588, 561, 826, 882);
        }
        return imagem;
    }

    static void Tecla(Graphics g, Color face, Color lateral, float x, float topo, float baseDaFace, float baseDaLateral)
    {
        Preencher(g, lateral, x, topo, 349, baseDaLateral - topo, 85);
        Preencher(g, face, x, topo, 349, baseDaFace - topo, 85);
    }

    static void Preencher(Graphics g, Color cor, float x, float y, float largura, float altura, float raio)
    {
        float d = raio * 2;
        using (var forma = new GraphicsPath())
        using (var pincel = new SolidBrush(cor))
        {
            forma.AddArc(x, y, d, d, 180, 90);
            forma.AddArc(x + largura - d, y, d, d, 270, 90);
            forma.AddArc(x + largura - d, y + altura - d, d, d, 0, 90);
            forma.AddArc(x, y + altura - d, d, d, 90, 90);
            forma.CloseFigure();
            g.FillPath(pincel, forma);
        }
    }

    static byte[] Png(Bitmap imagem)
    {
        using (var memoria = new MemoryStream())
        {
            imagem.Save(memoria, ImageFormat.Png);
            return memoria.ToArray();
        }
    }

    // Formato clássico do .ico (BMP de 32 bits de baixo para cima, mais a máscara),
    // que o System.Drawing.Icon do .NET Framework lê sem problemas.
    static byte[] Dib(Bitmap imagem)
    {
        int n = imagem.Width;
        int linhaDaMascara = ((n + 31) / 32) * 4;
        using (var memoria = new MemoryStream())
        using (var saida = new BinaryWriter(memoria))
        {
            saida.Write(40);
            saida.Write(n);
            saida.Write(n * 2);
            saida.Write((short)1);
            saida.Write((short)32);
            saida.Write(0);
            saida.Write(n * n * 4 + linhaDaMascara * n);
            saida.Write(0);
            saida.Write(0);
            saida.Write(0);
            saida.Write(0);
            for (int y = n - 1; y >= 0; y--)
                for (int x = 0; x < n; x++)
                {
                    Color c = imagem.GetPixel(x, y);
                    saida.Write(c.B);
                    saida.Write(c.G);
                    saida.Write(c.R);
                    saida.Write(c.A);
                }
            for (int y = n - 1; y >= 0; y--)
            {
                var linha = new byte[linhaDaMascara];
                for (int x = 0; x < n; x++)
                    if (imagem.GetPixel(x, y).A == 0)
                        linha[x / 8] |= (byte)(0x80 >> (x % 8));
                saida.Write(linha);
            }
            return memoria.ToArray();
        }
    }
}
'@
Add-Type -TypeDefinition $codigo -ReferencedAssemblies System.Drawing
[GeradorDeIcone]::Gerar((Join-Path $PSScriptRoot 'TapBoard.ico'))
