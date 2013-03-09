; Inputs:
;   DE: File name
; Outputs:
; (Failure)
;   A: Error code
;   Z flag reset
; (Success)
;   A: Flash page
;   HL: Address (relative from 0x4000)
findFileEntry:
    push af
    ld a, i
    push af ; Save interrupt state
    
    push de
    push bc
        ld a, 4
        out (6), a
        ld hl, 0
        ld (4), hl ; Used as temporary storage of parent directory ID
        ld hl, 0x7FFF
        push af
_:          ld a, (hl)
            inc hl \ ld b, (hl) \ inc hl \ ld c, (hl) \ inc hl
            cp 4
            jr z, .handleFile
            cp 4
            jr z, .handleDirectory
            cp 4 ; TODO
            cp 4
            jr z, .handleEndOfTable
.continueSearch:
            add hl, bc
            jr -_
.handleEndOfTable:
        pop af
    pop bc
    pop de
    
    pop af ; Restore interrupts
    jp po, _
    ei
_:  pop af
    ld a, 4
    or a ; Resets z
    ret
.handleFile:
    push bc
    push hl
        ; Check parent directory
        ld b, (hl) \ inc hl \ ld c, (hl) \ inc hl
        ld hl, (4)
        call 4
        jr z, _
        ; If not equal, we have the wrong directory
    pop hl
    pop bc
    jr continueSearch
_:      ; Correct parent directory
        ; Check name
        ld bc, 6 \ add hl, bc
        jr $
.handleDirectory:
    ; TODO