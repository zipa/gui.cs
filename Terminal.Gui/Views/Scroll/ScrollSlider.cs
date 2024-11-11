#nullable enable

using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     The ScrollSlider can be dragged with the mouse, constrained by the size of the Viewport of it's superview. The ScrollSlider can be
///     oriented either vertically or horizontally.
/// </summary>
/// <remarks>
///     <para>
///         If <see cref="View.Text"/> is set, it will be displayed centered within the slider. Set
///         <see cref="ShowPercent"/> to automatically have the Text
///         be show what percent the slider is to the Superview's Viewport size.
///     </para>
///     <para>
///        Used to represent the proportion of the visible content to the Viewport in a <see cref="Scroll"/>.
///     </para>
/// </remarks>
public class ScrollSlider : View, IOrientation, IDesignable
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public ScrollSlider ()
    {
        Id = "scrollSlider";
        WantMousePositionReports = true;

        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        OnOrientationChanged (Orientation);

        HighlightStyle = HighlightStyle.Hover;

        // Default size is 1
        Size = 1;

        FrameChanged += OnFrameChanged;
    }

    #region IOrientation members
    private readonly OrientationHelper _orientationHelper;

    /// <inheritdoc/>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;

        // Reset Position to 0 when changing orientation
        X = 0;
        Y = 0;
        //Position = 0;

        // Reset Size to 1 when changing orientation
        if (Orientation == Orientation.Vertical)
        {
            Width = Dim.Fill ();
            Height = 1;
        }
        else
        {
            Width = 1;
            Height = Dim.Fill ();
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        FillRect (Viewport, Glyphs.ContinuousMeterSegment);

        return true;
    }

    private bool _showPercent;

    /// <summary>
    ///     Gets or sets whether the ScrollSlider will set <see cref="View.Text"/> to show the percentage the slider
    ///     takes up within the <see cref="View.SuperView"/>'s Viewport.
    /// </summary>
    public bool ShowPercent
    {
        get => _showPercent;
        set
        {
            _showPercent = value;
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets the size of the ScrollSlider. This is a helper that simply gets or sets the Width or Height depending on the
    ///     <see cref="Orientation"/>. The size will be constrained such that the ScrollSlider will not go outside the Viewport of
    ///     the <see cref="View.SuperView"/>. The size will never be less than 1.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The dimension of the ScrollSlider that is perpendicular to the <see cref="Orientation"/> will be set to <see cref="Dim.Fill()"/>
    ///     </para>
    /// </remarks>
    public int Size
    {
        get
        {
            if (Orientation == Orientation.Vertical)
            {
                return Viewport.Height;
            }
            else
            {
                return Viewport.Width;
            }
        }
        set
        {
            if (Orientation == Orientation.Vertical)
            {
                Width = Dim.Fill ();
                int viewport = Math.Max (1, SuperView?.Viewport.Height ?? 1);
                Height = Math.Clamp (value, 1, viewport);
            }
            else
            {
                int viewport = Math.Max (1, SuperView?.Viewport.Width ?? 1);
                Width = Math.Clamp (value, 1, viewport);
                Height = Dim.Fill ();
            }
        }
    }

    /// <summary>
    ///     Gets the size of the viewport into the content being scrolled, bounded by <see cref="Size"/>.
    /// </summary>
    /// <remarks>
    ///     This is the SuperView's Viewport demension.
    /// </remarks>
    public int ViewportDimension => Orientation == Orientation.Vertical ? SuperView?.Viewport.Height ?? 0 : SuperView?.Viewport.Width ?? 0;

    private void OnFrameChanged (object? sender, EventArgs<Rectangle> e)
    {
        Position = Orientation == Orientation.Vertical ? e.CurrentValue.Y : e.CurrentValue.X;
    }

    private int _position;

    /// <summary>
    ///     Gets or sets the position of the ScrollSlider relative to the size of the ScrollSlider's Frame.
    ///     The position will be constrained such that the ScrollSlider will not go outside the Viewport of
    ///     the <see cref="View.SuperView"/>.
    /// </summary>
    public int Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            RaisePositionChangeEvents (ClampPosition (value));

            SetNeedsLayout ();
        }
    }

    public void SetPosition (int position)
    {
        _position = ClampPosition (position);

        if (Orientation == Orientation.Vertical)
        {
            Y = _position;
        }
        else
        {
            X = _position;
        }
    }

    private int ClampPosition (int newPosittion)
    {
        if (SuperView is null || !IsInitialized)
        {
            return 1;
        }

        if (Orientation == Orientation.Vertical)
        {
            return Math.Clamp (newPosittion, 0, Math.Max (0, ViewportDimension - Viewport.Height));
        }
        else
        {
            return Math.Clamp (newPosittion, 0, Math.Max (0, ViewportDimension - Viewport.Width));
        }
    }

    private void RaisePositionChangeEvents (int newPosition)
    {
        if (OnPositionChanging (_position, newPosition))
        {
            return;
        }

        CancelEventArgs<int> args = new (ref _position, ref newPosition);
        PositionChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        int scrollAmount = newPosition - _position;
        _position = newPosition;

        OnPositionChanged (_position);
        PositionChanged?.Invoke (this, new (in _position));

        OnScroll (scrollAmount);
        Scroll?.Invoke (this, new (in scrollAmount));

        RaiseSelecting (new CommandContext (Command.Select, null, null, scrollAmount));
    }

    /// <summary>
    ///     Called when <see cref="Position"/> is changing. Return true to cancel the change.
    /// </summary>
    protected virtual bool OnPositionChanging (int currentPos, int newPos) { return false; }

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

    /// <summary>Called when <see cref="Position"/> has changed.</summary>
    protected virtual void OnPositionChanged (int position) { }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;


    /// <summary>Called when <see cref="Position"/> has changed. Indicates how much to scroll.</summary>
    protected virtual void OnScroll (int scrollAmount) { }

    /// <summary>Raised when the <see cref="Position"/> has changed. Indicates how much to scroll.</summary>
    public event EventHandler<EventArgs<int>>? Scroll;

    /// <inheritdoc/>
    protected override bool OnDrawingText ()
    {
        if (!ShowPercent)
        {
            Text = string.Empty;

            return false;
        }

        if (SuperView is null)
        {
            return false;
        }

        if (Orientation == Orientation.Vertical)
        {
            Text = $"{(int)Math.Round ((double)Viewport.Height / SuperView!.GetContentSize ().Height * 100)}%";
        }
        else
        {
            Text = $"{(int)Math.Round ((double)Viewport.Width / SuperView!.GetContentSize ().Width * 100)}%";
        }

        return false;
    }

    /// <inheritdoc/>
    public override Attribute GetNormalColor () { return base.GetHotNormalColor (); }

    ///// <inheritdoc/>
    private int _lastLocation = -1;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (SuperView is null)
        {
            return false;
        }

        int location = Orientation == Orientation.Vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int offset = _lastLocation > -1 ? location - _lastLocation : 0;
        int superViewDimension = Orientation == Orientation.Vertical ? SuperView!.Viewport.Height : SuperView!.Viewport.Width;

        if (mouseEvent.IsPressed || mouseEvent.IsReleased)
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed) && _lastLocation == -1)
            {
                if (Application.MouseGrabView != this)
                {
                    Application.GrabMouse (this);
                    _lastLocation = location;
                }
            }
            else if (mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
            {
                if (Orientation == Orientation.Vertical)
                {
                    Y = Frame.Y + offset < 0
                                  ? 0
                            : Frame.Y + offset + Frame.Height > superViewDimension
                                ? Math.Max (superViewDimension - Frame.Height, 0)
                                : Frame.Y + offset;
                }
                else
                {
                    X = Frame.X + offset < 0
                                  ? 0
                            : Frame.X + offset + Frame.Width > superViewDimension
                                ? Math.Max (superViewDimension - Frame.Width, 0)
                                : Frame.X + offset;
                }
            }
            else if (mouseEvent.Flags == MouseFlags.Button1Released)
            {
                _lastLocation = -1;

                if (Application.MouseGrabView == this)
                {
                    Application.UngrabMouse ();
                }
            }
            return true;
        }
        return false;

    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        OrientationChanged += (sender, args) =>
                              {
                                  if (args.CurrentValue == Orientation.Vertical)
                                  {
                                      Width = Dim.Fill ();
                                      Height = 5;
                                  }
                                  else
                                  {
                                      Width = 5;
                                      Height = Dim.Fill();
                                  }
                              };

        Orientation = Orientation.Horizontal;
        ShowPercent = true;

        return true;
    }
}
