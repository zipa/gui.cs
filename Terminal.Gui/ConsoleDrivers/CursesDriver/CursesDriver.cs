//
// Driver.cs: Curses-based Driver
//

using System.Diagnostics;
using System.Runtime.InteropServices;
using Terminal.Gui.ConsoleDrivers;
using Unix.Terminal;

namespace Terminal.Gui;

/// <summary>This is the Curses driver for the gui.cs/Terminal framework.</summary>
internal class CursesDriver : ConsoleDriver
{
    public Curses.Window _window;
    private CursorVisibility? _currentCursorVisibility;
    private CursorVisibility? _initialCursorVisibility;
    private MouseFlags _lastMouseFlags;
    private UnixMainLoop _mainLoopDriver;

    public override int Cols
    {
        get => Curses.Cols;
        internal set
        {
            Curses.Cols = value;
            ClearContents ();
        }
    }

    public override int Rows
    {
        get => Curses.Lines;
        internal set
        {
            Curses.Lines = value;
            ClearContents ();
        }
    }

    public override bool SupportsTrueColor => true;

    /// <inheritdoc/>
    public override bool EnsureCursorVisibility ()
    {
        if (!(Col >= 0 && Row >= 0 && Col < Cols && Row < Rows))
        {
            GetCursorVisibility (out CursorVisibility cursorVisibility);
            _currentCursorVisibility = cursorVisibility;
            SetCursorVisibility (CursorVisibility.Invisible);

            return false;
        }

        SetCursorVisibility (_currentCursorVisibility ?? CursorVisibility.Default);

        return _currentCursorVisibility == CursorVisibility.Default;
    }

    /// <inheritdoc/>
    public override bool GetCursorVisibility (out CursorVisibility visibility)
    {
        visibility = CursorVisibility.Invisible;

        if (!_currentCursorVisibility.HasValue)
        {
            return false;
        }

        visibility = _currentCursorVisibility.Value;

        return true;
    }

    public override string GetVersionInfo () { return $"{Curses.curses_version ()}"; }

    public static bool Is_WSL_Platform ()
    {
        // xclip does not work on WSL, so we need to use the Windows clipboard vis Powershell
        //if (new CursesClipboard ().IsSupported) {
        //	// If xclip is installed on Linux under WSL, this will return true.
        //	return false;
        //}
        (int exitCode, string result) = ClipboardProcessRunner.Bash ("uname -a", waitForOutput: true);

        if (exitCode == 0 && result.Contains ("microsoft") && result.Contains ("WSL"))
        {
            return true;
        }

        return false;
    }

    public override bool IsRuneSupported (Rune rune)
    {
        // See Issue #2615 - CursesDriver is broken with non-BMP characters
        return base.IsRuneSupported (rune) && rune.IsBmp;
    }

    public override void Move (int col, int row)
    {
        base.Move (col, row);

        if (RunningUnitTests)
        {
            return;
        }

        if (IsValidLocation (col, row))
        {
            Curses.move (row, col);
        }
        else
        {
            // Not a valid location (outside screen or clip region)
            // Move within the clip region, then AddRune will actually move to Col, Row
            Curses.move (Clip.Y, Clip.X);
        }
    }

    public override void Refresh ()
    {
        UpdateScreen ();
        UpdateCursor ();
    }

    public override void SendKeys (char keyChar, ConsoleKey consoleKey, bool shift, bool alt, bool control)
    {
        KeyCode key;

        if (consoleKey == ConsoleKey.Packet)
        {
            var mod = new ConsoleModifiers ();

            if (shift)
            {
                mod |= ConsoleModifiers.Shift;
            }

            if (alt)
            {
                mod |= ConsoleModifiers.Alt;
            }

            if (control)
            {
                mod |= ConsoleModifiers.Control;
            }

            var cKeyInfo = new ConsoleKeyInfo (keyChar, consoleKey, shift, alt, control);
            cKeyInfo = ConsoleKeyMapping.DecodeVKPacketToKConsoleKeyInfo (cKeyInfo);
            key = ConsoleKeyMapping.MapConsoleKeyInfoToKeyCode (cKeyInfo);
        }
        else
        {
            key = (KeyCode)keyChar;
        }

        OnKeyDown (new Key (key));
        OnKeyUp (new Key (key));

        //OnKeyPressed (new KeyEventArgsEventArgs (key));
    }

