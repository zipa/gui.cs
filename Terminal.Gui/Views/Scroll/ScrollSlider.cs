#nullable enable

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
            SetNeedsDraw();
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
                return Frame.Height;
            }
            else
            {
                return Frame.Width;
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
    ///     Gets or sets the position of the ScrollSlider relative to the size of the ScrollSlider's Frame. This is a helper that simply gets or sets the X or Y depending on the
    ///     <see cref="Orientation"/>. The position will be constrained such that the ScrollSlider will not go outside the Viewport of
    ///     the <see cref="View.SuperView"/>.
    /// </summary>
    public int Position
    {
        get
        {
            if (Orientation == Orientation.Vertical)
            {
                return Frame.Y;
            }
            else
            {
                return Frame.X;
            }
        }
        set
        {
            if (Orientation == Orientation.Vertical)
            {
                int viewport = Math.Max (1, SuperView?.Viewport.Height ?? 1);
                Y = Math.Clamp (value, 0, viewport - Frame.Height);
            }
            else
            {
                int viewport = Math.Max (1, SuperView?.Viewport.Width ?? 1);
                X = Math.Clamp (value, 0, viewport - Frame.Width);
            }
        }
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText ()
    {
        if (!ShowPercent)
        {
            Text = string.Empty;

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
        Orientation = Orientation.Vertical;
        Width = 1;
        Height = 10;
        ShowPercent = true;

        return true;
    }
}
