# TapBoard — painel de botões para monitores touch, em AutoHotkey v2

Painel de botões feito para monitores de toque. Roda no Windows, aberto em tela
cheia num monitor portátil touch que fica na mesa como segunda tela. Tocar num botão
executa uma ação (brilho, volume, mídia, atalhos, apps) **sem tirar o foco do
app que está no monitor principal**.

## Hardware (contexto para decisões de layout e DPI)

- **PC:** AMD Ryzen 5 7600, RTX 5060 Ti 16GB, MSI Pro X870E, Windows
- **Monitor principal:** Samsung Odyssey G5 34" ultrawide, 3440x1440 @ 165Hz.
  O brilho é controlado via DDC/CI pelo Twinkle Tray.
- **Monitor do deck:** Antuvi 15,6" touch 10 pontos, 1920x1080 IPS @ 60Hz.
  Vídeo por mini HDMI; o toque chega ao PC pelo USB-C (HID touch).
- Os dois monitores podem ter escala de DPI diferentes no Windows. O deck deve
  se posicionar no monitor touch pelo índice ou pelas coordenadas do monitor,
  nunca por posição fixa em pixels.

## Stack

- **AutoHotkey v2** (somente v2; nada de sintaxe v1).
- **Twinkle Tray** pela linha de comando para o brilho do Odyssey.
- Um único script principal: `TapBoard.ahk` (antes se chamava `MiniDeck.ahk`).
- Não usar "Stream Deck" no nome, na interface nem na documentação: é marca
  registrada da Elgato.

## Arquitetura atual

- Janela `Gui` sem borda, em tela cheia no monitor do deck, `AlwaysOnTop`.
- **Não roubar foco é o requisito central.** A janela usa `WS_EX_NOACTIVATE`
  (`+E0x08000000`). Qualquer mudança precisa manter isso funcionando para
  **mouse e toque**, e isso inclui:
  - não chamar `WinActivate`, `Gui.Show()` sem `NA`, nem nada que ative a janela;
  - mostrar a janela com `Show("NA")`;
  - se o toque ativar a janela, tratar `WM_MOUSEACTIVATE` (0x21) e/ou
    `WM_POINTERACTIVATE` (0x24B) retornando `MA_NOACTIVATE` (3);
  - ações que enviam teclas (`Send`) devem chegar ao app em foco no monitor
    principal.
- **Botões são definidos numa lista no topo de `TapBoard.ahk`.** Cada item tem
  rótulo, cor e ação. Adicionar um botão novo deve exigir só uma linha nessa
  lista. A lógica de layout (grade) e de desenho fica separada da lista.

## Funções atuais

| Grupo   | Ações |
|---------|-------|
| Brilho do Odyssey | −10, +10, 15%, 50%, 100% (Twinkle Tray) |
| Áudio   | volume +/−, mudo |
| Mídia   | play/pause, anterior, próxima |
| Sistema | recorte de tela, Explorer, VS Code, Gerenciador de Tarefas, bloquear PC |

## Roadmap (ideias futuras, por ordem aproximada)

1. Páginas e pastas de botões (navegação dentro do deck)
2. Ícones nos botões (PNG/ICO, além de rótulo e cor)
3. Slider de brilho arrastável pelo toque
4. Perfis por app em foco (troca de página conforme o executável ativo no monitor principal)
5. Mutar o microfone com indicador de estado no botão
6. Controle de OBS (WebSocket) e Discord

## Regras para o Claude neste projeto

- **Manter tudo em AutoHotkey v2.** Não propor reescrever em outra linguagem
  nem adicionar dependências sem perguntar antes.
- **Preservar o comportamento de não roubar foco** em toda alteração. Se uma
  mudança puder afetá-lo, avisar explicitamente e explicar como testar.
- **Entregar o script completo atualizado**, não só trechos ou diffs soltos,
  quando eu pedir uma mudança no `TapBoard.ahk`.
- Manter a lista de botões no topo, legível e fácil de editar à mão.
- Comentários e mensagens de interface em português.
- Preferir APIs nativas do AHK v2 e chamadas `DllCall` documentadas a hacks
  com timers ou `Sleep`.
- Caminhos de executáveis externos (Twinkle Tray, VS Code etc.) ficam em
  variáveis de configuração no topo do script, perto da lista de botões.

## Como testar

O Claude não consegue tocar na tela, então o teste é manual:

1. Rodar `TapBoard.ahk` e confirmar que o deck abre no monitor touch, em tela cheia.
2. Deixar um editor de texto em foco no monitor principal, tocar em vários
   botões e confirmar que o cursor continua piscando no editor.
3. Testar com o mouse e com o dedo, porque os dois caminhos de ativação são diferentes.
4. Testar cada botão novo individualmente.
