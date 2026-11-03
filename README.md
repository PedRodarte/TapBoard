# TapBoard — painel de botões para monitores touch, sem instalar nada

Painel de botões feito para monitores de toque. Roda no Windows numa janela comum,
podendo ir para tela cheia num monitor portátil touch que fica na mesa como
segunda tela. Tocar num botão executa uma ação (brilho, volume, mídia, atalhos, apps) **sem
tirar o foco do app que está no monitor principal**.

## Como usar

1. Dê dois cliques em `build.cmd`. Ele compila o `TapBoard.cs` com o compilador
   que já vem no Windows, gera o `TapBoard.exe` e abre o painel.
2. Depois disso, basta abrir o `TapBoard.exe`.
3. Para escolher os botões, use **Arquivo > Escolher botões…** e marque na lista.
   Para mudar ícones, fonte, tema ou monitor, toque na engrenagem no canto do painel.
   Nada disso exige recompilar nem mexer em arquivos.

Ao abrir, o programa cria ou atualiza o próprio atalho no menu Iniciar.

Quem usa o TapBoard não precisa abrir pastas nem editar arquivos: os botões
prontos e os ícones deles vêm dentro do `.exe`. Por baixo, as configurações ficam em
`%APPDATA%\TapBoard\configuracoes.xml`, e só os ícones buscados na internet ou
escolhidos de um arquivo são copiados para `%APPDATA%\TapBoard\icones`. Se você mover o `TapBoard.exe` com a
opção de iniciar com o Windows ativa, abra-o uma vez no novo lugar.

**Exibir > Tela cheia** alterna o modo. Em janela, valem os controles
comuns do Windows: barra de título, bordas, maximizar, minimizar e enquadramento.
Para sair, use o ✕ da janela ou o ícone do TapBoard na bandeja do sistema.

O painel abre no tema do Windows até você escolher outro em **Exibir**. A escolha
fica salva.

## Hardware (contexto para decisões de layout e DPI)

- **PC:** AMD Ryzen 5 7600, RTX 5060 Ti 16GB, MSI Pro X870E, Windows
- **Monitor principal:** Samsung Odyssey G5 34" ultrawide, 3440x1440 @ 165Hz.
- **Monitor do deck:** Antuvi 15,6" touch 10 pontos, 1920x1080 IPS @ 60Hz.
  Vídeo por mini HDMI; o toque chega ao PC pelo USB-C (HID touch).
- O brilho do Odyssey é controlado via DDC/CI pela API do Windows (`dxva2.dll`).
  O Antuvi aceita o comando, mas a luz sobe e desce fora de ordem. Por isso o deck
  escurece, por padrão, com uma película preta que deixa passar o toque (10% a 100%, compensando o gama da tela). Nesse modo, o
  TapBoard mantém o brilho do próprio Antuvi em 100, para a película partir sempre
  da mesma base.
- Os dois monitores podem ter escala de DPI diferentes no Windows. O deck deve
  se posicionar no monitor touch pelo índice ou pelas coordenadas do monitor,
  nunca por posição fixa em pixels.

## Stack

- **C# 5 com Windows Forms**, compilado pelo `csc.exe` do .NET Framework, que já
  vem no Windows. Recursos de C# 6 em diante não compilam.
- **Sem dependências externas:** nada de instalar, baixar ou usar pacotes.
- Distribuição: só o `TapBoard.exe`, sem instalador e sem cadastro. O executável
  é gerado e fica fora do Git.
- Código: `TapBoard.cs` (painel e ações), `Configuracoes.cs` (dados salvos e janela
  de configurações), `Catalogo.cs` (botões prontos e a janela "Adicionar botões"),
  `Icones.cs` (leitor de SVG e busca de ícones) e `IconesEmbutidos.cs` (os SVGs que
  vêm dentro do programa).
