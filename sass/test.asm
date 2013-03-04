#define CPU15

.org 0x9D95
.equ test 0x1234
.db 0x12, 0x34, "AaBb"

#if true
    ld a, 10
#else
    ld a, 20
#endif

#ifdef CPU15
    ld a, 30
#else
    ld a, 40
#endif

#ifndef CPU15
    ld a, 50
#else
    ld a, 60
#endif