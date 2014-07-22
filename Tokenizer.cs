using System;
using System.Diagnostics;

namespace BasicSharp
{
    public enum Token {
        ERROR,
        ENDOFINPUT,
        // Variant Tokens.
        NUMBER,
        STRING,
        VARIABLE,

        // Single-Character Tokens.
        COMMA,
        SEMICOLON,
        PLUS,
        MINUS,
        AND,
        OR,
        ASTR,
        SLASH,
        MOD,
        LEFTPAREN,
        RIGHTPAREN,
        LT,
        GT,
        EQ,
        CR,

        // Basic Keywords.
        IF,
        THEN,
        ELSE,
        FOR,
        TO,
        NEXT,
        GOTO,
        GOSUB,
        RETURN,
        CALL,
        END,

        // Extended Keywords.
        PRINT,
        DELAY,
        SIN,
        COS,    
        TAN,
        RAND,

        // Device-specific Keywords.
        LED,

        // End of Tokens.
        NULL,
    };

    public class Tokenizer
    {
        class KeywordToken {
            public string keyword;
            public Token token;
        };

        private static KeywordToken[] keywords = new KeywordToken[] {
            // Basic Keywords.
            new KeywordToken() {    keyword = "if",     token = Token.IF},
            new KeywordToken() {    keyword = "then",   token = Token.THEN},
            new KeywordToken() {    keyword = "else",   token = Token.ELSE},
            new KeywordToken() {    keyword = "for",    token = Token.FOR},
            new KeywordToken() {    keyword = "to",     token = Token.TO},
            new KeywordToken() {    keyword = "next",   token = Token.NEXT},
            new KeywordToken() {    keyword = "goto",   token = Token.GOTO},
            new KeywordToken() {    keyword = "gosub",  token = Token.GOSUB},
            new KeywordToken() {    keyword = "return", token = Token.RETURN},
            new KeywordToken() {    keyword = "call",   token = Token.CALL},
            new KeywordToken() {    keyword = "end",    token = Token.END},
            // Extended Keywords.
            new KeywordToken() {    keyword = "print",  token = Token.PRINT},
            new KeywordToken() {    keyword = "delay",  token = Token.DELAY},
            new KeywordToken() {    keyword = "sin",    token = Token.SIN},
            new KeywordToken() {    keyword = "cos",    token = Token.COS},
            new KeywordToken() {    keyword = "tan",    token = Token.TAN},
            new KeywordToken() {    keyword = "rand",   token = Token.RAND},
            // Device-specific Keywords.
            new KeywordToken() {    keyword = "led",    token = Token.LED},
        };

        private int PC, NextPC;
        private char[] Program;

        //private char* ptr, nextptr;

        private const int MAX_NUMLEN = 5;

        private Token CurrentToken = Token.ERROR;

        public Tokenizer (char[] program)
        {
            Program = program;
            PC = 0;
            CurrentToken = GetNextToken();
        }

        public void Reset()
        {
            PC = 0;
            CurrentToken = GetNextToken ();
        }

        public void Next()
        {
            if(IsFinished()) {
                return;
            }

            Debug.WriteLine("tokenizer_next: {0}", NextPC);
            PC = NextPC;
            while(PC < Program.Length && Program[PC] == ' ') {
                ++ PC;
            }
            CurrentToken = GetNextToken();
            Debug.WriteLine("tokenizer_next: '{0}' {1}", PC, CurrentToken);
            return;
        }

        public void PrintError()
        {
            Debug.WriteLine("tokenizer_error_print: '{0}'", PC);
        }

        public Token GetToken()
        {
            return CurrentToken;
        }

        public bool IsFinished()
        {
            return (PC == Program.Length || CurrentToken == Token.ENDOFINPUT);
        }

        // Parsing contents.
        public int GetNumber()
        {
            return int.Parse(new string(Program, PC, NextPC - PC));
        }

        public string GetString()
        {
            if(GetToken() != Token.STRING) {
                return null;
            }

            int end = PC + 1;

            while (Program.Length > end && Program [end] != '"')
                end++;

            if (Program.Length == end)
                return null;

            return new string (Program, PC + 1, end - PC - 1);
        }

        public int GetVariable()
        {
            return char.ToLower(Program[PC]) - 'a';
        }

        // Scan tokens.
        private Token CheckSingleChar()
        {
            char reg = Program [PC];

            switch (reg) {
            case '\n':
                return Token.CR;
            case ',':
                return Token.COMMA;
            case ';':
                return Token.SEMICOLON;
            case '+':
                return Token.PLUS;
            case '-':
                return Token.MINUS;
            case '&':
                return Token.AND;
            case '|':
                return Token.OR;
            case '*':
                return Token.ASTR;
            case '/':
                return Token.SLASH;
            case '%':
                return Token.MOD;
            case '(':
                return Token.LEFTPAREN;
            case ')':
                return Token.RIGHTPAREN;
            case '<':
                return Token.LT;
            case '>':
                return Token.GT;
            case '=':
                return Token.EQ;
            }
            return Token.NULL;
        }

        private bool CheckKeyword(char[] token)
        {
            for (int i = 0; i < token.Length; i++) {
                if (char.ToLower(Program [PC + i]) != token [i])
                    return false;
            }

            return true;
        }

        private Token GetNextToken()
        {
            if (PC == Program.Length)
                return Token.ENDOFINPUT;

            char reg = char.ToLower(Program [PC]);

            if (reg == 0) {
                return Token.ENDOFINPUT;
            }

            if (char.IsDigit(reg)) {
                for (int i = 0; i < MAX_NUMLEN; ++ i) {
                    if(!char.IsDigit(Program[PC + i])) {
                        if(i > 0) {
                            NextPC = PC + i;
                            return Token.NUMBER;
                        } else {
                            Debug.WriteLine("get_next_token: error due to too short number");
                            return Token.ERROR;
                        }
                    }
                    if(!char.IsDigit(Program[PC + i])) {
                        Debug.WriteLine("get_next_token: error due to malformed number");
                        return Token.ERROR;
                    }
                }
                Debug.WriteLine("get_next_token: error due to too long number");
                return Token.ERROR;
            } else if(CheckSingleChar() != Token.NULL) {
                NextPC = PC + 1;
                return CheckSingleChar();
            } else if(reg == '"') {
                NextPC = PC;
                do {
                    ++ NextPC;
                } while(Program[NextPC] != '"');

                ++ NextPC;
                return Token.STRING;
            } else {
                foreach (var kt in keywords) {
                    if (CheckKeyword(kt.keyword.ToCharArray())) {
                        NextPC = PC + kt.keyword.Length;
                        return kt.token;
                    }
                }
            }

            if(reg >= 'a' && reg <= 'z') {
                NextPC = PC + 1;
                return Token.VARIABLE;
            }

            return Token.ERROR;
        }
    }
}

