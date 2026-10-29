<h1 align="center">EasyPixel</h1>

<p align="center">
  <b>Um editor de pixel art gratuito para quem não é artista (ainda).</b><br>
  Camadas, animação, qualquer tamanho de tela, arquivos <code>.aseprite</code> — e conversa bem com a EasyEngine.
</p>

<p align="center">
  <a href="https://github.com/tarl3s/EasyPixel/releases/latest/download/EasyPixel-win-x64.zip">
    <img src="https://img.shields.io/badge/Baixar-Windows%2010%2F11-18B98A?style=for-the-badge&logo=windows&logoColor=white" alt="Baixar para Windows">
  </a>
  <br>
  <sub><a href="https://github.com/tarl3s/EasyPixel/releases/latest">Última versão e novidades</a> · <a href="README.md">Read in English</a></sub>
</p>

---

## O que é o EasyPixel

O EasyPixel é um editor de pixel art e sprites com as ferramentas que você espera do Aseprite e do Piskel, feito para ser
fácil de aprender. É **gratuito, de código aberto (MIT) e sem contas, anúncios ou planos pagos**. Por enquanto roda no
**Windows**; nada no código impede uma versão para macOS / Linux depois.

É irmão da [EasyEngine](https://github.com/tarl3s/EasyEngine) (uma engine de jogos 2D para quem não gosta de programar) e do
EasyVocal. Cada um funciona sozinho; juntos, trocam sprites com um clique.

## Começando

1. **[Baixe o EasyPixel](https://github.com/tarl3s/EasyPixel/releases/latest/download/EasyPixel-win-x64.zip)** (Windows 10/11, 64 bits).
2. Descompacte em qualquer lugar e abra o **`EasyPixel.exe`**. Não precisa instalar mais nada.
   - O Windows pode avisar *"O Windows protegeu o computador"* porque o app ainda não é assinado: clique em **Mais informações → Executar assim mesmo**.
3. **Arquivo → Novo…**, escolha um tamanho e comece a desenhar. **Ajuda → Atalhos de teclado** lista todas as teclas (as do Aseprite funcionam).

Quando sai uma versão nova, o EasyPixel pergunta se você quer e instala quando você fecha o programa. Seus sprites nunca
são tocados. Dá para desligar a verificação em **Editar → Configurações**.

## O que ele faz

- **Ferramentas de desenho** — lápis, borracha, balde (contíguo ou global), linha, retângulo, elipse (cheia ou só contorno),
  sombreamento, clarear / escurecer, pincel de dither, spray, conta-gotas, desenho espelhado (horizontal e vertical).
- **Seleção** — retângulo, elipse, laço, varinha mágica; mover, espelhar, girar, redimensionar, recortar, copiar, colar, inverter, selecionar por cor.
- **Camadas** — grupos, opacidade, modos de mesclagem, mostrar / ocultar, travar, reordenar.
- **Animação** — quadros com duração própria, linha do tempo, tags (loops com nome), reprodução em loop / vai-e-volta / invertida,
  onion skin, janela de pré-visualização e pré-visualização em tela cheia.
- **Qualquer tamanho de tela** — de 1x1 a milhares de pixels, com zoom, arrastar, grade de pixels, grade de tiles e pré-visualização em mosaico.
- **Cor** — modo RGBA ou indexado (paleta) como no Aseprite, painel de paleta, carregar / salvar paletas (`.gpl`, `.hex`, `.pal`),
  reduzir cores, contorno, pixelar.
- **Ferramentas do sprite** — redimensionar, tamanho da tela, girar, espelhar.
- **Desfazer / refazer** para tudo, inclusive mudanças de camadas e quadros.
- **Arquivos**
  - Abre e salva **`.aseprite` / `.ase`** (sem perdas para tudo que o EasyPixel modela; avisa antes de salvar se o arquivo tiver
    algo que ele não entende), **`.piskel`** e o formato próprio **`.epx`**.
  - Abre PNG e GIF animado; **Arquivo → Importar sprite sheet…** corta uma sprite sheet PNG em quadros.
  - Exporta **PNG**, **GIF** animado e **sprite sheet** (PNG + JSON, horizontal / vertical / grade).
- **Aprender pixel art** — **Ajuda → Aprender pixel art** abre 18 lições, do primeiro sprite até folhas prontas para
  o jogo: pixels e tamanho da tela, linhas e curvas, formas, paletas de cores, luz e sombra, contornos, anti-aliasing,
  pontilhado (dithering), texturas, animação, ciclo de caminhada, personagens, tilesets, cenários, prédios, itens e
  ícones, efeitos, e como levar os sprites para um jogo. Cada lição tem figuras desenhadas pelo próprio EasyPixel e um
  exercício **Experimente** que abre uma tela de prática; **Verificar** diz o que está certo e o que corrigir, e a lição
  aprovada ganha uma ★. Salve a prática e abra depois: a barra da lição volta. As lições estão em inglês e em português
  (Brasil); os outros idiomas mostram o texto em inglês.
- **9 idiomas** — inglês, alemão, espanhol, francês, japonês, português (Brasil e Portugal), chinês (simplificado e tradicional).
- **Temas escuro e claro.**

## Integração com a EasyEngine

Com os dois instalados, eles se encontram sozinhos:

- No EasyPixel: **Arquivo → Importar sprite do EasyEngine…** deixa escolher um projeto, um objeto e sua animação, e editar.
- No EasyPixel: **Arquivo → Exportar para o EasyEngine** salva o sprite com os quadros e a velocidade onde a EasyEngine encontra.
- Na janela **Animações** da EasyEngine: **Importar do EasyPixel** traz o resultado de volta.

O EasyPixel nunca escreve dentro de um projeto da EasyEngine — só lê para importar; o resultado volta pelo lado da EasyEngine.

## Limites conhecidos

- Por enquanto cada cel guarda a tela inteira, então telas muito grandes com muitos quadros usam bastante memória
  (um sprite 4096x4096 precisa de uns 64 MB por quadro e camada).

## Roteiro

1. Modo tilemap; importação de arquivos PSD, `.pyxel` e `.ora`.
2. **Geração com IA** — descreva um sprite, ou peça mudanças e mais quadros; provedores locais (grátis, no seu PC) e na nuvem,
   com resultado limpo em pixels de verdade, que você continua editando à mão.

## Compilando do código-fonte

Precisa do [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build                          # 0 erros esperados
dotnet test                           # testes portáveis + testes de interface sem janela
dotnet run --project src/EasyPixel    # abre o editor
powershell -ExecutionPolicy Bypass -File tools/Publish-Release.ps1 -DryRun   # build autocontido + zip, sem publicar
```

Um build rodando do código-fonte nunca procura atualizações — o código é a atualização.

## Licença

[MIT](LICENSE) — livre para todos.
