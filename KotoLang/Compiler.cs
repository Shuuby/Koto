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
            new ParseRule(Unary,    null,    Precedence.NONE),       // TOKEN_BANG            
            new ParseRule(null,     Binary,  Precedence.EQUALITY),   // TOKEN_BANG_EQUAL      
            new ParseRule(null,     null,    Precedence.NONE),       // TOKEN_EQUAL           
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_EQUAL_EQUAL     
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_GREATER         
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_GREATER_EQUAL   
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_LESS            
            new ParseRule(null,     Binary,  Precedence.COMPARISON), // TOKEN_LESS_EQUAL      
            new ParseRule(Variable, null,    Precedence.NONE),       // TOKEN_IDENTIFIER      
            new ParseRule(String,   null,    Precedence.NONE),       // TOKEN_STRING          
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

        while (!Match(TokenType.EOF))
        {
            Declaration();
        } 

        EndCompiler();
        return !parser.hadError;
    }

    private void Advance()
    {
        parser.previous = parser.current;

        for (;;)
        {
            parser.current = scanner.ScanToken();
            //logger.LogPrint("T => {0}", parser.current.type.ToString());

            if (parser.current.type != TokenType.ERROR)
                break;

            ErrorAtCurrent(parser.current.content);
        }
    }

    private void Declaration()
    {
        if (Match(TokenType.VAR))
        {
            VarDeclaration();
        }
        else
        {
            Statement();
        }

        if (parser.panicMode) Synchronize();
    }

    private void VarDeclaration()
    {
        byte global = ParseVariable("Expected variable name.");

        if (Match(TokenType.EQUAL))
        {
            Expression();
        }
        else
        {
            EmitByte(OpCode.NIL);
        }

        Consume(TokenType.SEMICOLON, "Expected ';' after variable declaration.");

        DefineVariable(global);
    }

    private byte ParseVariable(string errorMessage)
    {
        Consume(TokenType.IDENTIFIER, errorMessage);
        return IdentifierConstant(ref parser.previous);
    }

    private byte IdentifierConstant(ref Token token)
    {
        return MakeConstant(new Value(new Obj(token)));
    }

    private void DefineVariable(byte global)
    {
        EmitBytes(OpCode.DEFINE_GLOBAL, global);
    }

    private void Variable(bool canAssign)
    {
        NamedVariable(parser.previous, canAssign);
    }

    private void NamedVariable(Token token, bool canAssign)
    {
        byte arg = IdentifierConstant(ref token);
        if (canAssign && Match(TokenType.EQUAL))
        {
            Expression();
            EmitBytes(OpCode.SET_GLOBAL, arg);
        }
        else
        {
            EmitBytes(OpCode.GET_GLOBAL, arg);
        }
    }

    private void Statement()
    {
        if (Match(TokenType.PRINT))
        {
            PrintStatement();
        }
        else
        {
            ExpressionStatement();
        }
    }

    private void PrintStatement()
    {
        Expression();
        Consume(TokenType.SEMICOLON, "Expected ';' after value.");
        EmitByte(OpCode.PRINT);
    }

    private void ExpressionStatement()
    {
        Expression();
        Consume(TokenType.SEMICOLON, "Expected ';' after expression.");
        EmitByte(OpCode.POP);
    }

    private void Expression()
    {
        ParsePrecedence(Precedence.ASSIGNMENT);
    }

    private void Number(bool canAssign)
    {
        double value = Convert.ToDouble(parser.previous.content);
        EmitConstant(new Value(value));
    }
    
    private void String(bool canAssign)
    {
        EmitConstant(new Value(new Obj(parser.previous.content)));
    }

    private void Grouping(bool canAssign)
    {
        Expression();
        Consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
    }

    private void Unary(bool canAssign)
    {
        TokenType operatorType = parser.previous.type;

        // Compile operand
        ParsePrecedence(Precedence.UNARY);

        // Emit operator instruction
        switch (operatorType)
        {
            case TokenType.BANG: EmitByte(OpCode.NOT); break;
            case TokenType.MINUS: EmitByte(OpCode.NEGATE); break;
            default:
                return; // Unreachable.
        }
    }

    private void Binary(bool canAssign)
    {
        // Remember the operator.
        TokenType operatorType = parser.previous.type;
        
        // Compile the right operand.
        ParseRule rule = GetRule(operatorType);
        ParsePrecedence((Precedence)(rule.precedence + 1)); // hehehee

        // Emit the operator instruction.
        switch (operatorType)
        {
            case TokenType.BANG_EQUAL: EmitBytes(OpCode.EQUAL, OpCode.NOT); break;
            case TokenType.EQUAL_EQUAL: EmitByte(OpCode.EQUAL); break;
            case TokenType.GREATER: EmitByte(OpCode.GREATER); break;
            case TokenType.GREATER_EQUAL: EmitBytes(OpCode.LESS, OpCode.NOT); break; // >= is the same as !(<)
            case TokenType.LESS: EmitByte(OpCode.LESS); break;
            case TokenType.LESS_EQUAL: EmitBytes(OpCode.GREATER, OpCode.NOT); break; // <= is the same as !(>)
            case TokenType.PLUS : EmitByte(OpCode.ADD); break;
            case TokenType.MINUS: EmitByte(OpCode.SUBTRACT); break;
            case TokenType.STAR : EmitByte(OpCode.MULTIPLY); break;
            case TokenType.SLASH: EmitByte(OpCode.DIVIDE); break;
            default:
                return; // Unreachable.
        }
    }

    private void Literal(bool canAssign)
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

    // Functions for adding new operations to the bytecode
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

    // Check if current token matches what we want
    // If it does, consume and return true
    // If not, leave it alone and return false
    private bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }

    // Helper for Match()
    private bool Check(TokenType type)
    {
        return parser.current.type == type;
    }


    private void ParsePrecedence(Precedence precedence)
    {
        Advance();
        Action<bool> prefixRule = GetRule(parser.previous.type).prefixFunction;
        if (prefixRule == null)
        {
            Error("Expected expression.");
            return;
        }

        bool canAssign = precedence <= Precedence.ASSIGNMENT;
        prefixRule(canAssign);

        while (precedence <= GetRule(parser.current.type).precedence)
        {
            Advance();
            Action<bool> infixRule = GetRule(parser.previous.type).infixFunction;
            infixRule(canAssign);
        }

        if (canAssign && Match(TokenType.EQUAL))
        {
            Error("Invalid assignment target.");
            Expression();
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

    private void Synchronize()
    {
        parser.panicMode = false;

        while (parser.current.type != TokenType.EOF)
        {
            if (parser.previous.type == TokenType.SEMICOLON) return;

            switch (parser.current.type)
            {
                case TokenType.CLASS:
                case TokenType.FUN:                                   
                case TokenType.VAR:                                   
                case TokenType.FOR:                                   
                case TokenType.IF:                                    
                case TokenType.WHILE:                                 
                case TokenType.PRINT:                                 
                case TokenType.RETURN:                                
                    return;
                
                default:
                    break; // Do nothing
            }

            Advance();
        }
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
    public Action<bool> prefixFunction;
    public Action<bool> infixFunction;
    public Precedence precedence;

    public ParseRule(Action<bool> prefix, Action<bool> infix, Precedence precedence)
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