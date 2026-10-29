# Itens e ícones
group: styles
summary: Itens e ícones legíveis em tamanhos pequenos, formas marcantes, luz e contorno consistentes em um conjunto inteiro.

## Imagens pequenas com um trabalho grande
Itens e ícones são a menor arte de um jogo e uma das mais importantes: uma poção no inventário, uma moeda no chão, uma chave no HUD, uma espada na loja. O jogador precisa reconhecê-los **instantaneamente**, em **16×16 ou menos**, muitas vezes por uma fração de segundo enquanto outra coisa está acontecendo.

Isso faz dos ícones uma lição de **legibilidade**. Um ícone bonito que o jogador não consegue identificar em 1× fracassou.

[[example:icon-set]] Um conjunto de cinco ícones 16×16: espada, poção, chave, moeda, escudo. Mesmo contorno, mesma luz vinda do alto à esquerda, mesma paleta.
[[example:icon-set-1x]] O mesmo conjunto no tamanho real. É assim que o jogador o vê: cada um ainda é legível.

## Primeiro a forma
A silhueta carrega a maior parte do significado, ainda mais que nos personagens:

- **Use o espaço todo.** Um ícone deve preencher a maior parte do seu quadrado: uma espada vai de canto a canto, uma poção ocupa a altura toda. Objetos pequenos no meio de um quadrado grande e vazio são difíceis de ver.
- **Exagere a parte que o identifica.** Uma chave é reconhecida pelo anel redondo e pelos dentes, então faça-os grandes. Uma poção, pelo frasco redondo e o gargalo estreito. Uma espada, pela guarda.
- **Use formas icônicas, não realistas.** O coração "clássico", a moeda redonda, o escudo em forma de brasão — os jogadores conhecem essas formas de muitos jogos. Lutar contra a convenção torna o seu ícone mais difícil de ler.
- **Incline objetos compridos na diagonal.** Uma espada, um cajado ou uma varinha cabe melhor num espaço quadrado de canto a canto, em geral com o cabo embaixo à esquerda.

## Menos detalhes, pixels mais marcantes
Em 16×16 não há espaço para detalhes pequenos. Um detalhe de um pixel ou é ruído ou é invisível.

[[example:icon-detail-bad]] Detalhe demais: uma lâmina de um pixel, muitas cores, uma gema e pontinhos. Em 1× parece um arranhão.
[[example:icon-detail-good]] A mesma espada com uma lâmina marcante de dois pixels, cinco cores e um contorno: clara em qualquer tamanho.

Regras práticas:

- **3 a 6 cores** por ícone, além do contorno. Cada cor é uma parte (lâmina, guarda, cabo) ou um degrau de luz.
- **Faça as partes finas com pelo menos 2 pixels de largura** sempre que der. Uma lâmina de 1 pixel se perde.
- **Use forte contraste de valor**: um brilho claro ao lado de uma sombra escura. Uma moeda precisa de um amarelo vivo e um laranja escuro, não de dois amarelos parecidos.
- **Um único brilho.** Um pixel ou um risco claro no alto à esquerda diz "brilhante" — perfeito para poções, moedas e gemas.

! Desenhar um ícone reduzindo uma pintura grande do objeto. Os detalhes viram borrão, as cores se multiplicam e a forma fica fina. Desenhe os ícones no tamanho real, pixel por pixel.

## Contorno e fundo
Os ícones aparecem sobre muitos fundos: espaços do inventário, o chão, a vitrine de uma loja, o escuro de uma caverna. Um **contorno** os mantém legíveis em todos eles.

- Um contorno escuro em volta de tudo é a escolha mais segura (um contorno colorido ou seletivo também funciona, se continuar mais escuro que todos os fundos sobre os quais vai ficar).
- Deixe **uma margem de 1 pixel** dentro do espaço, para o contorno não ser cortado e os ícones não se encostarem numa grade.
- Não aplique antialiasing na borda externa: o fundo atrás do ícone muda (veja o halo na lição de Antialiasing).

## Consistência em um conjunto
Os ícones são sempre vistos **juntos**: uma fileira de espaços do inventário, uma lista de loja. Um conjunto parece profissional quando todo ícone segue as mesmas regras:

- **A mesma direção de luz** — em geral do alto à esquerda — para os brilhos ficarem do mesmo lado em todos.
- **A mesma cor e a mesma espessura de contorno.**
- **A mesma paleta** e o mesmo número de tons por material: todos os metais usam a mesma rampa de aço, todo ouro a mesma rampa de ouro.
- **O mesmo tamanho no espaço.** Objetos grandes (um escudo) e pequenos (um anel) preenchem a maior parte do espaço; os ícones não são desenhados em escala entre si.
- **O mesmo estilo de detalhe**: se um ícone tem um risco de brilho, todos têm.

> Desenhe o conjunto inteiro numa só tela, lado a lado (por exemplo 5 espaços de 16×16 numa tela de 88×16). Você vê na hora se um ícone está escuro demais, pequeno demais ou iluminado do lado errado. Depois separe-os na hora de exportar.

! Fazer cada ícone num dia diferente, com cores diferentes. O conjunto parece vir de cinco jogos. Mantenha um arquivo de paleta e um "ícone de referência" para comparar com cada ícone novo.

## Itens no mundo
Itens largados no chão do jogo (uma moeda para coletar, um coração, uma chave) seguem as mesmas regras, mais duas:

- **Eles precisam se destacar dos tiles.** Faça os itens mais brilhantes e mais saturados que o chão, e contorne-os.
- **Um pouco de movimento ajuda**: um sobe e desce de 2–4 quadros, um brilho que passa pela moeda, uma pulsação lenta. Coisas em movimento chamam a atenção (veja a lição de Efeitos para brilhos).

## Uma rotina para um ícone
1. Crie uma tela 16×16 (ou o tamanho de ícone do seu jogo). Lembre-se da margem de 1 pixel.
2. Monte a silhueta com uma cor, preenchendo a maior parte do espaço.
3. Acrescente cores chapadas para cada parte (lâmina, guarda, cabo).
4. Acrescente um tom de sombra embaixo à direita e um brilho no alto à esquerda.
5. Faça o contorno.
6. Confira em 1×: **Quadro → Janela de prévia**, ou dê zoom a 100%. Se não dá para saber o que é, simplifique.

## Experimente
[[exercise:item-icon]]
Desenhe um **ícone de item 16×16** — uma poção, uma chave, uma moeda, um coração, um livro, o que o seu jogo precisar. Faça-o **preencher o espaço** (pelo menos 70% da largura ou da altura), mantenha uma **margem de 1 pixel**, dê a ele um **contorno**, use **3 a 8 cores** com **forte contraste** entre a mais escura e a mais clara, e desenhe-o **em uma só peça**.
- [ ] Olhei em 1× e sei na hora o que é.
- [ ] A luz vem do alto à esquerda, como nos meus outros ícones.
- [ ] Não há detalhes de um pixel que virem ruído.
