# Noções de animação
group: fundamentals
summary: Quadros e tempo, pausas, papel vegetal, comprimir e esticar, e seu primeiro idle.

## Quadros e tempo
Uma animação é uma sequência de imagens paradas, os **quadros (frames)**, mostradas uma depois da outra rápido o bastante para o olho enxergar movimento. No EasyPixel, cada quadro tem sua própria **duração** em milissegundos: quanto tempo ele fica na tela antes do próximo.

Animações de pixel art usam **poucos quadros**: um idle (parado) tem de 2 a 4, uma caminhada de 4 a 8, um ataque de 3 a 6. É menos do que num desenho animado, e isso faz parte do estilo: cada quadro conta, e o tempo faz boa parte do trabalho.

Tempos comuns:

- **100 ms** por quadro (10 quadros por segundo) é um bom padrão para caminhadas e loops.
- **60–80 ms** para ações rápidas: uma espadada, uma investida, um impacto.
- **150–400 ms** para movimentos lentos e calmos: respiração, uma bandeira com brisa leve, uma luz piscando.

Você não precisa usar a mesma duração em todos os quadros. Variar as durações dentro de uma animação é o que dá ritmo a ela. Defina o tempo de um quadro em **Quadro → Duração do quadro…**, toque a animação em **Quadro → Tocar / parar** e veja no tamanho real em **Quadro → Janela de prévia**.

## Pausas e espaçamento
Duas ideias da animação clássica fazem até três quadros parecerem vivos:

- **Pausas (holds)**: um quadro que fica na tela mais tempo que os outros. Uma espadada parece poderosa quando a preparação é segurada (250 ms), o golpe é rápido (60 ms por quadro) e a pose final é segurada de novo. Sem pausas, tudo se move na mesma velocidade e nada tem peso.
- **Espaçamento**: quanto o objeto anda entre dois quadros. Passos pequenos parecem lentos, passos grandes parecem rápidos. Objetos reais **aceleram e desaceleram suavemente**: começam devagar, ganham velocidade e diminuem de novo antes de parar.

Uma bola quicando mostra as duas coisas. No alto do quique a bola está lenta, então os quadros ficam próximos. Perto do chão ela está rápida, então ficam distantes.

[[example:bounce-spacing]] Um quique com um quadro a cada poucos pixels na horizontal: os desenhos se juntam no alto (lento) e se afastam perto do chão (rápido).

> Atue! Quique uma bola de verdade, balance o braço, acene com a cabeça e observe onde o movimento é lento e onde é rápido. Depois coloque as pausas e os passos grandes nos mesmos lugares.

## Comprimir e esticar (squash and stretch)
Coisas macias ou elásticas mudam de forma quando se movem. Uma bola **comprime** (fica mais larga e achatada) quando bate no chão e **estica** (fica mais alta e fina) quando cai ou voa rápido. Num único quadro parece exagerado, mas em movimento faz a bola parecer pesada e elástica, em vez de um recorte duro.

[[example:squash-stretch]] Em repouso, comprimida ao cair no chão (mais larga e achatada), esticada ao cair rápido (mais alta e fina).

A regra: **mantenha o volume mais ou menos igual**. Se a bola fica 4 pixels mais larga, ela fica uns 4 pixels mais baixa. Uma bola que cresce para todos os lados parece estar enchendo, não sendo comprimida.

Comprimir e esticar não serve só para bolas: um personagem se comprime um pouco ao aterrissar de um pulo, uma gosma se comprime a cada pulinho, um botão se comprime quando é apertado.

[[example:bounce-ball]] Uma bola quicando, comprimida no chão, esticada quando se move rápido, com uma breve pausa no alto.

## Papel vegetal (onion skin)
Ao desenhar um quadro, você precisa ver os quadros ao redor, senão o movimento dá saltos. O **papel vegetal (onion skin)** mostra os quadros anterior e seguinte de leve atrás do que você está desenhando, como um papel de decalque. Ative em **Exibir → Papel vegetal** e escolha quantos quadros mostrar e com que intensidade em **Quadro → Configurar papel vegetal…**.

[[example:onion-skin]] Papel vegetal: o quadro anterior em vermelho, o próximo em azul, e o quadro que você está desenhando por cima.

Trabalhe assim:

1. Desenhe primeiro as **poses-chave**: os extremos do movimento (ponto mais alto, ponto mais baixo, o começo e o fim de uma espadada).
2. Ative o papel vegetal e desenhe os quadros **intermediários**, colocando cada um entre os vizinhos.
3. Toque a animação com frequência. Conserte o quadro que dá o salto, não a animação inteira.

Novos quadros são criados com **Quadro → Novo quadro** (uma cópia do atual, pronta para ajustar) ou **Quadro → Novo quadro vazio**.

## Sua primeira animação: um idle balançando
Personagens de jogos quase nunca ficam perfeitamente parados. Mesmo quando o jogador não faz nada, o herói respira, se balança ou ginga. Essa animação **idle** mantém o jogo vivo, e é a mais fácil de fazer.

[[example:idle-bob]] Um idle de dois quadros: no segundo, a gosma está um pixel mais baixa e um pouco mais larga. Cada quadro dura 300 ms.

Um idle em 2 a 4 quadros:

1. Quadro 1: o personagem parado, normal.
2. Quadro 2: o corpo (cabeça e peito) 1 pixel mais baixo. Os pés ficam exatamente onde estavam!
3. Opcionalmente, quadros 3 e 4: de volta para cima, e um movimento extra minúsculo (cabelo, capa, uma piscada).
4. Dê a cada quadro de 200 a 400 ms: o idle é calmo.

! Mover o personagem inteiro, pés incluídos, numa animação idle. No jogo parece que ele está flutuando ou deslizando. Os pés ficam plantados; só o corpo se move.

! Dar a mesma duração a todos os quadros numa ação. Um soco em que a preparação, o golpe e a recuperação duram 100 ms não tem impacto. Segure as poses que importam e passe rápido pelas intermediárias.

## Loops
A maioria das animações de jogo **se repete em loop**: idle, caminhada, corrida, a chama de uma tocha. Um bom loop não tem salto visível entre o último quadro e o primeiro. Confira com **Quadro → Repetir** ligado: toque por um tempo e observe o momento em que volta ao início. Se houver um tranco, o último quadro deve levar naturalmente ao primeiro, como qualquer outro quadro leva ao seguinte.

## Experimente
[[exercise:bouncing-ball]]
Anime uma **bola quicando** na linha do chão. Faça **pelo menos 5 quadros**: em um deles a bola encosta no chão (o pixel mais baixo dela exatamente em cima da linha), em outro ela está **pelo menos 10 pixels mais alta**, e ela nunca afunda no chão. Adicione quadros com **Quadro → Novo quadro**, ative **Exibir → Papel vegetal** e toque com **Quadro → Tocar / parar**.
- [ ] A bola se comprime quando toca o chão (e estica quando se move rápido).
- [ ] Os quadros ficam mais próximos no alto do quique do que perto do chão.
- [ ] Toquei na Janela de prévia e ela se repete sem saltos.
