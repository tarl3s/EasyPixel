# Prédios e objetos de cenário
group: styles
summary: Desenhar um prédio (uma escola) em vista lateral, em vista 3/4 de cima e totalmente de cima, com janelas, portas, repetição e objetos de cenário.

## Um prédio, três vistas
Antes de desenhar um prédio, você precisa saber **de onde o jogador o enxerga**. A mesma escola é desenhada de jeitos muito diferentes dependendo da câmera do jogo, e misturar vistas num mesmo jogo é um dos caminhos mais rápidos para ele parecer errado. Nesta lição acompanhamos um exemplo do começo ao fim: **uma escola**.

Há três vistas comuns:

- **Vista lateral** — jogos de plataforma e de rolagem lateral. Você vê a frente do prédio chapada, como o desenho de uma fachada. Sem telhado visto de cima, sem planta baixa.
- **Vista 3/4** (nos RPGs, muitas vezes chamada só de "vista de cima") — a câmera olha de cima, em ângulo. Você vê o **telhado por cima** e a **parede da frente embaixo dele**, achatada e mais curta. É a vista clássica dos RPGs de cidade.
- **Totalmente de cima** — a câmera olha direto para baixo, como um mapa. Você vê **só o telhado**; as paredes ficam escondidas. Jogos de estratégia e alguns de tiro usam.

## Vista lateral: a fachada
Na vista lateral o prédio é uma frente plana feita de retângulos. Isso a torna a vista mais fácil e o melhor lugar para aprender como os prédios são montados.

[[example:school-side]] A escola em vista lateral: paredes de tijolo, a beirada do telhado, um frontão com um relógio, duas fileiras de janelas idênticas, uma placa sobre uma porta dupla, degraus, arbustos.

Comece pelas formas grandes e vá descendo aos detalhes:

1. **Massa**: um retângulo para o bloco principal na linha do chão. Acrescente um segundo bloco ou uma torre só se ajudar a silhueta (o frontão com o relógio diz "escola" ou "prefeitura" na hora).
2. **Telhado**: um telhado plano com beirada, ou inclinado, como um triângulo ou trapézio. A beirada do telhado sai um pouco além das paredes e projeta uma linha fina de sombra embaixo.
3. **Aberturas**: as janelas e a porta. Coloque-as numa grade regular — a mesma altura para todas as janelas de uma fileira, o mesmo espaçamento entre elas.
4. **Material**: tijolos, tábuas ou reboco. Mantenha a textura sutil nas paredes grandes: algumas linhas de argamassa, não cada tijolo.
5. **Detalhes por último**: uma placa, um relógio, uma bandeira, degraus, um poste de luz, um arbusto junto à porta.

## Janelas e repetição
Prédios são cheios de repetição: fileiras de janelas, linhas de tijolos, telhas. A repetição é boa — parece construída e organizada — mas só se as partes repetidas forem **exatamente iguais**.

O truque é simples: **desenhe uma janela e depois copie**. Selecione-a, **Editar → Copiar**, **Editar → Colar**, e mova a cópia para o lugar. Todas as janelas ficam perfeitamente iguais, e se você mudar o desenho pode copiar de novo.

[[example:windows-good]] Um desenho de janela copiado quatro vezes: a fachada parece arrumada e bem construída.
[[example:windows-bad]] Quatro janelas desenhadas uma a uma: tamanhos e alturas um pouco diferentes. O prédio parece torto e descuidado.

> Alinhe as janelas numa grade: mesma fileira de cima, mesmo espaçamento. Use **Exibir → Grade de blocos** com um tamanho de grade igual ao espaçamento das janelas para posicioná-las.

O que faz uma janela ser lida como janela, mesmo com 6×8 pixels:

- Uma **moldura** mais clara ou mais escura que a parede.
- Um **vidro** de cor fria (azul-céu de dia, luz amarela à noite).
- Um **reflexo**: um ou dois pixels claros num canto, sempre o mesmo canto em todas as janelas.
- Uma **travessa** em cruz ou um peitoril (uma linha clara embaixo da janela), se houver espaço.

As portas são mais largas e chegam ao chão. Uma porta dupla com duas maçanetas pequenas, degraus na frente e uma placa ou um toldo em cima diz "prédio público" — exatamente o que uma escola precisa.