    /// <inheritdoc/>
    public override bool SetCursorVisibility (CursorVisibility visibility)
    {
        if (_initialCursorVisibility.HasValue == false)
        {
            return false;
        }

        if (!RunningUnitTests)
        {
            Curses.curs_set (((int)visibility >> 16) & 0x000000FF);
        }

        if (visibility != CursorVisibility.Invisible)
        {
            Console.Out.Write (
                               EscSeqUtils.CSI_SetCursorStyle (
                                                               (EscSeqUtils.DECSCUSR_Style)(((int)visibility >> 24)
                                                                                            & 0xFF)
                                                              )
                              );
        }

        _currentCursorVisibility = visibility;

        return true;
    }

    public void StartReportingMouseMoves ()
    {
        if (!RunningUnitTests)
        {
            Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
        }
    }

    public void StopReportingMouseMoves ()
    {
        if (!RunningUnitTests)
        {
            Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);
        }
    }

    private readonly ManualResetEventSlim _waitAnsiResponse = new (false);
    private readonly CancellationTokenSource _ansiResponseTokenSource = new ();

    /// <inheritdoc />
    public override string WriteAnsiRequest (AnsiEscapeSequenceRequest ansiRequest)
    {
        if (_mainLoopDriver is null)
        {
            return string.Empty;
        }

        var response = string.Empty;

        try
        {
            lock (ansiRequest._responseLock)
            {
                ansiRequest.ResponseFromInput += (s, e) =>
                                                 {
                                                     Debug.Assert (s == ansiRequest);

                                                     ansiRequest.Response = response = e;

                                                     _waitAnsiResponse.Set ();
                                                 };

                _mainLoopDriver.EscSeqRequests.Add (ansiRequest, this);

                _mainLoopDriver._forceRead = true;
            }

            if (!_ansiResponseTokenSource.IsCancellationRequested)
            {
                _mainLoopDriver._waitForInput.Set ();

                _waitAnsiResponse.Wait (_ansiResponseTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
        finally
        {
            _mainLoopDriver._forceRead = false;

            if (_mainLoopDriver.EscSeqRequests.Statuses.TryPeek (out EscSeqReqStatus request))
            {
                if (_mainLoopDriver.EscSeqRequests.Statuses.Count > 0
                    && string.IsNullOrEmpty (request.AnsiRequest.Response))
                {
                    // Bad request or no response at all
                    _mainLoopDriver.EscSeqRequests.Statuses.TryDequeue (out _);
                }
            }

            _waitAnsiResponse.Reset ();
        }

        return response;
    }

    /// <inheritdoc />
    public override void WriteRaw (string ansi)
    {
        _mainLoopDriver.WriteRaw (ansi);
    }

    public override void Suspend ()
    {
        StopReportingMouseMoves ();

        if (!RunningUnitTests)
        {
            Platform.Suspend ();

            if (Force16Colors)
            {
                Curses.Window.Standard.redrawwin ();
                Curses.refresh ();
            }
        }

        StartReportingMouseMoves ();
    }

    public override void UpdateCursor ()
    {
        EnsureCursorVisibility ();

        if (!RunningUnitTests && Col >= 0 && Col < Cols && Row >= 0 && Row < Rows)
        {
            if (Force16Colors)
            {
                Curses.move (Row, Col);

                Curses.raw ();
                Curses.noecho ();
                Curses.refresh ();
            }
            else
            {
                _mainLoopDriver.WriteRaw (EscSeqUtils.CSI_SetCursorPosition (Row + 1, Col + 1));
            }
        }
    }

    public override void UpdateScreen ()
    {
        if (Force16Colors)
        {
            for (var row = 0; row < Rows; row++)
            {
                if (!_dirtyLines [row])
                {
                    continue;
                }

                _dirtyLines [row] = false;

                for (var col = 0; col < Cols; col++)
                {
                    if (Contents [row, col].IsDirty == false)
                    {
                        continue;
                    }

                    if (RunningUnitTests)
                    {
                        // In unit tests, we don't want to actually write to the screen.
                        continue;
                    }

                    Curses.attrset (Contents [row, col].Attribute.GetValueOrDefault ().PlatformColor);

                    Rune rune = Contents [row, col].Rune;

                    if (rune.IsBmp)
                    {
                        // BUGBUG: CursesDriver doesn't render CharMap correctly for wide chars (and other Unicode) - Curses is doing something funky with glyphs that report GetColums() of 1 yet are rendered wide. E.g. 0x2064 (invisible times) is reported as 1 column but is rendered as 2. WindowsDriver & NetDriver correctly render this as 1 column, overlapping the next cell.
                        if (rune.GetColumns () < 2)
                        {
                            Curses.mvaddch (row, col, rune.Value);
                        }
                        else /*if (col + 1 < Cols)*/
                        {
                            Curses.mvaddwstr (row, col, rune.ToString ());
                        }
                    }
                    else
                    {
                        Curses.mvaddwstr (row, col, rune.ToString ());

                        if (rune.GetColumns () > 1 && col + 1 < Cols)
                        {
                            // TODO: This is a hack to deal with non-BMP and wide characters.
                            //col++;
                            Curses.mvaddch (row, ++col, '*');
                        }
                    }
                }
            }

            if (!RunningUnitTests)
            {
                Curses.move (Row, Col);
                _window.wrefresh ();
            }
        }
        else
        {
            if (RunningUnitTests
                || Console.WindowHeight < 1
                || Contents.Length != Rows * Cols
                || Rows != Console.WindowHeight)
            {
                return;
            }

            var top = 0;
            var left = 0;
            int rows = Rows;
            int cols = Cols;
            var output = new StringBuilder ();
            Attribute? redrawAttr = null;
            int lastCol = -1;

            CursorVisibility? savedVisibility = _currentCursorVisibility;
            SetCursorVisibility (CursorVisibility.Invisible);

            for (int row = top; row < rows; row++)
            {
                if (Console.WindowHeight < 1)
                {
                    return;
                }

                if (!_dirtyLines [row])
                {
                    continue;
                }

                if (!SetCursorPosition (0, row))
                {
                    return;
                }

                _dirtyLines [row] = false;
                output.Clear ();

                for (int col = left; col < cols; col++)
                {
                    lastCol = -1;
                    var outputWidth = 0;

                    for (; col < cols; col++)
                    {
                        if (!Contents [row, col].IsDirty)
                        {
                            if (output.Length > 0)
                            {
                                WriteToConsole (output, ref lastCol, row, ref outputWidth);
                            }
                            else if (lastCol == -1)
                            {
                                lastCol = col;
                            }

                            if (lastCol + 1 < cols)
                            {
                                lastCol++;
                            }

                            continue;
                        }

                        if (lastCol == -1)
                        {
                            lastCol = col;
                        }

                        Attribute attr = Contents [row, col].Attribute.Value;

                        // Performance: Only send the escape sequence if the attribute has changed.
                        if (attr != redrawAttr)
                        {
                            redrawAttr = attr;

                            output.Append (
                                           EscSeqUtils.CSI_SetForegroundColorRGB (
                                                                                  attr.Foreground.R,
                                                                                  attr.Foreground.G,
                                                                                  attr.Foreground.B
                                                                                 )
                                          );

                            output.Append (
                                           EscSeqUtils.CSI_SetBackgroundColorRGB (
                                                                                  attr.Background.R,
                                                                                  attr.Background.G,
                                                                                  attr.Background.B
                                                                                 )
                                          );
                        }

                        outputWidth++;
                        Rune rune = Contents [row, col].Rune;
                        output.Append (rune);

                        if (Contents [row, col].CombiningMarks.Count > 0)
                        {
                            // AtlasEngine does not support NON-NORMALIZED combining marks in a way
                            // compatible with the driver architecture. Any CMs (except in the first col)
                            // are correctly combined with the base char, but are ALSO treated as 1 column
                            // width codepoints E.g. `echo "[e`u{0301}`u{0301}]"` will output `[é  ]`.
                            // 
                            // For now, we just ignore the list of CMs.
                            //foreach (var combMark in Contents [row, col].CombiningMarks) {
                            //	output.Append (combMark);
                            //}
                            // WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        }
                        else if (rune.IsSurrogatePair () && rune.GetColumns () < 2)
                        {
                            WriteToConsole (output, ref lastCol, row, ref outputWidth);
                            SetCursorPosition (col - 1, row);
                        }

                        Contents [row, col].IsDirty = false;
                    }
                }

                if (output.Length > 0)
                {
                    SetCursorPosition (lastCol, row);
                    Console.Write (output);
                }
            }

            // SIXELS
            foreach (var s in Application.Sixel)
            {
                SetCursorPosition (s.ScreenPosition.X, s.ScreenPosition.Y);
                Console.Write(s.SixelData);
            }

            SetCursorPosition (0, 0);

            _currentCursorVisibility = savedVisibility;

            void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
            {
                SetCursorPosition (lastCol, row);
                Console.Write (output);
                output.Clear ();
                lastCol += outputWidth;
                outputWidth = 0;
            }
        }
    }

    private bool SetCursorPosition (int col, int row)
    {
        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        Console.Out.Write (EscSeqUtils.CSI_SetCursorPosition (row + 1, col + 1));

        return true;
    }

    internal override void End ()
    {
        _ansiResponseTokenSource?.Cancel ();
        _ansiResponseTokenSource?.Dispose ();
        _waitAnsiResponse?.Dispose ();

        StopReportingMouseMoves ();
        SetCursorVisibility (CursorVisibility.Default);

        if (RunningUnitTests)
        {
            return;
        }

        // throws away any typeahead that has been typed by
        // the user and has not yet been read by the program.
        Curses.flushinp ();

        Curses.endwin ();
    }

    internal override MainLoop Init ()
    {
        _mainLoopDriver = new UnixMainLoop (this);

        if (!RunningUnitTests)
        {
            _window = Curses.initscr ();
            Curses.set_escdelay (10);

            // Ensures that all procedures are performed at some previous closing.
            Curses.doupdate ();

            // 
            // We are setting Invisible as default, so we could ignore XTerm DECSUSR setting
            //
            switch (Curses.curs_set (0))
            {
                case 0:
                    _currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Invisible;

                    break;

                case 1:
                    _currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Underline;
                    Curses.curs_set (1);

                    break;

                case 2:
                    _currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Box;
                    Curses.curs_set (2);

                    break;

                default:
                    _currentCursorVisibility = _initialCursorVisibility = null;

                    break;
            }

            if (!Curses.HasColors)
            {
                throw new InvalidOperationException ("V2 - This should never happen. File an Issue if it does.");
            }

            Curses.raw ();
            Curses.noecho ();

            Curses.Window.Standard.keypad (true);

            Curses.StartColor ();
            Curses.UseDefaultColors ();

            if (!RunningUnitTests)
            {
                Curses.timeout (0);
            }
        }

        CurrentAttribute = new Attribute (ColorName16.White, ColorName16.Black);

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Clipboard = new FakeDriver.FakeClipboard ();
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
            {
                Clipboard = new MacOSXClipboard ();
            }
            else
            {
                if (Is_WSL_Platform ())
                {
                    Clipboard = new WSLClipboard ();
                }
                else
                {
                    Clipboard = new CursesClipboard ();
                }
            }
        }

        ClearContents ();
        StartReportingMouseMoves ();

        if (!RunningUnitTests)
        {
            Curses.CheckWinChange ();
            ClearContents ();

            if (Force16Colors)
            {
                Curses.refresh ();
            }
        }

        return new MainLoop (_mainLoopDriver);
    }

    internal void ProcessInput (UnixMainLoop.PollData inputEvent)
    {
        switch (inputEvent.EventType)
        {
            case UnixMainLoop.EventType.Key:
                ConsoleKeyInfo consoleKeyInfo = inputEvent.KeyEvent;

                KeyCode map = ConsoleKeyMapping.MapConsoleKeyInfoToKeyCode (consoleKeyInfo);

                if (map == KeyCode.Null)
                {
                    break;
                }

                OnKeyDown (new Key (map));
                OnKeyUp (new Key (map));

                break;
            case UnixMainLoop.EventType.Mouse:
                MouseEventArgs me = new MouseEventArgs { Position = inputEvent.MouseEvent.Position, Flags = inputEvent.MouseEvent.MouseFlags };
                OnMouseEvent (me);

                break;
            case UnixMainLoop.EventType.WindowSize:
                Size size = new (inputEvent.WindowSizeEvent.Size.Width, inputEvent.WindowSizeEvent.Size.Height);
                ProcessWinChange (inputEvent.WindowSizeEvent.Size);

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }

    private void ProcessWinChange (Size size)
    {
        if (!RunningUnitTests && Curses.ChangeWindowSize (size.Height, size.Width))
        {
            ClearContents ();
            OnSizeChanged (new SizeChangedEventArgs (new (Cols, Rows)));
        }
    }

    private static KeyCode MapCursesKey (int cursesKey)
    {
        switch (cursesKey)
        {
            case Curses.KeyF1: return KeyCode.F1;
            case Curses.KeyF2: return KeyCode.F2;
            case Curses.KeyF3: return KeyCode.F3;
            case Curses.KeyF4: return KeyCode.F4;
            case Curses.KeyF5: return KeyCode.F5;
            case Curses.KeyF6: return KeyCode.F6;
            case Curses.KeyF7: return KeyCode.F7;
            case Curses.KeyF8: return KeyCode.F8;
            case Curses.KeyF9: return KeyCode.F9;
            case Curses.KeyF10: return KeyCode.F10;
            case Curses.KeyF11: return KeyCode.F11;
            case Curses.KeyF12: return KeyCode.F12;
            case Curses.KeyUp: return KeyCode.CursorUp;
            case Curses.KeyDown: return KeyCode.CursorDown;
            case Curses.KeyLeft: return KeyCode.CursorLeft;
            case Curses.KeyRight: return KeyCode.CursorRight;
            case Curses.KeyHome: return KeyCode.Home;
            case Curses.KeyEnd: return KeyCode.End;
            case Curses.KeyNPage: return KeyCode.PageDown;
            case Curses.KeyPPage: return KeyCode.PageUp;
            case Curses.KeyDeleteChar: return KeyCode.Delete;
            case Curses.KeyInsertChar: return KeyCode.Insert;
            case Curses.KeyTab: return KeyCode.Tab;
            case Curses.KeyBackTab: return KeyCode.Tab | KeyCode.ShiftMask;
            case Curses.KeyBackspace: return KeyCode.Backspace;
            case Curses.ShiftKeyUp: return KeyCode.CursorUp | KeyCode.ShiftMask;
            case Curses.ShiftKeyDown: return KeyCode.CursorDown | KeyCode.ShiftMask;
            case Curses.ShiftKeyLeft: return KeyCode.CursorLeft | KeyCode.ShiftMask;
            case Curses.ShiftKeyRight: return KeyCode.CursorRight | KeyCode.ShiftMask;
            case Curses.ShiftKeyHome: return KeyCode.Home | KeyCode.ShiftMask;
            case Curses.ShiftKeyEnd: return KeyCode.End | KeyCode.ShiftMask;
            case Curses.ShiftKeyNPage: return KeyCode.PageDown | KeyCode.ShiftMask;
            case Curses.ShiftKeyPPage: return KeyCode.PageUp | KeyCode.ShiftMask;
            case Curses.AltKeyUp: return KeyCode.CursorUp | KeyCode.AltMask;
            case Curses.AltKeyDown: return KeyCode.CursorDown | KeyCode.AltMask;
            case Curses.AltKeyLeft: return KeyCode.CursorLeft | KeyCode.AltMask;
            case Curses.AltKeyRight: return KeyCode.CursorRight | KeyCode.AltMask;
            case Curses.AltKeyHome: return KeyCode.Home | KeyCode.AltMask;
            case Curses.AltKeyEnd: return KeyCode.End | KeyCode.AltMask;
            case Curses.AltKeyNPage: return KeyCode.PageDown | KeyCode.AltMask;
            case Curses.AltKeyPPage: return KeyCode.PageUp | KeyCode.AltMask;
            case Curses.CtrlKeyUp: return KeyCode.CursorUp | KeyCode.CtrlMask;
            case Curses.CtrlKeyDown: return KeyCode.CursorDown | KeyCode.CtrlMask;
            case Curses.CtrlKeyLeft: return KeyCode.CursorLeft | KeyCode.CtrlMask;
            case Curses.CtrlKeyRight: return KeyCode.CursorRight | KeyCode.CtrlMask;
            case Curses.CtrlKeyHome: return KeyCode.Home | KeyCode.CtrlMask;
            case Curses.CtrlKeyEnd: return KeyCode.End | KeyCode.CtrlMask;
            case Curses.CtrlKeyNPage: return KeyCode.PageDown | KeyCode.CtrlMask;
            case Curses.CtrlKeyPPage: return KeyCode.PageUp | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyUp: return KeyCode.CursorUp | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyDown: return KeyCode.CursorDown | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyLeft: return KeyCode.CursorLeft | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyRight: return KeyCode.CursorRight | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyHome: return KeyCode.Home | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyEnd: return KeyCode.End | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyNPage: return KeyCode.PageDown | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftCtrlKeyPPage: return KeyCode.PageUp | KeyCode.ShiftMask | KeyCode.CtrlMask;
            case Curses.ShiftAltKeyUp: return KeyCode.CursorUp | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyDown: return KeyCode.CursorDown | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyLeft: return KeyCode.CursorLeft | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyRight: return KeyCode.CursorRight | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyNPage: return KeyCode.PageDown | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyPPage: return KeyCode.PageUp | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyHome: return KeyCode.Home | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.ShiftAltKeyEnd: return KeyCode.End | KeyCode.ShiftMask | KeyCode.AltMask;
            case Curses.AltCtrlKeyNPage: return KeyCode.PageDown | KeyCode.AltMask | KeyCode.CtrlMask;
            case Curses.AltCtrlKeyPPage: return KeyCode.PageUp | KeyCode.AltMask | KeyCode.CtrlMask;
            case Curses.AltCtrlKeyHome: return KeyCode.Home | KeyCode.AltMask | KeyCode.CtrlMask;
            case Curses.AltCtrlKeyEnd: return KeyCode.End | KeyCode.AltMask | KeyCode.CtrlMask;
            default: return KeyCode.Null;
        }
    }

    #region Color Handling

    /// <summary>Creates an Attribute from the provided curses-based foreground and background color numbers</summary>
    /// <param name="foreground">Contains the curses color number for the foreground (color, plus any attributes)</param>
    /// <param name="background">Contains the curses color number for the background (color, plus any attributes)</param>
    /// <returns></returns>
    private static Attribute MakeColor (short foreground, short background)
    {
        //var v = (short)((ushort)foreground | (background << 4));
        var v = (short)(((ushort)(foreground & 0xffff) << 16) | (background & 0xffff));

        // TODO: for TrueColor - Use InitExtendedPair
        Curses.InitColorPair (v, foreground, background);

        return new Attribute (
                              Curses.ColorPair (v),
                              CursesColorNumberToColorName16 (foreground),
                              CursesColorNumberToColorName16 (background)
                             );
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     In the CursesDriver, colors are encoded as an int. The foreground color is stored in the most significant 4
    ///     bits, and the background color is stored in the least significant 4 bits. The Terminal.GUi Color values are
    ///     converted to curses color encoding before being encoded.
    /// </remarks>
    public override Attribute MakeColor (in Color foreground, in Color background)
    {
        if (!RunningUnitTests && Force16Colors)
        {
            return MakeColor (
                              ColorNameToCursesColorNumber (foreground.GetClosestNamedColor16 ()),
                              ColorNameToCursesColorNumber (background.GetClosestNamedColor16 ())
                             );
        }

        return new Attribute (
                              0,
                              foreground,
                              background
                             );
    }

    private static short ColorNameToCursesColorNumber (ColorName16 color)
    {
        switch (color)
        {
            case ColorName16.Black:
                return Curses.COLOR_BLACK;
            case ColorName16.Blue:
                return Curses.COLOR_BLUE;
            case ColorName16.Green:
                return Curses.COLOR_GREEN;
            case ColorName16.Cyan:
                return Curses.COLOR_CYAN;
            case ColorName16.Red:
                return Curses.COLOR_RED;
            case ColorName16.Magenta:
                return Curses.COLOR_MAGENTA;
            case ColorName16.Yellow:
                return Curses.COLOR_YELLOW;
            case ColorName16.Gray:
                return Curses.COLOR_WHITE;
            case ColorName16.DarkGray:
                return Curses.COLOR_GRAY;
            case ColorName16.BrightBlue:
                return Curses.COLOR_BLUE | Curses.COLOR_GRAY;
            case ColorName16.BrightGreen:
                return Curses.COLOR_GREEN | Curses.COLOR_GRAY;
            case ColorName16.BrightCyan:
                return Curses.COLOR_CYAN | Curses.COLOR_GRAY;
            case ColorName16.BrightRed:
                return Curses.COLOR_RED | Curses.COLOR_GRAY;
            case ColorName16.BrightMagenta:
                return Curses.COLOR_MAGENTA | Curses.COLOR_GRAY;
            case ColorName16.BrightYellow:
                return Curses.COLOR_YELLOW | Curses.COLOR_GRAY;
            case ColorName16.White:
                return Curses.COLOR_WHITE | Curses.COLOR_GRAY;
        }

        throw new ArgumentException ("Invalid color code");
    }

    private static ColorName16 CursesColorNumberToColorName16 (short color)
    {
        switch (color)
        {
            case Curses.COLOR_BLACK:
                return ColorName16.Black;
            case Curses.COLOR_BLUE:
                return ColorName16.Blue;
            case Curses.COLOR_GREEN:
                return ColorName16.Green;
            case Curses.COLOR_CYAN:
                return ColorName16.Cyan;
            case Curses.COLOR_RED:
                return ColorName16.Red;
            case Curses.COLOR_MAGENTA:
                return ColorName16.Magenta;
            case Curses.COLOR_YELLOW:
                return ColorName16.Yellow;
            case Curses.COLOR_WHITE:
                return ColorName16.Gray;
            case Curses.COLOR_GRAY:
                return ColorName16.DarkGray;
            case Curses.COLOR_BLUE | Curses.COLOR_GRAY:
                return ColorName16.BrightBlue;
            case Curses.COLOR_GREEN | Curses.COLOR_GRAY:
                return ColorName16.BrightGreen;
            case Curses.COLOR_CYAN | Curses.COLOR_GRAY:
                return ColorName16.BrightCyan;
            case Curses.COLOR_RED | Curses.COLOR_GRAY:
                return ColorName16.BrightRed;
            case Curses.COLOR_MAGENTA | Curses.COLOR_GRAY:
                return ColorName16.BrightMagenta;
            case Curses.COLOR_YELLOW | Curses.COLOR_GRAY:
                return ColorName16.BrightYellow;
            case Curses.COLOR_WHITE | Curses.COLOR_GRAY:
                return ColorName16.White;
        }

        throw new ArgumentException ("Invalid curses color code");
    }

    #endregion
}

internal static class Platform
{
    private static int _suspendSignal;

    /// <summary>Suspends the process by sending SIGTSTP to itself</summary>
    /// <returns>True if the suspension was successful.</returns>
    public static bool Suspend ()
    {
        int signal = GetSuspendSignal ();

        if (signal == -1)
        {
            return false;
        }

        killpg (0, signal);

        return true;
    }

    private static int GetSuspendSignal ()
    {
        if (_suspendSignal != 0)
        {
            return _suspendSignal;
        }

        nint buf = Marshal.AllocHGlobal (8192);

        if (uname (buf) != 0)
        {
            Marshal.FreeHGlobal (buf);
            _suspendSignal = -1;

            return _suspendSignal;
        }

        try
        {
            switch (Marshal.PtrToStringAnsi (buf))
            {
                case "Darwin":
                case "DragonFly":
                case "FreeBSD":
                case "NetBSD":
                case "OpenBSD":
                    _suspendSignal = 18;

                    break;
                case "Linux":
                    // TODO: should fetch the machine name and
                    // if it is MIPS return 24
                    _suspendSignal = 20;

                    break;
                case "Solaris":
                    _suspendSignal = 24;

                    break;
                default:
                    _suspendSignal = -1;

                    break;
            }

            return _suspendSignal;
        }
        finally
        {
            Marshal.FreeHGlobal (buf);
        }
    }

    [DllImport ("libc")]
    private static extern int killpg (int pgrp, int pid);

    [DllImport ("libc")]
    private static extern int uname (nint buf);
}
