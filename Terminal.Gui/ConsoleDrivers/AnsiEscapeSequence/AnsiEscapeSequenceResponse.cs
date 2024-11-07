#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes a response received from the console as a result of a request being sent via <see cref="AnsiEscapeSequenceRequest"/>.
/// </summary>
public class AnsiEscapeSequenceResponse
{
    // QUESTION: Should this be nullable to indicate there was no error, or is string.Empty sufficient?
    /// <summary>
    ///     Gets the error string received from e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    ///     .
    /// </summary>
    public required string Error { get; init; }

    // QUESTION: Does string.Empty indicate no response recevied? If not, perhaps make this property nullable?
    /// <summary>
    ///     Gets the Response string received from e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    ///     .
    /// </summary>
    public required string? Response { get; init; }

    // QUESTION: Does string.Empty indicate no terminator expected? If not, perhaps make this property nullable?
    /// <summary>
    ///     <para>
    ///         Gets the terminator that uniquely identifies the response received from
    ///         the console. e.g. for
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///         </see>
    ///         the terminator is
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Terminator</cref>
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         After sending a request, the first response with matching terminator will be matched
    ///         to the oldest outstanding request.
    ///     </para>
    /// </summary>
    public required string Terminator { get; init; }

    /// <summary>
    ///     The value expected in the response after the CSI e.g.
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_ReportTerminalSizeInChars.Value</cref>
    ///     </see>
    ///     should result in a response of the form <c>ESC [ 8 ; height ; width t</c>. In this case, <see cref="ExpectedResponseValue"/>
    ///     will be <c>"8"</c>.
    /// </summary>

    public string? ExpectedResponseValue { get; init; }
}
