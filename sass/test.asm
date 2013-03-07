.define USB

#ifdef USB
    ld a, 10
#else
    #ifdef notdefined
        ld a, 20
    #else
        ld a, 30
    #endif
#endif