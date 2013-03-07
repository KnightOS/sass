.macro lcall(id, addr)
    rst rlcall
    .db id
    call addr
.endmacro
.macro stdio(addr)
    lcall(stdioId, addr)
.endmacro
stdioId .equ $03
printLine .equ 21

lcall(stdioId, printLine)