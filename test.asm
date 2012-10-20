    LD A, 1
label1:
    LD B, 2
    LD A, B \ LD B, A
    LD HL, label1
    LD (label1), A
    LD (IX + 14), A