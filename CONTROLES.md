# Controles

O jogo usa os bindings genéricos `<Gamepad>` do Unity Input System para controles reconhecidos como Xbox, DualShock e DualSense. Teclado e mouse continuam disponíveis.

| Ação | Xbox | PlayStation |
| --- | --- | --- |
| Mover | Analógico esquerdo ou direcional | Analógico esquerdo ou direcional |
| Atacar | X | Quadrado |
| Abrir/fechar inventário | Y | Triângulo |
| Selecionar slot anterior/próximo | LB / RB | L1 / R1 |
| Marcar item e confirmar troca no inventário | A | Cruz |
| Cancelar troca ou fechar inventário | B | Círculo |
| Pausar/retomar | Menu (Start) | Options |
| Navegar nos menus | Analógico ou direcional | Analógico ou direcional |
| Confirmar opção ou avançar diálogo | A | Cruz |
| Voltar nos submenus | B | Círculo |

Para organizar o inventário: abra-o, marque um slot com A/Cruz, escolha o destino com os botões superiores e confirme novamente. Os slots de origem e destino ficam destacados. B/Círculo cancela a marcação; sem marcação, fecha o inventário.

Os menus selecionam um botão automaticamente quando o painel abre ou a seleção anterior deixa de estar disponível. Movimento e ataques continuam bloqueados quando o tempo do jogo está pausado. O inventário não pode abrir sobre pausa, game over ou escolha de maldição.

A opção selecionada nos menus recebe uma borda amarela, inclusive na pausa e nos submenus. A borda não captura cliques nem altera o tamanho dos botões.

Na tela de controles, um controle conectado exibe um guia com ícones de Xbox ou PlayStation (DualShock/DualSense). O guia atualiza ao conectar, desconectar ou trocar o controle em uso. Sem controle conectado, a tela original de remapeamento do teclado reaparece. O guia do controle apresenta os comandos padrão; o remapeamento continua na tela do teclado.

## Verificação no Play Mode

1. Inicie o jogo pelo menu usando apenas o controle e avance a intro com A/Cruz.
2. Teste movimento no analógico e no direcional; ataque com X/Quadrado.
3. Troque os oito slots com LB/RB ou L1/R1, incluindo a passagem do último ao primeiro.
4. Abra o inventário, troque dois itens e cancele uma troca. Confirme que não abre sobre outro menu pausado.
5. Pause com Menu/Options, abra opções/controles e volte com B/Círculo. Retome o jogo.
6. Escolha uma maldição e teste reiniciar/voltar ao menu na tela de game over.
7. Alterne entre controle e teclado/mouse e teste desconectar/reconectar o controle.
8. Confirme que a borda amarela acompanha a seleção ao navegar pela pausa, opções, maldições e game over.
9. Na tela de controles, conecte/desconecte um controle e confira a troca entre os ícones e o remapeamento do teclado. Compare Xbox com DualShock/DualSense.

A compilação foi verificada com as bibliotecas locais do Unity. A conexão física USB/Bluetooth e o comportamento em cada modelo de controle precisam ser verificados no equipamento de destino.
