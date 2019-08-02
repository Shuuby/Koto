using Godot;
using System;

public class Obj
{
    public ObjType type;
    string stringContent;

    // This is used by the compiler to save identifiers as constants
    public Obj(Token token)
    {
        this.type = ObjType.STRING;
        this.stringContent = token.content;
    }

    public Obj(string stringContent, bool trimQuotes = true)
    {
        this.type = ObjType.STRING;
        // Trim the quotation marks
        this.stringContent = trimQuotes ? stringContent.Substring(1, stringContent.Length-2) : stringContent;
    }

    public string AsString()
    {
        if (type == ObjType.STRING)
            return stringContent;
        else
            throw new Exception("Object conversion error.");
    }

    public override string ToString()
    {
        if (type == ObjType.STRING)
            return stringContent;
        else
            return "NILOBJ";
    }
}

public enum ObjType
{
    STRING
};