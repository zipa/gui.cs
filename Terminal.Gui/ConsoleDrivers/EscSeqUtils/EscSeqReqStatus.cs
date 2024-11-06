#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents the status of an ANSI escape sequence request made to the terminal using
///     <see cref="EscSeqRequests"/>.
/// </summary>
/// <remarks></remarks>
public class EscSeqReqStatus
{
    /// <summary>Creates a new state of escape sequence request.</summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    public EscSeqReqStatus (AnsiEscapeSequenceRequest ansiRequest) { AnsiRequest = ansiRequest; }

    /// <summary>Gets the Escape Sequence Terminator (e.g. ESC[8t ... t is the terminator).</summary>
    public AnsiEscapeSequenceRequest AnsiRequest { get; }
}
