#nullable enable

using System.Collections.Concurrent;

namespace Terminal.Gui;

// TODO: This class is a singleton. It should use the singleton pattern.
/// <summary>
///     Manages ANSI Escape Sequence requests and responses. The list of <see cref="AnsiEscapeSequenceRequestStatus"/>
///     contains the
///     status of the request. Each request is identified by the terminator (e.g. ESC[8t ... t is the terminator).
/// </summary>
public class AnsiEscapeSequenceRequests
{
    /// <summary>
    ///     Adds a new request for the ANSI Escape Sequence defined by <paramref name="ansiRequest"/>. Adds a
    ///     <see cref="AnsiEscapeSequenceRequestStatus"/> instance to <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    /// <param name="driver">The driver in use.</param>
    public void Add (AnsiEscapeSequenceRequest ansiRequest, ConsoleDriver? driver = null)
    {
        lock (Statuses)
        {
            Statuses.Enqueue (new (ansiRequest));

            if (driver is null)
            {
                Console.Out.Write (ansiRequest.Request);
                Console.Out.Flush ();
            }
            else
            {
                driver.WriteRaw (ansiRequest.Request);
            }
        }
    }

    /// <summary>
    ///     Indicates if a <see cref="AnsiEscapeSequenceRequestStatus"/> with the <paramref name="terminator"/> exists in the
    ///     <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="terminator"></param>
    /// <param name="seqReqStatus"></param>
    /// <returns><see langword="true"/> if exist, <see langword="false"/> otherwise.</returns>
    public bool HasResponse (string terminator, out AnsiEscapeSequenceRequestStatus? seqReqStatus)
    {
        lock (Statuses)
        {
            Statuses.TryPeek (out seqReqStatus);

            bool result = seqReqStatus?.AnsiRequest.Terminator == terminator;

            if (result)
            {
                return true;
            }

            seqReqStatus = null;

            return false;
        }
    }

    /// <summary>
    ///     Removes a request defined by <paramref name="seqReqStatus"/>. If a matching
    ///     <see cref="AnsiEscapeSequenceRequestStatus"/> is
    ///     found and the number of outstanding requests is greater than 0, the number of outstanding requests is decremented.
    ///     If the number of outstanding requests is 0, the <see cref="AnsiEscapeSequenceRequestStatus"/> is removed from
    ///     <see cref="Statuses"/>.
    /// </summary>
    /// <param name="seqReqStatus">The <see cref="AnsiEscapeSequenceRequestStatus"/> object.</param>
    public void Remove (AnsiEscapeSequenceRequestStatus? seqReqStatus)
    {
        lock (Statuses)
        {
            Statuses.TryDequeue (out AnsiEscapeSequenceRequestStatus? request);

            if (request != seqReqStatus)
            {
                throw new InvalidOperationException ("Both EscSeqReqStatus objects aren't equals.");
            }
        }
    }

    /// <summary>Gets the <see cref="AnsiEscapeSequenceRequestStatus"/> list.</summary>
    public ConcurrentQueue<AnsiEscapeSequenceRequestStatus> Statuses { get; } = new ();
}
