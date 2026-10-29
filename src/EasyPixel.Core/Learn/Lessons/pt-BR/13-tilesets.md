# Tilesets
group: styles
summary: Tamanhos de grade, tiles sem emendas, tiles de borda e de canto, variações e teste na prévia lado a lado.

## O que é um tileset
A maioria dos mundos de jogos 2D é construída com **tiles** (blocos): pequenas imagens quadradas colocadas numa grade, como os azulejos de um banheiro. Um tile de grama repetido cem vezes vira um campo. Algumas dezenas de tiles (grama, terra, as bordas onde se encontram, paredes, água) podem construir uma fase inteira. Um conjunto de tiles que se encaixam é um **tileset**.

Tiles poupam muito trabalho, mas trazem dois desafios: os tiles precisam se juntar **sem emendas visíveis**, e a repetição **não pode ser óbvia**. Esta lição cobre os dois, além das peças de borda e de canto que toda plataforma e todo caminho precisam.

## Escolhendo o tamanho do tile
Escolha um único tamanho de tile para o jogo todo e faça seus personagens caberem nele:

- **16×16** — o tamanho mais comum. Plataformas clássicos e RPGs vistos de cima usam; um personagem costuma ter um tile de largura e um ou dois de altura.
- **32×32** — mais espaço para detalhe e textura, comum em jogos de pixel art modernos. Os personagens têm cerca de um tile de largura.
- **8×8** — bem retrô, usado em jogos minúsculos e em tilesets detalhados montados com peças menores.

O tamanho do tile e o tamanho do personagem andam juntos: o herói deve passar por um corredor de um tile e pousar numa plataforma de um tile. No EasyPixel, **Exibir → Configurar grade** ajusta a grade de blocos para o tamanho do seu tile, e **Exibir → Grade de blocos** a mostra sobre a tela.

## Tiles sem emendas
Um tile é **sem emendas (seamless)** quando cópias dele, lado a lado, se juntam sem uma linha visível. O que importa são as bordas: o que toca a borda direita precisa continuar naturalmente na borda esquerda, e o que toca a borda de baixo precisa continuar no topo, porque é isso que vai ficar ao lado.

[[example:tile-seam-bad]] Com emendas: claro à esquerda, escuro à direita, uma mancha amarela cortada pela borda, uma linha escura embaixo. Todas as bordas do tile aparecem.
[[example:tile-seam-good]] Sem emendas: o mesmo tile repetido, e não dá para ver onde um termina e o próximo começa.

Como fazer um:

1. Preencha o tile inteiro com a cor base. Tiles de chão **não têm pixels transparentes**; um buraco deixa o fundo aparecer através do chão.
2. Acrescente textura (tufos de grama, pedrinhas, rachaduras) primeiro no **meio** do tile.
3. Quando um detalhe cruza uma borda, continue-o na borda **oposta**: um tufo que sai pela direita reaparece na esquerda na mesma altura.
4. Mantenha o brilho geral parelho no tile. Um tile mais claro de um lado do que do outro mostra um padrão de listras quando se repete, mesmo que os pixels das bordas combinem.
5. Ative **Exibir → Prévia lado a lado**. O EasyPixel desenha cópias da tela ao redor dela, e você pode continuar desenhando enquanto observa as emendas.

[[example:tile-single]] Um tile de grama 16×16: uma cor base e pequenos tufos posicionados para dar a volta pelas bordas.

> Trabalhe com a Prévia lado a lado ligada desde o primeiro pixel. Consertar uma emenda depois é muito mais difícil do que nunca fazer uma.

! Sombrear um tile de chão como se fosse um objeto, com um canto claro e um canto escuro. Um tile repetido não tem uma fonte de luz própria; sombreie os pequenos detalhes dentro dele e mantenha o brilho geral do tile uniforme.

## Escondendo a repetição
Mesmo um tile sem emendas perfeito pode parecer papel de parede quando se repete numa área grande: o olho percebe o padrão, ainda mais se um detalhe se destaca.

[[example:tile-repeat-bad]] Uma pedra grande e brilhante no tile: repetida, forma uma grade óbvia de pedras.

Maneiras de evitar isso:

- Mantenha a textura **pequena e de baixo contraste**. Muitos detalhes pequenos se misturam; um detalhe grande se repete.
- Evite detalhes que se alinham em fileiras ou colunas retas.
- Faça **variações**: duas ou três versões do mesmo tile com os detalhes em lugares diferentes, e uma com uma característica rara (uma flor, uma rachadura, um osso). Misture-as ao montar a fase, usando a rara com moderação.

## Bordas e cantos
O chão não é infinito: uma plataforma tem uma borda de cima com grama, laterais e cantos. Você precisa de um tile para cada caso. O conjunto básico é um **bloco 3×3**: quatro cantos, quatro bordas e um tile do meio. Com essas nove peças você monta um retângulo de qualquer tamanho.

[[example:tileset-edges]] As nove peças de um conjunto 3×3 (afastadas) e uma plataforma montada com elas.

Regras para os tiles de borda:

- O tile do meio é o seu tile sem emendas. Todo tile de borda precisa ser sem emendas com ele ao longo do lado que compartilham.
- Os tiles de borda se repetem **ao longo da borda**: o tile da borda de cima precisa ser sem emendas consigo mesmo, da esquerda para a direita.
- Os cantos arredondam ou chanfram a forma, para as plataformas não parecerem retângulos recortados.
- Contorne a borda externa da plataforma (em cima e nas laterais) se os seus personagens têm contorno, para a superfície onde se anda se destacar do fundo.

Depois você pode acrescentar **cantos internos** (para chão em forma de L), rampas nos ângulos limpos de 1:1 e 2:1 da lição de Linhas, e peças de um tile de largura. Muitos jogos usam 16 ou 47 tiles para um tipo de chão, para que toda combinação de vizinhos tenha uma peça.

! Desenhar uma fase inteira como uma imagem grande e cortá-la em tiles depois. Os tiles não vão servir para nenhum outro desenho de fase. Projete os tiles primeiro e depois monte as fases com eles.

## Testando um tileset
- Use **Exibir → Prévia lado a lado** para um tile só.
- Para um conjunto, crie uma tela de teste com alguns tiles de largura (por exemplo 96×64 para tiles de 16 px), ative a **Grade de blocos** e monte uma pequena plataforma ou sala com cópias dos seus tiles. Olhe em 1× na janela de prévia.
- Coloque um personagem em cima. Um tileset que fica bonito sozinho ainda pode ser carregado ou claro demais para os personagens se destacarem sobre ele. O chão deve ser um pouco mais escuro e menos saturado que os personagens.

## Experimente
[[exercise:seamless-tile]]
Faça um **tile de chão 16×16** (grama, terra, pedra ou areia) que se repete sem emendas. Preencha **todos os pixels**, use **3 a 6 cores** e garanta que os detalhes que cruzam uma borda continuem na borda oposta. Ative **Exibir → Prévia lado a lado** enquanto desenha.
- [ ] Olhei com Exibir → Prévia lado a lado e não encontro as bordas do tile.
- [ ] Nenhum detalhe se destaca quando o tile se repete.
