.macro test(foo)
    ld a, foo
    ld b, 10
    call 0
.endmacro

test(b)
add a, b