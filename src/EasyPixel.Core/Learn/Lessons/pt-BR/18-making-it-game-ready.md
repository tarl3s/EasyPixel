# Deixando pronto para o jogo
group: styles
summary: Folhas de sprites, tags para cada animação, quadros alinhados e como enviar sua arte ao EasyEngine com Importar do EasyPixel.

## Do desenho ao jogo
Um sprite que fica ótimo no editor ainda precisa entrar no jogo de um jeito que o jogo entenda. Uma engine precisa saber: **quais imagens** formam cada animação, **em que ordem**, **com que velocidade** e **onde o personagem fica** em cada quadro. Esta lição cobre os hábitos que tornam essa etapa tranquila, e o caminho direto do EasyPixel para o EasyEngine.

## Um arquivo por personagem, uma tag por animação
Mantenha todas as animações de um personagem em **um só documento**: idle, caminhada, pulo, ataque, dano, uma depois da outra na linha do tempo. Depois dê nome a cada animação com uma **tag**: uma faixa de quadros com nome.

Para criar uma tag, escolha seus quadros na linha do tempo (clique no primeiro, Shift+clique no último) e use **Quadro → Nova tag com os quadros escolhidos…**. Dê a ela o nome que o jogo vai usar: **idle**, **walk**, **jump**, **attack**. Nomes curtos, em minúsculas e sem espaços são o hábito mais seguro, porque esses nomes vão parar no código e nas configurações do jogo.

[[example:sheet-tags]] Um documento com duas animações: os quadros 1–2 com a tag "idle" (verde), os quadros 3–6 com a tag "walk" (azul).

Uma tag também tem uma **direção** (clique com o botão direito na tag na linha do tempo): Para frente, Ao contrário ou Vai e volta (para frente e depois para trás, sem repetir as pontas — perfeito para um idle que respira). Todo quadro deve pertencer a uma tag; quadros sobrando costumam ser testes esquecidos.

> Mantenha o mesmo tamanho de tela em todas as animações de um personagem. Se o ataque precisa de uma espada saindo para fora, faça a tela inteira larga o bastante para ela, e mantenha o personagem no mesmo lugar em todos os quadros.

## Quadros alinhados
No jogo, a engine coloca todo quadro do sprite na **mesma posição**. Se os pés do seu personagem estão na linha 20 num quadro e na linha 21 no seguinte, o personagem vai tremer para cima e para baixo enquanto a animação toca, como se o chão estivesse sacudindo.

[[example:frames-aligned-good]] Alinhados: os pés ficam na mesma linha em todos os quadros, então a caminhada fica no lugar.
[[example:frames-aligned-bad]] Desalinhados: cada quadro está deslocado um pixel, então quem anda treme e desliza.

Regras de alinhamento:

- Escolha uma **linha do chão** para o personagem inteiro e mantenha o pé mais baixo nela em todos os quadros, de todas as animações (exceto pulos, em que sair do chão é justamente a ideia).
- Mantenha o centro do corpo sobre a mesma coluna. Um ciclo de caminhada anda **no lugar**; é o jogo que move o sprite.
- Quando você copia um quadro para começar o próximo (**Quadro → Novo quadro**), a posição se mantém de graça. Use o papel vegetal (onion skin) para conferir.

! Desenhar cada animação numa tela de tamanho diferente, ou com o personagem em outro lugar. No jogo, ao passar de idle para caminhada o personagem dá um pulo para o lado. Um tamanho de tela, uma linha do chão, um centro para todas as animações.

## Durações e velocidade
Cada quadro tem a sua própria duração (**Quadro → Duração do quadro…**). Muitas engines, e as animações simples do EasyEngine, tocam uma animação em **uma só velocidade** (quadros por segundo). Ao exportar, o EasyPixel transforma a duração média dos quadros da tag numa velocidade: 100 ms por quadro vira 10 fps. Se a sua animação depende de pausas (um primeiro quadro longo), confira como ela fica no jogo; você pode repetir um quadro para fazer uma pausa que sobreviva a uma velocidade fixa.

