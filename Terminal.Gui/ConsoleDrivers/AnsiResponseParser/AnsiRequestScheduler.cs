#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
/// Manages <see cref="AnsiEscapeSequenceRequest"/> made to an <see cref="IAnsiResponseParser"/>.
/// Ensures there are not 2+ outstanding requests with the same terminator, throttles request sends
/// to prevent console becoming unresponsive and handles evicting ignored requests (no reply from
/// terminal).
/// </summary>
internal class AnsiRequestScheduler
{
    private readonly IAnsiResponseParser _parser;
    private readonly Func<DateTime> _now;

    private readonly List<Tuple<AnsiEscapeSequenceRequest, DateTime>> _requests = new ();

    /// <summary>
    ///<para>
    /// Dictionary where key is ansi request terminator and value is when we last sent a request for
    /// this terminator. Combined with <see cref="_throttle"/> this prevents hammering the console
    /// with too many requests in sequence which can cause console to freeze as there is no space for
    /// regular screen drawing / mouse events etc to come in.
    /// </para>
    /// <para>
    /// When user exceeds the throttle, new requests accumulate in <see cref="_requests"/> (i.e. remain
    /// queued).
    /// </para>
    /// </summary>
    private readonly ConcurrentDictionary<string, DateTime> _lastSend = new ();

    /// <summary>
    /// Number of milliseconds after sending a request that we allow
    /// another request to go out.
    /// </summary>
    private TimeSpan _throttle = TimeSpan.FromMilliseconds (100);
    private TimeSpan _runScheduleThrottle = TimeSpan.FromMilliseconds (100);

    /// <summary>
    /// If console has not responded to a request after this period of time, we assume that it is never going
    /// to respond. Only affects when we try to send a new request with the same terminator - at which point
    /// we tell the parser to stop expecting the old request and start expecting the new request.
    /// </summary>
    private TimeSpan _staleTimeout = TimeSpan.FromSeconds (5);


    private DateTime _lastRun;
    public AnsiRequestScheduler (IAnsiResponseParser parser, Func<DateTime>? now = null)
    {
        _parser = parser;
        _now = now ?? (() => DateTime.Now);
        _lastRun = _now ();
    }
    /// <summary>
    /// Sends the <paramref name="request"/> immediately or queues it if there is already
    /// an outstanding request for the given <see cref="AnsiEscapeSequenceRequest.Terminator"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <returns><see langword="true"/> if request was sent immediately. <see langword="false"/> if it was queued.</returns>
    public bool SendOrSchedule (AnsiEscapeSequenceRequest request)
    {

        if (CanSend (request, out var reason))
        {
            Send (request);
            return true;
        }

        if (reason == ReasonCannotSend.OutstandingRequest)
        {
            EvictStaleRequests (request.Terminator);

            // Try again after evicting
            if (CanSend (request, out _))
            {
                Send (request);
                return true;
            }
        }

        _requests.Add (Tuple.Create (request, _now()));
        return false;
    }

    /// <summary>
    /// Looks to see if the last time we sent <paramref name="withTerminator"/>
    /// is a long time ago. If so we assume that we will never get a response and
    /// can proceed with a new request for this terminator (returning <see langword="true"/>).
    /// </summary>
    /// <param name="withTerminator"></param>
    /// <returns></returns>
    private bool EvictStaleRequests (string withTerminator)
    {
        if (_lastSend.TryGetValue (withTerminator, out var dt))
        {
            if (_now() - dt > _staleTimeout)
            {
                _parser.StopExpecting (withTerminator, false);

                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// Identifies and runs any <see cref="_requests"/> that can be sent based on the
    /// current outstanding requests of the parser.
    /// </summary>
    /// <param name="force">Repeated requests to run the schedule over short period of time will be ignored.
    /// Pass <see langword="true"/> to override this behaviour and force evaluation of outstanding requests.</param>
    /// <returns><see langword="true"/> if a request was found and run. <see langword="false"/>
    /// if no outstanding requests or all have existing outstanding requests underway in parser.</returns>
    public bool RunSchedule (bool force = false)
    {
        if (!force && _now() - _lastRun < _runScheduleThrottle)
        {
            return false;
        }

        var opportunity = _requests.FirstOrDefault (r => CanSend (r.Item1, out _));

        if (opportunity != null)
        {
            _requests.Remove (opportunity);
            Send (opportunity.Item1);

            return true;
        }

        return false;
    }

    private void Send (AnsiEscapeSequenceRequest r)
    {
        _lastSend.AddOrUpdate (r.Terminator, _=>_now(), (_, _) => _now());
        _parser.ExpectResponse (r.Terminator, r.ResponseReceived, false);
        r.Send ();
    }

    private bool CanSend (AnsiEscapeSequenceRequest r, out ReasonCannotSend reason)
    {
        if (ShouldThrottle (r))
        {
            reason = ReasonCannotSend.TooManyRequests;
            return false;
        }

        if (_parser.IsExpecting (r.Terminator))
        {
            reason = ReasonCannotSend.OutstandingRequest;
            return false;
        }

        reason = default;
        return true;
    }

    private bool ShouldThrottle (AnsiEscapeSequenceRequest r)
    {
        if (_lastSend.TryGetValue (r.Terminator, out DateTime value))
        {
            return _now() - value < _throttle;
        }

        return false;
    }
}

internal enum ReasonCannotSend
{
    /// <summary>
    /// No reason given.
    /// </summary>
    None = 0,

    /// <summary>
    /// The parser is already waiting for a request to complete with the given terminator.
    /// </summary>
    OutstandingRequest,

    /// <summary>
    /// There have been too many requests sent recently, new requests will be put into
    /// queue to prevent console becoming unresponsive.
    /// </summary>
    TooManyRequests
}
