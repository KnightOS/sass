findFileEntry:
    push af
    ld a, i
    push af ; Save interrupt state
    di
    
    push de
    push bc
        ld a, 4
        out (6), a
        ld hl, 0
        ld (4), hl ; Used as temporary storage of parent directory ID
        ld hl, 0x7FFF
        push af
            push de \ call 4 \ pop de
            jp z, 4
_:          ld a, (hl)
            dec hl \ ld c, (hl) \ dec hl \ ld b, (hl) \ dec hl
            cp 4
            jr z, .handleDirectory
            cp 4 ; TODO
            cp 4
            jr z, findFileEntry_handleEndOfTable
.continueSearch:
            sbc hl, bc
            ; TODO: Handle running off the page
            jr -_
handleDirectory:
    ; TODO

findFileEntry_handleEndOfTable: