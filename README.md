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

You may customize sass's usage at the command line with a number of parameters.

**--encoding \[name]**: Sets the string encoding to use for string literals. Note that .asciiz and related directives
  will use the specified encoding, not ASCII. The default is "utf-8".

**--help**: Displays information on sass usage and basic documentation. *Aliases: -h -? /? /help -help*

**--include \[path(s)]**: Modifies the include path. This should be a semicolon-delimited list of paths to look
  for files included with <> in. Example: `--include "/foo;/bar"` *Aliases: --inc*

**--input-file \[file]**: An alternative way to specify the input file. *Aliases: --input*

**--instruction-set \[set]**: Specifies the instruction set to use. May be a key to reference an internal set
  (see [below](#supported-architectures)), or a path to a user-specified instruction set file. *Aliases: --instr*

**--listing \[file]**: Specifies a file to output a listing to. *Aliases: -l*

**--list-encodings**: Lists all available string encodings and their keys for use in --encoding. This will terminate
  sass without assembling any files.

**--output-file \[file]**: An alternative way to specify the output file. *Aliases: --output*

**--symbols [file]**: Outputs all labels and their addresses to a file using `.equ` so that they may be included
  again elsewhere. *Aliases: -s*

**--verbose**: Outputs a listing to standard output after assembly completion. *Aliases: -v*

## Supported Architectures

Out of the box, sass supports the following architectures:

* z80 [`z80`]
* *GameBoy Assembly [`LR35902` or `gbz80`]* (Planned)

In [brackets], the instruction set key is shown for use whith the --instruction-set command line parameter.
If not otherwise specified, the default instruction set is z80.

User-specified instruction sets may be added. *TODO: Document instruction set format*

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

Macros are defined through `.macro` and may be used in most conditions. To define a macro, use something like
this:

    .macro example
        ld a, b
    .endmacro

This defines a parameterless macro called "example". You needn't indent the contents of the macro; it's done
here for clarity. You may also define parameters:

    .macro example(foo, bar)
        ld a, foo
        ld b, bar
    .endmacro

This is a simple substitution macro. When called (like `example(a, b)`), 'foo' and 'bar' will be replaced with
'a' and 'b' respectively. Here's a more in-depth example:

    .macro example(foo, bar)
        ld a, foo
        ld b, bar
    .endmacro
    .macro add(a, b)
        a+b
    .endmacro
    example(1, 2) ; Becomes ld a, 1 \ ld b, 2
    ld a, add(2, 3) ; Becomes ld a, 2+3

## Pre-Processor Directives

Directives are indicated by a '.' or '#' as the first character, as in "#include \<foo.h>".

**ascii "\[text]"**: Converts "text" to the global string encoding (**not** ASCII) and inserts it into the output.

**asciiz "\[text]"**: Converts "text" to the global string encoding (**not** ASCII) and inserts it into the output,
  postfixed with a zero.

**asciip "\[text]"**: Converts "text" to the global string encoding (**not** ASCII) and inserts it into the output,
  prefixed with its 8-bit length.

**block \[size]**: Sets aside *size* bytes, all set to 0. See **fill** if you require a value other than 0.

**db \[value], \[value], ...**: Inserts any number of 8-bit *values* into the output.

**dw \[value], \[value], ...**: Inserts any number of n-bit *values* into the output, where n is the
  number of bits to a word in the target architecture.

**define \[key] \[value]**: Creates a one-line macro, whose name is "key" and whose replacement text is "value".

**echo \[message], \[message], ...**: Echos any number of *messages* to the console at assembly time. If
  *message* is not a string, it will be treated as an expression and echoed as a number.

**else**: If the matching if, ifdef, or ifndef directive evalulates to false, the code between this and the matching
  endif directive will be inserted instead.

**endif**: Closes a corresponding if, ifdef, or ifndef directive.

**equ \[key] \[value]**: Creates a symbol whose name is "key", with "value" as the value. "value" must be a valid
  expression.

**fill \[size], (value)**: Inserts *size* number of *values* into the output. Default *value* is 0.

**if \[expression]**: If "expression" evalutates to zero, all the code until the matching endif directive will be
  omitted from the output.

**ifdef \[symbol]**: If "symbol" is not defined as a symbol or macro, all the code until the matching endif directive
  will be omitted from the output.

**include \[path]**: Inserts the specified file's contents into the assembly. \[path] may be `"localfile"` or
  `<includedfile>`, where the former expects the file to be in the current working directory, and the latter
  looks for any files within the include path specified at the command line.

**org \[value]**: Sets the internal program counter to *value*. This does not add to the output, but will affect
  labels defined later on.

**What's the difference between define and equ?** Define creates a macro. These have more overheard and take longer
to evaluate during assembly, and should be used sparingly. Equ "equates" a name with a value, and creates a symbol,
which has far less overhead. Use this one if you can. One example of where you need to use define is to define a string
constant. Here's some example uses of each:

    .define text "Hello, world"
    .equ otherText "Hello, world" ; Doesn't work
    .equ value 0x1234 - 28
    
    .asciiz text
    ld hl, value
    
    .define function(arg1, arg2) ld a, arg1 \ ld b, arg2
    
    function(10, 20) ; Becomes ld a, 10 \ ld b, 20
    
    .define add(arg1, arg2) arg1 + arg2
    
    ld a, add(10, value) ; Can use symbols in macro invocation
    
    .define foo ; Adds a symbol where "foo" equals 1 (special case, doesn't create a macro)

# Compiling from Source

**Windows**: "msbuild" from the root directory of the project.

**Linux/Mac**: "xbuild" from the root directory of the project.

# Getting Involved

Feel free to fork and submit pull requests with your changes. You'll have good results with small, focused
pull requests, rather than broad, sweeping ones. You can also submit issues for bugs or feature requests.
You may email me as well at [sir@cmpwn.com](mailto:sir@cmpwn.com). For general z80 assistance, you should
join #ti on irc.eversible.com (EFNet).