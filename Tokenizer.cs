using System;
using System.Diagnostics;

namespace BasicSharp
{
    public enum Token {
        TOKENIZER_ERROR,
        // Variant Tokens.
        TOKENIZER_ENDOFINPUT,
        TOKENIZER_NUMBER,
        TOKENIZER_STRING,
        TOKENIZER_VARIABLE,

        // Single-Character Tokens.
        TOKENIZER_COMMA,
        TOKENIZER_SEMICOLON,
        TOKENIZER_PLUS,
        TOKENIZER_MINUS,
        TOKENIZER_AND,
        TOKENIZER_OR,
        TOKENIZER_ASTR,
        TOKENIZER_SLASH,
        TOKENIZER_MOD,
        TOKENIZER_LEFTPAREN,
        TOKENIZER_RIGHTPAREN,
        TOKENIZER_LT,
        TOKENIZER_GT,
        TOKENIZER_EQ,
        TOKENIZER_CR,

        // Basic Keywords.
        TOKENIZER_PRINT,
        TOKENIZER_IF,
        TOKENIZER_THEN,
        TOKENIZER_ELSE,
        TOKENIZER_FOR,
        TOKENIZER_TO,
        TOKENIZER_NEXT,
        TOKENIZER_GOTO,
        TOKENIZER_GOSUB,
        TOKENIZER_RETURN,
        TOKENIZER_CALL,
        TOKENIZER_END,

        // Extended Keywords.
        TOKENIZER_SIN,
        TOKENIZER_COS,
        TOKENIZER_TAN,
        TOKENIZER_RAND,

        // Device-specific Keywords.
        TOKENIZER_LED,

        // End of Tokens.
        TOKENIZER_NULL,
    };

    public class Tokenizer
    {
        class KeywordToken {
            public string keyword;
            public Token token;
        };

        private static KeywordToken[] keywords = new KeywordToken[] {
            // Basic Keywords.
            new KeywordToken() {    keyword = "print",  token = Token.TOKENIZER_PRINT},
            new KeywordToken() {    keyword = "if",     token = Token.TOKENIZER_IF},
            new KeywordToken() {    keyword = "then",   token = Token.TOKENIZER_THEN},
            new KeywordToken() {    keyword = "else",   token = Token.TOKENIZER_ELSE},
            new KeywordToken() {    keyword = "for",    token = Token.TOKENIZER_FOR},
            new KeywordToken() {    keyword = "to",     token = Token.TOKENIZER_TO},
            new KeywordToken() {    keyword = "next",   token = Token.TOKENIZER_NEXT},
            new KeywordToken() {    keyword = "goto",   token = Token.TOKENIZER_GOTO},
            new KeywordToken() {    keyword = "gosub",  token = Token.TOKENIZER_GOSUB},
            new KeywordToken() {    keyword = "return", token = Token.TOKENIZER_RETURN},
            new KeywordToken() {    keyword = "call",   token = Token.TOKENIZER_CALL},
            new KeywordToken() {    keyword = "end",    token = Token.TOKENIZER_END},
            // Extended Keywords.
            new KeywordToken() {    keyword = "sin",    token = Token.TOKENIZER_SIN},
            new KeywordToken() {    keyword = "cos",    token = Token.TOKENIZER_COS},
            new KeywordToken() {    keyword = "tan",    token = Token.TOKENIZER_TAN},
            new KeywordToken() {    keyword = "rand",   token = Token.TOKENIZER_RAND},
            // Device-specific Keywords.
            new KeywordToken() {    keyword = "led",    token = Token.TOKENIZER_LED},
        };

        private int PC, NextPC;
        private char[] Program;

        //private char* ptr, nextptr;

        private const int MAX_NUMLEN = 5;