## Folhas de sprites (sprite sheets)
Muitas engines querem uma **folha de sprites (sprite sheet)**: todos os quadros organizados numa única imagem, em uma fileira ou numa grade, mais uma descrição de onde fica cada quadro. **Arquivo → Exportar…** → **Folha de sprites (PNG + JSON)** grava os dois: o PNG com os quadros e um arquivo JSON com a posição de cada quadro, a duração e as suas tags.

[[example:sheet-layout]] Uma folha de sprites horizontal: quatro quadros de caminhada lado a lado, cada um numa célula do tamanho da tela.

As opções de exportação:

- **Disposição**: Horizontal (uma fileira), Vertical (uma coluna) ou Grade (fileiras com um número fixo de **Colunas**).
- **Espaçamento**: pixels vazios entre os quadros. Algumas engines borram ou "vazam" quadros vizinhos uns nos outros ao escalar a folha; 1–2 pixels de espaçamento evitam isso.

Outras escolhas de exportação em **Arquivo → Exportar…**: um PNG único do quadro atual (para um objeto parado, como um tile ou o prédio da escola), ou um **GIF animado** para compartilhar sua animação.

## Exportando para o EasyEngine
Se você faz o seu jogo com o **EasyEngine**, há um atalho: sem escolher arquivos, sem recortar folhas.

1. No EasyPixel, termine o sprite e as tags dele, e salve (**Arquivo → Salvar**).
2. Use **Arquivo → Exportar para o EasyEngine**. O EasyPixel grava **uma exportação por tag** (ou uma para a linha do tempo toda, se não houver tags): os quadros como imagens e a velocidade deles, na pasta de exportação do EasyPixel. Ele avisa o que exportou.
3. No EasyEngine, selecione o objeto, abra a janela **Animações** dele e clique em **Importar do EasyPixel**.
4. Escolha a exportação na lista (ela mostra o nome, o número de quadros, a velocidade e a hora).
5. Escolha para onde os quadros vão: **Adicionar a** uma animação que você está editando, ou **Nova animação** com o nome da tag.

As suas tags **idle** e **walk** viram animações do EasyEngine com os quadros e a velocidade certos. Para mudar a arte depois, edite o mesmo documento no EasyPixel, exporte de novo e importe de novo.

O EasyPixel também faz o caminho inverso: **Arquivo → Importar sprite do EasyEngine…** abre um sprite de um projeto do EasyEngine para você melhorá-lo. O EasyPixel nunca altera o projeto do EasyEngine; ele só exporta, e o EasyEngine importa.

## Uma lista de conferência antes de exportar
- Todo quadro pertence a uma tag, e cada tag tem um nome claro.
- Os pés estão na mesma linha do chão em todos os quadros; os loops se repetem sem salto.
- O tamanho da tela é o mesmo em todas as animações do personagem.
- Camadas escondidas ou auxiliares (rascunhos, guias) estão ocultas ou apagadas: as exportações juntam as camadas **visíveis**.
- Você olhou cada animação em 1× em **Quadro → Janela de prévia**.

## Experimente
[[exercise:game-ready]]
O documento tem o **idle** de uma gosma (quadros 1–2) e a **caminhada** de um herói (quadros 3–6). Deixe-o pronto para o jogo: crie **duas tags** — uma para os quadros do idle e outra para os da caminhada — com nomes claros, para que **todo quadro pertença a uma tag**. Escolha os quadros na linha do tempo e use **Quadro → Nova tag com os quadros escolhidos…**. Mantenha os pés na linha do chão.
- [ ] Minhas tags têm os nomes que o jogo vai usar (como idle e walk).
- [ ] Tentei Arquivo → Exportar para o EasyEngine (ou Arquivo → Exportar… → Folha de sprites (PNG + JSON)) e olhei o que ele gravou.
