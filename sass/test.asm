.macro foobar()
    .echo "foobar"
.endmacro
.macro foobar(asdf)
    .echo "foobar asdf"
.endmacro

foobar()
foobar(bc)
add a, 10