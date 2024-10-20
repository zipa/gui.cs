#nullable enable
namespace Terminal.Gui;

public interface IAnsiResponseParser
{
    AnsiResponseParserState State { get; }
    void ExpectResponse (string terminator, Action<string> response);

    /// <summary>
    /// Returns true if there is an existing expectation (i.e. we are waiting a response
    /// from console) for the given <paramref name="requestTerminator"/>.
    /// </summary>
    /// <param name="requestTerminator"></param>
    /// <returns></returns>
    bool IsExpecting (string requestTerminator);
}
