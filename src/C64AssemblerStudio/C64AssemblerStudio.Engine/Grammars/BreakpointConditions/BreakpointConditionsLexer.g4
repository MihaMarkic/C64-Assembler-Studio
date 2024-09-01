lexer grammar BreakpointConditionsLexer;

OPEN_PARENS              : '(';
CLOSE_PARENS             : ')';
PLUS                     : '+';
MINUS                    : '-';
STAR                     : '*';
DIV                      : '/';
BITWISE_AND              : '&';
BITWISE_OR               : '|';
OP_AND                   : '&&';
OP_OR                    : '||';
AT                       : '@';
COLON                    : ':';
LT                       : '<';
GT                       : '>';
GT_OR_EQ                 : '>=';
LT_OR_EQ                 : '<=';
NEQ                      : '!=';
EQ                       : '==';
DOT                      : '.';

HEX_NUMBER: '$' HEX_DIGIT+;
DEC_NUMBER: DIGIT+;
fragment HEX_DIGIT: [0-9a-fA-F];
fragment DIGIT: [0-9];

UNQUOTED_STRING: [a-zA-Z][a-zA-Z0-9]*; // string can't start with digit

WS : [ \n\t\r]+ -> skip ; // skip spaces, tabs

ErrorChar : . ;