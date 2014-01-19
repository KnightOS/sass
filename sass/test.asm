.macro setBankA(page)
    .if page & 0x80
        .echo "high"
        ld a, 1
        out (0x0E), a
        ld a, page & 0x7F
        out (6), a
    .else
        .echo "low"
        xor a
        out (0x0E), a
        ld a, page & 0x7F
        out (6), a
    .endif
.endmacro

setBankA(4)