        private Token CurrentToken = Token.TOKENIZER_ERROR;

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
            if(Finished()) {
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

        public Token GetToken()
        {
            return CurrentToken;
        }

        public int GetNumber()
        {
            return int.Parse(new string(Program, PC, NextPC - PC));
        }

        public int GetVariable()
        {
            return char.ToLower(Program[PC]) - 'a';
        }

        public string GetString()
        {
            if(GetToken() != Token.TOKENIZER_STRING) {
                return null;
            }

            int end = PC + 1;

            while (Program.Length > end && Program [end] != '"')
                end++;

            if (Program.Length == end)
                return null;

            return new string (Program, PC + 1, end - PC - 1);
        }

        public bool Finished()
        {
            return (PC == Program.Length || CurrentToken == Token.TOKENIZER_ENDOFINPUT);
        }

        public void PrintError()
        {
            Debug.WriteLine("tokenizer_error_print: '{0}'", PC);
        }

        /*---------------------------------------------------------------------------*/
        private Token SingleChar()
        {
            char reg = Program [PC];

            switch (reg) {
            case '\n':
                return Token.TOKENIZER_CR;
            case ',':
                return Token.TOKENIZER_COMMA;
            case ';':
                return Token.TOKENIZER_SEMICOLON;
            case '+':
                return Token.TOKENIZER_PLUS;
            case '-':
                return Token.TOKENIZER_MINUS;
            case '&':
                return Token.TOKENIZER_AND;
            case '|':
                return Token.TOKENIZER_OR;
            case '*':
                return Token.TOKENIZER_ASTR;
            case '/':
                return Token.TOKENIZER_SLASH;
            case '%':
                return Token.TOKENIZER_MOD;
            case '(':
                return Token.TOKENIZER_LEFTPAREN;
            case ')':
                return Token.TOKENIZER_RIGHTPAREN;
            case '<':
                return Token.TOKENIZER_LT;
            case '>':
                return Token.TOKENIZER_GT;
            case '=':
                return Token.TOKENIZER_EQ;
            }
            return Token.TOKENIZER_NULL;
        }

        private Token GetNextToken()
        {
            if (PC == Program.Length)
                return Token.TOKENIZER_ENDOFINPUT;

            char reg = char.ToLower(Program [PC]);

            if (reg == 0) {
                return Token.TOKENIZER_ENDOFINPUT;
            }

            if (char.IsDigit(reg)) {
                for (int i = 0; i < MAX_NUMLEN; ++ i) {
                    if(!char.IsDigit(Program[PC + i])) {
                        if(i > 0) {
                            NextPC = PC + i;
                            return Token.TOKENIZER_NUMBER;
                        } else {
                            Debug.WriteLine("get_next_token: error due to too short number");
                            return Token.TOKENIZER_ERROR;
                        }
                    }
                    if(!char.IsDigit(Program[PC + i])) {
                        Debug.WriteLine("get_next_token: error due to malformed number");
                        return Token.TOKENIZER_ERROR;
                    }
                }
                Debug.WriteLine("get_next_token: error due to too long number");
                return Token.TOKENIZER_ERROR;
            } else if(SingleChar() != Token.TOKENIZER_NULL) {
                NextPC = PC + 1;
                return SingleChar();
            } else if(reg == '"') {
                NextPC = PC;
                do {
                    ++ NextPC;
                } while(Program[NextPC] != '"');

                ++ NextPC;
                return Token.TOKENIZER_STRING;
            } else {
                foreach (var kt in keywords) {
                    if (CheckToken(kt.keyword.ToCharArray())) {
                        NextPC = PC + kt.keyword.Length;
                        return kt.token;
                    }
                }
            }

            if(reg >= 'a' && reg <= 'z') {
                NextPC = PC + 1;
                return Token.TOKENIZER_VARIABLE;
            }

            return Token.TOKENIZER_ERROR;
        }

        private bool CheckToken(char[] token)
        {
            for (int i = 0; i < token.Length; i++) {
                if (char.ToLower(Program [PC + i]) != token [i])
                    return false;
            }

            return true;
        }
    }
}

