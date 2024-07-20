<Query Kind="Statements" />

const string instructionsSource = """
ADC: 'adc';
AND: 'and';
ASL: 'asl';
BCC: 'bcc';
BCS: 'bcs';
BEQ: 'beq';
BIT: 'bit';
BMI: 'bmi';
BNE: 'bne';
BPL: 'bpl';
BRA: 'bra';
BRK: 'brk';
BVC: 'bvc';
BVS: 'bvs';
CLC: 'clc';
CLD: 'cld';
CLI: 'cli';
CLV: 'clv';
CMP: 'cmp';
CPX: 'cpx';
CPY: 'cpy';
DEC: 'dec';
DEX: 'dex';
DEY: 'dey';
EOR: 'eor';
INC: 'inc';
INX: 'inx';
INY: 'iny';
JMP: 'jmp';
JSR: 'jsr';
LDA: 'lda';
LDY: 'ldy';
LDX: 'ldx';
LSR: 'lsr';
NOP: 'nop';
ORA: 'ora';
PHA: 'pha';
PHX: 'phx';
PHY: 'phy';
PHP: 'php';
PLA: 'pla';
PLP: 'plp';
PLY: 'ply';
ROL: 'rol';
ROR: 'ror';
RTI: 'rti';
RTS: 'rts';
SBC: 'sbc';
SEC: 'sec';
SED: 'sed';
SEI: 'sei';
STA: 'sta';
STX: 'stx';
STY: 'sty';
STZ: 'stz';
TAX: 'tax';
TAY: 'tay';
TSX: 'tsx';
TXA: 'txa';
TXS: 'txs';
TYA: 'tya';
""";

const string operatorsSource = """
PLUS                     : '+';
MINUS                    : '-';
STAR                     : '*';
DIV                      : '/';
PERCENT                  : '%';
AMP                      : '&';
BITWISE_OR               : '|';
CARET                    : '^';
BANG                     : '!';
TILDE                    : '~';
AT                       : '@';
ASSIGNMENT               : '=';
LT                       : '<';
GT                       : '>';
INTERR                   : '?';
DOUBLE_COLON             : '::';
OP_COALESCING            : '??';
OP_INC                   : '++';
OP_DEC                   : '--';
OP_AND                   : '&&';
OP_OR                    : '||';
OP_PTR                   : '->';
OP_EQ                    : '==';
OP_NE                    : '!=';
OP_LE                    : '<=';
OP_GE                    : '>=';
OP_ADD_ASSIGNMENT        : '+=';
OP_SUB_ASSIGNMENT        : '-=';
OP_MULT_ASSIGNMENT       : '*=';
OP_DIV_ASSIGNMENT        : '/=';
OP_MOD_ASSIGNMENT        : '%=';
OP_AND_ASSIGNMENT        : '&=';
OP_OR_ASSIGNMENT         : '|=';
OP_XOR_ASSIGNMENT        : '^=';
OP_LEFT_SHIFT            : '<<';
OP_RIGHT_SHIFT           : '>>';
OP_LEFT_SHIFT_ASSIGNMENT : '<<=';
OP_COALESCING_ASSIGNMENT : '??=';
OP_RANGE                 : '..';
""";

const string separatorsSource = """
DOT                      : '.';
COMMA                    : ',';
COLON                    : ':' ; //{ this.OnColon(); };
SEMICOLON                : ';';
""";

const string bracketsSource = """
OPEN_BRACE               : '{' ; //{ this.OnOpenBrace(); };
CLOSE_BRACE              : '}' ; //{ this.OnCloseBrace(); };
OPEN_BRACKET             : '[';
CLOSE_BRACKET            : ']';
OPEN_PARENS              : '(';
CLOSE_PARENS             : ')';
""";

const string directiveSources = """
BINARY_TEXT: 'binary';
C64_TEXT: 'c64';
TEXT_TEXT: 'text';
ENCODING: 'encoding';
FILL: 'fill';
FILLWORD: 'fillword'; 
LOHIFILL: 'lohifill';
BYTE: 'byte' | 'by';
WORD: 'word' | 'wo';
DWORD: 'dword' | 'dw';
CPU: 'cpu';
CPU6502NOILLEGALS: '_6502NoIllegals';
CPU6502: '_6502';
DTV: 'dtv';
CPU65C02: '_65c02';
ASSERT: 'assert';
ASSERTERROR: 'asserterror';
PRINT: 'print';
PRINTNOW: 'printnow';
VAR: 'var';
CONST: 'const';
IF: 'if';
ELSE: 'else';
ERRORIF: 'errorif';
EVAL: 'eval';
FOR: 'for';
WHILE: 'while';
STRUCT: 'struct';
DEFINE: 'define';
FUNCTION: 'function';
RETURN: 'return';
MACRO: 'macro';
PSEUDOCOMMAND: 'pseudocommand';
PSEUDOPC: 'pseudopc';
UNDEF: 'undef';
ENDIF: 'endif';
ELIF: 'elif';
IMPORT: 'import';
IMPORTONCE: 'importonce';
IMPORTIF: 'importif';
NAMESPACE: 'namespace'; 
SEGMENT: 'segment';
SEGMENTDEF: 'segmentdef';
SEGMENTOUT: 'segmentout';
MODIFY: 'modify';
FILEMODIFY: 'fileModify';
PLUGIN: 'plugin';
LABEL: 'label';
FILE: 'file';
DISK: 'disk';
PC: 'pc';
BREAK: 'break';
WATCH: 'watch';
ZP: 'zp';
""";

const string colorSources = """
BLACK: 'BLACK';
WHITE: 'WHITE';
RED: 'RED';
CYAN: 'CYAN';
PURPLE: 'PURPLE';
GREEN: 'GREEN';
BLUE: 'BLUE';
YELLOW: 'YELLOW';
ORANGE: 'ORANGE';
BROWN: 'BROWN';
LIGHT_RED: 'LIGHT_RED';
DARK_GRAY: 'DARK_GRAY';
DARK_GREY: 'DARK_GREY';
GRAY: 'GRAY';
GREY: 'GREY';
LIGHT_GREEN: 'LIGHT_GREEN';
LIGHT_BLUE: 'LIGHT_BLUE';
LIGHT_GRAY: 'LIGHT_GRAY';
LIGHT_GREY: 'LIGHT_GREY';
""";


void CreateEntries(string comment, string text, string tokenType)
{
	const string prefix = "\t\t\t\t";

	Console.WriteLine($"{prefix}// {comment}");
	string? line;
	var reader = new StringReader(text);
	while ((line = reader.ReadLine()) is not null)
	{
		Console.WriteLine($"{prefix}{{ KickAssemblerLexer.{line.Split(':')[0]}, TokenType.{tokenType} }},");
	}
}

CreateEntries("Instructions", instructionsSource, "Instruction");
CreateEntries("Brackets", bracketsSource, "Bracket");
CreateEntries("Operators", operatorsSource, "Operator");
CreateEntries("Directives", directiveSources, "Directive");
CreateEntries("Colors", colorSources, "Color");

Console.WriteLine("{prefix}// Colors");
reader = new StringReader(colorSources);
while ((line = reader.ReadLine()) is not null)
{
	Console.WriteLine($"\t\t\t{{ KickAssemblerLexer.{line.Split(':')[0]}, TokenType.Color }},");
}