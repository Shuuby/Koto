using System;
using System.Collections.Generic;

public enum OpCode
{
    CONSTANT,
    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,
    NEGATE,
    RETURN,
};

public class Chunk
{
    public List<Byte> code = null;
    public int Count { get { return code.Count; } }

    // Should use our own class for the values of constants
    public List<double> constants = null;
    public List<int> lines = null;

    public Chunk()
    {
        code = new List<byte>();
        constants = new List<double>();
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

    public int AddConstant(double value)
    {
        constants.Add(value);
        return constants.Count - 1;
    }
}