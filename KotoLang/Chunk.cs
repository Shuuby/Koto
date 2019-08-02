using System;
using System.Collections.Generic;

public enum OpCode
{
    CONSTANT,
    NIL,
    TRUE,
    FALSE,
    POP,
    EQUAL,
    GREATER,
    LESS,
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,
    NOT,
    NEGATE,
    PRINT,
    RETURN,
};

public class Chunk
{
    public List<Byte> code = null;
    public int Count { get { return code.Count; } }

    // Should use our own class for the values of constants
    public List<Value> constants = null;
    public List<int> lines = null;

    public Chunk()
    {
        code = new List<byte>();
        constants = new List<Value>();
        lines = new List<int>();
    }

    public void Add(OpCode newOp, int line)
    {
        Add((byte)newOp, line);
    }

    public void Add(int newInt, int line)
    {
        Add((byte)newInt, line);
    }

    public void Add(byte newByte, int line)
    {
        code.Add(newByte);
        lines.Add(line);
    }

    public int AddConstant(Value value)
    {
        constants.Add(value);
        return constants.Count - 1;
    }
}