.org 0x9D95
.equ test 0x1234
.db 0x12, 0x34
start:
    add a, b
#include "test2.asm"
    ld a, 10
    add b, a
    ld de, test
bar: