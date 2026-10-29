# Fundos e parallax
group: styles
summary: Profundidade em camadas, perspectiva atmosférica, menos contraste ao longe, camadas de parallax que se repetem e como manter o jogador legível.

## Para que serve um fundo
O fundo define o clima e o lugar — uma cidade ensolarada, uma floresta escura, o pátio da escola ao entardecer — mas tem uma regra acima de todas: **ele não pode competir com o jogo**. O jogador, os inimigos, as plataformas e os itens precisam ser lidos na hora por cima dele. Um fundo lindo que esconde o herói é um fundo ruim.

Por isso os fundos costumam ser:

- **Com menos contraste** que o primeiro plano.
- **Menos saturados**, com menos detalhes.
- **Mais escuros ou mais claros no geral** que os personagens, para os personagens se destacarem.

## Camadas de profundidade
Jogos de vista lateral montam os fundos com **camadas** a distâncias diferentes: o céu, montanhas distantes, colinas mais próximas, árvores e, por fim, o chão onde o jogador anda. Quando a câmera se move, cada camada rola numa velocidade diferente: as distantes devagar, as próximas rápido. Essa diferença, chamada de **parallax**, cria uma forte sensação de profundidade com imagens planas.

[[example:parallax-layers]] Parallax: o céu e as montanhas distantes ficam parados, as colinas se movem um pouco, o chão próximo e a árvore se movem mais.

Velocidades típicas, como fração do movimento da câmera:

- Céu: **0** (não se move) ou bem lento.
- Montanhas distantes: **0,1–0,25**.
- Colinas e prédios ao longe: **0,3–0,5**.
- Árvores e cercas próximas: **0,6–0,8**.
- O chão onde se joga: **1** (acompanha a câmera).

No EasyPixel, desenhe cada camada do parallax em uma **camada** própria (**Camada → Nova camada**) para vê-las juntas, e depois exporte-as como imagens separadas para o jogo.

## Perspectiva atmosférica
O ar entre você e uma montanha distante não é vazio: névoa e luz se espalham nele. Quanto mais longe algo está, mais ele esmaece em direção à cor do céu. Os pintores chamam isso de **perspectiva atmosférica**, e é a ferramenta de profundidade mais poderosa que você tem:

- Camadas **distantes**: claras, perto da cor do céu, **pouco contraste**, pouca saturação, poucos detalhes, sem contorno.
- Camadas **do meio**: valores médios, um pouco mais de cor e de detalhe.
- Camadas **próximas**: os escuros mais profundos, a maior saturação, o maior detalhe.

[[example:atmosphere-bad]] Todas as serras no mesmo verde forte com contorno preto: fica chato, como recortes de papelão à mesma distância.
[[example:atmosphere-good]] A serra distante pálida e enevoada, a do meio cinza-azulada, a próxima escura e verde: profundidade instantânea com as mesmas formas.

> Para fazer uma camada distante, pegue as cores da próxima e misture-as até a metade do caminho para a cor do céu. Duas ou três cores bastam; mantenha-as próximas em valor.

! Contornar as camadas do fundo do mesmo jeito que os personagens. Contornos puxam as coisas para a frente; nas camadas distantes eles achatam a profundidade e fazem o fundo brigar com o jogador. Deixe os contornos para as coisas com que o jogador interage.

## Camadas que se repetem na horizontal
Uma fase costuma ser muito mais larga que uma imagem de fundo, então cada camada **se repete na horizontal**: quando a câmera passa pela borda direita dela, vem a borda esquerda. Como um tile, a camada precisa ser **sem emendas da esquerda para a direita**: a colina que sai pela borda direita precisa continuar na borda esquerda na mesma altura.

[[example:hills-strip]] Uma camada de colinas que dá a volta: a borda direita encontra a esquerda na mesma altura, então pode se repetir para sempre.
[[example:hills-strip-bad]] Com emenda: as colinas são altas à direita e baixas à esquerda, então aparece um penhasco a cada repetição.

Confira com **Exibir → Prévia lado a lado**, que desenha cópias da tela ao redor dela. Mais algumas regras para camadas de parallax:

- A parte de baixo de cada camada (exceto o céu) precisa ser **preenchida de ponta a ponta**. Um buraco deixa a camada de trás aparecer.
- Faça a largura da camada um bom múltiplo da tela (ou de 16 ou 32 px). Formas que se repetem no mesmo ritmo da largura são fáceis de emendar: uma colina a cada 32 pixels numa camada de 64 pixels.
- Não coloque um único ponto de referência exclusivo numa camada curta que se repete: um castelo que passa a cada dois segundos entrega a repetição. Coloque pontos de referência em camadas largas e lentas, ou como objetos separados.

## Mantendo o jogador legível
Coloque o seu personagem por cima do fundo e olhe em 1× antes de dar o fundo por terminado.

- Se o personagem some no fundo, **diminua o contraste do fundo** ou mude os valores (um céu mais escuro atrás de um herói claro, ou o contrário).
- Evite texturas cheias bem atrás da área de jogo. O detalhe fica no alto da tela e ao longe.
- Mantenha as cores do fundo um pouco mais frias e acinzentadas que as dos personagens. Cores quentes e saturadas avançam; cores frias e cinzentas ficam para trás.

! Fazer do fundo a imagem mais detalhada do jogo. O jogador deixa de enxergar o herói, os inimigos e as bordas das plataformas. Simplifique até o primeiro plano ser lido de relance.

## Uma rotina para um fundo com parallax
1. Pinte o céu primeiro: um degradê (com pontilhado, se a paleta for pequena) ou uma cor chapada.
2. Desenhe a camada mais distante: uma silhueta simples, pálida e perto da cor do céu, sem emendas da esquerda para a direita.
3. Acrescente cada camada mais próxima mais escura, mais saturada e mais detalhada que a de trás.
4. Teste cada camada com Exibir → Prévia lado a lado.
5. Coloque um sprite de personagem por cima e semicerre os olhos: ele precisa se destacar.

## Experimente
[[exercise:parallax-hills]]
Pinte uma **camada de colinas distantes** para um fundo com parallax: colinas que **se repetem sem emendas da esquerda para a direita**, chegam **à borda de baixo de ponta a ponta** e ficam enevoadas — **no máximo 3 cores**, próximas em brilho. Deixe o céu acima transparente: outra camada vai guardá-lo. Ative **Exibir → Prévia lado a lado** para conferir a volta.
- [ ] Na Prévia lado a lado não vejo onde a camada recomeça.
- [ ] Minhas colinas são pálidas e de baixo contraste, como se estivessem longe.
- [ ] Nenhum ponto de referência se destaca e entrega a repetição.
