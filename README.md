# TapBoard — painel de botões para monitores touch, sem instalar nada

Painel de botões feito para monitores de toque. Roda no Windows e abre em janela,
podendo ir para tela cheia num monitor portátil touch que fica na mesa como
segunda tela. Tocar num botão executa uma ação (brilho, volume, mídia, atalhos, apps) **sem
tirar o foco do app que está no monitor principal**.

## Como usar

1. Dê dois cliques em `build.cmd`. Ele compila o `TapBoard.cs` com o compilador
   que já vem no Windows, gera o `TapBoard.exe` e abre o painel.
2. Depois disso, basta abrir o `TapBoard.exe`.
3. Para mudar botões, ícones, fonte, tema ou monitor, abra **Configurações** pelo
   botão do painel, pela engrenagem da janela ou pelo ícone na bandeja do sistema.
   Nada disso exige recompilar.

As configurações ficam em `%APPDATA%\TapBoard\configuracoes.xml`, e os ícones
baixados, em `%APPDATA%\TapBoard\icones`. Se você mover o `TapBoard.exe` com a
opção de iniciar com o Windows ativa, abra-o uma vez no novo lugar.

O botão **Janela / Tela cheia** alterna o modo. Em janela, arraste pela barra
superior e redimensione pelo canto inferior direito. Para sair, use o ✕ da
janela ou o ícone do TapBoard na bandeja do sistema.

O painel abre no tema do Windows até você escolher outro: o botão **Tema claro /
escuro** alterna na hora e a escolha fica salva.

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
  de configurações) e `Icones.cs` (leitor de SVG e busca de ícones).
- Internet só na busca de ícones, pela API gratuita da [Iconify](https://iconify.design),
  sem chave. A busca fica limitada às coleções Lucide (ISC), Tabler e Phosphor
  (MIT) e Material Design Icons (Apache 2.0).
- A busca aceita português: termos comuns são traduzidos por uma lista em
  `Icones.cs`, e os demais pela API gratuita [MyMemory](https://mymemory.translated.net),
  sem chave. Só o termo buscado é enviado, e sem tradução a busca usa o termo original.
- Não usar "Stream Deck" no nome, na interface nem na documentação: é marca
  registrada da Elgato.

## Arquitetura atual

- Janela sem borda do Windows, sempre no topo. Sem o monitor do deck, abre em
  janela no monitor principal, para não cobri-lo.
- **Não roubar foco é o requisito central.** A janela usa `WS_EX_NOACTIVATE`.
  Qualquer mudança precisa manter isso funcionando para **mouse e toque**, e
  isso inclui:
  - manter `ShowWithoutActivation` e tratar `WM_MOUSEACTIVATE` e
    `WM_POINTERACTIVATE` retornando "não ativar" (3);
  - usar `WS_EX_TOPMOST` em `CreateParams`, nunca a propriedade `TopMost`, que
    ignora `ShowWithoutActivation`;
  - não usar controles filhos: os botões são desenhados na própria janela;
  - não usar a barra de título nem a borda do Windows: o laço de arraste do
    sistema ativa a janela. Por isso a barra, o ✕ e a alça são desenhados pelo
    TapBoard, que move e redimensiona a janela por conta própria;
  - ações que enviam teclas devem chegar ao app em foco no monitor principal.
- O Snap do Windows não atua numa janela que nunca recebe foco. Por isso, arrastar
  o painel até o topo do monitor abre o seletor próprio (metades, terços, quartos).
- A janela de configurações é a exceção: ela recebe foco de propósito, porque é
  onde se digita. Ela não pode ser usada como caminho para ativar o painel.
- **Botões ficam salvos nas configurações.** Cada item tem rótulo, cor, ação,
  parâmetro, ícone, largura e, nos de brilho, o monitor. A lista inicial fica em `Padrao`, no topo de
  `TapBoard.cs`, e a lógica de layout e de desenho fica separada dela.
- A grade segue a ideia do Bootstrap: 12 colunas fixas, com cada botão ocupando 3,
  6, 9 ou 12 e descendo para a linha seguinte quando não cabe.
- Ícones SVG são desenhados pelo próprio TapBoard e assumem a cor do texto do
  botão. As cores do arquivo são ignoradas.
- O toque é tratado direto em `WM_POINTER*`, e o mouse passa pelos mesmos métodos.

## Funções atuais

| Grupo   | Ações |
|---------|-------|
| Brilho  | −10, +10, 15%, 50%, 100% (DDC/CI), em todos os monitores, só no principal ou só no do deck |
| Áudio   | volume +/−, mudo |
| Mídia   | play/pause, anterior, próxima |
| Sistema | recorte de tela, Explorer, VS Code, Gerenciador de Tarefas, bloquear PC |
| Painel  | alternar entre janela e tela cheia, alternar tema claro e escuro |

## Roadmap (ideias futuras, por ordem aproximada)

1. Páginas e pastas de botões (navegação dentro do deck)
2. Slider de brilho arrastável pelo toque
3. Perfis por app em foco (troca de página conforme o executável ativo no monitor principal)
4. Mutar o microfone com indicador de estado no botão
5. Controle de OBS (WebSocket) e Discord

## Regras para o Claude neste projeto

- **Manter tudo em C# 5 com o que já vem no Windows.** Não propor reescrever em
  outra linguagem nem adicionar dependências sem perguntar antes.
- **Preservar o comportamento de não roubar foco** em toda alteração. Se uma
  mudança puder afetá-lo, avisar explicitamente e explicar como testar.
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
4. No modo janela, arrastar, redimensionar e alternar para tela cheia, confirmando
   que o foco não sai do editor.
5. Abrir as configurações, alterar um botão, salvar e reabrir o TapBoard para
   confirmar que a alteração continua lá.
6. Testar cada botão novo individualmente.