- Ícones embutidos: [Material Design Icons](https://pictogrammers.com/library/mdi/),
  licença Apache 2.0. Na configuração, aparecem como `embutido:mdi-<nome>`. Para
  incluir outro, acrescente o SVG da Iconify (`https://api.iconify.design/mdi/<nome>.svg`)
  em `IconesEmbutidos.cs`.
- O contador de notificações lê, por UI Automation (`UIAutomationClient.dll`, que
  vem no Windows, na pasta `WPF` do .NET Framework), o texto que cada app deixa no
  seu botão da barra de tarefas, o mesmo que os leitores de tela leem. Sem login e
  sem internet. Se a Microsoft mudar esse texto, o contador some, e o botão continua
  funcionando.
- O ícone do programa é o `TapBoard.ico`, embutido no .exe pelo `build.cmd`. Ele é
  gerado pelo `gerar-icone.ps1`, que só precisa rodar de novo se o desenho mudar.
- Internet só na busca de ícones, pela API gratuita da [Iconify](https://iconify.design),
  sem chave. A busca fica limitada às coleções Lucide (ISC), Tabler e Phosphor
  (MIT) e Material Design Icons (Apache 2.0).
- A busca aceita português: termos comuns são traduzidos por uma lista em
  `Icones.cs`, e os demais pela API gratuita [MyMemory](https://mymemory.translated.net),
  sem chave. Só o termo buscado é enviado, e sem tradução a busca usa o termo original.
- Não usar "Stream Deck" no nome, na interface nem na documentação: é marca
  registrada da Elgato.

## Arquitetura atual

- Janela comum do Windows, com barra de título, bordas, maximizar, enquadramento
  e ícone na barra de tarefas. Ficar sempre visível é opção nas configurações.
  Sem o monitor do deck, abre em janela no monitor principal, para não cobri-lo.
- **Tocar nos botões não tira o foco do app em uso**, para mouse e toque. Isso inclui:
  - tratar `WM_MOUSEACTIVATE` e `WM_POINTERACTIVATE` retornando "não ativar" (3)
    só quando o toque cai na área dos botões (`HTCLIENT`);
  - não usar controles filhos: os botões são desenhados na própria janela;
  - ações que enviam teclas devem chegar ao app em foco no monitor principal.
- Barra de título e bordas ativam a janela, como em qualquer programa. Depois de
  usá-las, os atalhos do painel só voltam ao outro app quando ele recebe o foco de novo.
- A janela de configurações recebe foco de propósito, porque é onde se digita.
- **Botões ficam salvos nas configurações.** Cada item tem rótulo, cor, ação,
  parâmetro, ícone, largura e, nos de brilho, o monitor. Os botões prontos ficam em
  `Catalogo.cs`; a lista da primeira vez (`Padrao`, no topo de `TapBoard.cs`) só
  escolhe alguns deles pelo rótulo.
- A grade segue a ideia do Bootstrap: 12 colunas fixas, com cada botão ocupando 3,
  6, 9 ou 12 e descendo para a linha seguinte quando não cabe.
- Ícones SVG são desenhados pelo próprio TapBoard e assumem a cor do texto do
  botão. As cores do arquivo são ignoradas.
- O toque é tratado direto em `WM_POINTER*`, e o mouse passa pelos mesmos métodos.

## Funções atuais

| Grupo   | Ações |
|---------|-------|
| Brilho  | −10, +10, 15%, 50%, 100% (DDC/CI), em todos os monitores, só no principal ou só no do deck |
| Áudio   | volume +/−, mudo, mudo do microfone |
| Mídia   | play/pause, anterior, próxima |
| Sistema | recorte de tela, Explorer, VS Code, Gerenciador de Tarefas, bloquear PC |

No topo do painel fica uma linha só, como a do VS Code: ícone, menus Arquivo
(Escolher botões, Sair), Exibir (tela cheia, sempre visível, tema) e Ajuda, o
título, a engrenagem das configurações e, em janela, minimizar, maximizar e fechar.
A barra de título do Windows sai (`WM_NCCALCSIZE`); o vazio da linha arrasta a
janela, duplo clique maximiza e a borda de cima redimensiona (`WM_NCHITTEST`). Tocar na barra não tira o foco do app em
uso; abrir um menu ativa o painel, e ao fechar o foco volta para o app. As ações
de alternar janela, alternar tema e abrir configurações continuam disponíveis
para quem quiser um botão.

Botões que abrem um programa trazem para a frente a janela dele, se já estiver
aberto (menos o Explorer, que abre uma janela nova). Eles também mostram um
contador vermelho quando o app tem notificações na barra de tarefas, como o Teams
ou o WhatsApp.

### Ações genéricas

Três ações cobrem a maior parte dos programas: num levantamento de 124 programas
para Windows, atalho de teclado, abrir endereço e rodar comando servem para 88%
deles, com peso pela prioridade de cada um.

| Ação | O que pôr no parâmetro | Exemplos |
|------|------------------------|----------|
| Atalho de teclado | Teclas unidas por `+`. Várias combinações separadas por espaço viram uma sequência. | `Ctrl+Shift+M` (mutar no Teams), `Alt+F10` (replay da NVIDIA), `Win+.` (emojis), `Ctrl+K Ctrl+S` |
| Digitar texto | O texto, digitado letra por letra no app em foco. | assinatura, e-mail, resposta pronta |
| Abrir programa, arquivo ou site | Caminho, site ou URI. | `ms-settings:sound`, `steam://rungameid/<ID>`, `obsidian://`, `https://docs.new` |
| Rodar comando | Uma linha do cmd, rodada sem janela, a partir da pasta do usuário. Se terminar com erro, aparece um aviso na bandeja. | `wt -p PowerShell -d C:\AIProjects`, `cd /d C:\AIProjects\Blog && git pull`, `wsl --shutdown` |

No atalho valem letras, números, pontuação, `F1` a `F24` e os nomes `Ctrl`,
`Shift`, `Alt`, `Win`, `Esc`, `Enter`, `Tab`, `Espaço`, `Backspace`, `Del`, `Ins`,
`Home`, `End`, `PgUp`, `PgDn`, `PrtSc`, `Cima`, `Baixo`, `Esquerda` e `Direita`.
Atalho e texto vão para o app em foco, porque o painel nunca toma o foco.

### Escolher botões

**Arquivo > Escolher botões…** (ou **Escolher…** nas configurações) abre a lista
com tudo o que o TapBoard sabe fazer, com uma caixa na frente de cada item: marcado
é estar no painel. Marcar põe o botão no fim; desmarcar tira. Pelo menu, vale na
hora. Tem busca no topo e duas abas:

- **Prontos** (160+ itens): som e microfone, brilho, mídia, atalhos do Windows
  (recorte, gravar a tela, áreas de trabalho virtuais, encostar janelas, lupa,
  emojis…), pastas e páginas das Configurações do Windows (Wi-Fi, Bluetooth, som,
  tela…), apps do Windows, programas instalados (detectados pelo registro: Chrome,
  Office, Teams, Zoom, Spotify, Discord, OBS, Steam…), edição, navegador, reuniões
  (Teams, Zoom, Meet, Discord), atalhos do VS Code, Excel e PowerPoint quando
  instalados, reações (👍 ❤️ 😂) e energia (desligar ou reiniciar em 1 minuto, com
  cancelar). Pares como Volume − / + são um item só e viram uma tecla partida.
- **Macros e personalizados**: modelos de atalho, sequência de teclas, texto,
  programa, site, comando e brilho. Tocar num deles cria o botão e abre o editor.

### Grade

No topo de Configurações > Geral fica o tamanho da grade, em colunas × linhas
(4 × Auto por padrão). Cada um aceita de 1 a 10 ou "Auto". Colunas em Auto ligam a
grade fluida: cabem tantos botões por linha quantos a "Largura mínima" permitir,
então um painel mais largo ganha colunas e um mais estreito perde. Linhas em Auto
são só as que os botões precisam; com um número, sobram linhas vazias para botões
novos. Se os botões não couberem, o painel ganha as linhas que faltarem. A largura de cada botão é contada em casas
(meia, 1, 2 ou 3) ou "Linha inteira". Dois botões de meia casa seguidos formam
uma tecla partida ao meio, sem espaço nem cantos arredondados entre as metades, e
botões de meia casa com ícone mostram só o ícone. O texto e o ícone diminuem se o botão ficar estreito.

## Roadmap (ideias futuras, por ordem aproximada)

1. Modelos de cores prontos para escolher nas configurações: Escuro (botões pretos
   com um detalhe de cor por grupo), Terrosa (a atual) e Neon (todas as cores vivas)
2. Mais itens no catálogo (OBS: gravar; Steam: abrir um jogo; pastas favoritas)
3. Vários decks em abas na barra do topo, como as abas do Bloco de Notas, e
   pastas de botões dentro de um deck
4. Multi-ação: vários passos em sequência, com atraso entre eles
5. Slider de brilho arrastável pelo toque
6. Perfis por app em foco (troca de página conforme o executável ativo no monitor principal)
7. Controle de OBS (WebSocket) e Discord

## Regras para o Claude neste projeto

- **Manter tudo em C# 5 com o que já vem no Windows.** Não propor reescrever em
  outra linguagem nem adicionar dependências sem perguntar antes.
- **Manter os controles comuns do Windows.** Não trocar barra, bordas ou arraste
  por versões desenhadas à mão para proteger o foco: foi decisão do usuário.
- **Tocar nos botões não pode tirar o foco do app em uso.** Se uma mudança puder
  afetar isso, avisar explicitamente e explicar como testar.
- **Manter o projeto completo e compilando** a cada mudança, não só trechos ou
  diffs soltos.
- Tudo que o usuário puder ajustar deve ficar na janela de configurações, não no código.
- Não adicionar login nem conta: o TapBoard roda só no PC, sem servidor.
- Comentários e mensagens de interface em português.
- Preferir APIs nativas do Windows, com `DllImport` documentado, a hacks com
  timers ou `Sleep`.
- Caminhos de executáveis externos (VS Code etc.) ficam no parâmetro do botão.

## Como testar

O Claude não consegue tocar na tela, então o teste com o dedo é manual:

1. Rodar `build.cmd` e confirmar que o deck abre em janela no monitor touch.
2. Deixar um editor de texto em foco no monitor principal, tocar em vários
   botões e confirmar que o cursor continua piscando no editor.
3. Testar com o mouse e com o dedo, porque os dois caminhos de ativação são diferentes.
4. No modo janela, arrastar, redimensionar, maximizar, enquadrar e alternar para
   tela cheia. Usar a barra ativa o TapBoard; depois de voltar ao editor, os botões
   não devem tirar o foco dele.
5. Abrir as configurações, alterar um botão, salvar e reabrir o TapBoard para
   confirmar que a alteração continua lá.
6. Testar cada botão novo individualmente.
