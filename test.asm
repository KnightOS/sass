    LD A, 1
label1:
    LD B, 2
    LD A, B \ LD B, A
    LD HL, label1
    LD (label1), A
    LD (IX + 14), A
    .dw 0x15
    .echo "Hello, world!", $
    .fill 20, $
    .org 0x1234
label2:
    LD BC, label2
#include "test2.asm"