grammar Formula;

/* Cell expression grammar, variant 8: operations 1, 3, 4, 5.
   Higher alternative = higher priority. */

formula
    : expression EOF
    ;

expression
    : <assoc=right> expression POW expression          # Power
    | sign=(PLUS | MINUS) expression                   # Unary
    | expression op=(MUL | DIV) expression             # Multiplicative
    | expression op=(PLUS | MINUS) expression          # Additive
    | name=(INC | DEC) LPAREN expression RPAREN        # Function
    | LPAREN expression RPAREN                         # Parentheses
    | NUMBER                                           # Number
    | CELL                                             # Reference
    ;

INC    : [iI][nN][cC] ;
DEC    : [dD][eE][cC] ;
CELL   : [a-zA-Z]+ [0-9]+ ;
NAME   : [a-zA-Z]+ ;
NUMBER : [0-9]+ ;

PLUS   : '+' ;
MINUS  : '-' ;
MUL    : '*' ;
DIV    : '/' ;
POW    : '^' ;
LPAREN : '(' ;
RPAREN : ')' ;

WS     : [ \t\r\n]+ -> skip ;