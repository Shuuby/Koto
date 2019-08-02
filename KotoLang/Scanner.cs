using Godot;
using System;
using System.Collections.Generic;
//using static Globals.Logger;

public class Scanner
{
    private string source;
    private char current;
    private int currentLine = 1;
    private int currentIndex = 0;
    private int start = 0;

    private Dictionary<string, TokenType> identifiers = new Dictionary<string, TokenType>()
    {
        { "and", TokenType.AND },
        { "class", TokenType.CLASS },
        { "else", TokenType.ELSE },
        { "false", TokenType.FALSE },
        { "for", TokenType.FOR },
        { "fun", TokenType.FUN },
        { "if", TokenType.IF },
        { "nil", TokenType.NIL },
        { "or", TokenType.OR },
        { "print", TokenType.PRINT },
        { "return", TokenType.RETURN },
        { "super", TokenType.SUPER },
        { "this", TokenType.THIS },
        { "true", TokenType.TRUE },
        { "var", TokenType.VAR },
        { "while", TokenType.WHILE }
    };

    public Scanner(string source)
    {
        this.source = source;
        this.currentLine = 1;
        this.currentIndex = 0;
        this.start = 0;
        //this.current = source[currentIndex];
    }

    public Token ScanToken()
    {
        //Log("\nScanToken():\n{0}-{1}", currentLine, currentIndex);
        SkipWhitespace();

        // We're at the beginning of a token when ScanToken is called
        start = currentIndex;

        // Check for EOF
        if (IsAtEnd()) return MakeToken(TokenType.EOF, "EOF");

        char c = Advance();

        //LogPrint(", char = {0}", c.ToString());
        
        // Identifiers
        if (IsAlpha(c)) return IdentifierToken();

        // Numbers
        if (IsDigit(c)) return NumberToken();  

        switch (c)
        {
            case '(': return MakeToken(TokenType.LEFT_PAREN); 
            case ')': return MakeToken(TokenType.RIGHT_PAREN);
            case '{': return MakeToken(TokenType.LEFT_BRACE); 
            case '}': return MakeToken(TokenType.RIGHT_BRACE);
            case ';': return MakeToken(TokenType.SEMICOLON);  
            case ',': return MakeToken(TokenType.COMMA);      
            case '.': return MakeToken(TokenType.DOT);        
            case '-': return MakeToken(TokenType.MINUS);      
            case '+': return MakeToken(TokenType.PLUS);       
            case '/': return MakeToken(TokenType.SLASH);      
            case '*': return MakeToken(TokenType.STAR);
            case '!':                                                        
                return MakeToken(Match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);  
            case '=':                                                        
                return MakeToken(Match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
            case '<':                                                        
                return MakeToken(Match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);  
            case '>':                                                        
                return MakeToken(Match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
            
            // Strings
            case '"': return StringToken();
        }

        return ErrorToken("Unexpected character.");
    }

    private void SkipWhitespace()
    {
        for (;;)
        {
            char c = Peek();

            switch (c)
            {
                case ' ':
                case '\r':
                case '\t':
                    Advance();
                    break;
                
                case '\n':
                    currentLine++;
                    Advance();
                    break;

                // Comments
                case '/':
                    // Need two /
                    if (PeekNext() == '/')
                    {
                        // Advance to the end without consuming the endline
                        // This is to ensure we increment currentLine on the next iteration
                        while (Peek() != '\n' && !IsAtEnd()) Advance();
                    }
                    else
                    {
                        return;
                    }
                    break;

                default:
                    return;
            }
        }
    }

    private char Peek()
    {
        if (IsAtEnd())
            return '\0';
        return source[currentIndex];
    }

    private char PeekNext()
    {
        if (IsAtEnd())
            return '\0';
        return source[currentIndex + 1];
    }

    private char Advance()
    {
        currentIndex++;
        return source[currentIndex-1];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd())
            return false;
        if (source[currentIndex] != expected)
            return false;

        currentIndex++;
        return true;
    }

    private bool IsAtEnd()
    {
        //LogPrint("IsAtEnd(): {0}/{1} -> {2}", currentIndex, source.Length-1, currentIndex >= source.Length-1);
        if (currentIndex >= source.Length-1) return true;
        else return false;
    }

    private bool IsDigit(char c)
    {
        return Char.IsDigit(c);
        //return c >= '0' && c <= '9';
    }

    private bool IsAlpha(char c)
    {
        return Char.IsLetter(c);
        /* return  (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                 c == '_'; */
    }

    private TokenType IdentifierType()
    {
        string identifier = source.Substring(start, currentIndex - start);
        //GD.Print("identifier string = '", identifier, "'");

        TokenType type;
        if (!identifiers.TryGetValue(identifier, out type))
            type = TokenType.IDENTIFIER;

        //GD.Print("identifier type = '", type.ToString() + "'");

        return type;
    }

    private Token MakeToken(TokenType type, string content = "")
    {
        Token token = new Token();
        token.type = type;
        token.start = start;
        token.length = currentIndex - start;// + 1;

        if (content == "")
            token.content = source.Substring(token.start, token.length);
        else
            token.content = content;

        token.line = currentLine;


        return token;
    }

    private Token ErrorToken(string error)
    {
        return MakeToken(TokenType.ERROR, error);
    }

    private Token StringToken()
    {
        // Consume until we reach the closing quote
        while (Peek() != '"' && !IsAtEnd())
        {
            // Support multiline strings
            if (Peek() == '\n') currentLine++;
            Advance();
        }
        if ( IsAtEnd()) return ErrorToken("Unterminated string.");

        // The closing quote
        Advance();

        return MakeToken(TokenType.STRING);
    }

    private Token NumberToken()
    {
        while (IsDigit(Peek())) Advance();

        // Look for fractional
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            // Consume the '.'
            Advance();

            while(IsDigit(Peek())) Advance();
        }

        return MakeToken(TokenType.NUMBER);
    }

    private Token IdentifierToken()
    {
        while (IsAlpha(Peek()) || IsDigit(Peek())) Advance();
        return MakeToken(IdentifierType());
    }
}

public class Token
{
    public TokenType type;
    public int length;
    public int start;
    public int line;
    public string content;
}

public enum TokenType
{                                        
  // Single-character tokens.                         
  LEFT_PAREN, RIGHT_PAREN,                
  LEFT_BRACE, RIGHT_BRACE,                
  COMMA, DOT, MINUS, PLUS,    
  SEMICOLON, SLASH, STAR,

  // One or two character tokens.                     
  BANG, BANG_EQUAL,                       
  EQUAL, EQUAL_EQUAL,                     
  GREATER, GREATER_EQUAL,                 
  LESS, LESS_EQUAL,                       

  // Literals.                                        
  IDENTIFIER, STRING, NUMBER,       

  // Keywords.                                        
  AND, CLASS, ELSE, FALSE,    
  FOR, FUN, IF, NIL, OR,
  PRINT, RETURN, SUPER, THIS, 
  TRUE, VAR, WHILE,                 

  ERROR,                                        
  EOF                                           
}