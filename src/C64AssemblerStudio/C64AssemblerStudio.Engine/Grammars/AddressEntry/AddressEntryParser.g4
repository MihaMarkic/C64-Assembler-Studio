parser grammar AddressEntryParser;

options {
    tokenVocab = AddressEntryLexer;
}

arguments
    : OPEN_PARENS arguments CLOSE_PARENS 
    | argument PLUS argument
    | argument MINUS argument
    | argument STAR argument
    | argument DIV argument   
    | argument;

argument 
    : UNQUOTED_STRING 
    | DEC_NUMBER 
    | HEX_NUMBER 
    | BIN_NUMBER;