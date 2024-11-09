#nullable enable

using System.ComponentModel;
using System.Drawing;

namespace Terminal.Gui;

/// <summary>
///     Indicates the size of scrollable content and provides a visible element, referred to as the "ScrollSlider" that
///     that is sized to
///     show the proportion of the scrollable content to the size of the <see cref="View.Viewport"/>. The ScrollSlider
///     can be dragged with the mouse. A Scroll can be oriented either vertically or horizontally and is used within a
///     <see cref="ScrollBar"/>.
/// </summary>
/// <remarks>
///     <para>
///         By default, this view cannot be focused and does not support keyboard.
///     </para>
/// </remarks>
public class Scroll : View, IOrientation, IDesignable
{
    internal readonly ScrollSlider _slider;

    /// <inheritdoc/>
    public Scroll ()
    {
        _slider = new ();
        Add (_slider);
        _slider.FrameChanged += OnSliderOnFrameChanged;

        CanFocus = false;

        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        // This sets the width/height etc...
        OnOrientationChanged (Orientation);
    }


    /// <inheritdoc />
    protected override void OnSubviewLayout (LayoutEventArgs args)
    {
        if (ViewportDimension < 1)
        {
            _slider.Size = 1;

            return;
        }
        _slider.Size = (int)Math.Clamp (Math.Floor ((double)ViewportDimension * ViewportDimension / (Size)), 1, ViewportDimension);
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

        X = 0;
        Y = 0;

        if (Orientation == Orientation.Vertical)
        {
            Width = 1;
            Height = Dim.Fill ();
        }
        else
        {
            Width = Dim.Fill ();
            Height = 1;
        }

        _slider.Orientation = newOrientation;
    }

    #endregion

    /// <summary>
    ///     Gets or sets whether the Scroll will show the percentage the slider
    ///     takes up within the <see cref="Size"/>.
    /// </summary>
    public bool ShowPercent
    {
        get => _slider.ShowPercent;
        set => _slider.ShowPercent = value;
    }

    private int ViewportDimension => Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

    private int _size;

    /// <summary>
    ///     Gets or sets the total size of the content that can be scrolled.
    /// </summary>
    public int Size
    {
        get => _size;
        set
        {
            if (value == _size || value < 0)
            {
                return;
            }

            _size = value;
            OnSizeChanged (_size);
            SizeChanged?.Invoke (this, new (in _size));
        }
    }

    /// <summary>Called when <see cref="Size"/> has changed. </summary>
    protected virtual void OnSizeChanged (int size) { }

    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? SizeChanged;

    private int _position;

    private void OnSliderOnFrameChanged (object? sender, EventArgs<Rectangle> args)
    {
        if (ViewportDimension == 0)
        {
            return;
        }
        int framePos = Orientation == Orientation.Vertical ? args.CurrentValue.Y : args.CurrentValue.X;
        double pos = ((double)ViewportDimension * ViewportDimension / (Size)) * framePos;
        RaisePositionChangeEvents (_position, (int)pos);
    }

    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, relative to <see cref="Size"/>.
    /// </summary>
    public int Position
    {
        get => _position;
        set => RaisePositionChangeEvents (_position, value);
    }

    private void RaisePositionChangeEvents (int current, int value)
    {
        if (OnPositionChanging (current, value))
        {
            _slider.Position = current;
            return;
        }

        CancelEventArgs<int> args = new (ref current, ref value);
        PositionChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            _slider.Position = current;
            return;
        }

        // This sets the slider position and clamps the value
        _slider.Position = value;

        if (_slider.Position == _position)
        {
            return;
        }

        _position = value;

        OnPositionChanged (_position);
        PositionChanged?.Invoke (this, new (in value));
    }

    /// <summary>
    ///     Called when <see cref="Position"/> is changing. Return true to cancel the change.
    /// </summary>
    protected virtual bool OnPositionChanging (int currentPos, int newPos)
    {
        return false;
    }

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

    /// <summary>Called when <see cref="Position"/> has changed.</summary>
    protected virtual void OnPositionChanged (int position) { }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;


    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        FillRect (Viewport, Glyphs.Stipple);

        return true;
    }


    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        OrientationChanged += (sender, args) =>
                              {
                                  if (args.CurrentValue == Orientation.Vertical)
                                  {
                                      Width = 1;
                                      Height = Dim.Fill ();
                                  }
                                  else
                                  {
                                      Width = Dim.Fill ();
                                      Height = 1;
                                  }
                              };

        Width = 1;
        Height = Dim.Fill ();
        Size = 1000;
        Position = 10;



        return true;
    }
}
