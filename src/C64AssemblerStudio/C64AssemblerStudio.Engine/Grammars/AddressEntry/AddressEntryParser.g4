parser grammar AddressEntryParser;

options {
    tokenVocab = AddressEntryLexer;
}

arguments
    : OPEN_PARENS arguments CLOSE_PARENS        # Parens
    | left=arguments STAR right=arguments       # Multiplication
    | left=arguments DIV right=arguments        # Division
    | left=arguments PLUS right=arguments       # Plus
    | left=arguments MINUS right=arguments      # Minus
    | argument                                  # Arg
    ; 

argument 
    : UNQUOTED_STRING           # Label
    | DEC_NUMBER                # DecNumber
    | HEX_NUMBER                # HexNumber
    | BIN_NUMBER                # BinNumber
    ;