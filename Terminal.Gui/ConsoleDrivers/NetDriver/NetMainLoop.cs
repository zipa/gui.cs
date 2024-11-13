#nullable enable

using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Mainloop intended to be used with the .NET System.Console API, and can be used on Windows and Unix, it is
///     cross-platform but lacks things like file descriptor monitoring.
/// </summary>
/// <remarks>This implementation is used for NetDriver.</remarks>
internal class NetMainLoop : IMainLoopDriver
{
    internal NetEvents? _netEvents;

    /// <summary>Invoked when a Key is pressed.</summary>
    internal Action<NetEvents.InputResult>? ProcessInput;

    private readonly ManualResetEventSlim _eventReady = new (false);
    private readonly CancellationTokenSource _eventReadyTokenSource = new ();
    private readonly CancellationTokenSource _inputHandlerTokenSource = new ();
    private readonly ConcurrentQueue<NetEvents.InputResult> _resultQueue = new ();
    private MainLoop? _mainLoop;
    bool IMainLoopDriver.ForceRead { get; set; }
    ManualResetEventSlim IMainLoopDriver.WaitForInput { get; set; } = new (false);

    /// <summary>Initializes the class with the console driver.</summary>
    /// <remarks>Passing a consoleDriver is provided to capture windows resizing.</remarks>
    /// <param name="consoleDriver">The console driver used by this Net main loop.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public NetMainLoop (ConsoleDriver consoleDriver)
    {
        ArgumentNullException.ThrowIfNull (consoleDriver);

        if (!ConsoleDriver.RunningUnitTests)
        {
            _netEvents = new (consoleDriver);
        }
    }

    void IMainLoopDriver.Setup (MainLoop mainLoop)
    {
        _mainLoop = mainLoop;

        if (!ConsoleDriver.RunningUnitTests)
        {
            Task.Run (NetInputHandler, _inputHandlerTokenSource.Token);
        }
    }

    void IMainLoopDriver.Wakeup () { _eventReady.Set (); }

    bool IMainLoopDriver.EventsPending ()
    {
        ((IMainLoopDriver)this).WaitForInput.Set ();

        if (_resultQueue.Count > 0 || _mainLoop!.CheckTimersAndIdleHandlers (out int waitTimeout))
        {
            return true;
        }

        try
        {
            if (!_eventReadyTokenSource.IsCancellationRequested)
            {
                // Note: ManualResetEventSlim.Wait will wait indefinitely if the timeout is -1. The timeout is -1 when there
                // are no timers, but there IS an idle handler waiting.
                _eventReady.Wait (waitTimeout, _eventReadyTokenSource.Token);

                _eventReadyTokenSource.Token.ThrowIfCancellationRequested ();

                if (!_eventReadyTokenSource.IsCancellationRequested)
                {
                    return _resultQueue.Count > 0 || _mainLoop.CheckTimersAndIdleHandlers (out _);
                }
            }
        }
        catch (OperationCanceledException)
        {
            return true;
        }
        finally
        {
            _eventReady.Reset ();
        }

        // If cancellation was requested then always return true
        return true;
    }

    void IMainLoopDriver.Iteration ()
    {
        while (_resultQueue.TryDequeue (out NetEvents.InputResult inputRecords))
        {
            ProcessInput?.Invoke (inputRecords);
        }
    }

    void IMainLoopDriver.TearDown ()
    {
        _inputHandlerTokenSource.Cancel ();
        _inputHandlerTokenSource.Dispose ();
        _eventReadyTokenSource.Cancel ();
        _eventReadyTokenSource.Dispose ();

        _eventReady.Dispose ();
        ((IMainLoopDriver)this).WaitForInput.Dispose ();

        _resultQueue.Clear ();
        _netEvents?.Dispose ();
        _netEvents = null;

        _mainLoop = null;
    }

    private void NetInputHandler ()
    {
        while (_mainLoop is { })
        {
            try
            {
                if (!_inputHandlerTokenSource.IsCancellationRequested && !((IMainLoopDriver)this).ForceRead)
                {
                    try
                    {
                        ((IMainLoopDriver)this).WaitForInput.Wait (_inputHandlerTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        if (ex is OperationCanceledException or ObjectDisposedException)
                        {
                            return;
                        }

                        throw;
                    }

                    ((IMainLoopDriver)this).WaitForInput.Reset ();
                }

                ProcessInputQueue ();
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void ProcessInputQueue ()
    {
        if (_resultQueue.Count == 0 || ((IMainLoopDriver)this).ForceRead)
        {
            NetEvents.InputResult? result = _netEvents!.DequeueInput ();

            if (result.HasValue)
            {
                _resultQueue.Enqueue (result.Value);

                _eventReady.Set ();
            }
        }
    }
}
