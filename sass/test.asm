.macro kld(to, from)
    rst $08
    ld to, from
.endmacro

kld((totalThreads), a)