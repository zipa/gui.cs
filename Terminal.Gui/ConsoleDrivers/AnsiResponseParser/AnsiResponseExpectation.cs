#nullable enable
namespace Terminal.Gui;

public record AnsiResponseExpectation (string Terminator, Action<string> Response)
{
    public bool Matches (string cur)
    {
        return cur.EndsWith (Terminator);
    }
}
