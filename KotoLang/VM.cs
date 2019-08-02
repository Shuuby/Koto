using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
//using static Globals.Logger;

public enum InterpretResult
{
    OK,
    COMPILE_ERROR,
    RUNTIME_ERROR
}

public class VM
{
    private Chunk currentChunk = null;

    // Instruction pointer
    // Points to the next instruction about to be executed
    // In C this should be a pointer to the memory address of the bytecode array
    private byte ip;

    //private const int STACK_MAX = 256;
    private Stack<Value> stack;

    private Compiler compiler;

    // This is for debugging. Perhaps wrap in #if (DEBUG) #endif ?
    private Disassembler disassembler;

    private Logger logger;

    public VM(Logger logger)
    {
        this.logger = logger;
        stack = new Stack<Value>();
        compiler = new Compiler(logger);
        disassembler = new Disassembler(logger);
    }

    public InterpretResult Interpret(string source)
    {
        Chunk chunk = new Chunk();

        if (!compiler.Compile(source, ref chunk))
        {
            return InterpretResult.COMPILE_ERROR;
        }

        currentChunk = chunk;
        ip = 0;

        InterpretResult result = Run();

        return result;
    }

    private InterpretResult Run()
    {
        for (;;)
        {
            // Get next instruction
            OpCode instruction = (OpCode)ReadByte();

            // Decode / dispatch instruction
            switch(instruction)
            {
                case OpCode.CONSTANT:
                    Value constant = ReadConstant();
                    stack.Push(constant);
                    break;

                case OpCode.NIL: stack.Push(new Value()); break;
                case OpCode.TRUE: stack.Push(new Value(true)); break;
                case OpCode.FALSE: stack.Push(new Value(false)); break;

                case OpCode.POP: stack.Pop(); break;

                case OpCode.EQUAL:
                    EqualityOp();
                    break;
                    
                case OpCode.GREATER:
                    if (!BinaryOp((a, b) => a > b))
                        return InterpretResult.RUNTIME_ERROR;
                    break;

                case OpCode.LESS:
                    if (!BinaryOp((a, b) => a < b))
                        return InterpretResult.RUNTIME_ERROR;
                    break;

                case OpCode.ADD:
                    if (!BinaryOp((a, b) => a + b))
                        return InterpretResult.RUNTIME_ERROR;
                    break;

                case OpCode.SUBTRACT:
                    if (!BinaryOp((a, b) => a - b))
                        return InterpretResult.RUNTIME_ERROR;
                    break;

                case OpCode.MULTIPLY:
                    if (!BinaryOp((a, b) => a * b))
                        return InterpretResult.RUNTIME_ERROR;
                    break;

                case OpCode.DIVIDE:
                    if (!BinaryOp((a, b) => a / b))
                        return InterpretResult.RUNTIME_ERROR;
                    break;

                case OpCode.NOT:
                    stack.Push(new Value(IsFalsey(stack.Pop())));
                    break;

                case OpCode.NEGATE:
                    if (!Peek().IsNumber())
                    {
                        RuntimeError("Operand must be a number.");
                        return InterpretResult.RUNTIME_ERROR;
                    }
                    stack.Push(new Value(-stack.Pop().AsNumber()));
                    break;

                case OpCode.PRINT:
                    logger.LogPrint(stack.Pop().ToString());
                    break;
                
                case OpCode.RETURN:
                    // Exit
                    return InterpretResult.OK;

                
            }
        }
    }

    private byte ReadByte()
    {
        byte instruction = currentChunk.code[ip];
        ip++;
        return instruction;
    }

    private Value ReadConstant()
    {
        int constantIndex =(int)ReadByte();
        return currentChunk.constants[constantIndex];
    }
    
    private void EqualityOp()
    {
        Value b = stack.Pop();
        Value a = stack.Pop();
        stack.Push(new Value(ValuesEqual(a, b)));
    }

    private bool BinaryOp<T>(Func<double, double, T> op)
    {
        if (!Peek().IsNumber() || !Peek(1).IsNumber())
        {
            RuntimeError("Operands must be numbers.");
            return false; //InterpretResult.RUNTIME_ERROR;
        }
        double b = stack.Pop().AsNumber();
        double a = stack.Pop().AsNumber();

        // Have to do runtime conversion to actual value from generic func return
        // This sucks. Look into making Value completely generic?
        var genericResult = op(a, b);
        Value valueResult = null;

        if (typeof(T) == typeof(bool))
            valueResult = new Value((bool)Convert.ChangeType(genericResult, typeof(bool)));
        else if (typeof(T) == typeof(double))
            valueResult = new Value((double)Convert.ChangeType(genericResult, typeof(double)));
        else
        {
            RuntimeError("Binary operation value conversion failed.");
            return false;
        }

        stack.Push(valueResult);
        return true;
    }

    private void RuntimeError(string msg, params object[] args)
    {
        string message = String.Format(msg, args);
        logger.LogPrint(message);
        int line = currentChunk.lines[ip];
        logger.LogPrint("[line {0}] in script.", line);
    }

    // Utility wrapper for peeking into the stack using Linq
    private Value Peek(int depth = 0)
    {
        return depth == 0 ? stack.Peek() : stack.Skip(depth).First();
    }

    private bool IsFalsey(Value value)
    {
        return value.IsNil() || (value.IsBool() && !value.AsBool());
    }

    private bool ValuesEqual(Value a, Value b)
    {
        if (a.type != b.type) return false;

        switch (a.type)
        {
            case ValueType.BOOL:    return a.AsBool() == b.AsBool();
            case ValueType.NIL:     return true;
            case ValueType.NUMBER:  return a.AsNumber() == b.AsNumber();
            default:
                return false; // Unreachable.
        }
    }
}
