# Texturas e materiais
group: fundamentals
summary: Como fazer metal, madeira, pedra, tecido e grama parecerem o que são, com textura que não seja ruído.

## Materiais são uma questão de luz
Por que o metal parece metal e o tecido parece tecido? Não pela cor: uma camisa cinza e uma espada cinza podem usar os mesmos cinzas. A diferença está em **como o material trata a luz** e em **que marcas tem na superfície**. Materiais brilhantes fazem brilhos nítidos e fortes e muito contraste; materiais macios fazem degradês suaves; materiais ásperos quebram a luz em muitos pedacinhos.

Então, ao desenhar um material, faça duas perguntas:

1. **Quão brilhante ele é?** Isso decide o contraste entre luz e sombra e o formato do brilho.
2. **Qual é o padrão da superfície?** Veios, rachaduras, tijolos, dobras, folhas: as marcas que dizem ao olho no que ele está tocando.

[[example:materials]] Metal, madeira, pedra, tecido e grama, todos com a mesma paleta de 16 cores.

## Metal
O metal é o material mais brilhante que você vai desenhar com frequência. Ele reflete o ambiente, então tem **contraste altíssimo**: escuros bem escuros ao lado de brilhos quase brancos.

- Use uma rampa de cinza frio ou cinza-azulado com um grande salto até a cor do brilho.
- Faça os brilhos **nítidos e longos**: riscos finos ao longo da forma (um risco diagonal numa placa lisa, uma linha ao longo da lâmina de uma espada), não pontos redondos e suaves.
- Coloque o escuro mais escuro bem ao lado da luz mais clara. Esse contraste duro é o que se lê como "brilhante".
- Acrescente detalhes que digam "fabricado": rebites (um pixel claro com um escuro embaixo à direita), emendas, arranhões.

O ouro também é metal: uma rampa de marrom-alaranjado a amarelo e a quase branco, com os mesmos brilhos nítidos.

## Madeira
A madeira é quente, fosca e **direcional**: o veio corre ao longo da tábua.

- Use uma rampa quente (vermelhos e laranjas, escurecidos em direção ao roxo).
- Desenhe o veio como **linhas longas e levemente onduladas** em um tom mais escuro, acompanhando o comprimento da tábua. Quebre as linhas com frequência; listras contínuas parecem código de barras.
- Tábuas têm uma borda superior clara e um vão escuro entre elas.
- Um nó (um pequeno oval escuro com o veio se curvando ao redor) torna a madeira reconhecível na hora. Um ou dois bastam.

## Pedra
A pedra é fosca e **volumosa**. A textura vem de blocos, rachaduras e lascas, não de ruído.

- Use uma rampa de cinza com variação para o frio (cinza-azulado) nas sombras e levemente quente na luz.
- Divida a superfície em **blocos irregulares** separados por linhas escuras de argamassa. Varie os tamanhos.
- Dê a cada bloco uma borda clara em cima à esquerda e uma mais escura embaixo à direita, para parecer estufado.
- Acrescente algumas **rachaduras** (linhas escuras curtas que mudam de direção) e lascas. Deixe-as em grupos.

## Tecido
O tecido é macio: **pouco contraste, transições suaves, dobras**.

- Use uma rampa com degraus pequenos e uniformes; evite brilhos quase brancos, a não ser que seja seda ou cetim.
- Desenhe as dobras como um vinco escuro com uma crista clara ao lado. As dobras partem de onde o tecido é segurado ou puxado (ombros, cinto, joelhos) e se abrem em leque.
- Costuras e barras (uma linha de pontos tracejados) dizem "pano".

## Grama e folhagem
A grama é uma **massa de pequenas formas repetidas**: folhinhas, tufos, folhas.

- Não desenhe cada folha. Desenhe pequenos grupos (um "v" ou um tufo de 2 a 3 pixels) num tom mais claro sobre uma base média, com um pixel mais escuro embaixo de cada um.
- Varie o espaçamento e evite fileiras retas.
- Para arbustos e árvores, pense em tufos de folhas, cada tufo sombreado como uma bolinha, em vez de folhas soltas.

## Textura versus ruído
O erro de textura mais comum é o **ruído**: pixels soltos de cores aleatórias espalhados pela superfície "para deixar áspera". Em 1× parece sujeira ou chuvisco de TV, e destrói o sombreamento.

[[example:texture-noise]] Ruído: pontinhos aleatórios por todo lado. Parece sujo, não parece pedra.
[[example:texture-clusters]] Textura: as mesmas cores agrupadas em blocos, linhas de argamassa, bordas e algumas rachaduras. Parece uma parede de pedra.

Uma boa textura é feita de **aglomerados**: grupos de pixels que formam uma figura — uma rachadura, uma linha de tábua, uma dobra. Como regra prática, a maioria dos pixels deve ter pelo menos um vizinho da mesma cor. Se você achar muitos pixels solitários, junte-os em linhas e manchas ou remova.

! Espalhar pixels soltos aleatórios para fingir aspereza. Faça a textura com aglomerados e linhas que acompanhem o material: veios ao longo da madeira, rachaduras na pedra, dobras no tecido.

## A textura acompanha a forma
A textura não é um adesivo colado sobre uma forma chapada. Ela precisa acompanhar o sombreamento e a forma do objeto:

- No lado iluminado, as marcas de textura são **mais claras e mais raras**; na sombra são mais escuras e podem ser mais densas.
- Num objeto redondo, as linhas se curvam com a superfície: as tábuas de um barril se curvam, as listras de uma camisa envolvem o corpo.
- O detalhe diminui com a distância: uma parede distante precisa de menos textura do que a parede ao lado da qual o jogador está.

> Desenhe o sombreamento primeiro com tons chapados e acrescente a textura por cima. Se a textura briga com o sombreamento (marcas claras na sombra), o objeto perde o volume.

! Usar a mesma densidade de textura em todo lugar. Grandes áreas lisas com textura densa cansam; deixe espaços calmos para o olho descansar e ponha mais detalhe onde o jogador olha.

## Experimente
[[exercise:material-block]]
Desenhe um **bloco de 16×16 de um material** — uma parede de pedra, uma caixa de madeira, uma placa de metal, uma parede de tijolos ou um pedaço de tecido. Preencha **todos os pixels**, use **de 3 a 5 cores** e dê a ele uma textura que se leia bem: blocos, veios, rebites, dobras. Mantenha **em aglomerados**: nem liso, nem ruído.
- [ ] Alguém olhando em 1× saberia dizer de que material se trata.
- [ ] Minhas marcas de textura são aglomerados e linhas, não pixels soltos espalhados.
- [ ] A luz vem de um lado só e a textura a acompanha.
