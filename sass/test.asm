global1:
    ld a, b
.local:
    jp .local
global2:
    ld b, a
.local:
    jp .local