## Vista 3/4: telhado e parede
Na vista 3/4 a câmera está inclinada, então você vê duas faces do prédio: o **telhado**, visto de cima, e a **parede da frente**, vista de frente mas **mais curta** que na vista lateral (o ângulo a achata).

[[example:school-34]] A escola em vista 3/4: o telhado, com suas fileiras de telhas, ocupa a parte de cima; a parede da frente, com as mesmas janelas e a porta, é mais curta embaixo dele; e a sombra cai no chão.

Regras para a vista 3/4:

- O telhado costuma ocupar **metade ou mais** da altura do prédio na tela.
- A parede da frente é mais curta: as janelas ficam um pouco mais baixas e largas, mas mantenha o **mesmo desenho** do resto da sua arte.
- Só a parede da **frente** aparece. As paredes laterais ficam escondidas, ou aparecem como uma faixa fina se o prédio for desenhado levemente virado (a maioria dos RPGs não faz isso).
- A **base** do prédio (onde ele toca o chão) é a parte de baixo da parede da frente. Os personagens passam na frente e atrás dele, então o jogo precisa saber onde fica essa linha.
- Uma **sombra** no chão, do lado oposto à luz, firma o prédio no chão.

! Desenhar o telhado visto de cima mas as paredes em perspectiva com pontos de fuga. Prédios de RPG usam uma vista 3/4 simples e chapada: as verticais continuam verticais e as horizontais continuam horizontais. Perspectiva de verdade torna impossível alinhar tiles e sprites.

## Totalmente de cima
Na vista totalmente de cima você só vê o telhado. Para ele ser lido como escola, e não só como um retângulo:

[[example:school-top]] A escola vista direto de cima: o telhado com uma cumeeira, claraboias, uma caixa de ar-condicionado, um toldo sobre a porta na borda da frente e uma sombra que mostra que o prédio é alto.

- Dê **estrutura** ao telhado: uma linha de cumeeira, painéis ou telhas, chaminés, claraboias, máquinas.
- Mostre a **entrada** na borda: um toldo, um caminho que leva até ela, uma placa no chão.
- Use a **sombra** para mostrar a altura: um prédio mais alto projeta uma sombra mais larga.

## Objetos de cenário (props)
**Props** são os objetos que tornam um lugar crível: carteiras, cadeiras, uma lousa, armários, uma lixeira, bancos, um bicicletário. Seguem as mesmas regras dos prédios, na **mesma vista** que todo o resto.

[[example:school-props]] Objetos de uma escola em vista lateral: uma carteira e uma cadeira, uma lousa, um armário, uma lixeira — mesma luz, mesmo contorno, mesma paleta.

- Desenhe os props na **mesma escala** dos personagens: uma porta precisa ser mais alta que o herói, e uma carteira chega mais ou menos na cintura dele.
- Use a **mesma direção de luz e o mesmo estilo de contorno** do resto do jogo.
- Faça algumas versões dos props comuns (uma carteira arrumada e uma bagunçada) para as salas não parecerem copiadas e coladas.

! Desenhar o prédio numa escala que não combina com os personagens: portas menores que o herói, ou janelas maiores que o jogador inteiro. Coloque um sprite de personagem ao lado do prédio enquanto desenha.

## Uma rotina para a sua escola
1. Decida a vista do seu jogo. Mantenha-a em todos os prédios e props.
2. Coloque um sprite de personagem na tela para dar escala. Desenhe a linha do chão.
3. Monte a massa e o telhado em cores chapadas.
4. Desenhe **uma** janela e **uma** porta; copie a janela para cada lugar numa grade.
5. Acrescente a textura da parede, a placa, o relógio e a sombra.
6. Olhe em 1× ao lado dos seus tiles e personagens.

## Experimente
[[exercise:school-front]]
Desenhe uma **escola** (ou qualquer prédio de que você precise) em **vista lateral**, em pé na linha do chão. Desenhe **uma janela e depois copie**, de modo que pelo menos **três janelas sejam exatamente iguais**. Use **4 a 12 cores**, mantenha o prédio em uma só peça (uma placa ou porta separada pode, até três peças no total) e faça-o apoiar **bem na linha do chão**.
- [ ] Minhas janelas estão alinhadas numa grade, com o mesmo espaçamento.
- [ ] A porta é mais alta que um personagem e chega ao chão.
- [ ] Alguma coisa mostra que é uma escola (uma placa, um relógio, um sino, uma bandeira).
