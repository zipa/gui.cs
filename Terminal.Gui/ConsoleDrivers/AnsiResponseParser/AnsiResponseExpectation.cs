#nullable enable
namespace Terminal.Gui;

internal record AnsiResponseExpectation (string Terminator, Action<IHeld> Response)
{
    public bool Matches (string cur)
    {
        return cur.EndsWith (Terminator);
    }
}