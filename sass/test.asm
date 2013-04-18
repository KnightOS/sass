.macro example(abc)
	rst 0
	jp abc
.endmacro
homeLoop:
	di
.org 253
	example(homeLoop)
	foo:
	.echo foo