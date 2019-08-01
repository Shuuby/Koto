using Godot;
using System;
//using static Globals.Logger;

public class Compiler
{
    private int MAX_CONSTANT_IN_CHUNK = 256;

    private Scanner scanner;
    private Parser parser;
    private Chunk compilingChunk;
    private ParseRule[] rules;
    private Disassembler disassembler;

    private Logger logger;

    public Compiler(Logger logger)
    {
        this.logger = logger;

        parser = new Parser();
        disassembler = new Disassembler(logger);

        rules = new ParseRule[] {
            new ParseRule(Grouping, null,    Precedence.NONE),       // TOKEN_LEFT_PAREN      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_RIGHT_PAREN     
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_LEFT_BRACE
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_RIGHT_BRACE     
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_COMMA           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_DOT             
            new ParseRule(Unary,    Binary,  Precedence.TERM),       // TOKEN_MINUS           
            new ParseRule(null,     Binary,  Precedence.TERM),       // TOKEN_PLUS            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_SEMICOLON       
            new ParseRule(null,     Binary,  Precedence.FACTOR),     // TOKEN_SLASH           
            new ParseRule(null,     Binary,  Precedence.FACTOR),     // TOKEN_STAR            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_BANG            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_BANG_EQUAL      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_EQUAL           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_EQUAL_EQUAL     
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_GREATER         
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_GREATER_EQUAL   
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_LESS            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_LESS_EQUAL      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_IDENTIFIER      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_STRING          
            new ParseRule(Number,   null,    Precedence.NONE),       // TOKEN_NUMBER          
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_AND             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_CLASS           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_ELSE            
            new ParseRule(Literal,  null,    Precedence.NONE),       // TOKEN_FALSE           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_FOR             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_FUN             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_IF              
            new ParseRule(Literal,  null,    Precedence.NONE),       // TOKEN_NIL             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_OR              
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_PRINT           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_RETURN          
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_SUPER           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_THIS            
            new ParseRule(Literal,  null,    Precedence.NONE),       // TOKEN_TRUE            
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_VAR             
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_WHILE           
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_ERROR           
            new ParseRule(null,     null,    Precedence.NONE)        // TOKEN_EOF
        };
    }

    public bool Compile(string source, ref Chunk chunk)
    {
        scanner = new Scanner(source);

        compilingChunk = chunk;

        Advance();
        Expression();
        Consume(TokenType.EOF, "Expected end of expression.");

        EndCompiler();
        return !parser.hadError;
    }

    private void Advance()
    {
        parser.previous = parser.current;

        for (;;)
        {
            parser.current = scanner.ScanToken();
            if (parser.current.type != TokenType.ERROR) break;

            ErrorAtCurrent(parser.current.content);
        }
    }

    private void Expression()
    {
        ParsePrecedence(Precedence.ASSIGNMENT);
    }

    private void Number()
    {
        double value = Convert.ToDouble(parser.previous.content);
        EmitConstant(new Value(value));
    }

    private void Grouping()
    {
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
    }

    private void Unary()
    {
        TokenType operatorType = parser.previous.type;

        // Compile operand
        ParsePrecedence(Precedence.UNARY);

        // Emit operator instruction
        switch (operatorType)
        {
            case TokenType.MINUS: EmitByte(OpCode.NEGATE); break;
            default:
                return; // Unreachable.
        }
    }

    private void Binary()
    {
        // Remember the operator.
        TokenType operatorType = parser.previous.type;
        
        // Compile the right operand.
        ParseRule rule = GetRule(operatorType);
        ParsePrecedence((Precedence)(rule.precedence + 1)); // hehehee

        // Emit the operator instruction.
        switch (operatorType)
        {
            case TokenType.PLUS : EmitByte(OpCode.ADD); break;
            case TokenType.MINUS: EmitByte(OpCode.SUBTRACT); break;
            case TokenType.STAR : EmitByte(OpCode.MULTIPLY); break;
            case TokenType.SLASH: EmitByte(OpCode.DIVIDE); break;
            default:
                return; // Unreachable.
        }
    }

