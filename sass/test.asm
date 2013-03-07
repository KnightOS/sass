.equ lang_forceQuit_position 61 * 256 + 50
ld e, 55 - (61 - (lang_forceQuit_position >> 8))