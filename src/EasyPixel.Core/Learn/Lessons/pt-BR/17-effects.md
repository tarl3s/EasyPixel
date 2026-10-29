# Efeitos
group: styles
summary: Fogo, fumaça, explosões e brilhos como animações curtas: formas, rampas de cor, tempo e como os efeitos terminam.

## Efeitos são antes de tudo animação
Efeitos — fogo, fumaça, explosões, brilhos, magia, golpes — são o que faz um jogo parecer "suculento". Uma espadada sem um clarão parece fraca; o mesmo golpe com uma faísca de 3 quadros parece ótimo. A boa notícia: os efeitos são **curtos, pequenos e tolerantes**. Ninguém inspeciona um único quadro de uma explosão; o que importa é a **forma ao longo do tempo** e o **tempo**.

Os efeitos seguem algumas regras diferentes das dos personagens:

- **Mudam muito de forma** de um quadro para o outro. Tudo bem — é o que os deixa cheios de energia.
- **Usam rampas vivas e saturadas**: branco e amarelo no ponto mais quente, laranja e vermelho em volta, escuro nas bordas.
- **Muitas vezes não têm contorno**, ou têm um colorido, para parecerem luz e não objetos.
- **Têm um tempo de vida**: começam, crescem e terminam. Até um fogo em loop tem um ritmo.

## Fogo
O fogo é um efeito em loop: uma chama que nunca para. Monte-o em camadas, de fora para dentro:

1. **Forma**: uma gota, larga embaixo e pontuda em cima. Faça a ponta se inclinar e balançar um pouco a cada quadro.
2. **Cores**: vermelho por fora, laranja por dentro, amarelo no núcleo, e um pequeno ponto branco-incandescente perto da base, onde o fogo é mais quente.
3. **Movimento**: em cada quadro, desloque a ponta para a esquerda ou para a direita e mude a altura dela. Lance uma brasa (um único pixel laranja) acima da chama em alguns quadros.
4. **Loop**: 3 a 6 quadros de uns 80–120 ms cada. O fogo fica melhor levemente irregular.

[[example:fx-fire]] Um loop de fogo de 4 quadros: a ponta balança e treme, uma brasa salta, o núcleo continua brilhante perto da base.

> As chamas se movem **para cima e para dentro**: um pixel de fogo nunca desce. Anime as formas subindo e afunilando, e o olho lê calor.

## Fumaça
A fumaça é o oposto do fogo: **macia, lenta e que vai sumindo**. Uma nuvem de fumaça:

- **Sobe** um pouco a cada quadro.
- **Cresce** à medida que se espalha.
- **Fica mais clara e mais fina** até desaparecer.

O truque da pixel art para "mais fina" é o **pontilhado (dithering)**: nos últimos quadros, desenhe só um pixel sim, outro não da nuvem (um tabuleiro de xadrez), depois ainda menos. A nuvem se desfaz e some sem precisar de cores transparentes.

[[example:fx-smoke]] Uma nuvem de fumaça subindo, crescendo, ficando mais clara e depois afinando em xadrez até sumir.

Use uma rampa de cinza deslocada para o frio (cinza-azulado) na maior parte da fumaça; cinzas mais quentes para poeira; escuros para óleo queimando.

## Explosões
Uma explosão é um efeito **de uma só vez** com uma história clara em 5 a 8 quadros:

1. **Clarão**: uma forma pequena e muito brilhante (branco e amarelo). 1 quadro, bem curto (40–60 ms).
2. **Bola de fogo**: expande rápido — núcleo amarelo, corpo laranja, borda vermelha. 1–2 quadros.
3. **Pico**: o maior quadro, que muitas vezes vira um anel quando o centro se apaga, com detritos voando.
4. **Fumaça**: o fogo vira bolas de cinza-escuro que continuam se espalhando.
5. **Dissipação**: a fumaça se desfaz (pontilhado de novo) e desaparece. O último quadro é quase vazio.

[[example:fx-explosion]] Uma explosão: clarão, bola de fogo, anel com faíscas, bolas de fumaça, e depois a fumaça se desfaz e esmaece.

O tempo é o que convence: **rápido no começo, lento no fim**. O clarão e a bola de fogo ficam na tela por um piscar de olhos; a fumaça demora.

! Terminar uma explosão no maior quadro, ou com uma grande nuvem que simplesmente some. O efeito "estoura" para fora da tela. Deixe-o se desfazer nos últimos quadros para terminar suavemente.

! Fazer do primeiro quadro o maior. Um efeito que começa em tamanho máximo não tem impacto; o crescimento do pequeno ao grande é o que se lê como "bum".

## Brilhos e cintilações
Brilhos mostram que algo é mágico, brilhante ou pode ser pego. O brilho clássico é uma **estrela em forma de cruz** que cresce e encolhe:

1. Um único pixel brilhante.
2. Uma cruz pequena (braços de 1–2 pixels), com o centro branco.
3. Uma cruz grande com diagonais curtas: o pico.
4. Encolhendo de volta a uma cruz pequena, e depois sumindo.

[[example:fx-sparkle]] Uma cintilação de 4 quadros: um ponto, uma cruz pequena, uma estrela grande com diagonais, e de volta para baixo.

Coloque brilhos em itens no mundo, num baú de tesouro, numa porta mágica. Desencontre vários brilhos no tempo para não piscarem todos juntos.

## Cores para efeitos
- Os efeitos podem usar as **cores mais brilhantes da sua paleta**: branco, amarelo, azul-água. Personagens e tiles devem deixá-las para os efeitos, para os efeitos se destacarem.
- Use **rampas com mudança de matiz**: o fogo vai de branco → amarelo → laranja → vermelho → roxo escuro; a magia vai de branco → azul-água → azul → roxo.
- Um quadro de branco puro sobre o efeito inteiro por um quadro (um **clarão de golpe**) faz os impactos parecerem fortes.

## Tempo para efeitos
- Impactos e clarões: **30–60 ms** por quadro.
- Explosões: **50–80 ms** no começo, **100–150 ms** para a fumaça.
- Fogo e loops: **80–120 ms**.
- Brilhos: **60–100 ms**.

Defina as durações em **Quadro → Duração do quadro…** e veja o resultado em **Quadro → Janela de prévia**: efeitos são julgados em movimento, nunca quadro a quadro.

## Experimente
[[exercise:explosion]]
Anime um **efeito de uma só vez** — uma explosão, uma nuvem de fumaça ou uma rajada mágica — em **pelo menos 5 quadros**. Ele deve **começar pequeno**, crescer até o seu **maior quadro depois do primeiro** e **esmaecer**: o último quadro com no máximo metade do tamanho do maior. Use **3 a 6 cores**.
- [ ] O começo é rápido e o esmaecer é mais lento.
- [ ] As cores mais quentes (branco, amarelo) estão no meio e nos primeiros quadros, e as cores de fumaça no fim.
- [ ] Na Janela de prévia ele se lê como um único "bum", não como uma forma piscando.