    private void Literal()
    {
        switch (parser.previous.type)
        {
            case TokenType.FALSE: EmitByte(OpCode.FALSE); break;
            case TokenType.TRUE: EmitByte(OpCode.TRUE); break;
            case TokenType.NIL: EmitByte(OpCode.NIL); break;
            default:
                return; // Unreachable.
        }
    }

    private ParseRule GetRule(TokenType token)
    {
        //logger.LogPrint("GetRule: {0}({1})", token, (int)token);
        return rules[(int)token];
    }

    private void EmitByte(OpCode newOp) { EmitByte((byte)newOp); }
    private void EmitByte(byte newByte)
    {
        CurrentChunk().Add(newByte, parser.previous.line);
    }

    private void EmitBytes(OpCode newOp1, byte newOp2) { EmitBytes((byte) newOp1, (byte)newOp2); }
    private void EmitBytes(OpCode newOp1, OpCode newOp2) { EmitBytes((byte) newOp1, (byte)newOp2); }
    private void EmitBytes(byte newByte1, byte newByte2)
    {
        EmitByte(newByte1);
        EmitByte(newByte2);
    }

    private void EmitReturn()
    {
        EmitByte(OpCode.RETURN);
    }

    private void EmitConstant(Value value)
    {
        EmitBytes(OpCode.CONSTANT, MakeConstant(value));
    }

    private byte MakeConstant(Value value)
    {
        int constantIndex = CurrentChunk().AddConstant(value);
        if (constantIndex > MAX_CONSTANT_IN_CHUNK)
        {
            Error("Too many constants in one chunk.");
            return 0;
        }
        return (byte)constantIndex;
    }

    private Chunk CurrentChunk()
    {
        return compilingChunk;
    }

    private void Consume(TokenType type, string message)
    {
        if (parser.current.type == type)
        {
            Advance();
            return;
        }

        ErrorAtCurrent(message);
    }

    private void ParsePrecedence(Precedence precedence)
    {
        Advance();
        Action prefixRule = GetRule(parser.previous.type).prefixFunction;
        if (prefixRule == null)
        {
            Error("Expected expression.");
            return;
        }

        prefixRule();

        while (precedence <= GetRule(parser.current.type).precedence)
        {
            Advance();
            Action infixRule = GetRule(parser.previous.type).infixFunction;
            infixRule();
        }
    }

    private void ErrorAtCurrent(string message)
    {
        ErrorAt(parser.current, message);
    }

    private void Error(string message)
    {
        ErrorAt(parser.previous, message);
    }

    private void ErrorAt(Token token, string message)
    {
        if (parser.panicMode) return;
        parser.panicMode = true;

        logger.Log("[{0}] Error", token.line);

        if (token.type == TokenType.EOF)
            logger.Log(" at end");
        else if (token.type == TokenType.ERROR)
        {
            // Nothing
        }
        else
            logger.Log(" at {0,6}", token.start);
        
        logger.LogPrint(": {0}", message);

        parser.hadError = true;
    }

    private void EndCompiler()
    {
        EmitReturn();
#if DEBUG
        if (!parser.hadError)
            disassembler.DisassembleChunk(CurrentChunk(), "Bytecode disassembly");
#endif
    }

}

public class Parser
{
    public Token previous;
    public Token current;
    public bool hadError = false;
    public bool panicMode = false;
}

public class ParseRule
{
    public Action prefixFunction;
    public Action infixFunction;
    public Precedence precedence;

    public ParseRule(Action prefix, Action infix, Precedence precedence)
    {
        this.prefixFunction = prefix;
        this.infixFunction = infix;
        this.precedence = precedence;
    }
}

public enum Precedence
{
    NONE,                    
    ASSIGNMENT,  // =        
    OR,          // or       
    AND,         // and      
    EQUALITY,    // == !=    
    COMPARISON,  // < > <= >=
    TERM,        // + -      
    FACTOR,      // * /      
    UNARY,       // ! -      
    CALL,        // . () []  
    PRIMARY
}