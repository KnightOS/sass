.macro assert(testString, expected)
    ld hl, testString
    call hexToA
    cp expected
    jr nz, .fail
.endmacro
test_hexToA:
    assert(.test1, 0x00)
    assert(.test2, 0xFF)
    assert(.test3, 0x10)
    assert(.test4, 0xA4)
    xor a
    ret
.fail:
    ld a, 1
    ret
.test1:
    .db "00", 0
.test2:
    .db "FF", 0
.test3:
    .db "10", 0
.test4:
    .db "A4", 0
;.undefine assert