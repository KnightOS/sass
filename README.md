# SirCmpwn's Assembler

SirCmpwn's Assembler (sass) is a multiplatform, two-pass assembler written in C#. Presently, it supports
z80 out of the box, as well as any user-provided instruction sets. It runs on Windows, Linux and Mac, as
well as several other platforms in the form of a library.

## Using SASS

On Linux or Mac, make sure that you have Mono installed, and prepend any commands to run Organic with
"mono", similar to running Java programs. Note that the default Mono on Ubuntu isn't the full install,
remove it and install "mono-complete".

### Command Line Usage

Usage: sass.exe [parameters] [input file] [output file]

[output file] is optional, and [input file].bin will be used if it is not specified.

#### Parameters

**TODO**

## Syntax

*All examples are given in z80 assembly*

SASS's assembly syntax attempts to be as close to other popular syntaxes as possible. Here's some example
z80 code that assembles under SASS:

        ld b, 20 + 0x13
    label1:
        inc c
        djnz label1

SASS is completely case-insensitive, and whitespace may be used as you please. The exception is that
dropping newlines requires you to add \ in their place, like so: "ld a, 1 \ add a, b". However, any
number of tabs or spaces in any location is permissible, though "AD D a, b" is invalid, as is
"ADDa, b". Labels may use the "label:" form (preferred), or the ":label" form.

Additionally, SASS supports nearly every form that an instruction may be presented in. For instance, each
of the following lines of code is acceptable:

    cp a, (ix + 10)
    cp a, (10 + ix)
    cp (ix + 10)
    cp (10 + ix)
    cp (ix)
    cp a, (ix)

### Expressions

Anywhere a number is required, you may use an expression. Expressions are evaluated with respect to
order of operations, using C precedence. The following operators are available:

    * / % + - << >> < <= > >= == != & ^ | && ||

Boolean operators will assemble to "1" if true, or "0" if false.

### Relative Addressing

**TODO**

### Local Labels

**TODO**

### Macros

**TODO**

## Pre-Processor Directives

Directives are indicated by a '.' or '#' as the first character, as in "#include <foo.h>".

**block \[size]**: Sets aside *size* bytes, all set to 0. See **fill** if you require a value other than 0.

**db \[value], \[value], ...**: Inserts any number of 8-bit *value*s into the output.

**dw \[value], \[value], ...**: Inserts any number of n-bit *value*s into the output, where n is the
  number of bits to a word in the target architecture.

**echo \[message], \[message], ...**: Echos any number of *message*s to the console at assembly time. If
  *message* is not a string, it will be treated as an expression and echoed as a number.

**fill \[size], (value)**: Inserts *size* number of *value*s into the output. Default *value* is 0.

**org \[value]**: Sets the internal program counter to *value*. This does not add to the output.

# Compiling from Source

**Windows**: "msbuild" from the root directory of the project.

**Linux/Mac**: "xbuild" from the root directory of the project.

# Getting Involved

Feel free to fork and submit pull requests with your changes. You'll have good results with small, focused
pull requests, rather than broad, sweeping ones. You can also submit issues for bugs or feature requests.
You may email me as well at sir@cmpwn.com. For general z80 assistance, you should join #ti on
irc.freenode.net.