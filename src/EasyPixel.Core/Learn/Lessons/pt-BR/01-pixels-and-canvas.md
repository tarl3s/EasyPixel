# Pixels e tamanho da tela
group: fundamentals
summary: A grade, como escolher 16, 32 ou 64 pixels, o zoom e como avaliar seu trabalho em 1×.

## Cada pixel é uma decisão
Pixel art é uma imagem feita em uma grade pequena, onde você coloca cada quadradinho à mão. Essa é toda a diferença para a pintura: numa pintura, uma pincelada cobre milhares de pixels e ninguém olha para um deles em particular. Na pixel art, um único pixel pode ser um olho, um brilho na espada ou a quina de um telhado. Mude esse pixel e o sprite inteiro muda.

Isso parece assustador, mas é uma boa notícia. Um sprite de 16×16 tem só 256 quadradinhos. Você consegue olhar para cada um deles, perguntar "esse pixel ajuda?" e corrigir. Nada fica escondido. Esta primeira lição trata da própria grade: qual tamanho dar à tela, como usar o zoom enquanto trabalha e como conferir o resultado do jeito que o jogador vai ver.

[[example:potion-16]] Uma poção de 16×16 com zoom. Cada quadrado é um pixel.

Olhe a poção. Ela usa sete cores: preto no contorno (outline), um branco e um cinza-azulado claro no vidro, um laranja, um vermelho e um ameixa escuro no líquido, e um amarelo na rolha. Cada pixel tem uma função. Os pixels brancos do lado esquerdo são o reflexo no vidro; os mais escuros do lado direito são o lado sombreado do líquido. Você vai aprender a colocá-los nas próximas lições. Por enquanto, repare em como é pouca coisa para dizer "poção".

## Escolhendo o tamanho da tela
O tamanho da tela é a primeira decisão e define quase tudo o que vem depois: quanto detalhe cabe, quanto tempo o sprite leva para ficar pronto e o tamanho dele no jogo. Tamanhos comuns e para que servem:

- **8×8** — ícones minúsculos, projéteis, moedas, letras de uma fonte pixelada. Só cabe a forma essencial.
- **16×16** — o tamanho clássico para personagens, itens e tiles em jogos de estilo retrô. Pequeno o bastante para terminar rápido, grande o bastante para um rosto com dois olhos.
- **32×32** — espaço para expressões, detalhes de roupa, uma arma na mão e sombreamento simples com três ou quatro tons por cor.
- **64×64** ou maior — chefões, retratos, cenários detalhados. Cada aumento de tamanho dobra o trabalho: um sprite de 64×64 tem 16 vezes mais pixels que um de 16×16.

[[example:canvas-sizes]] O mesmo slime em 16×16 e em 32×32. O maior tem espaço para um brilho e uma faixa de sombra; o menor diz a mesma coisa com menos pixels.

> Comece pequeno. Um sprite de 16×16 terminado ensina mais do que um de 64×64 abandonado. Quando você conseguir fazer um personagem legível em 16×16, passe para 32×32.

Escolha um tamanho para cada tipo de coisa do seu jogo e mantenha. Se o seu herói tem 16×32 (16 de largura, 32 de altura), os outros personagens devem ser desenhados mais ou menos na mesma escala, e os tiles devem seguir a grade do mundo, geralmente 16×16. Um jogo em que cada sprite tem sua própria escala fica bagunçado, mesmo que cada sprite seja lindo sozinho.

## Zoom para desenhar, 1× para julgar
Você desenha com zoom, geralmente entre 800% e 1600%, para que cada pixel seja um quadrado grande e fácil de clicar. Use **Ctrl +** e **Ctrl −** para mudar o zoom, **Ctrl 0** para ajustar o sprite à tela e segure **Espaço** e arraste para se mover. Ligue **Exibir → Grade de pixels** quando precisar contar pixels.

