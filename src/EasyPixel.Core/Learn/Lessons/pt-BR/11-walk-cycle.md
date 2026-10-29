# Ciclo de caminhada
group: fundamentals
summary: As poses de contato e de passagem, caminhadas de 4 e 6 quadros, o sobe e desce do corpo, os braços e andar no lugar.

## O que é um ciclo de caminhada
Um **ciclo de caminhada** é uma animação curta em loop: o personagem dá dois passos (pé esquerdo, pé direito) e termina exatamente onde começou, pronto para recomeçar. Num jogo, o ciclo toca **no lugar** — o personagem não anda pela tela — e é o jogo que move o sprite pela fase enquanto o ciclo toca.

Todo mundo vê gente andando todo dia, então uma caminhada errada é notada na hora. A boa notícia: uma caminhada de pixel art convincente precisa de apenas **4 a 8 quadros**, e é construída com algumas poses-chave que sempre funcionam.

## As poses-chave
Todo passo de uma caminhada passa pelas mesmas poses:

- **Contato**: as pernas estão o mais afastadas possível. O calcanhar da frente acabou de tocar o chão, e o pé de trás está prestes a empurrar. Essa pose define a passada.
- **Descida** (ou amortecimento): o pé da frente recebe o peso, o joelho dobra e o corpo inteiro fica no ponto **mais baixo**.
- **Passagem**: a perna de trás balança para a frente e passa por baixo do corpo. A perna de apoio está esticada, então o corpo fica no ponto **mais alto**.
- **Subida**: o corpo se impulsiona rumo ao próximo contato.

Depois as mesmas poses se repetem com as pernas trocadas. Dois passos formam um ciclo completo.

[[example:walk-poses]] Contato, descida e passagem: as pernas afastadas, o peso aterrissando (corpo mais baixo), uma perna sob o corpo (corpo mais alto). A linha pontilhada marca a altura da cabeça na pose de contato.

## A caminhada de 4 quadros
A caminhada mais simples usa só as poses de contato e de passagem:

1. Contato (perna direita à frente).
2. Passagem (corpo 1 pixel mais alto).
3. Contato (perna esquerda à frente).
4. Passagem (corpo 1 pixel mais alto).

[[example:walk-4frame]] Uma caminhada de 4 quadros: contato, passagem, contato com as pernas trocadas, passagem. 150 ms por quadro.

Isso basta para sprites pequenos (16 a 24 pixels de altura). Dê a cada quadro de 120 a 180 ms. Mais rápido que isso e o personagem parece correr parado; mais lento e parece andar na lama.

## A caminhada de 6 quadros
Com mais pixels para trabalhar, acrescente a pose de **descida** a cada passo: contato, descida, passagem, e depois o mesmo com as pernas trocadas. O corpo afunda na "descida" e sobe na "passagem", o que dá peso e ritmo à caminhada.

[[example:walk-6frame]] Uma caminhada de 6 quadros: contato, descida (mais baixo), passagem (mais alto), e o mesmo com a outra perna.

Uma caminhada de 8 quadros acrescenta também a pose de **subida**. Mais quadros deixam a caminhada mais suave, mas cada um precisa ser desenhado para cada direção em que o personagem olha, então a maioria dos jogos para em 6 ou 8.

## O sobe e desce
O corpo **sobe e desce** durante a caminhada: mais alto na passagem, mais baixo na descida (ou no contato, numa caminhada de 4 quadros). Num sprite pequeno a diferença é de apenas **1 pixel**, e esse único pixel é o que faz a caminhada parecer viva. Sem ele, o personagem desliza como se estivesse sobre rodinhas.

> Mova toda a parte de cima do corpo junta — cabeça, tronco e braços — quando ele sobe e desce. Se só a cabeça se mexe, parece que o personagem está balançando a cabeça.

## Pernas, braços e o lado de trás
- **Os pés ficam na linha do chão.** Em todo quadro pelo menos um pé toca o chão, e nenhum pé fica abaixo dele. Um sprite cujos pés flutuam ou afundam em alguns quadros vai pular de um jeito estranho no jogo.
- **Os braços balançam no sentido oposto ao das pernas.** Quando a perna direita está à frente, o braço direito está atrás. Esse contrabalanço equilibra o corpo. Em sprites pequenos, um braço é só um traço de 2–3 pixels que vai para a frente e para trás.
- **Os membros do lado de trás são mais escuros.** A perna e o braço do lado mais distante do corpo ficam na sombra. Desenhá-los um tom mais escuro os separa dos da frente e deixa a pose legível.
- **Incline-se levemente para a frente** para uma caminhada decidida, e para trás para uma caminhada relaxada ou orgulhosa.

! Deslizar o personagem para a frente pela tela de um quadro para o outro. O ciclo precisa ficar no lugar: é o jogo que move o sprite. Se a sua caminhada se desloca, o personagem vai voltar de um salto toda vez que o loop recomeçar.

! Desenhar as pernas como dois palitos que balançam como uma tesoura, com o corpo fixo numa só altura. Sem o sobe e desce e sem a pose de descida, a caminhada não tem peso.

## Montando passo a passo
1. Desenhe a pose de **contato**. Confira se os pés estão na linha do chão.
2. Copie o quadro (**Quadro → Novo quadro**) e desenhe a pose de **passagem**: uma perna sob o corpo, a outra levantada um pouco atrás, corpo 1 pixel mais alto.
3. Copie o primeiro quadro de contato e **troque as pernas** (e os braços). Repita para a segunda pose de passagem.
4. Ative **Exibir → Papel vegetal** (onion skin) para comparar cada quadro com os vizinhos, e corrija os saltos.
5. Toque com **Quadro → Repetir** ligado e veja em **Quadro → Janela de prévia** em 1×.
6. Para uma caminhada de 6 quadros, insira um quadro de **descida** depois de cada contato.

## Experimente
[[exercise:walk-cycle]]
Faça um **ciclo de caminhada de pelo menos 4 quadros** para um personagem pequeno, andando **no lugar**. Em todos os quadros o pé mais baixo deve ficar **exatamente na linha do chão**, o corpo deve **subir e descer** (o topo da cabeça 1 pixel mais alto nos quadros de passagem) e o personagem não pode deslizar para os lados de um quadro para o outro. A linha pontilhada é uma referência de altura para você ver o sobe e desce.
- [ ] As pernas se trocam entre as duas poses de contato, e os braços balançam no sentido oposto ao das pernas.
- [ ] A perna de trás (e o braço) tem um tom mais escuro que a da frente.
- [ ] Toquei em Repetir e não há salto quando recomeça.
