# Pontilhado (dithering)
group: fundamentals
summary: Xadrez e padrões ordenados para misturar cores, fazer degradês e texturas, e quando o dithering atrapalha.

## Misturando cores que você não tem
Com uma paleta pequena, você muitas vezes vai querer uma cor intermediária entre duas das suas: um céu que vai de azul profundo a azul claro, uma sombra que suaviza aos poucos. Dá para acrescentar mais cores, mas existe um truque mais antigo: o **dithering** (pontilhado). Coloque duas cores num padrão fino e, de longe, o olho as mistura numa terceira.

O padrão mais simples é o **xadrez**: as duas cores se alternam como as casas de um tabuleiro. Em 1× ele se lê como uma mistura 50/50.

[[example:dither-checker]] Azul-marinho à esquerda, azul à direita e um xadrez das duas no meio: lê-se como uma cor entre elas.

Os jogos antigos usavam dithering em todo lugar porque as paletas eram minúsculas. Hoje é uma escolha de estilo e um jeito de manter as paletas pequenas — e, bem usado, dá à pixel art uma textura artesanal muito bonita.

## Padrões ordenados
Um xadrez é uma mistura de 50%. Para 25% ou 75%, use padrões mais esparsos: um pixel da segunda cor em cada bloco de 2×2, depois dois, depois três. Padrões que crescem em uma ordem fixa assim se chamam **dithering ordenado** (ou padrões de Bayer, por causa do homem que os descreveu). Eles são regulares, por isso ficam arrumados e não piscam em animações.

Para passar de uma cor a outra numa área, vá de 0% a 25%, 50%, 75% e 100% da segunda cor:

[[example:dither-gradient]] Ameixa virando laranja com um padrão ordenado: a densidade do laranja cresce da esquerda para a direita.

O EasyPixel tem uma ferramenta **Pincel de pontilhado**: ela pinta a cor atual apenas nos pixels de um padrão (um xadrez ou um padrão ordenado, escolhido nas opções da ferramenta), sempre alinhado com a grade da tela. Preencha uma área com uma cor, passe a outra por cima com o pincel e você tem um xadrez perfeito sem colocar cada pixel à mão.

> Mantenha o padrão alinhado à grade da imagem inteira, sem recomeçar a cada forma. Dois xadrezes desalinhados fazem uma emenda feia onde se encontram.

## Degradês sem faixas
Sem dithering, um degradê feito de quatro cores são quatro **faixas** chapadas com degraus duros entre elas. Às vezes é exatamente o visual que você quer (é limpo e muito "pixel art"). Mas em céus, brilhos e luz suave, as bordas duras parecem listras. Uma faixa estreita de dithering em cada divisa suaviza os degraus mantendo as mesmas quatro cores.

[[example:dither-banding]] Quatro cores em faixas duras: os degraus são óbvios.
[[example:dither-smooth]] As mesmas quatro cores com uma faixa pontilhada em cada divisa: um degradê suave.

Repare que a segunda imagem **não usa cores novas**. Essa é a mágica do dithering: suavidade sem aumentar a paleta.

## Dithering como textura
O dithering também faz textura: a superfície áspera da pedra, da areia, de um tapete felpudo, o grão de uma foto antiga. Alguns pixels de um padrão na área de sombra de uma rocha fazem com que ela pareça rugosa em vez de plástica.

Num objeto sombreado, a regra é: **pontilhe só onde dois tons se encontram**, numa faixa estreita. Deixe o meio de cada tom sólido.

[[example:dither-ball-good]] Pontilhado só nas zonas estreitas onde dois tons se encontram: a bola mantém a forma e ganha uma transição suave.
[[example:dither-ball-noisy]] Pontilhado espalhado por todo lado: a bola vira ruído e perde a forma.

## Quando o dithering atrapalha
É fácil exagerar no dithering. Cuidado com estes casos:

- **Sprites pequenos.** Um xadrez num personagem de 16×16 parece sujeira. Personagens e itens costumam ler melhor com cores chapadas e sombreamento limpo.
- **Coisas em movimento.** Um padrão pontilhado num sprite que se move ou muda de escala pisca e "nada". Pontilhe fundos e objetos parados, não personagens rápidos.
- **Pares de alto contraste.** Um xadrez de preto e branco não é um cinza suave, é um padrão que vibra. Pontilhe entre cores **vizinhas** de uma rampa.
- **Colocação aleatória.** Pixels soltos espalhados não são dithering, são ruído. O dithering de verdade segue um padrão regular.

! Pontilhar entre duas cores muito distantes (preto e branco, vermelho e verde). O padrão zumbe em vez de misturar. Pontilhe apenas entre vizinhas de uma rampa.

! Cobrir todas as áreas com dithering porque parece "retrô". Áreas chapadas descansam o olho; o dithering é um tempero, não a refeição.

## Uma rotina para um degradê pontilhado
1. Escolha de 2 a 4 cores vizinhas de uma rampa (para um céu: azul-marinho, azul, azul-céu).
2. Preencha a área em faixas chapadas, do escuro ao claro, com espaçamento uniforme.
3. Em cada divisa, acrescente uma faixa de 2 a 4 pixels: primeiro um padrão esparso da cor seguinte, depois um xadrez, depois a cor seguinte com um padrão esparso da anterior.
4. Mantenha todos os padrões alinhados à grade da tela.
5. Olhe em 1×. Se ainda vê as faixas, alargue as faixas pontilhadas; se parece ruidoso, estreite-as.

## Experimente
[[exercise:dither-sky]]
Pinte um **céu** que preencha a tela inteira, **escuro no alto e claro no horizonte** (embaixo). Use **de 2 a 4 cores** e misture-as com **dithering**: xadrezes ou padrões ordenados onde uma cor se transforma na seguinte. O Pincel de pontilhado deixa isso rápido.
- [ ] Em 1× o céu parece um degradê suave, não listras.
- [ ] Pontilhei apenas entre cores vizinhas e meus padrões estão alinhados.
