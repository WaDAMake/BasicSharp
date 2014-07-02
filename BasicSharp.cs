using System;
using System.Diagnostics;

namespace BasicSharp
{
    public class Interpreter
    {
        Tokenizer tokenizer = null;

        private const int MAX_STRINGLEN = 40;
        private const int MAX_GOSUB_STACK_DEPTH = 10;

        private int[] gosub_stack = new int[MAX_GOSUB_STACK_DEPTH];
        private int gosub_stack_ptr;

        public uBasic (char[] program)
        {
            for_stack_ptr = gosub_stack_ptr = 0;
            tokenizer = new Tokenizer (program);
            ended = false;
        }

        struct for_state {
            public int line_after_for;
            public int for_variable;
            public int to;
        };

        private const int MAX_FOR_STACK_DEPTH = 4;
        private for_state[] for_stack = new for_state[MAX_FOR_STACK_DEPTH];
        private int for_stack_ptr;

        private const int MAX_VARNUM = 26;
        private int[] variables = new int[MAX_VARNUM];

        private bool ended;

        /*---------------------------------------------------------------------------*/
        private void accept(Token token)
        {
            if(token != tokenizer.GetToken()) {
                Debug.WriteLine("Token not what was expected (expected {0}, got {1})", token, tokenizer.GetToken());
                tokenizer.PrintError();
                //exit(1);
            }
            Debug.WriteLine("Expected {0}, got it", token);
            tokenizer.Next();
        }
        /*---------------------------------------------------------------------------*/
        private int varfactor()
        {
            int r;
            Debug.WriteLine("varfactor: obtaining {0} from variable {1}", variables[tokenizer.GetVariable()], tokenizer.GetVariable());
            r = ubasic_get_variable(tokenizer.GetVariable());
            accept(Token.TOKENIZER_VARIABLE);
            return r;
        }
        /*---------------------------------------------------------------------------*/
        private int factor()
        {
            int r;

            Debug.WriteLine("factor: token {0}", tokenizer.GetToken());
            switch(tokenizer.GetToken()) {
            case Token.TOKENIZER_NUMBER:
                r = tokenizer.GetNumber();
                Debug.WriteLine("factor: number {0}", r);
                accept(Token.TOKENIZER_NUMBER);
                break;
            case Token.TOKENIZER_LEFTPAREN:
                accept(Token.TOKENIZER_LEFTPAREN);
                r = expr();
                accept(Token.TOKENIZER_RIGHTPAREN);
                break;
            default:
                r = varfactor();
                break;
            }
            return r;
        }
        /*---------------------------------------------------------------------------*/
        private int term()
        {
            int f1, f2;
            Token op;

            f1 = factor();
            op = tokenizer.GetToken();
            Debug.WriteLine("term: token {0}", op);
            while(op == Token.TOKENIZER_ASTR ||
                op == Token.TOKENIZER_SLASH ||
                op == Token.TOKENIZER_MOD) {
                tokenizer.Next();
                f2 = factor();
                Debug.WriteLine("term: {0} {1} {2}", f1, op, f2);
                switch (op) {
                case Token.TOKENIZER_ASTR:
                    f1 = f1 * f2;
                    break;
                case Token.TOKENIZER_SLASH:
                    f1 = f1 / f2;
                    break;
                case Token.TOKENIZER_MOD:
                    f1 = f1 % f2;
                    break;
                }
                op = tokenizer.GetToken();
            }
            Debug.WriteLine("term: {0}", f1);
            return f1;
        }
        /*---------------------------------------------------------------------------*/
        private int expr()
        {
            int t1, t2;
            Token op;

            t1 = term();
            op = tokenizer.GetToken();
            Debug.WriteLine("expr: token {0}", op);
            while(op == Token.TOKENIZER_PLUS ||
                op == Token.TOKENIZER_MINUS ||
                op == Token.TOKENIZER_AND ||
                op == Token.TOKENIZER_OR) {
                tokenizer.Next();
                t2 = term();
                Debug.WriteLine("expr: {0} {1} {2}", t1, op, t2);
                switch (op) {
                case Token.TOKENIZER_PLUS:
                    t1 = t1 + t2;
                    break;
                case Token.TOKENIZER_MINUS:
                    t1 = t1 - t2;
                    break;
                case Token.TOKENIZER_AND:
                    t1 = t1 & t2;
                    break;
                case Token.TOKENIZER_OR:
                    t1 = t1 | t2;
                    break;
                }
                op = tokenizer.GetToken();
            }
            Debug.WriteLine("expr: {0}", t1);
            return t1;
        }
        /*---------------------------------------------------------------------------*/
        private bool relation()
        {
            bool r;
            int r1, r2;
            Token op;

            r1 = expr();
            r = r1 > 0;
            op = tokenizer.GetToken();
            Debug.WriteLine("relation: token {0}", op);
            while(op == Token.TOKENIZER_LT ||
                op == Token.TOKENIZER_GT ||
                op == Token.TOKENIZER_EQ) {
                tokenizer.Next();
                r2 = expr();
                Debug.WriteLine("relation: {0} {1} {2}", r1, op, r2);
                switch(op) {
                case Token.TOKENIZER_LT:
                    r = r1 < r2;
                    break;
                case Token.TOKENIZER_GT:
                    r = r1 > r2;
                    break;
                case Token.TOKENIZER_EQ:
                    r = r1 == r2;
                    break;
                }
                op = tokenizer.GetToken();
            }
            return r;
        }
        /*---------------------------------------------------------------------------*/
        private void jump_linenum(int linenum)
        {
            tokenizer.Reset ();
            while(tokenizer.GetNumber() != linenum) {
                do {
                    do {
                        tokenizer.Next();
                    } while(tokenizer.GetToken() != Token.TOKENIZER_CR &&
                        tokenizer.GetToken() != Token.TOKENIZER_ENDOFINPUT);
                    if(tokenizer.GetToken() == Token.TOKENIZER_CR) {
                        tokenizer.Next();
                    }
                } while(tokenizer.GetToken() != Token.TOKENIZER_NUMBER);
                Debug.WriteLine("jump_linenum: Found line {0}", tokenizer.GetNumber());
            }
        }
        /*---------------------------------------------------------------------------*/
        private void goto_statement()
        {
            accept(Token.TOKENIZER_GOTO);
            jump_linenum(tokenizer.GetNumber());
        }
        /*---------------------------------------------------------------------------*/
        private void print_statement()
        {
            accept(Token.TOKENIZER_PRINT);
            do {
                Debug.WriteLine("Print loop");
                if(tokenizer.GetToken() == Token.TOKENIZER_STRING) {
                    Print(tokenizer.GetString());
                    tokenizer.Next();
                } else if(tokenizer.GetToken() == Token.TOKENIZER_COMMA) {
                    Print(" ");
                    tokenizer.Next();
                } else if(tokenizer.GetToken() == Token.TOKENIZER_SEMICOLON) {
                    tokenizer.Next();
                } else if(tokenizer.GetToken() == Token.TOKENIZER_VARIABLE ||
                    tokenizer.GetToken() == Token.TOKENIZER_NUMBER) {
                    Print(String.Format("{0}", expr()));
                } else {
                    break;
                }
            } while(tokenizer.GetToken() != Token.TOKENIZER_CR &&
                tokenizer.GetToken() != Token.TOKENIZER_ENDOFINPUT);

            Print("\n");
            Debug.WriteLine("End of print");
            tokenizer.Next();
        }
        /*---------------------------------------------------------------------------*/
        private void if_statement()
        {
            bool r;

            accept(Token.TOKENIZER_IF);

            r = relation();
            Debug.WriteLine("if_statement: relation {0}", r);
            accept(Token.TOKENIZER_THEN);
            if(r) {
                statement();
            } else {
                do {
                    tokenizer.Next();
                } while(tokenizer.GetToken() != Token.TOKENIZER_ELSE &&
                    tokenizer.GetToken() != Token.TOKENIZER_CR &&
                    tokenizer.GetToken() != Token.TOKENIZER_ENDOFINPUT);
                if(tokenizer.GetToken() == Token.TOKENIZER_ELSE) {
                    tokenizer.Next();
                    statement();
                } else if(tokenizer.GetToken() == Token.TOKENIZER_CR) {
                    tokenizer.Next();
                }
            }
        }
        /*---------------------------------------------------------------------------*/
        private void let_statement()
        {
            int var;

            var = tokenizer.GetVariable();

            accept(Token.TOKENIZER_VARIABLE);
            accept(Token.TOKENIZER_EQ);
            ubasic_set_variable(var, expr());
            Debug.WriteLine("let_statement: assign {0} to {1}\n", variables[var], var);
            accept(Token.TOKENIZER_CR);

        }
        /*---------------------------------------------------------------------------*/
        private void gosub_statement()
        {
            int linenum;
            accept(Token.TOKENIZER_GOSUB);
            linenum = tokenizer.GetNumber();
            accept(Token.TOKENIZER_NUMBER);
            accept(Token.TOKENIZER_CR);
            if(gosub_stack_ptr < MAX_GOSUB_STACK_DEPTH) {
                gosub_stack[gosub_stack_ptr] = tokenizer.GetNumber();
                gosub_stack_ptr++;
                jump_linenum(linenum);
            } else {
                Debug.WriteLine("gosub_statement: gosub stack exhausted");
            }
        }
        /*---------------------------------------------------------------------------*/
        private void return_statement()
        {
            accept(Token.TOKENIZER_RETURN);
            if(gosub_stack_ptr > 0) {
                gosub_stack_ptr--;
                jump_linenum(gosub_stack[gosub_stack_ptr]);
            } else {
                Debug.WriteLine("return_statement: non-matching return");
            }
        }
        /*---------------------------------------------------------------------------*/
        private void next_statement()
        {
            int var;

            accept(Token.TOKENIZER_NEXT);
            var = tokenizer.GetVariable();
            accept(Token.TOKENIZER_VARIABLE);
            if(for_stack_ptr > 0 &&
                var == for_stack[for_stack_ptr - 1].for_variable) {
                ubasic_set_variable(var,
                    ubasic_get_variable(var) + 1);
                if(ubasic_get_variable(var) <= for_stack[for_stack_ptr - 1].to) {
                    jump_linenum(for_stack[for_stack_ptr - 1].line_after_for);
                } else {
                    for_stack_ptr--;
                    accept(Token.TOKENIZER_CR);
                }
            } else {
                Debug.WriteLine("next_statement: non-matching next (expected {0}, found {1})\n", for_stack[for_stack_ptr - 1].for_variable, var);
                accept(Token.TOKENIZER_CR);
            }

        }
        /*---------------------------------------------------------------------------*/
        private void for_statement()
        {
            int for_variable, to;

            accept(Token.TOKENIZER_FOR);
            for_variable = tokenizer.GetVariable();
            accept(Token.TOKENIZER_VARIABLE);
            accept(Token.TOKENIZER_EQ);
            ubasic_set_variable(for_variable, expr());
            accept(Token.TOKENIZER_TO);
            to = expr();
            accept(Token.TOKENIZER_CR);

            if(for_stack_ptr < MAX_FOR_STACK_DEPTH) {
                for_stack[for_stack_ptr].line_after_for = tokenizer.GetNumber();
                for_stack[for_stack_ptr].for_variable = for_variable;
                for_stack[for_stack_ptr].to = to;
                Debug.WriteLine("for_statement: new for, var {0} to {1}\n",
                    for_stack[for_stack_ptr].for_variable,
                    for_stack[for_stack_ptr].to);

                for_stack_ptr++;
            } else {
                Debug.WriteLine("for_statement: for stack depth exceeded");
            }
        }
        /*---------------------------------------------------------------------------*/
        private void end_statement()
        {
            accept(Token.TOKENIZER_END);
            ended = true;
        }
        /*---------------------------------------------------------------------------*/
        private void statement()
        {
            Token token;

            token = tokenizer.GetToken();

            switch(token) {
            case Token.TOKENIZER_PRINT:
                print_statement();
                break;
            case Token.TOKENIZER_IF:
                if_statement();
                break;
            case Token.TOKENIZER_GOTO:
                goto_statement();
                break;
            case Token.TOKENIZER_GOSUB:
                gosub_statement();
                break;
            case Token.TOKENIZER_RETURN:
                return_statement();
                break;
            case Token.TOKENIZER_FOR:
                for_statement();
                break;
            case Token.TOKENIZER_NEXT:
                next_statement();
                break;
            case Token.TOKENIZER_END:
                end_statement();
                break;
//            case Token.TOKENIZER_LET:
//                accept(Token.TOKENIZER_LET);
                /* Fall through. */
            case Token.TOKENIZER_VARIABLE:
                let_statement();
                break;
            default:
                Debug.WriteLine ("ubasic.c: statement(): not implemented {0}", token);
                //exit(1);
                break;
            }
        }
        /*---------------------------------------------------------------------------*/
        private void line_statement()
        {
            Debug.WriteLine("----------- Line number {0} ---------\n", tokenizer.GetNumber());
            /*    current_linenum = tokenizer_num();*/
            accept(Token.TOKENIZER_NUMBER);
            statement();
            return;
        }
        /*---------------------------------------------------------------------------*/
        public void ubasic_run()
        {
            if(tokenizer.Finished()) {
                Debug.WriteLine("uBASIC program finished");
                return;
            }

            line_statement();
        }
        /*---------------------------------------------------------------------------*/
        public bool ubasic_finished()
        {
            return ended || tokenizer.Finished();
        }
        /*---------------------------------------------------------------------------*/
        void ubasic_set_variable(int varnum, int value)
        {
            if(varnum > 0 && varnum <= MAX_VARNUM) {
                variables[varnum] = value;
            }
        }
        /*---------------------------------------------------------------------------*/
        int ubasic_get_variable(int varnum)
        {
            if(varnum > 0 && varnum <= MAX_VARNUM) {
                return variables[varnum];
            }
            return 0;
        }
        /*---------------------------------------------------------------------------*/

        #region uBasic Instructions
        void Print(string msg) {
            Console.Write (msg);
        }
        #endregion
    }
}

