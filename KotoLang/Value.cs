using Godot;
using System;

// This will probably require refactoring at some point.
// We just crudely save the actual value in here without
// any thought of memory management or pointers to objects
// in heap. It's horrible. I love it.

public class Value
{
    public ValueType type;
    private bool boolean;
    private double number;
    private Obj obj;

    public Value(bool boolean)
    {
        this.type = ValueType.BOOL;
        this.boolean = boolean;
    }

    public Value(double number)
    {
        this.type = ValueType.NUMBER;
        this.number = number;
    }
    
    public Value(Obj obj)
    {
        this.type = ValueType.OBJ;
        this.obj = obj;
    }

    public Value()
    {
        this.type = ValueType.NIL;
    }

    public bool AsBool()
    {
        if (IsBool())
            return boolean;
        else
            throw new Exception("Value conversion error.");
    }

    public double AsNumber()
    {
        if (IsNumber())
            return number;
        else
            throw new Exception("Value conversion error.");
    }

    public Obj AsObj()
    {
        if (IsObj())
            return obj;
        else
            throw new Exception("Value conversion error.");
    }

    public string AsString()
    {
        if (IsString())
            return AsObj().AsString();
        else
            throw new Exception("Value conversion error.");
    }

    // Obj helpers
    public ObjType GetObjType()
    {
        return AsObj().type;
    }

    public bool IsString()
    {
        return IsObjType(ObjType.STRING);
    }

    public bool IsObjType(ObjType type)
    {
        return IsObj() && AsObj().type == type;
    }

    public bool IsBool() { return type == ValueType.BOOL ? true : false; }
    public bool IsNumber() { return type == ValueType.NUMBER ? true : false; }
    public bool IsNil() { return type == ValueType.NIL ? true : false; }
    public bool IsObj() {return type == ValueType.OBJ ? true : false; }

    public override string ToString()
    {
        if (IsBool())
            return boolean.ToString();
        else if (IsNumber())
            return number.ToString();
        else if (IsObj())
            return obj.ToString();
        else
            return "NIL";
    }

}

public enum ValueType
{
    BOOL,
    NIL,
    NUMBER,
    OBJ
};
