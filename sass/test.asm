    ld a, foo
#if 0
    ld a, 10
    #if 1
        ld a, 30
    #else
        ld a, 40
    #endif
#else
foo:
    ld a, 20
#endif