#nullable enable
//
// mainloop.cs: Linux/Curses MainLoop implementation.
//

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Terminal.Gui;

/// <summary>Unix main loop, suitable for using on Posix systems</summary>
/// <remarks>
///     In addition to the general functions of the MainLoop, the Unix version can watch file descriptors using the
///     AddWatch methods.
/// </remarks>
internal class UnixMainLoop (ConsoleDriver consoleDriver) : IMainLoopDriver
{
    /// <summary>Condition on which to wake up from file descriptor activity.  These match the Linux/BSD poll definitions.</summary>
    [Flags]
    public enum Condition : short
    {
        /// <summary>There is data to read</summary>
        PollIn = 1,

        /// <summary>Writing to the specified descriptor will not block</summary>
        PollOut = 4,

        /// <summary>There is urgent data to read</summary>
        PollPri = 2,

        /// <summary>Error condition on output</summary>
        PollErr = 8,

        /// <summary>Hang-up on output</summary>
        PollHup = 16,

        /// <summary>File descriptor is not open.</summary>
        PollNval = 32
    }

    private readonly CursesDriver _cursesDriver = (CursesDriver)consoleDriver ?? throw new ArgumentNullException (nameof (consoleDriver));
    private MainLoop? _mainLoop;
    private Pollfd []? _pollMap;
    private readonly ConcurrentQueue<PollData> _pollDataQueue = new ();
    private readonly ManualResetEventSlim _eventReady = new (false);
    ManualResetEventSlim IMainLoopDriver.WaitForInput { get; set; } = new (false);
    private readonly ManualResetEventSlim _windowSizeChange = new (false);
    private readonly CancellationTokenSource _eventReadyTokenSource = new ();
    private readonly CancellationTokenSource _inputHandlerTokenSource = new ();

    void IMainLoopDriver.Wakeup () { _eventReady.Set (); }

    void IMainLoopDriver.Setup (MainLoop mainLoop)
    {
        _mainLoop = mainLoop;

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

        try
        {
            // Setup poll for stdin (fd 0)
            _pollMap = new Pollfd [1];
            _pollMap [0].fd = 0;         // stdin (file descriptor 0)
            _pollMap [0].events = (short)Condition.PollIn; // Monitor input for reading
        }
        catch (DllNotFoundException e)
        {
            throw new NotSupportedException ("libc not found", e);
        }

        AnsiEscapeSequenceRequestUtils.ContinuousButtonPressed += EscSeqUtils_ContinuousButtonPressed;

        Task.Run (CursesInputHandler, _inputHandlerTokenSource.Token);
        Task.Run (WindowSizeHandler, _inputHandlerTokenSource.Token);
    }

    private static readonly int TIOCGWINSZ = GetTIOCGWINSZValue ();

    private const string PlaceholderLibrary = "compiled-binaries/libGetTIOCGWINSZ"; // Placeholder, won't directly load

    [DllImport (PlaceholderLibrary, EntryPoint = "get_tiocgwinsz_value")]
    private static extern int GetTIOCGWINSZValueInternal ();

    public static int GetTIOCGWINSZValue ()
    {
        // Determine the correct library path based on the OS
        string libraryPath = Path.Combine (
                                           AppContext.BaseDirectory,
                                           "compiled-binaries",
                                           RuntimeInformation.IsOSPlatform (OSPlatform.OSX) ? "libGetTIOCGWINSZ.dylib" : "libGetTIOCGWINSZ.so");

        // Load the native library manually
        nint handle = NativeLibrary.Load (libraryPath);

        // Ensure the handle is valid
        if (handle == nint.Zero)
        {
            throw new DllNotFoundException ($"Unable to load library: {libraryPath}");
        }

        return GetTIOCGWINSZValueInternal ();
    }

    private void EscSeqUtils_ContinuousButtonPressed (object? sender, MouseEventArgs e)
    {
        _pollDataQueue.Enqueue (EnqueueMouseEvent (e.Flags, e.Position));
    }

