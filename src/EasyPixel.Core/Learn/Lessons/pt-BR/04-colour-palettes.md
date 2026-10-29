# Paletas de cores
group: fundamentals
summary: Paletas limitadas, rampas de cores de 4 a 6 valores, variação de matiz, saturação e como checar o contraste em tons de cinza.

## Por que artistas de pixel art limitam as cores
Uma foto pode ter milhões de cores. Um bom sprite de pixel art costuma ter **menos de 16**, e um jogo inteiro muitas vezes divide uma única paleta de 16 a 32 cores. Isso não é nostalgia: uma paleta pequena é um dos principais motivos de a pixel art parecer limpa.

- **Toda cor é visível.** Com poucas cores, cada uma é uma decisão clara. Quinze tons de um verde quase igual viram borrão no tamanho do jogo.
- **Tudo combina.** Quando o herói, as árvores e a escola usam a mesma paleta, parecem fazer parte do mesmo mundo.
- **É mais rápido.** Escolher entre oito amostras é mais rápido do que misturar uma cor nova para cada pixel, e você nunca fica na dúvida de "qual vermelho usei no telhado?".

Os consoles antigos obrigavam a isso: o Game Boy tinha **4** tons, os primeiros computadores tinham **8** ou **16** cores. Hoje nada obriga você, mas o hábito continua valendo a pena.

[[example:palette-sizes]] Quatro tons (estilo Game Boy), oito cores e as dezesseis da paleta Sweetie 16, que estas lições usam.

Quantas usar? Um bom ponto de partida:

- **4 a 8 cores** para um sprite pequeno (16×16), contorno incluído.
- **16 cores** para um jogo pequeno inteiro, ou para um personagem detalhado.
- **32 cores** para um jogo maior, com muitos materiais e lugares.

No EasyPixel, as cores do documento ficam no painel **Paleta**. Carregue uma paleta pronta pelo menu ⋯ do painel → **Carregar paleta…**, adicione a cor atual com ＋ e mude para **Sprite → Modo de cor → Indexado (paleta)** se quiser que o EasyPixel mantenha cada pixel dentro da paleta por você. **Sprite → Reduzir cores…** traz de volta para uma paleta uma imagem que tem cores demais.

> Comece todo projeto novo escolhendo (ou criando) a paleta primeiro. Desenhar antes e "arrumar as cores depois" quase nunca funciona.

## Rampas: do escuro ao claro
Dentro de uma paleta, as cores se organizam em **rampas** (*ramps*). Uma rampa é o conjunto de tons de um material, ordenado do mais escuro ao mais claro: a rampa da pele, a das folhas, a dos tijolos. Você já conheceu as rampas na lição Luz e sombreamento; aqui vamos ver como construir uma boa.

Uma rampa para um sprite tem **de 4 a 6 valores**: uma sombra profunda, uma sombra, a cor base, uma luz e talvez um brilho. Com menos de 4, os objetos ficam chapados; com mais de 6, os degraus ficam pequenos demais para aparecer no tamanho do jogo.

Regras para uma boa rampa:

1. **Cada degrau é claramente mais claro que o anterior.** O brilho (o "valor") precisa subir passo a passo, sem dois vizinhos que pareçam quase iguais.
2. **Os degraus são mais ou menos uniformes.** Um salto enorme no meio cria uma faixa dura em objetos redondos.
3. **As pontas chegam longe.** A cor mais escura deve ser bem escura e a mais clara bem clara, senão o sprite inteiro fica cinzento e nebuloso.

## Variação de matiz (hue shifting)
O jeito preguiçoso de fazer uma rampa é pegar uma cor e acrescentar preto para as sombras e branco para as luzes. Funciona, mas o resultado é sem graça e sujo, porque na vida real sombras e luzes têm cores próprias.

[[example:ramp-flat]] Um único matiz escurecido com preto: as sombras ficam sujas.
[[example:ramp-hue-shift]] Variação de matiz: um roxo-vinho escuro na sombra, depois vermelho, laranja e amarelo na luz, cerca de 25 graus de matiz por degrau.

