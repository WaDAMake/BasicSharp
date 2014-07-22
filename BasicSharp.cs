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

        private Random RandNumber = null;

        public Interpreter(char[] program)
        {
            for_stack_ptr = gosub_stack_ptr = 0;
            tokenizer = new Tokenizer (program);
            ended = false;

            RandNumber = new Random ();
            MathFunctionDelegate = MathFunctionHandler;
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
        private void AcceptToken(Token token)
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
            AcceptToken(Token.VARIABLE);
            return r;
        }
        /*---------------------------------------------------------------------------*/
        private int factor()
        {
            int r;

            Debug.WriteLine("factor: token {0}", tokenizer.GetToken());
            switch(tokenizer.GetToken()) {
            case Token.NUMBER:
                r = tokenizer.GetNumber();
                Debug.WriteLine("factor: number {0}", r);
                AcceptToken(Token.NUMBER);
                break;
            case Token.LEFTPAREN:
                AcceptToken(Token.LEFTPAREN);
                r = expr();
                AcceptToken(Token.RIGHTPAREN);
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
            while(op == Token.ASTR ||
                op == Token.SLASH ||
                op == Token.MOD) {
                tokenizer.Next();
                f2 = factor();
                Debug.WriteLine("term: {0} {1} {2}", f1, op, f2);
                switch (op) {
                case Token.ASTR:
                    f1 = f1 * f2;
                    break;
                case Token.SLASH:
                    f1 = f1 / f2;
                    break;
                case Token.MOD:
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
            while(op == Token.PLUS ||
                op == Token.MINUS ||
                op == Token.AND ||
                op == Token.OR) {
                tokenizer.Next();
                t2 = term();
                Debug.WriteLine("expr: {0} {1} {2}", t1, op, t2);
                switch (op) {
                case Token.PLUS:
                    t1 = t1 + t2;
                    break;
                case Token.MINUS:
                    t1 = t1 - t2;
                    break;
                case Token.AND:
                    t1 = t1 & t2;
                    break;
                case Token.OR:
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
            while (op == Token.LT ||
                op == Token.GT ||
                op == Token.EQ) {
                tokenizer.Next();
                r2 = expr();
                Debug.WriteLine("relation: {0} {1} {2}", r1, op, r2);
                switch(op) {
                case Token.LT:
                    r = r1 < r2;
                    break;
                case Token.GT:
                    r = r1 > r2;
                    break;
                case Token.EQ:
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
                    } while(tokenizer.GetToken() != Token.CR &&
                        tokenizer.GetToken() != Token.ENDOFINPUT);
                    if(tokenizer.GetToken() == Token.CR) {
                        tokenizer.Next();
                    }
                } while(tokenizer.GetToken() != Token.NUMBER);
                Debug.WriteLine("jump_linenum: Found line {0}", tokenizer.GetNumber());
            }
        }
        /*---------------------------------------------------------------------------*/
        private void goto_statement()
        {
            AcceptToken(Token.GOTO);
            jump_linenum(tokenizer.GetNumber());
        }
        /*---------------------------------------------------------------------------*/
        private void delay_statement()
        {
            AcceptToken (Token.DELAY);

            int delay = expr ();

            HandleDelayStatement (delay);

            tokenizer.Next ();
        }
        /*---------------------------------------------------------------------------*/
        private void led_statement()
        {
            AcceptToken (Token.LED);

            byte r = (byte)expr ();
            AcceptToken (Token.COMMA);

            byte g = (byte)expr ();
            AcceptToken (Token.COMMA);

            byte b = (byte)expr ();

            HandleLEDStatement (r, g, b);

            tokenizer.Next ();
        }

        private void print_statement()
        {
            AcceptToken (Token.PRINT);
            do {
                Debug.WriteLine("Print loop");
                if(tokenizer.GetToken() == Token.STRING) {
                    HandlePrintStatement(tokenizer.GetString());
                    tokenizer.Next();
                } else if(tokenizer.GetToken() == Token.COMMA) {
                    HandlePrintStatement(" ");
                    tokenizer.Next();
                } else if(tokenizer.GetToken() == Token.SEMICOLON) {
                    tokenizer.Next();
                } else if(tokenizer.GetToken() == Token.VARIABLE ||
                    tokenizer.GetToken() == Token.NUMBER) {
                    HandlePrintStatement(String.Format("{0}", expr()));
                } else {
                    break;
                }
            } while(tokenizer.GetToken() != Token.CR &&
                tokenizer.GetToken() != Token.ENDOFINPUT);

            Debug.WriteLine("End of print");
            tokenizer.Next();
        }
        /*---------------------------------------------------------------------------*/
        private void if_statement()
        {
            bool r;

            AcceptToken(Token.IF);

            r = relation();
            Debug.WriteLine("if_statement: relation {0}", r);
            AcceptToken (Token.THEN);
            if(r) {
                statement();
            } else {
                do {
                    tokenizer.Next();
                } while(tokenizer.GetToken() != Token.ELSE &&
                    tokenizer.GetToken() != Token.CR &&
                    tokenizer.GetToken() != Token.ENDOFINPUT);
                if(tokenizer.GetToken() == Token.ELSE) {
                    tokenizer.Next();
                    statement();
                } else if(tokenizer.GetToken() == Token.CR) {
                    tokenizer.Next();
                }
            }
        }
        /*---------------------------------------------------------------------------*/
        private void LetStatement()
        {
            int v;
            int r = 0;

            v = tokenizer.GetVariable();

            AcceptToken (Token.VARIABLE);
            AcceptToken (Token.EQ);

            Token op = tokenizer.GetToken ();

            // Random Number.
            if (op == Token.RAND) {
                AcceptToken (Token.RAND);
                AcceptToken (Token.LEFTPAREN);
                r = expr ();
                AcceptToken (Token.RIGHTPAREN);

                MathFunctionParams p = HandleMathFunction (r, 0, op);

                if (p != null)
                    r = p.Result;

            } else if (op == Token.SIN ||
                       op == Token.COS ||
                       op == Token.TAN) {
                int d, b;

                AcceptToken (op);
                AcceptToken (Token.LEFTPAREN);
                d = expr ();
                AcceptToken (Token.COMMA);
                b = expr ();
                AcceptToken (Token.RIGHTPAREN);

                MathFunctionParams p = HandleMathFunction (d, b, op);

                if (p != null)
                    r = p.Result;
            } else {
                r = expr ();
            }

            ubasic_set_variable(v, r);
            Debug.WriteLine("let_statement: assign {0} to {1}\n", variables[v], v);
            AcceptToken (Token.CR);

        }
        /*---------------------------------------------------------------------------*/
        private void gosub_statement()
        {
            int linenum;
            AcceptToken (Token.GOSUB);
            linenum = tokenizer.GetNumber();
            AcceptToken (Token.NUMBER);
            AcceptToken (Token.CR);
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
            AcceptToken (Token.RETURN);
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

            AcceptToken (Token.NEXT);
            var = tokenizer.GetVariable();
            AcceptToken (Token.VARIABLE);
            if(for_stack_ptr > 0 &&
                var == for_stack[for_stack_ptr - 1].for_variable) {
                ubasic_set_variable(var,
                    ubasic_get_variable(var) + 1);
                if(ubasic_get_variable(var) <= for_stack[for_stack_ptr - 1].to) {
                    jump_linenum(for_stack[for_stack_ptr - 1].line_after_for);
                } else {
                    for_stack_ptr--;
                    AcceptToken (Token.CR);
                }
            } else {
                Debug.WriteLine("next_statement: non-matching next (expected {0}, found {1})\n", for_stack[for_stack_ptr - 1].for_variable, var);
                AcceptToken (Token.CR);
            }

        }
        /*---------------------------------------------------------------------------*/
        private void for_statement()
        {
            int for_variable, to;

            AcceptToken (Token.FOR);
            for_variable = tokenizer.GetVariable();
            AcceptToken (Token.VARIABLE);
            AcceptToken (Token.EQ);
            ubasic_set_variable(for_variable, expr());
            AcceptToken (Token.TO);
            to = expr();
            AcceptToken (Token.CR);

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
            AcceptToken (Token.END);
            ended = true;
        }
        /*---------------------------------------------------------------------------*/
        private void statement()
        {
            Token token;

            token = tokenizer.GetToken();

            switch(token) {
            case Token.DELAY:
                delay_statement ();
                break;
            case Token.LED:
                led_statement ();
                break;
            case Token.PRINT:
                print_statement();
                break;
            case Token.IF:
                if_statement();
                break;
            case Token.GOTO:
                goto_statement();
                break;
            case Token.GOSUB:
                gosub_statement();
                break;
            case Token.RETURN:
                return_statement();
                break;
            case Token.FOR:
                for_statement();
                break;
            case Token.NEXT:
                next_statement();
                break;
            case Token.END:
                end_statement();
                break;
            case Token.VARIABLE:
                LetStatement();
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
            AcceptToken (Token.NUMBER);
            statement();
            return;
        }
        /*---------------------------------------------------------------------------*/
        public void ubasic_run()
        {
            if(tokenizer.IsFinished()) {
                Debug.WriteLine("uBASIC program finished");
                return;
            }

            line_statement();
        }
        /*---------------------------------------------------------------------------*/
        public bool ubasic_finished()
        {
            return ended || tokenizer.IsFinished ();
        }
        /*---------------------------------------------------------------------------*/
        void ubasic_set_variable(int varnum, int value)
        {
            if(varnum >= 0 && varnum < MAX_VARNUM) {
                variables[varnum] = value;
            }
        }
        /*---------------------------------------------------------------------------*/
        int ubasic_get_variable(int varnum)
        {
            if(varnum >= 0 && varnum < MAX_VARNUM) {
                return variables[varnum];
            }
            return 0;
        }
        /*
         * Instruction Delegates
         */
        public EventHandler<int> DelayDelegate;

        private void HandleDelayStatement(int delay)
        {
            if (DelayDelegate != null) {
                DelayDelegate (this, delay);
            }
        }

        public class LEDColorParameters
        {
            public byte RedColor;
            public byte GreenColor;
            public byte BlueColor;
        }

        public EventHandler<LEDColorParameters> LEDDelegate;

        private void HandleLEDStatement(byte r, byte g, byte b)
        {
            if (LEDDelegate != null) {
                LEDDelegate (this, new LEDColorParameters {
                    RedColor = r,
                    GreenColor = g,
                    BlueColor = b,
                });
            }
        }

        public EventHandler<string> PrintDelegate = null;

        private void HandlePrintStatement(string s)
        {
            if (PrintDelegate != null) {
                PrintDelegate (this, s);
            }
        }

        public class MathFunctionParams
        {
            public int Operand1;
            public int Operand2;
            public Token Op;
            public int Result;
        }

        public EventHandler<MathFunctionParams> MathFunctionDelegate;

        // Default Math Function Delegate.
        private void MathFunctionHandler (object sender, Interpreter.MathFunctionParams e)
        {
            if (e.Op == Token.RAND) {
                e.Result = RandNumber.Next (e.Operand1);
                Debug.WriteLine ("Random Number: {0}", e.Result);
                return;
            }

            double value = e.Operand1;
            value *= (3.1415926 / 180);


            switch (e.Op) {
            case Token.SIN:
                value = Math.Sin (value);
                break;
            case Token.COS:
                value = Math.Cos (value);
                break;
            case Token.TAN:
                value = Math.Tan (value);
                break;
            }

            e.Result = (int)(value * e.Operand2);
        }
            
        private MathFunctionParams HandleMathFunction(int op1, int op2, Token func)
        {
            MathFunctionParams p = null;

            if (MathFunctionDelegate != null) {
                p = new MathFunctionParams {
                    Operand1 = op1,
                    Operand2 = op2,
                    Op = func,
                };
                MathFunctionDelegate (this, p);
            }
            return p;
        }
    }
}

