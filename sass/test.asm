#if 1
    ld a, 10
#else
    #if 1
        ld a, 20
    #else
        ld a, 30
    #endif
    ld a, 40
#endif