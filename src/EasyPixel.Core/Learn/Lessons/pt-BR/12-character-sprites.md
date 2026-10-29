# Sprites de personagens
group: styles
summary: Proporções (chibi x realista), heróis de 16 e 32 px, direções para onde olham e personagens legíveis.

## O personagem vem primeiro
O jogador olha para o herói mais do que para qualquer outra coisa no jogo. Um bom sprite de personagem precisa ser **legível de relance** — quem é, para onde está virado, o que está fazendo — no tamanho pequeno em que o jogo o mostra. O detalhe vem depois.

Antes de desenhar, responda três perguntas:

1. **Qual será o tamanho dele na tela?** Escolha o tamanho do sprite a partir do tamanho do seu bloco (tile): um personagem costuma ocupar um bloco de largura e um ou dois de altura (16×16, 16×24, 16×32 ou 32×32).
2. **Qual é a visão do jogo?** Vista lateral (plataforma) ou vista de cima (RPG)? Isso decide de quais direções você precisa.
3. **O que o torna reconhecível?** Um chapéu, a cor do cabelo, uma espada enorme, um cachecol. Escolha uma ou duas características e faça-as marcantes.

## Proporções: chibi e realista
As proporções de um personagem costumam ser medidas em **cabeças**: quantas vezes a cabeça cabe na altura total.

- **Chibi** (ou "super-deformed"): cerca de **2 cabeças** de altura. Cabeça enorme, corpo pequeno, pernas curtas. É o estilo mais comum para sprites pequenos, porque o rosto e a expressão continuam legíveis mesmo com 16 pixels.
- **Realista**: cerca de **5 a 7 cabeças** de altura. Pernas longas, cabeça pequena. Precisa de mais pixels: no mínimo 32, muitas vezes 48 ou 64, senão o rosto vira um único pixel.
- No meio, **3 a 4 cabeças** é um meio-termo popular para personagens de 32 pixels.

[[example:proportions]] Esquerda: um herói chibi, duas cabeças de altura. Direita: um herói mais alto, de umas cinco cabeças. Os dois são legíveis, mas o rosto do chibi sobrevive em um tamanho menor.

Seja qual for a sua escolha, use **as mesmas proporções em todos os personagens** do jogo. Um vilão realista ao lado de um herói chibi parece vir de jogos diferentes.

> Faça a cabeça grande e os olhos simples: dois pixels escuros (ou 1×2 num sprite maior) bastam. O jogador lê o rosto primeiro, então dê espaço a ele.

## 16 pixels ou 32 pixels?
- **16 px**: cada pixel é uma decisão. Os olhos têm 1 pixel cada, as mãos 1 pixel, os pés 2 pixels. Use cores chapadas, 4–8 delas, e conte com a silhueta e os blocos de cor. Animar é rápido.
- **32 px**: espaço para um rosto com expressão, detalhes da roupa, uma mão segurando uma espada, sombreamento com 3–4 tons por material. Animar leva muito mais tempo: cada quadro tem quatro vezes mais pixels.

Para um primeiro jogo, um chibi de 16×16 ou 16×24 é a escolha sensata: você vai desenhá-lo em muitas poses e direções, e um sprite pequeno mantém isso administrável.

## Direções para onde olha
- Um jogo de **vista lateral** (plataforma) precisa do personagem olhando para a **direita**. Olhar para a esquerda costuma ser o mesmo sprite espelhado (o jogo faz isso, ou **Editar → Espelhar na horizontal**). Confira se nada quebra ao espelhar, como um texto na camiseta ou uma espada que deve ficar sempre na mesma mão.
- Um jogo **visto de cima** precisa de pelo menos **frente (baixo), costas (cima) e lado**; o outro lado é o lado espelhado. Alguns jogos acrescentam as quatro diagonais, para oito direções.

[[example:facing-dirs]] O mesmo chibi de frente, de lado (direita) e de costas. O cabelo, a camiseta e as proporções ficam idênticos entre as vistas.

Mantenha tudo consistente entre as direções: a mesma altura, as mesmas cores, os pés na mesma linha. Quando o jogo troca do sprite de frente para o de lado, o personagem não pode dar um salto nem mudar de tamanho.

## Truques de legibilidade
- **Teste da silhueta.** Preencha o sprite de preto. Dá para saber o que é e para onde está virado? Se não, force a forma: cabelo maior, uma postura, um braço afastado do corpo.
- **Contraste com o fundo.** Um contorno escuro (ou seletivo) separa o personagem de qualquer fundo. A maioria dos jogos contorna os personagens mesmo quando os blocos não têm contorno.
- **Uma cor de destaque.** Um cachecol vermelho ou um chapéu amarelo atrai o olhar e faz o herói ser achado numa cena cheia.
- **Mãos e pés grandes e simples.** Em tamanhos pequenos, uma mão é uma mancha de 1–2 pixels. Tudo bem.

[[example:hero-silhouette]] O herói de lado e a sua silhueta: o cabelo, o nariz e os pés mostram para onde ele olha, mesmo em preto.

! Desenhar um personagem lindo numa tela grande (100 px de altura) e depois reduzi-lo para 16 px para o jogo. Os detalhes viram borrão e as cores se multiplicam. Desenhe no tamanho final desde o começo.

! Desenhar o personagem um pouco diferente em cada direção (um pixel mais alto de frente, os olhos mais altos de lado). Parecem vários personagens; mantenha uma folha de referência e compare.

## Uma rotina para um personagem novo
1. Crie uma tela do tamanho do sprite, com um pouco de folga. Desenhe uma **linha do chão** onde os pés ficam.
2. Monte a silhueta com uma cor só: cabeça, corpo, pernas. Confira as proporções.
3. Preencha cores chapadas para pele, cabelo e roupas. Mantenha a paleta pequena.
4. Acrescente o rosto: os olhos primeiro, a boca só se couber.
5. Faça o contorno (**Sprite → Contorno** dá uma primeira passada) e depois corrija o contorno à mão.
6. Sombreie com uma única direção de luz, 1–2 tons por material.
7. Olhe em 1×, ao lado de um bloco do seu jogo.

## Experimente
[[exercise:hero-sprite]]
Desenhe um **herói** para o seu jogo em pé na linha do chão: cerca de **duas cabeças de altura** (a linha pontilhada marca mais ou menos onde fica o queixo de um chibi, na metade da altura), com **contorno**, de **3 a 8 cores**, e os pés apoiados **exatamente na linha do chão**. Mantenha tudo dentro da tela, com uma margem de um pixel.
- [ ] Meu herói passa no teste da silhueta: em preto, ainda dá para saber o que é.
- [ ] O rosto é legível em 1×.
- [ ] Ele tem uma característica que o torna reconhecível (um chapéu, um cachecol, a cor do cabelo…).
