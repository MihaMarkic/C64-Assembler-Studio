lexer grammar AddressEntryLexer;

OPEN_PARENS              : '(';
CLOSE_PARENS             : ')';
PLUS                     : '+';
MINUS                    : '-';
STAR                     : '*';
DIV                      : '/';

DEC_NUMBER: DEC_DIGIT+; 
HEX_NUMBER: '$' HEX_DIGIT+ ;
BIN_NUMBER: '%' BIN_DIGIT+ ;
fragment INPUT_CHAR: ~[\r\n]; // everything except newline
fragment DEC_DIGIT: [0-9] ;
fragment HEX_DIGIT: [0-9a-fA-F] ;
fragment BIN_DIGIT: '0' | '1';

UNQUOTED_STRING: [a-zA-Z0-9]+;

EOL: '\r\n' | '\r' | '\n' ;
WS : [ \t]+ -> skip ; // skip spaces, tabs, newlines