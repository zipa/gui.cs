#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes an ongoing ANSI request sent to the console.
///     Send a request using <see cref="ConsoleDriver.TryWriteAnsiRequest"/> which will return the response.
/// </summary>
public class AnsiEscapeSequenceRequest
{
    internal readonly object _responseLock = new (); // Per-instance lock

    /// <summary>
    ///     Gets the response received from the request.
    /// </summary>
    public AnsiEscapeSequenceResponse? AnsiEscapeSequenceResponse { get; internal set; }

    /// <summary>
    ///     The value expected in the response after the CSI e.g.
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_ReportTerminalSizeInChars.Value</cref>
    ///     </see>
    ///     should result in a response of the form <c>ESC [ 8 ; height ; width t</c>. In this case,
    ///     <see cref="ExpectedResponseValue"/>
    ///     will be <c>"8"</c>.
    /// </summary>
    public string? ExpectedResponseValue { get; init; }

    /// <summary>
    ///     Gets the request string to send e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    /// </summary>
    public required string Request { get; init; }

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

    internal void RaiseResponseFromInput (string? response, AnsiEscapeSequenceRequest? request)
    {
        ProcessResponse (response, request);

        ResponseFromInput?.Invoke (this, AnsiEscapeSequenceResponse);
    }

    /// <summary>
    ///     Raised with the response object and validation.
    /// </summary>
    internal event EventHandler<AnsiEscapeSequenceResponse?>? ResponseFromInput;

    /// <summary>
    ///     Process the <see cref="AnsiEscapeSequenceResponse"/> of an ANSI escape sequence request.
    /// </summary>
    /// <param name="response">The response.</param>
    private void ProcessResponse (string? response, AnsiEscapeSequenceRequest? request)
    {
        var error = new StringBuilder ();
        var values = new string? [] { null };

        try
        {
            if (!string.IsNullOrEmpty (response) && !response.StartsWith (AnsiEscapeSequenceRequestUtils.KeyEsc))
            {
                throw new InvalidOperationException ($"Invalid Response: {response}");
            }

            if (string.IsNullOrEmpty (Terminator))
            {
                throw new InvalidOperationException ("Terminator request is empty.");
            }

            if (string.IsNullOrEmpty (response))
            {
                throw new InvalidOperationException ("Response request is null.");
            }

            if (!string.IsNullOrEmpty (response) && !response.EndsWith (Terminator [^1]))
            {
                string resp = string.IsNullOrEmpty (response) ? "" : response.Last ().ToString ();

                throw new InvalidOperationException ($"Terminator ends with '{resp}'\nand doesn't end with: '{Terminator [^1]}'");
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
                (string? _, string? _, values, string? _) = AnsiEscapeSequenceRequestUtils.GetEscapeResult (response?.ToCharArray ());
            }

            if (request is { } && !string.IsNullOrEmpty (request.ExpectedResponseValue) && request.ExpectedResponseValue != values [0])
            {
                error.AppendLine ($"Error executing ANSI request:\nValue ends with '{values [0]}'\nand doesn't end with: '{ExpectedResponseValue! [^1]}'");
            }
        }

        AnsiEscapeSequenceResponse = new ()
        {
            Response = response, Error = error.ToString (),
            Terminator = string.IsNullOrEmpty (response) ? "" : response [^1].ToString (),
            ExpectedResponseValue = values [0],
            Valid = string.IsNullOrWhiteSpace (error.ToString ()) && !string.IsNullOrWhiteSpace (response)
        };
    }
}
