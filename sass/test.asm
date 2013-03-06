.macro kcall(addr)
    rst 0
    call addr
.endmacro
.macro kcall(cc, addr)
    rst 0
    call cc, addr
.endmacro

kcall(1234)
kcall(z, 1234)