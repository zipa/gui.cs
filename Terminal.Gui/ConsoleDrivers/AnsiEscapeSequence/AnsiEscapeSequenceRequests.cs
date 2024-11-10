#nullable enable

using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Manages ANSI Escape Sequence requests and responses. The list of <see cref="AnsiEscapeSequenceRequestStatus"/>
///     contains the
///     status of the request. Each request is identified by the terminator (e.g. ESC[8t ... t is the terminator).
/// </summary>
public static class AnsiEscapeSequenceRequests
{
    /// <summary>
    ///     Adds a new request for the ANSI Escape Sequence defined by <paramref name="ansiRequest"/>. Adds a
    ///     <see cref="AnsiEscapeSequenceRequestStatus"/> instance to <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="ansiRequest">The <see cref="AnsiEscapeSequenceRequest"/> object.</param>
    public static void Add (AnsiEscapeSequenceRequest ansiRequest)
    {
        lock (ansiRequest._responseLock)
        {
            Statuses.Enqueue (new (ansiRequest));
        }

        System.Diagnostics.Debug.Assert (Statuses.Count > 0);
    }

    /// <summary>
    ///     Clear the <see cref="Statuses"/> property.
    /// </summary>
    public static void Clear ()
    {
        lock (Statuses)
        {
            Statuses.Clear ();
        }
    }

    /// <summary>
    ///     Indicates if a <see cref="AnsiEscapeSequenceRequestStatus"/> with the <paramref name="terminator"/> exists in the
    ///     <see cref="Statuses"/> list.
    /// </summary>
    /// <param name="terminator"></param>
    /// <param name="seqReqStatus"></param>
    /// <returns><see langword="true"/> if exist, <see langword="false"/> otherwise.</returns>
    public static bool HasResponse (string terminator, out AnsiEscapeSequenceRequestStatus? seqReqStatus)
    {
        lock (Statuses)
        {
            Statuses.TryPeek (out seqReqStatus);

            return seqReqStatus?.AnsiRequest.Terminator == terminator;
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
    public static void Remove (AnsiEscapeSequenceRequestStatus? seqReqStatus)
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
    public static ConcurrentQueue<AnsiEscapeSequenceRequestStatus> Statuses { get; } = new ();
}
