#nullable enable
namespace Terminal.Gui;

public interface IAnsiResponseParser
{
    AnsiResponseParserState State { get; }
    void ExpectResponse (string terminator, Action<string> response);
}
