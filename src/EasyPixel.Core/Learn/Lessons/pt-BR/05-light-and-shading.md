# Luz e sombreamento
group: fundamentals
summary: Uma única fonte de luz, sombra de forma e sombra projetada, brilhos, rampas de cores com variação de matiz e como evitar o pillow shading.

## Sombrear é escolher uma luz
O sombreamento é o que faz uma forma chapada parecer redonda, sólida e pesada. Não é "mais escuro nas bordas": é o resultado de **uma única decisão**, de onde vem a luz. Cada pixel de sombreamento decorre dessa decisão. As partes do objeto voltadas para a luz ficam iluminadas; as que se afastam dela caem na sombra.

Por isso, antes de colocar qualquer tom, decida onde está a sua luz. Na maioria dos jogos a luz vem do **canto superior esquerdo** (ou direto de cima), e todos os sprites do jogo usam a mesma direção. Anote, desenhe um solzinho no canto da tela se ajudar e nunca mude a direção dentro de uma mesma imagem.

[[example:light-source]] Luz vinda do alto à esquerda: iluminado em cima à esquerda, sombra embaixo à direita, um pequeno brilho onde a luz bate mais direto.

## As partes da luz e da sombra
Olhe qualquer objeto redondo sob uma lâmpada e você vai encontrar as mesmas partes. A pixel art usa todas elas, simplificadas em poucas cores:

- **Luz** — o lado voltado para a luz, na cor principal iluminada do objeto.
- **Brilho** (*highlight*) — um pequeno ponto (de um a quatro pixels num sprite pequeno) onde a superfície enfrenta a luz de forma mais direta. Em coisas brilhantes é quase branco; em coisas foscas é só o tom mais claro. Mantenha pequeno: um brilho grande deixa tudo com cara de molhado.
- **Sombra de forma** (ou sombra de núcleo) — o lado virado para longe da luz. Ela acompanha o formato do objeto, então numa bola é um crescente do lado oposto.
- **Luz refletida** — uma faixa um pouco mais clara bem na borda do lado sombreado, luz que voltou do chão. Opcional, mas faz as coisas parecerem redondas. Mantenha-a mais escura do que qualquer parte iluminada.
- **Sombra projetada** — a sombra que o objeto lança no chão ou em outros objetos, do lado oposto à luz. Geralmente é a área mais escura e firma o objeto no chão, para que ele não flutue.

[[example:cast-shadow]] A sombra projetada no chão, longe da luz, faz a bola ficar apoiada no piso.

Numa caixa as partes são ainda mais simples: cada face plana recebe um tom. A face voltada para a luz é a mais clara, a virada para o outro lado é a mais escura, e a intermediária recebe um tom médio.

[[example:cube-faces]] Luz vinda de cima e da esquerda: face de cima mais clara, face esquerda média, face direita mais escura.

## Pillow shading
O erro de sombreamento mais comum tem nome: **pillow shading** ("sombreamento de travesseiro"). Acontece quando você sombreia "do contorno para dentro": escuro em todas as bordas, anéis mais claros em direção ao meio, como se a luz viesse dos olhos de quem olha. O objeto fica inchado e chapado, como um travesseiro, e não há direção de luz nenhuma.

[[example:pillow-shading]] Pillow shading: anéis escuros em volta, claro no meio. Onde está a luz? Em lugar nenhum.
[[example:light-source]] As mesmas cores com uma luz vinda do alto à esquerda.

! Pillow shading: escurecer todas as bordas por igual. Escolha uma direção de luz e deixe o lado voltado para ela claro, até junto do contorno.

A correção é sempre a mesma: decida onde está a luz e deixe a borda voltada para ela **clara**. O lado da sombra recebe os tons escuros, e o lado iluminado não. Se o seu sombreamento parece igual em todos os lados, é pillow shading.

## Rampas de cores
Uma **rampa** é o conjunto de tons que você usa para um material, ordenado do mais escuro ao mais claro. Para um sprite pequeno, de 3 a 5 tons por material bastam: uma sombra, uma base, uma luz e talvez uma sombra profunda e um brilho. Mais tons que isso raramente aparecem no tamanho do jogo e deixam o sprite com aspecto borrado.

Cada degrau da rampa deve ser um salto de brilho (de "valor") claro e visível. Se dois tons vizinhos parecem quase iguais, junte-os; se um salto é enorme, acrescente um tom intermediário.

O jeito óbvio de fazer tons mais escuros, misturando a cor base com preto, dá cores sujas e sem vida:

[[example:ramp-flat]] Uma rampa feita acrescentando preto: as sombras ficam sujas e acinzentadas.

## Variação de matiz (hue shifting)
A luz real raramente é branca pura, e as sombras reais não são pretas. A luz do sol é quente (um pouco amarela), e as sombras pegam o azul frio do céu. Pintores usam isso, e artistas de pixel art contam com isso: **varie o matiz** ao longo da rampa. Leve os tons mais claros para cores **quentes** (amarelo, laranja) e os mais escuros para cores **frias** (azul, roxo). A saturação costuma ser mais alta no meio da rampa e um pouco menor nas duas pontas.

[[example:ramp-hue-shift]] A mesma ideia com variação de matiz: roxo-vinho profundo, depois vermelho, laranja e um amarelo quente para a luz, cada degrau a cerca de 25 graus no círculo cromático. Rico e vivo.

Por exemplo, a rampa de uma maçã vermelha pode ir de um roxo escuro na sombra mais profunda, passando por carmim e vermelho, até o laranja na luz e um amarelo claro no brilho. Uma folha verde: sombras verde-azuladas escuras, verde, depois uma luz verde-amarelada. Uma pedra cinza: sombras cinza-azuladas e luzes quentes, levemente amareladas.

> Varie o matiz cerca de 10 a 30 graus por degrau no círculo cromático e, ao mesmo tempo, reduza o brilho de forma bem clara. Se a rampa parece um arco-íris, você foi longe demais.

! Usar preto puro nas sombras e branco puro nos brilhos. O preto mata a cor do material, e o branco deixa com cara de plástico. Use uma cor escura e fria para a sombra mais profunda e uma cor clara e quente para o brilho.

## Uma rotina de sombreamento
1. Preencha a forma com a cor base (o meio da rampa).
2. Decida a direção da luz.
3. Pinte a sombra de forma com o próximo tom mais escuro no lado oposto à luz, acompanhando o formato.
4. Acrescente o tom mais claro no lado voltado para a luz.
5. Acrescente um pequeno brilho e, se quiser, uma sombra mais profunda onde a forma se afasta mais (e embaixo das coisas).
6. Acrescente a sombra projetada no chão.
7. Olhe em 1×. Se um tom não aparece ali, remova.

Use as cores da sua paleta em vez de escolher novas para cada pixel: um sprite com uma rampa planejada de quatro cores fica mais limpo do que um com quinze tons aleatórios.

## Experimente
[[exercise:shade-ball]]
A tela tem uma bola laranja chapada, e a marca de sol no canto diz que a luz vem do **alto à esquerda**. Sombreie a bola: use **de 3 a 6 cores** no total, deixe o lado superior esquerdo iluminado e o inferior direito na sombra, e acrescente um pequeno brilho. A paleta já traz uma rampa com variação de matiz pronta para você (roxo, vermelho, laranja, amarelo).
- [ ] A sombra mais escura está embaixo à direita, não em toda a borda.
- [ ] Acrescentei um pequeno brilho no lado iluminado.
- [ ] Meus tons ficam mais quentes perto da luz e mais frios perto da sombra.
