using System;
using Godot;
//using static Globals.Logger;

public class Disassembler
{
    private Logger logger;

    public Disassembler(Logger logger)
    {
        this.logger = logger;
    }

    public void DisassembleChunk(Chunk chunk, string name)
    {
        logger.LogPrint("== {0} ==", name);

        for (int offset = 0; offset < chunk.Count;)
        {
            offset = DisassembleInstruction(chunk, offset);
        }
    }

    public int DisassembleInstruction(Chunk chunk, int offset)
    {
        logger.Log("{0,4:d} ", offset);

        if (offset > 0 && chunk.lines[offset] == chunk.lines[offset-1])
            logger.Log("   | ");
        else
            logger.Log("{0,4:d} ", chunk.lines[offset]);

        Byte instruction = chunk.code[offset];

        switch((OpCode)instruction)
        {
            case OpCode.CONSTANT:
                return ConstantInstruction("OP_CONSTANT", chunk, offset);

            case OpCode.ADD:
                return SimpleInstruction("OP_ADD", offset);

            case OpCode.SUBTRACT:
                return SimpleInstruction("OP_SUBTRACT", offset);

            case OpCode.MULTIPLY:
                return SimpleInstruction("OP_MULTIPLY", offset);

            case OpCode.DIVIDE:
                return SimpleInstruction("OP_DIVIDE", offset);

            case OpCode.NEGATE:
                return SimpleInstruction("OP_NEGATE", offset);

            case OpCode.RETURN:
                return SimpleInstruction("OP_RETURN", offset);

            default:
                logger.LogPrint("Unknown opcode: {0}", instruction);
                return offset + 1;   
        }
    }

    private int ConstantInstruction(string name, Chunk chunk, int offset)
    {
        // get constant index from next byte
        byte constantIndex = chunk.code[offset + 1];
        logger.Log("{0,-16:d} {1,4:d} ", name, constantIndex);
        logger.LogPrint("{0:f}", chunk.constants[constantIndex]);

        // skip over the constant index
        return offset + 2;
    }

    private int SimpleInstruction(string name, int offset)
    {
        logger.LogPrint("{0}", name);

        return offset + 1;
    }

    
}