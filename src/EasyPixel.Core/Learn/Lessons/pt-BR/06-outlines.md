# Contornos
group: fundamentals
summary: Contornos pretos, coloridos e seletivos (sel-out), linhas internas e quando deixar o contorno de fora.

## Por que sprites têm contorno
Um contorno (*outline*) é uma linha de pixels escuros em volta da borda de um sprite. Ele separa o sprite do que estiver atrás, para que a forma seja legível em qualquer fundo: céu claro, caverna escura, grama cheia de detalhes. É por isso que a maioria dos sprites de jogos, principalmente personagens e itens que o jogador precisa notar, tem um.

[[example:outline-none]] Sem contorno: a borda da maçã depende do fundo que está atrás.
[[example:outline-black]] Um contorno preto: a forma é legível em qualquer fundo.

O contorno também faz parte do estilo. Um contorno grosso e escuro parece ousado e cartunesco; um colorido e suave parece delicado e pictórico; nenhum contorno parece macio e realista. Escolha um estilo para o jogo todo e use em todos os sprites do mesmo tipo.

## As regras de um contorno limpo
- **Um pixel de espessura.** Em tamanhos de sprite, um contorno de dois pixels come metade do desenho. Mantenha um pixel em todo lugar, exceto onde você engrossa de propósito para dar peso (veja abaixo).
- **Linhas limpas.** O contorno é uma linha, então tudo da lição Linhas e curvas vale aqui: inclinações limpas, curvas suaves, **sem duplos**. A maioria dos sprites com cara de peludos tem duplos no contorno.
- **Fechado.** Dê a volta inteira. Uma abertura deixa o fundo "vazar" para dentro do sprite.
- **Por fora ou na borda.** Em geral você desenha o contorno nos pixels logo fora da forma preenchida. O EasyPixel pode fazer isso por você: **Sprite → Contorno** acrescenta um contorno de um pixel em volta de tudo o que está na camada atual, na cor primária.

> Desenhe o contorno depois de montar a forma e antes de sombrear. Fica mais fácil julgar o sombreamento ao lado de um contorno, e corrigir o contorno depois significa sombrear a borda de novo.

! Contornar cada pequeno detalhe dentro do sprite com a mesma linha preta. O sprite vira página de livro de colorir. Deixe o preto para a borda externa; por dentro, use linhas mais suaves (veja Linhas internas abaixo).

## Contornos coloridos
O preto puro é forte, mas pode parecer duro, principalmente em sprites suaves ou vivos. Um **contorno colorido** usa uma versão bem escura de uma cor em vez do preto: um roxo profundo, um marrom escuro, um azul escuro. Ele continua separando o sprite do fundo, mas se mistura melhor com as cores de dentro.

[[example:outline-colored]] Um contorno ameixa escuro em vez de preto: mais suave, e ainda legível.

Uma boa escolha é a cor mais escura da sua paleta que não seja preto, muitas vezes um roxo ou azul escuro, porque combina com as sombras frias do sombreamento com variação de matiz.

## Contornos seletivos (sel-out)
O estilo mais refinado é o **contorno seletivo**, ou sel-out. Em vez de uma cor em toda a volta, cada parte do contorno é um **tom mais escuro da cor em que ela encosta**. Junto da pele vermelha da maçã, o contorno é vermelho escuro; junto da folha verde, é verde escuro. E ele acompanha a luz: no lado iluminado o contorno é mais claro (às vezes quase some), no lado da sombra é o mais escuro.

[[example:outline-selout]] Sel-out: cada pixel do contorno é um tom mais escuro do vizinho, mais escuro no lado da sombra.

Regras práticas do sel-out:

- Pegue a cor do contorno da rampa do material em que ele encosta, um ou dois degraus abaixo do tom mais escuro que está ao lado.
- No lado voltado para a luz, use um tom mais claro da rampa, ou a cor base, para que aquela borda brilhe.
- No lado da sombra, use a cor mais escura daquela rampa, ou a cor do seu contorno, para que o sprite continue se separando de fundos escuros.
- Onde dois materiais encostam no fundo bem perto um do outro, escolha um; não coloque duas cores de contorno lado a lado.

> Comece com um contorno preto ou colorido, sombreie o sprite e depois repinte o contorno aos poucos. O Conta-gotas (I ou Alt) pega a cor ao lado do contorno para você escolher o próximo tom mais escuro da paleta.

! Deixar o sel-out tão claro que o contorno some também no lado da sombra. O lado da sombra precisa continuar escuro, senão o sprite se funde a fundos escuros.

## Linhas internas
Dentro de um sprite você ainda precisa de linhas para separar as partes: um braço na frente do corpo, um cinto, a borda de um capuz. Se você as desenhar em preto como o contorno externo, o sprite fica poluído e chapado. Use **linhas internas**: um tom mais escuro da cor ao redor, um ou dois degraus abaixo na rampa, não preto.

[[example:inner-lines]] O contorno externo é preto; onde o braço cruza o corpo, uma linha interna verde escura mantém os dois separados sem cortar a forma ao meio.

Use o mínimo possível de linhas internas. Muitas vezes uma mudança de tom (braço claro contra corpo sombreado) separa as partes sem linha nenhuma.

## Espessura e peso
Alguns artistas engrossam o contorno no lado da sombra e na parte de baixo do sprite, com dois pixels em vez de um em alguns pontos. Isso sugere peso e sombra e faz o sprite parecer plantado no chão. Faça de propósito e do mesmo jeito em todos os sprites, ou não faça.

## Quando deixar o contorno de fora
Nem tudo precisa de contorno. Fundos e cenários distantes normalmente não têm: contornos puxam as coisas para a frente, então fundos contornados disputam a atenção com os personagens. Tiles grandes, efeitos como fogo e fumaça e coisas macias como nuvens costumam ficar melhores sem. Uma receita comum: personagens, itens e objetos interativos com contorno; fundo sem. Assim o jogador vê de relance o que pode tocar.

## Experimente
[[exercise:outline-apple]]
A tela tem uma maçã sem contorno. Dê a ela um **contorno fechado**, de um pixel de espessura, em toda a volta da maçã, do cabo e da folha. Use preto, uma cor escura, ou experimente um sel-out com tons mais escuros de vermelho e verde. O Lápis com Pixel perfeito ligado, ou **Sprite → Contorno**, ajudam a começar.
- [ ] Meu contorno tem um pixel de espessura e não tem duplos.
- [ ] (Bônus) Tentei um sel-out: tons mais escuros do preenchimento em vez de uma cor só, mais escuros no lado da sombra.
