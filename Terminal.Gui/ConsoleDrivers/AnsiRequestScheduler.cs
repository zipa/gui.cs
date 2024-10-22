#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui;

public class AnsiRequestScheduler(IAnsiResponseParser parser)
{
    private readonly List<AnsiEscapeSequenceRequest> _requests = new  ();

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
    private ConcurrentDictionary<string, DateTime> _lastSend = new ();

    private TimeSpan _throttle = TimeSpan.FromMilliseconds (100);
    private TimeSpan _runScheduleThrottle = TimeSpan.FromMilliseconds (100);

    /// <summary>
    /// Sends the <paramref name="request"/> immediately or queues it if there is already
    /// an outstanding request for the given <see cref="AnsiEscapeSequenceRequest.Terminator"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <returns><see langword="true"/> if request was sent immediately. <see langword="false"/> if it was queued.</returns>
    public bool SendOrSchedule (AnsiEscapeSequenceRequest request )
    {
        if (CanSend(request))
        {
            Send (request);

            return true;
        }
        else
        {
            _requests.Add (request);
            return false;
        }
    }

    private DateTime _lastRun = DateTime.Now;

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
        if (!force && DateTime.Now - _lastRun < _runScheduleThrottle)
        {
            return false;
        }

        var opportunity = _requests.FirstOrDefault (CanSend);

        if (opportunity != null)
        {
            _requests.Remove (opportunity);
            Send (opportunity);

            return true;
        }

        return false;
    }

    private void Send (AnsiEscapeSequenceRequest r)
    {
        _lastSend.AddOrUpdate (r.Terminator,(s)=>DateTime.Now,(s,v)=>DateTime.Now);
        parser.ExpectResponse (r.Terminator,r.ResponseReceived);
        r.Send ();
    }

    public bool CanSend (AnsiEscapeSequenceRequest r)
    {
        if (ShouldThrottle (r))
        {
            return false;
        }

        return !parser.IsExpecting (r.Terminator);
    }

    private bool ShouldThrottle (AnsiEscapeSequenceRequest r)
    {
        if (_lastSend.TryGetValue (r.Terminator, out DateTime value))
        {
            return DateTime.Now - value < _throttle;
        }

        return false;
    }
}