Mas o jogador nunca vê seu sprite tão grande. Ele vê no tamanho real, ou em 2× ou 3× numa tela moderna. Algumas coisas que parecem boas com zoom somem ou viram ruído em 1×, e alguns "erros" desaparecem. Por isso, confira com frequência: abra **Quadro → Janela de prévia**, deixe em 1× ou 2× e mantenha ao lado da tela enquanto trabalha. Aperte **F** para uma prévia em tela cheia.

[[example:potion-1x]] A poção em 1×
[[example:potion-2x]] e em 2×, do jeito que um jogador pode ver.

Em 1× a poção é um pontinho de cor, e mesmo assim dá para saber o que é, porque a forma e as cores principais estão claras. Esse é o objetivo de todo sprite: ser legível no tamanho real. Se você só entende o seu sprite com zoom, simplifique.

## Um tamanho de pixel por imagem
Todos os pixels de uma imagem precisam ter o mesmo tamanho. É tentador desenhar as formas grandes com blocos de 2×2 (é mais rápido) e depois acrescentar os detalhes finos com pixels simples. O resultado se chama **mixels** (pixels misturados) e deixa a imagem com cara de quebrada, como duas imagens coladas.

[[example:mixels-bad]] Mixels: as folhas são feitas com blocos de 2×2, o tronco com pixels simples.
[[example:mixels-good]] Um único tamanho de pixel em tudo: a árvore parece uma imagem só.

O mesmo vale para redimensionar. Se você aumentar um sprite pronto para 150% ou girar poucos graus, os pixels ficam com tamanhos desiguais. Amplie só por números inteiros (2×, 3×, 4×) e faça isso ao exportar, não enquanto desenha.

! Desenhar grande e depois encolher a imagem. Encolher borra ou perde pixels, e você perde o controle sobre cada um deles. Desenhe no tamanho final desde o começo.

## Agrupamentos limpos, sem ruído
Pixels isolados, cercados por outra cor, são difíceis de ler. Alguns, de propósito, criam brilhos ou textura. Demais viram ruído: o olho não consegue agrupá-los em formas e o sprite parece sujo. Artistas de pixel art chamam um pixel solitário de "órfão" e normalmente agrupam as cores em pequenos aglomerados.

[[example:orphans-bad]] Pixels soltos aleatórios numa pedra: ruído.
[[example:orphans-good]] As mesmas cores agrupadas em aglomerados: um brilho no canto superior esquerdo, uma sombra no canto inferior direito.

! Acrescentar detalhe espalhando pixels soltos por toda parte. Mais pixels não é mais detalhe. Alguns aglomerados limpos sempre funcionam melhor do que muitos pontinhos espalhados.

## Menos cores, imagem mais forte
Artistas iniciantes costumam usar cores demais, escolhendo um tom um pouco diferente para cada pixel. Cada cor a mais é mais uma coisa que o olho precisa separar. Limites deixam os sprites mais fortes: com quatro cores você é obrigado a decidir o que realmente importa. Muitos sprites clássicos usavam só três cores mais transparência. Comece todo sprite escolhendo um pequeno conjunto de cores da paleta e fique com ele; só acrescente uma cor quando souber dizer para que ela serve.

> Ferramentas que ajudam aqui: o **Lápis (B)** coloca pixels simples, a **Borracha (E)** remove, e o **Conta-gotas (I)**, ou segurar **Alt**, pega uma cor que já está na imagem, para você reaproveitar cores em vez de inventar quase-cópias.

## Experimente
[[exercise:first-sprite]]
Desenhe um objeto pequeno numa tela de 16×16: uma poção, uma moeda, uma chave, um cogumelo, um coração. Use **no máximo 4 cores** (uma pode ser o contorno), deixe uma margem de um pixel dentro do quadrado-guia clarinho para que nada encoste na borda e evite pixels soltos. Confira na janela de prévia em 1× antes de clicar em Verificar.
- [ ] Olhei em 1× e ainda consigo dizer o que é.
- [ ] Todos os pixels têm o mesmo tamanho (nada de blocos 2×2 fingindo ser um pixel).
- [ ] Agrupei as cores em aglomerados em vez de espalhar pixels soltos.
