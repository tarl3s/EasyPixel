# Linhas e curvas
group: fundamentals
summary: Linhas pixel perfeitas, inclinações limpas (1:1, 2:1, 3:1) e curvas suaves sem serrilhado.

## Uma linha é uma escada
Numa grade não existem diagonais de verdade. Toda linha que não seja perfeitamente horizontal ou vertical vira uma escada de pequenos trechos horizontais (ou verticais). O segredo das linhas limpas em pixel art é controlar esses trechos: o tamanho de cada um e como eles se sucedem.

Quando os trechos seguem um padrão regular, o olho os junta numa linha suave. Quando o tamanho muda ao acaso, o olho enxerga cada degrau e a linha parece quebrada. Esses degraus visíveis e desiguais são chamados de **serrilhado** (*jaggies*).

[[example:jaggies-bad]] Serrilhado: trechos de 1, 4, 1, 2, 5, 1… pixels. A linha balança.
[[example:jaggies-good]] Trechos de 2, 3, 2, 3, 2 que se repetem por igual: a mesma inclinação, agora reta.

As duas linhas sobem a mesma altura na mesma largura. A única diferença é como os pixels são divididos entre os trechos. Essa é toda a habilidade desta lição.

## As inclinações limpas
Algumas inclinações são perfeitamente regulares numa grade. Decore-as, porque você vai usá-las o tempo todo:

- **1:1** — um pixel para o lado, um para cima. Uma diagonal perfeita de 45°, todos os trechos com 1 pixel.
- **2:1** — dois para o lado, um para cima. Trechos de 2. É a inclinação da maior parte da arte isométrica.
- **3:1** — três para o lado, um para cima. Trechos de 3, uma inclinação suave.
- **1:2** e **1:3** — o mesmo, mas em pé: trechos de 2 ou 3 pixels subindo, um para o lado.

[[example:slopes]] Da esquerda para a direita: 1:1, 2:1, 3:1 e uma linha íngreme 1:2.

Qualquer outro ângulo precisa de uma mistura de tamanhos, por exemplo 2, 3, 2, 3 para uma inclinação entre 2:1 e 3:1. Tudo bem, desde que o padrão se repita de forma uniforme. Um padrão como 2, 2, 3, 2, 2, 3 parece reto; um como 1, 4, 2, 3 nunca vai parecer. Quando você pode escolher o ângulo, prefira uma das inclinações limpas: elas sempre ficam nítidas.

> A **ferramenta Linha (L)** desenha retas para você. Arraste do início ao fim e observe o tamanho dos trechos: se saírem desiguais, mova o ponto final um ou dois pixels até ficarem regulares.

## Duplos: o pixel extra no canto
Quando você desenha uma linha à mão livre com o Lápis, normalmente aparecem pixels extras a cada degrau, em forma de L: a linha vai para o lado, depois para cima, depois para o lado. Esses pixels de canto deixam a linha grossa e serrilhada em alguns pontos e fina em outros. Eles se chamam **duplos** (*doubles*).

[[example:doubles-bad]] Uma diagonal à mão livre com um canto em L a cada degrau.
[[example:doubles-good]] A mesma linha sem os pixels de canto: um pixel de espessura em todo lugar.

Ligue **Pixel perfeito** na barra de opções do Lápis (no alto). O EasyPixel então remove o canto de cada L enquanto você desenha, e as linhas à mão livre saem limpas. Você também pode tirar os duplos à mão com a Borracha: um pixel é um duplo quando encosta em um pixel ao lado e em um pixel acima ou abaixo, e esses dois também se tocam no canto.

! Deixar uma linha "mais grossa" duplicando-a de forma irregular. Se a linha precisa ser mais grossa, faça com dois pixels de espessura em toda a extensão, de propósito. Uma linha com um pixel num ponto e dois em outro parece erro.

## Curvas são inclinações que mudam
Uma curva é uma linha cuja inclinação muda o tempo todo. Em pixels, isso significa que o tamanho dos trechos muda, e o segredo de uma curva suave é que eles mudem **aos poucos e em uma só direção**. Em um quarto de círculo, os trechos vão encurtando: por exemplo 5, 3, 2, 1, 1, 1 e depois as mesmas contagens subindo: 1, 1, 1, 2, 3, 5.

[[example:curve-bad]] Trechos de 3, 1, 4, 1, 2…: a curva tem calombos e partes achatadas.
[[example:curve-good]] Trechos de 5, 3, 2, 1, 1, 1: a curva dobra com suavidade.

A regra prática: nunca coloque um trecho longo entre dois curtos, nem um curto entre dois longos, a menos que você queira um calombo ali. Se os tamanhos forem 4, 3, 2, 2, 1, 1, a curva é suave. Se forem 4, 2, 3, 1, 2, a curva balança, mesmo que o formato geral esteja certo.

> Para conferir uma curva, conte os trechos em voz alta. Se a sequência sobe e desce como "4, 2, 3, 1", corrija o que destoa. A maioria dos calombos é um trecho com um pixel a mais ou a menos.

! Copiar uma curva de um desenho suave pixel por pixel. Um círculo perfeito traçado a partir de um desenho vetorial raramente dá trechos limpos. Desenhe a curva e depois ajuste os trechos à mão até a sequência ficar suave.

## Linhas em sprites de verdade
Num sprite pronto, as linhas são principalmente contornos e bordas: as costas de um personagem, a aba de um chapéu, a curva de um escudo. As mesmas regras valem. O contorno de um personagem com trechos limpos parece firme e legível. Um com serrilhado parece peludo, mesmo que as cores e o sombreamento estejam bons. Antes de colorir um sprite, percorra o contorno uma vez e confira cada escada e cada curva.

Bordas retas e longas ficam mais fáceis com as inclinações limpas, e é por isso que tantos prédios e plataformas em jogos de pixel art são desenhados em 1:1 e 2:1. Para coisas orgânicas, como cabelo, folhas e nuvens, use curvas com trechos que mudam suavemente e evite longas linhas perfeitamente retas, que deixam tudo rígido.

## Experimente
[[exercise:clean-lines]]
No guia você vai encontrar pares de pontos vermelhos. Desenhe uma linha entre cada par: a primeira com inclinação 1:1, a segunda 2:1 e a terceira 3:1. Depois, se quiser, siga o arco pontilhado no canto inferior direito com uma curva suave. Use **uma cor** (no máximo duas), deixe cada linha com **um pixel de espessura** e **sem duplos**. A ferramenta Linha ou o Lápis com Pixel perfeito ligado ajudam.
- [ ] Minha linha 2:1 avança exatamente dois pixels para o lado a cada pixel para cima, do começo ao fim.
- [ ] Os trechos da minha curva ficam mais curtos (ou mais longos) de forma suave, sem calombos.
