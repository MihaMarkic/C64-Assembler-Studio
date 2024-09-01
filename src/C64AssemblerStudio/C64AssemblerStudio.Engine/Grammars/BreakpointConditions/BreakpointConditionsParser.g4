parser grammar BreakpointConditionsParser;

options {
    tokenVocab = BreakpointConditionsLexer;
}

root : condition EOF;

condition
    : OPEN_PARENS condition CLOSE_PARENS                # ConditionParens
    | left=condition operator right=condition           # ConditionOperation
    | argument                                          # ConditionArguments
    ;
    
operator
    : GT
    | GT_OR_EQ
    | LT
    | LT_OR_EQ
    | EQ
    | NEQ
    | OP_AND
    | OP_OR
    | STAR
    | DIV
    | PLUS
    | MINUS
    | BITWISE_AND
    | BITWISE_OR
    ;

argument 
    : AT bank=UNQUOTED_STRING COLON condition            # Bank
    | memspace=variable COLON register                   # Memspace
    | DOT UNQUOTED_STRING                                # Label
    | register                                           # ArgumentRegister
    | HEX_NUMBER                                         # HexNumber
    ;
    
register
    : UNQUOTED_STRING
    ;
    
variable
    : UNQUOTED_STRING 
    | DEC_NUMBER;