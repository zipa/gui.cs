#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes an ongoing ANSI request sent to the console.
///     Use <see cref="ResponseReceived"/> to handle the response
///     when the console answers the request.
/// </summary>
public class AnsiEscapeSequenceRequest
{
    internal readonly object _responseLock = new (); // Per-instance lock

    /// <summary>
    ///     Gets the request string to send e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    /// </summary>
    public required string Request { get; init; }

    // QUESTION: Could the type of this propperty be AnsiEscapeSequenceResponse? This would remove the
    // QUESTION: removal of the redundant Rresponse, Terminator, and ExpectedRespnseValue properties from this class?
    // QUESTION: Does string.Empty indicate no response recevied? If not, perhaps make this property nullable?
    /// <summary>
    ///     Gets the response received from the request.
    /// </summary>
    public string Response { get; internal set; } = string.Empty;

    /// <summary>
    ///     Raised when the console responds with an ANSI response code that matches the
    ///     <see cref="Terminator"/>
    /// </summary>
    public event EventHandler<AnsiEscapeSequenceResponse>? ResponseReceived;

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
    ///     Attempt an ANSI escape sequence request which may return a response or error.
    /// </summary>
    /// <param name="ansiRequest">The ANSI escape sequence to request.</param>
    /// <param name="result">
    ///     When this method returns <see langword="true"/>, the response. <see cref="AnsiEscapeSequenceResponse.Error"/> will
    ///     be <see cref="string.Empty"/>.
    /// </param>
    /// <returns>A <see cref="AnsiEscapeSequenceResponse"/> with the response, error, terminator, and value.</returns>
    public static bool TryRequest (AnsiEscapeSequenceRequest ansiRequest, out AnsiEscapeSequenceResponse result)
    {
        var error = new StringBuilder ();
        var values = new string? [] { null };

        try
        {
            ConsoleDriver? driver = Application.Driver;

            // Send the ANSI escape sequence
            ansiRequest.Response = driver?.WriteAnsiRequest (ansiRequest)!;

            if (!string.IsNullOrEmpty (ansiRequest.Response) && !ansiRequest.Response.StartsWith (EscSeqUtils.KeyEsc))
            {
                throw new InvalidOperationException ($"Invalid Response: {ansiRequest.Response}");
            }

            if (string.IsNullOrEmpty (ansiRequest.Terminator))
            {
                throw new InvalidOperationException ("Terminator request is empty.");
            }

            if (!ansiRequest.Response.EndsWith (ansiRequest.Terminator [^1]))
            {
                string resp = string.IsNullOrEmpty (ansiRequest.Response) ? "" : ansiRequest.Response.Last ().ToString ();

                throw new InvalidOperationException ($"Terminator ends with '{resp}'\nand doesn't end with: '{ansiRequest.Terminator [^1]}'");
            }
        }
        catch (Exception ex)
        {
            error.AppendLine ($"Error executing ANSI request:\n{ex.Message}");
        }
        finally
        {
            if (string.IsNullOrEmpty (error.ToString ()))
            {
                (string? _, string? _, values, string? _) = EscSeqUtils.GetEscapeResult (ansiRequest.Response.ToCharArray ());
            }
        }

        AnsiEscapeSequenceResponse ansiResponse = new ()
        {
            Response = ansiRequest.Response, Error = error.ToString (),
            Terminator = string.IsNullOrEmpty (ansiRequest.Response) ? "" : ansiRequest.Response [^1].ToString (), ExpectedResponseValue = values [0]
        };

        // Invoke the event if it's subscribed
        ansiRequest.ResponseReceived?.Invoke (ansiRequest, ansiResponse);

        result = ansiResponse;

        return string.IsNullOrWhiteSpace (result.Error) && !string.IsNullOrWhiteSpace (result.Response);
    }

    /// <summary>
    ///     The value expected in the response after the CSI e.g.
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_ReportTerminalSizeInChars.Value</cref>
    ///     </see>
    ///     should result in a response of the form <c>ESC [ 8 ; height ; width t</c>. In this case, <see cref="ExpectedResponseValue"/>
    ///     will be <c>"8"</c>.
    /// </summary>
    public string? ExpectedResponseValue { get; init; }

    internal void RaiseResponseFromInput (AnsiEscapeSequenceRequest ansiRequest, string response) { ResponseFromInput?.Invoke (ansiRequest, response); }

    // QUESTION: What is this for? Please provide a descriptive comment.
    internal event EventHandler<string>? ResponseFromInput;
}
