# Formas e silhuetas
group: fundamentals
summary: Formas legíveis primeiro: o teste da silhueta, a linguagem das formas e poses fortes.

## Forma antes do detalhe
No tamanho do jogo, um sprite é visto por uma fração de segundo, muitas vezes em movimento. O jogador não estuda os botões dele nem o desenho da capa. Ele o reconhece pela **forma**: o contorno da figura inteira contra o fundo. Se a forma está clara, o sprite funciona até com cores simples. Se a forma é uma mancha, nenhum detalhe vai salvá-lo.

É por isso que artistas experientes de pixel art começam pela forma grande e só acrescentam detalhes no fim. Monte a figura inteira com uma ou duas cores chapadas primeiro. Mude as partes de lugar enquanto isso é barato. Só quando a forma estiver bem legível você acrescenta cores, sombreamento e contorno.

## O teste da silhueta
Preencha seu sprite com uma única cor escura, de modo que só reste a forma externa: a silhueta. Agora pergunte: ainda dá para saber o que é? É um cavaleiro, um mago, um slime, um baú? Dá para saber para onde está virado e o que está segurando?

[[example:silhouette-good]] Um cavaleiro legível: pluma, espada para fora, escudo de lado, pernas afastadas. Mesmo em preto você sabe o que é.
[[example:silhouette-bad]] O mesmo cavaleiro todo encolhido: braços colados ao corpo, espada escondida, pernas juntas. A silhueta é uma cápsula.

Os dois cavaleiros têm as mesmas partes e as mesmas cores. A diferença está só em onde as partes ficam. No legível, cada parte importante **se destaca** do corpo e tem um vão de fundo ao redor: a espada, o escudo, a pluma, o espaço entre as pernas. No outro, tudo se sobrepõe ao corpo, e a silhueta perde tudo isso.

> Um teste rápido de silhueta sem mexer no seu trabalho: afaste bastante o zoom (Ctrl −) ou aperte os olhos até as cores se misturarem. O que você ainda enxerga é a silhueta. Para um teste rigoroso, duplique a camada (Camada → Duplicar camada), pinte cada cor dela de preto com o Balde (G) com **Contíguo** desligado e oculte a cópia de novo quando terminar.

## Faça as partes importantes se destacarem
Alguns hábitos fazem quase qualquer sprite ficar mais legível:

- **Separe os membros.** Deixe pelo menos um pixel de fundo entre os braços e o corpo e entre as pernas. Em 16 pixels, esse vão muitas vezes é a única coisa que mostra que há duas pernas.
- **Segure os itens longe do corpo.** Uma espada desenhada na frente do peito desaparece. A mesma espada segurada de lado, inclinada, diz "guerreiro" na hora.
- **Exagere a característica principal.** O chapéu de um mago, as orelhas de um coelho, a pluma de um cavaleiro: faça a característica que identifica o personagem maior do que seria na vida real.
- **Use uma pose clara.** Personagens parados, totalmente retos e simétricos parecem duros. Uma leve inclinação, um pé à frente, uma arma erguida: pequenas assimetrias deixam a pose legível e viva.

! Desenhar o personagem totalmente de frente, simétrico e com os braços caídos. É a pose mais fácil de desenhar e a mais difícil de ler. Gire o personagem um pouco para o lado (vista de três quartos) ou dê a ele uma pose de ação.

## A linguagem das formas
Formas simples carregam sentimentos, e os jogadores os percebem sem pensar. Use isso de propósito ao projetar personagens, inimigos e itens:

- **Formas redondas** (círculos, curvas suaves) passam amizade, maciez, inocência, fofura. Slimes, ajudantes, itens para coletar.
- **Formas quadradas** (blocos, topos retos, ângulos retos) passam estabilidade, força, lentidão, confiança. Golens, guardas, muros, baús.
- **Triângulos** (pontas, espetos, ângulos agudos) passam perigo, velocidade, agressividade. Vilões, espinhos, inimigos a evitar.

[[example:shape-language]] Redondo, quadrado, triângulo: amigável, resistente, perigoso.

Misture-as para contar uma história. Um personagem redondo com uma arma triangular é um herói simpático que sabe lutar. Um corpo quadrado com ombros triangulares e afiados é um chefão pesado e perigoso. Ao projetar um inimigo, pergunte o que o jogador deve sentir e escolha a forma principal antes de escolher qualquer cor.

## Tamanho e proporção
As proporções também mudam a leitura de um sprite. Personagens pequenos de jogos costumam ter a cabeça grande (de um terço à metade da altura do corpo, às vezes mais), porque o rosto carrega a expressão, e uma cabeça minúscula em 16 pixels não comporta olhos. Esse estilo se chama **chibi**. Proporções mais altas e realistas (cabeça com cerca de um sexto ou um sétimo da altura) pedem uma tela maior, com pelo menos 32 pixels de altura, para continuarem legíveis.

Seja qual for a sua escolha, mantenha igual em todo o jogo: se o herói tem cabeça grande, os aldeões também devem ter.

> A legibilidade também depende do que está ao redor do sprite. Um personagem escuro sobre um fundo escuro não tem silhueta nenhuma. Deixe seus personagens um pouco mais claros ou mais saturados do que os cenários por onde andam, ou dê a eles um contorno (veja a lição Contornos).

## Da silhueta ao sprite pronto
Um bom jeito de trabalhar, para qualquer sprite:

1. Monte a silhueta com uma cor chapada. Teste duas ou três versões lado a lado e fique com a mais clara.
2. Divida em partes principais com cores chapadas (pele, roupa, cabelo, arma), ainda sem sombreamento.
3. Confira em 1× na janela de prévia. Corrija o que se misturar.
4. Só agora acrescente sombreamento, contornos e pequenos detalhes.

! Começar pelos olhos ou pelo rosto e construir o corpo ao redor. Você vai polir detalhes que depois precisarão mudar de lugar. Forma primeiro, detalhes por último.

## Experimente
[[exercise:silhouette]]
Desenhe um personagem, criatura ou objeto como **silhueta em uma cor** na tela de 32×32, em pé sobre a linha de chão clarinha. Faça-o **grande e marcante** (ele deve ocupar boa parte da tela, sem virar uma mancha), em **uma ou duas peças**, e deixe uma margem ao redor. Dê a ele pelo menos uma característica que se destaque.
- [ ] Outra pessoa conseguiria adivinhar o que é só pela forma.
- [ ] Braços, pernas ou itens segurados estão separados do corpo por vãos.
- [ ] Tem uma característica exagerada que o identifica (um chapéu, orelhas, uma arma, uma cauda…).