    private void WindowSizeHandler ()
    {
        var ws = new Winsize ();
        ioctl (0, TIOCGWINSZ, ref ws);

        // Store initial window size
        int rows = ws.ws_row;
        int cols = ws.ws_col;

        while (_inputHandlerTokenSource is { IsCancellationRequested: false })
        {
            try
            {
                _windowSizeChange.Wait (_inputHandlerTokenSource.Token);
                _windowSizeChange.Reset ();

                while (!_inputHandlerTokenSource.IsCancellationRequested)
                {
                    // Wait for a while then check if screen has changed sizes
                    Task.Delay (500, _inputHandlerTokenSource.Token).Wait (_inputHandlerTokenSource.Token);

                    ioctl (0, TIOCGWINSZ, ref ws);

                    if (rows != ws.ws_row || cols != ws.ws_col)
                    {
                        rows = ws.ws_row;
                        cols = ws.ws_col;

                        _pollDataQueue.Enqueue (EnqueueWindowSizeEvent (rows, cols));

                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _eventReady.Set ();
        }
    }

    bool IMainLoopDriver.ForceRead { get; set; }
    private int _retries;

    private void CursesInputHandler ()
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
        if (_pollDataQueue.Count == 0 || ((IMainLoopDriver)this).ForceRead)
        {
            while (!_inputHandlerTokenSource.IsCancellationRequested)
            {
                try
                {
                    Task.Delay (100, _inputHandlerTokenSource.Token).Wait (_inputHandlerTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                int n = poll (_pollMap!, (uint)_pollMap!.Length, 0);

                if (n > 0)
                {
                    // Check if stdin has data
                    if ((_pollMap [0].revents & (int)Condition.PollIn) != 0)
                    {
                        // Allocate memory for the buffer
                        var buf = new byte [2048];
                        nint bufPtr = Marshal.AllocHGlobal (buf.Length);

                        try
                        {
                            // Read from the stdin
                            int bytesRead = read (_pollMap [0].fd, bufPtr, buf.Length);

                            if (bytesRead > 0)
                            {
                                // Copy the data from unmanaged memory to a byte array
                                var buffer = new byte [bytesRead];
                                Marshal.Copy (bufPtr, buffer, 0, bytesRead);

                                // Convert the byte array to a string (assuming UTF-8 encoding)
                                string data = Encoding.UTF8.GetString (buffer);

                                if (AnsiEscapeSequenceRequestUtils.IncompleteCkInfos is { })
                                {
                                    data = data.Insert (0, AnsiEscapeSequenceRequestUtils.ToString (AnsiEscapeSequenceRequestUtils.IncompleteCkInfos));
                                    AnsiEscapeSequenceRequestUtils.IncompleteCkInfos = null;
                                }

                                // Enqueue the data
                                ProcessEnqueuePollData (data);
                            }
                        }
                        finally
                        {
                            // Free the allocated memory
                            Marshal.FreeHGlobal (bufPtr);
                        }
                    }

                    if (_retries > 0)
                    {
                        _retries = 0;
                    }

                    break;
                }

                if (AnsiEscapeSequenceRequestUtils.IncompleteCkInfos is null && AnsiEscapeSequenceRequests.Statuses.Count > 0)
                {
                    if (_retries > 1)
                    {
                        if (AnsiEscapeSequenceRequests.Statuses.TryPeek (out AnsiEscapeSequenceRequestStatus? seqReqStatus))
                        {
                            lock (seqReqStatus.AnsiRequest._responseLock)
                            {
                                AnsiEscapeSequenceRequests.Statuses.TryDequeue (out _);

                                seqReqStatus.AnsiRequest.RaiseResponseFromInput (null);
                            }
                        }

                        _retries = 0;
                    }
                    else
                    {
                        _retries++;
                    }
                }
                else
                {
                    _retries = 0;
                }
            }
        }

        if (_pollDataQueue.Count > 0)
        {
            _eventReady.Set ();
        }
    }

    private void ProcessEnqueuePollData (string pollData)
    {
        foreach (string split in AnsiEscapeSequenceRequestUtils.SplitEscapeRawString (pollData))
        {
            EnqueuePollData (split);
        }
    }

    private void EnqueuePollData (string pollDataPart)
    {
        ConsoleKeyInfo [] cki = AnsiEscapeSequenceRequestUtils.ToConsoleKeyInfoArray (pollDataPart);

        ConsoleKey key = 0;
        ConsoleModifiers mod = 0;
        ConsoleKeyInfo newConsoleKeyInfo = default;

        AnsiEscapeSequenceRequestUtils.DecodeEscSeq (
                                                     ref newConsoleKeyInfo,
                                                     ref key,
                                                     cki,
                                                     ref mod,
                                                     out string c1Control,
                                                     out string code,
                                                     out string [] values,
                                                     out string terminating,
                                                     out bool isMouse,
                                                     out List<MouseFlags> mouseFlags,
                                                     out Point pos,
                                                     out AnsiEscapeSequenceRequestStatus? seqReqStatus,
                                                     AnsiEscapeSequenceRequestUtils.ProcessMouseEvent
                                                    );

        if (isMouse)
        {
            foreach (MouseFlags mf in mouseFlags)
            {
                _pollDataQueue.Enqueue (EnqueueMouseEvent (mf, pos));
            }

            return;
        }

        if (newConsoleKeyInfo != default)
        {
            _pollDataQueue.Enqueue (EnqueueKeyboardEvent (newConsoleKeyInfo));
        }
    }

    private PollData EnqueueMouseEvent (MouseFlags mouseFlags, Point pos)
    {
        var mouseEvent = new MouseEvent { Position = pos, MouseFlags = mouseFlags };

        return new () { EventType = EventType.Mouse, MouseEvent = mouseEvent };
    }

    private PollData EnqueueKeyboardEvent (ConsoleKeyInfo keyInfo)
    {
        return new () { EventType = EventType.Key, KeyEvent = keyInfo };
    }

    private PollData EnqueueWindowSizeEvent (int rows, int cols)
    {
        return new () { EventType = EventType.WindowSize, WindowSizeEvent = new () { Size = new (cols, rows) } };
    }

    bool IMainLoopDriver.EventsPending ()
    {
        ((IMainLoopDriver)this).WaitForInput.Set ();
        _windowSizeChange.Set ();

        if (_mainLoop!.CheckTimersAndIdleHandlers (out int waitTimeout))
        {
            return true;
        }

        try
        {
            if (!_eventReadyTokenSource.IsCancellationRequested)
            {
                _eventReady.Wait (waitTimeout, _eventReadyTokenSource.Token);
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

        if (!_eventReadyTokenSource.IsCancellationRequested)
        {
            return _pollDataQueue.Count > 0 || _mainLoop.CheckTimersAndIdleHandlers (out _);
        }

        return true;
    }

    void IMainLoopDriver.Iteration ()
    {
        // Dequeue and process the data
        while (_pollDataQueue.TryDequeue (out PollData inputRecords))
        {
            _cursesDriver.ProcessInput (inputRecords);
        }
    }

    void IMainLoopDriver.TearDown ()
    {
        AnsiEscapeSequenceRequestUtils.ContinuousButtonPressed -= EscSeqUtils_ContinuousButtonPressed;

        _inputHandlerTokenSource.Cancel ();
        _inputHandlerTokenSource.Dispose ();
        ((IMainLoopDriver)this).WaitForInput?.Dispose ();

        _windowSizeChange.Dispose();

        _pollDataQueue.Clear ();

        _eventReadyTokenSource.Cancel ();
        _eventReadyTokenSource.Dispose ();
        _eventReady.Dispose ();

        _mainLoop = null;
    }

    internal void WriteRaw (string ansiRequest)
    {
        // Write to stdout (fd 1)
        write (STDOUT_FILENO, ansiRequest, ansiRequest.Length);
    }

    [DllImport ("libc")]
    private static extern int poll ([In] [Out] Pollfd [] ufds, uint nfds, int timeout);

    [DllImport ("libc")]
    private static extern int read (int fd, nint buf, nint n);

    // File descriptor for stdout
    private const int STDOUT_FILENO = 1;

    [DllImport ("libc")]
    private static extern int write (int fd, string buf, int n);

    [DllImport ("libc", SetLastError = true)]
    private static extern int ioctl (int fd, int request, ref Winsize ws);

    [StructLayout (LayoutKind.Sequential)]
    private struct Pollfd
    {
        public int fd;
        public short events;
        public readonly short revents;
    }

    /// <summary>
    ///     Window or terminal size structure. This information is stored by the kernel in order to provide a consistent
    ///     interface, but is not used by the kernel.
    /// </summary>
    [StructLayout (LayoutKind.Sequential)]
    public struct Winsize
    {
        public ushort ws_row;    // Number of rows
        public ushort ws_col;    // Number of columns
        public ushort ws_xpixel; // Width in pixels (unused)
        public ushort ws_ypixel; // Height in pixels (unused)
    }

    #region Events

    public enum EventType
    {
        Key = 1,
        Mouse = 2,
        WindowSize = 3
    }

    public struct MouseEvent
    {
        public Point Position;
        public MouseFlags MouseFlags;
    }

    public struct WindowSizeEvent
    {
        public Size Size;
    }

    public struct PollData
    {
        public EventType EventType;
        public ConsoleKeyInfo KeyEvent;
        public MouseEvent MouseEvent;
        public WindowSizeEvent WindowSizeEvent;

        public readonly override string ToString ()
        {
            return (EventType switch
                    {
                        EventType.Key => ToString (KeyEvent),
                        EventType.Mouse => MouseEvent.ToString (),
                        EventType.WindowSize => WindowSizeEvent.ToString (),
                        _ => "Unknown event type: " + EventType
                    })!;
        }

        /// <summary>Prints a ConsoleKeyInfoEx structure</summary>
        /// <param name="cki"></param>
        /// <returns></returns>
        public readonly string ToString (ConsoleKeyInfo cki)
        {
            var ke = new Key ((KeyCode)cki.KeyChar);
            var sb = new StringBuilder ();
            sb.Append ($"Key: {(KeyCode)cki.Key} ({cki.Key})");
            sb.Append ((cki.Modifiers & ConsoleModifiers.Shift) != 0 ? " | Shift" : string.Empty);
            sb.Append ((cki.Modifiers & ConsoleModifiers.Control) != 0 ? " | Control" : string.Empty);
            sb.Append ((cki.Modifiers & ConsoleModifiers.Alt) != 0 ? " | Alt" : string.Empty);
            sb.Append ($", KeyChar: {ke.AsRune.MakePrintable ()} ({(uint)cki.KeyChar}) ");
            string s = sb.ToString ().TrimEnd (',').TrimEnd (' ');

            return $"[ConsoleKeyInfo({s})]";
        }
    }

    #endregion
}
