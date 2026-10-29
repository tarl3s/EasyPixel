# Anti-aliasing
group: fundamentals
summary: Suavizar os degraus das curvas com cores intermediárias, à mão: onde ajuda e onde atrapalha.

## O problema da escada
Um pixel é um quadrado, então toda curva e toda linha inclinada na pixel art é, na verdade, uma escada. Na lição Linhas e curvas você aprendeu a deixar essas escadas limpas: trechos regulares, sem cantos duplos. Mas até uma curva limpa parece um pouco serrilhada quando é grande e o contraste é alto, como uma linha preta num fundo branco.

O **anti-aliasing** (AA, para abreviar) suaviza os degraus. Onde a linha passa de uma fileira para a seguinte, você acrescenta um pixel de uma **cor intermediária** — mais escura que o fundo, mais clara que a linha — no canto do degrau. De uma distância normal o olho mistura, e a curva parece mais suave do que a grade permite de verdade.

[[example:aa-none]] Uma curva limpa sem anti-aliasing: correta, mas os degraus aparecem.
[[example:aa-good]] A mesma curva com um pixel intermediário em cada degrau: a curva parece mais suave. Os lados retos e o meio a 45° (uma diagonal 1:1) ficam como estão.

Programas de pintura fazem isso automaticamente em cada pincelada. Artistas de pixel art fazem **à mão**, só onde ajuda, porque o anti-aliasing automático borra tudo e enche a imagem de cores que você não escolheu.

## Como posicionar os pixels de AA
Olhe para um degrau da sua curva: a linha corre ao longo de uma fileira e depois salta na diagonal para a seguinte. Os dois pixels no canto desse salto, um de cada lado da linha, são o lugar do AA.

1. **Escolha o pixel de canto mais próximo da curva "verdadeira"** — por onde a linha passaria se pudesse atravessar um pixel. Em geral é o lado de dentro do degrau, o canto que a curva quer preencher.
2. **Escolha a cor pela quantidade de curva que passa por ele.** Se a curva verdadeira cobre quase todo aquele pixel, use uma cor próxima da linha; se só roça, use uma cor próxima do fundo. Dois tons intermediários (por exemplo um cinza escuro e um claro entre o preto e o branco) já bastam.
3. **Trechos mais longos pedem AA mais longo.** Onde a linha corre reta por 4 ou 5 pixels antes de subir, o AA pode ter 2 pixels de comprimento, ficando mais claro longe do degrau.
4. **Deixe em paz as retas e as diagonais 1:1.** Uma linha perfeitamente horizontal, vertical ou de 45° não tem degraus ásperos para suavizar.

> Use cores da sua paleta como cores de AA. O meio de uma rampa entre a cor da linha e a do fundo muitas vezes já tem o tom de que você precisa.

## Onde não usar
O anti-aliasing é uma ferramenta para curvas grandes e de alto contraste. Em muitos lugares ele atrapalha:

- **Sprites muito pequenos.** Num personagem de 16×16, um pixel de AA é do tamanho de um olho. Ele borra os detalhes em vez de suavizar. Sprites pequenos ficam mais nítidos sem nenhum AA.
- **Partes retas.** Pixels colocados ao lado de uma reta só a deixam grossa e felpuda.
- **Em todo lugar ao mesmo tempo.** AA em todas as bordas transforma pixel art nítida numa imagem borrada, com cara de foto reduzida.

[[example:aa-overdone]] Exagero: pixels cinza ao lado de cada pixel da linha, inclusive nos lados retos. A forma fica mole e suja.

! Aplicar anti-aliasing em todas as bordas "para ficar suave". O AA só pertence aos cantos onde a curva dá o degrau; em retas e sprites pequenos ele só acrescenta borrão.

## AA contra a transparência: o halo
Existe uma armadilha com sprites: o fundo em que você desenhou não é o fundo que o jogo vai mostrar. Se você aplicar anti-aliasing na **borda externa** de um sprite contra uma página branca, esses pixels intermediários claros ficam no sprite. Coloque o sprite numa caverna escura no jogo e ele brilha com uma franja clara, um **halo**.

[[example:aa-halo]] Esquerda: a moeda recebeu AA contra o branco, então uma franja clara a contorna no fundo escuro do jogo. Direita: sem AA na borda externa.

Portanto:

- **Não aplique anti-aliasing na borda externa** de um sprite que se move sobre fundos diferentes. Mantenha o contorno nítido.
- Aplique anti-aliasing **por dentro** do sprite, entre as cores dele, onde você conhece os dois lados (um escudo redondo contra o braço que o segura, uma curva dentro de um rosto).
- Para um tile ou fundo que sempre é desenhado sobre a mesma cor, o AA contra essa cor não tem problema.

! Aplicar anti-aliasing no contorno de um personagem numa tela branca e depois estranhar o anel pálido que aparece no jogo. Confira os sprites sobre um fundo escuro antes de exportar (uma camada escura temporária embaixo do desenho resolve).

## AA, contornos e pontilhado
O anti-aliasing trabalha junto com as outras técnicas:

- Com um **contorno preto**, pixels de AA entre o contorno e o preenchimento (dentro da forma) suavizam cabeças e barrigas redondas sem tocar na borda externa.
- Com **contornos seletivos** (sel-out), o próprio contorno já muda de cor ao redor da forma, o que funciona como um anti-aliasing embutido.
- O **pontilhado** (*dithering*, na próxima lição) mistura duas cores com um padrão; o AA as mistura com uma única cor intermediária. O AA é para bordas, o pontilhado é para áreas.

## Uma rotina
1. Desenhe a curva limpa primeiro: pixel perfeita, trechos regulares, sem duplos. O AA não salva uma linha bagunçada.
2. Decida se a curva precisa mesmo de AA: ela é grande, de alto contraste e importante? Se não, pare.
3. Acrescente um pixel intermediário em cada degrau, no canto mais próximo da curva verdadeira.
4. Em trechos longos, acrescente um segundo pixel, mais claro, ao longo do trecho.
5. Olhe em 1× em **Quadro → Janela de prévia**. Se a curva parece mais grossa ou mais borrada em vez de mais suave, remova pixels.

## Experimente
[[exercise:aa-curve]]
A tela tem uma forma preta sobre branco: dois **lados retos** e uma **curva**. Suavize a curva com anti-aliasing: acrescente **pelo menos 6** pixels intermediários (os cinzas da paleta) nos cantos onde a curva passa de uma linha ou coluna para a seguinte. Deixe os lados retos em paz e mantenha cada pixel de AA encostado na linha.
- [ ] Meus pixels de AA estão nos cantos dos degraus, não ao longo dos lados retos.
- [ ] Olhando em 1×, a curva parece mais suave, não mais grossa.
