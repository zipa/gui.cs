#nullable enable

using System.ComponentModel;

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

    /// <inheritdoc/>
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
            SetNeedsLayout ();
        }
    }

    /// <summary>Called when <see cref="Size"/> has changed. </summary>
    protected virtual void OnSizeChanged (int size) { }

    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? SizeChanged;

    #region SliderPosition
    private void OnSliderOnFrameChanged (object? sender, EventArgs<Rectangle> args)
    {
        if (ViewportDimension == 0)
        {
            return;
        }

        int framePos = Orientation == Orientation.Vertical ? args.CurrentValue.Y : args.CurrentValue.X;

        RaiseSliderPositionChangeEvents (CalculateSliderPosition (_contentPosition), framePos);
    }

    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, within the Viewport.
    /// </summary>
    public int SliderPosition
    {
        get => CalculateSliderPosition (_contentPosition);
        set => RaiseSliderPositionChangeEvents (_slider.Position, value);
    }

    private void RaiseSliderPositionChangeEvents (int currentSliderPosition, int newSliderPosition)
    {
        if (/*newSliderPosition > Size - ViewportDimension ||*/ currentSliderPosition == newSliderPosition)
        {
            return;
        }

        if (OnSliderPositionChanging (currentSliderPosition, newSliderPosition))
        {
            return;
        }

        CancelEventArgs<int> args = new (ref currentSliderPosition, ref newSliderPosition);
        SliderPositionChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        // This sets the slider position and clamps the value
        _slider.Position = newSliderPosition;

        ContentPosition = (int)Math.Round ((double)newSliderPosition / (ViewportDimension - _slider.Size) * (Size - ViewportDimension));

        OnSliderPositionChanged (newSliderPosition);
        SliderPositionChanged?.Invoke (this, new (in newSliderPosition));
    }

    /// <summary>
    ///     Called when <see cref="SliderPosition"/> is changing. Return true to cancel the change.
    /// </summary>
    protected virtual bool OnSliderPositionChanging (int currentSliderPosition, int newSliderPosition) { return false; }

    /// <summary>
    ///     Raised when the <see cref="SliderPosition"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? SliderPositionChanging;

    /// <summary>Called when <see cref="SliderPosition"/> has changed.</summary>
    protected virtual void OnSliderPositionChanged (int position) { }

    /// <summary>Raised when the <see cref="SliderPosition"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? SliderPositionChanged;

    private int CalculateSliderPosition (int contentPosition)
    {
        if (Size - ViewportDimension == 0)
        {
            return 0;
        }

        return (int)Math.Round ((double)contentPosition / (Size - ViewportDimension) * (ViewportDimension - _slider.Size));
    }

    #endregion SliderPosition

    #region ContentPosition

    private int _contentPosition;

    /// <summary>
    ///     Gets or sets the position of the ScrollSlider within the range of 0...<see cref="Size"/>.
    /// </summary>
    public int ContentPosition
    {
        get => _contentPosition;
        set
        {
            if (value == _contentPosition)
            {
                return;
            }

            RaiseContentPositionChangeEvents (value);
        }
    }

    private void RaiseContentPositionChangeEvents (int newContentPosition)
    {
        // Clamp the value between 0 and Size - ViewportDimension
        newContentPosition = (int)Math.Clamp (newContentPosition, 0, Math.Max (0, Size - ViewportDimension));

        if (OnContentPositionChanging (_contentPosition, newContentPosition))
        {
            return;
        }

        CancelEventArgs<int> args = new (ref _contentPosition, ref newContentPosition);
        ContentPositionChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        _contentPosition = newContentPosition;

        SliderPosition = CalculateSliderPosition (_contentPosition);

        OnContentPositionChanged (_contentPosition);
        ContentPositionChanged?.Invoke (this, new (in _contentPosition));
    }

    /// <summary>
    ///     Called when <see cref="ContentPosition"/> is changing. Return true to cancel the change.
    /// </summary>
    protected virtual bool OnContentPositionChanging (int currentPos, int newPos) { return false; }

    /// <summary>
    ///     Raised when the <see cref="ContentPosition"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? ContentPositionChanging;

    /// <summary>Called when <see cref="ContentPosition"/> has changed.</summary>
    protected virtual void OnContentPositionChanged (int position) { }

    /// <summary>Raised when the <see cref="ContentPosition"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? ContentPositionChanged;

    #endregion ContentPosition

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        FillRect (Viewport, Glyphs.Stipple);

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnMouseClick (MouseEventArgs args)
    {
        if (!args.IsSingleClicked)
        {
            return false;
        }

        if (Orientation == Orientation.Vertical)
        {
            // If the position is w/in the slider frame ignore
            if (args.Position.Y >= _slider.Frame.Y && args.Position.Y < _slider.Frame.Y + _slider.Frame.Height)
            {
                return false;
            }

            SliderPosition = args.Position.Y;
        }
        else
        {
            // If the position is w/in the slider frame ignore
            if (args.Position.X >= _slider.Frame.X && args.Position.X < _slider.Frame.X + _slider.Frame.Width)
            {
                return false;
            }

            SliderPosition = args.Position.X;
        }

        return true;
    }

    /// <summary>
    ///     Gets or sets the amount each mouse hweel event will incremenet/decrement the <see cref="ContentPosition"/>.
    /// </summary>
    /// <remarks>
    ///     The default is 1.
    /// </remarks>
    public int Increment { get; set; } = 1;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (SuperView is null)
        {
            return false;
        }

        if (!mouseEvent.IsWheel)
        {
            return false;
        }

        if (Orientation == Orientation.Vertical)
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledDown))
            {
                ContentPosition += Increment;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledUp))
            {
                ContentPosition -= Increment;
            }
        }
        else
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledRight))
            {
                ContentPosition += Increment;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledLeft))
            {
                ContentPosition -= Increment;
            }
        }

        return true;
    }

    /// <inheritdoc/>
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
        ContentPosition = 10;

        return true;
    }
}