A **variação de matiz** (*hue shifting*) é mudar um pouco o matiz a cada degrau: em direção às cores **quentes** (amarelo, laranja) conforme a rampa clareia, e em direção às cores **frias** (azul, roxo) conforme escurece. A luz do sol é quente e as sombras pegam o azul do céu, então rampas com variação de matiz parecem iluminadas por um sol de verdade. Uma folha verde vai de um verde-azulado na sombra a um verde-amarelado na luz; uma maçã vermelha vai do roxo ao vermelho e ao laranja.

> Mova o matiz cerca de 10 a 30 graus por degrau no círculo cromático. As luzes puxam para o amarelo, as sombras para o azul ou o roxo. Mais do que isso e a rampa vira um arco-íris.

As rampas podem **compartilhar cores**. A cor mais escura da rampa da pele pode ser o mesmo roxo da sombra do tecido, e o verde mais claro pode ser o amarelo da areia. Cores compartilhadas mantêm a paleta pequena e dão unidade à imagem.

[[example:ramps-shared]] Três rampas (quente, plantas, água) feitas com 13 cores: o azul-marinho e o amarelo são usados duas vezes.

## Saturação
**Saturação** é a intensidade de uma cor: o cinza não tem nenhuma, um vermelho puro tem muita. Cores totalmente saturadas por toda parte ficam berrantes e baratas, e cansam o olho. Algumas orientações:

- Deixe o **meio** da rampa o mais saturado; as pontas mais escura e mais clara, um pouco menos.
- Use as cores mais fortes para o que importa: o herói, os itens, o perigo. Fundos e áreas grandes devem ser mais calmos.
- Cores muito escuras e muito claras ficam naturais com saturação baixa. Uma sombra ao mesmo tempo escura e muito saturada parece neon.

! Escolher todas as cores com saturação máxima porque parecem vivas e divertidas no seletor de cor. Na tela elas gritam, e nada se destaca porque tudo se destaca.

## Valor: o teste em tons de cinza
A propriedade mais importante de uma cor é o seu **valor**: o quanto ela é clara ou escura, ignorando o matiz. O olho lê as formas primeiro pelo valor e depois pela cor. Duas cores com matizes diferentes mas o mesmo valor — um vermelho e um verde do mesmo brilho — se fundem, e a forma desaparece.

O teste é simples: **imagine o sprite em tons de cinza** (ou aperte os olhos até as cores sumirem). Se as partes ainda são fáceis de distinguir, os valores estão bons. Se tudo vira uma mancha cinza, mude os valores, não os matizes.

[[example:value-bad]] Chapéu vermelho, pintas verdes, caule marrom: muita cor, mas um valor só. Em tons de cinza (à direita) é uma mancha cinza.
[[example:value-good]] O mesmo cogumelo com valores planejados: contorno escuro, chapéu vermelho médio, pintas e caule claros. Ele continua legível em tons de cinza.

! Escolher as cores só pelo matiz ("chapéu vermelho, pintas verdes") sem conferir os valores. Jogadores daltônicos e telas pequenas perdem a forma por completo.

## Criando a sua própria paleta
1. Escolha os principais materiais do seu jogo: pele, cabelo, tecido, madeira, pedra, grama, céu.
2. Para cada um, faça uma rampa de 4 a 5 cores com variação de matiz e um degrau de valor bem claro entre vizinhas.
3. Junte cores quase iguais entre rampas. Reaproveite a mais escura e a mais clara sempre que der.
4. Desenhe um sprite de teste e um tile de teste com a paleta, olhe em 1× e em tons de cinza e ajuste.
5. Salve com ⋯ → **Salvar paleta…** para que todos os arquivos do jogo usem as mesmas cores.

## Experimente
[[exercise:palette-ramp]]
Preencha as **cinco caixas** com uma cor cada para formar uma rampa do **escuro** (esquerda) ao **claro** (direita). Cada caixa deve ser claramente mais clara que a anterior, a mais clara deve puxar para o **quente** (rumo ao amarelo) e a mais escura para o **frio** (rumo ao azul ou roxo) — e as pontas devem ter brilhos bem distantes. Use a ferramenta Retângulo (preenchido) ou o Lápis.
- [ ] Olhei minha rampa imaginando em tons de cinza e todos os degraus aparecem.
- [ ] Nenhum degrau é muito maior que os outros.
- [ ] As cores não parecem um arco-íris: o matiz muda só um pouco a cada degrau.
