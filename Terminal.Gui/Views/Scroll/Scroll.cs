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
        base.Add (_slider);
        _slider.Scroll += SliderOnScroll;
        _slider.PositionChanged += SliderOnPositionChanged;

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

    private int? _viewportDimension;

    /// <summary>
    ///     Gets or sets the size of the viewport into the content being scrolled, bounded by <see cref="Size"/>.
    /// </summary>
    /// <remarks>
    ///     If not explicitly set, will be the appropriate dimension of the Scroll's Frame.
    /// </remarks>
    public int ViewportDimension
    {
        get
        {
            if (_viewportDimension.HasValue)
            {
                return _viewportDimension.Value;
            }
            return Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;

        }
        set => _viewportDimension = value;
    }

    private int _size;

    /// <summary>
    ///     Gets or sets the size of the content that can be scrolled.
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

    private void SliderOnPositionChanged (object? sender, EventArgs<int> e)
    {
        if (ViewportDimension == 0)
        {
            return;
        }

        int calculatedSliderPos = CalculateSliderPosition (_contentPosition);

        ContentPosition = (int)Math.Round ((double)e.CurrentValue / (ViewportDimension - _slider.Size) * (Size - ViewportDimension));

        RaiseSliderPositionChangeEvents (calculatedSliderPos, e.CurrentValue);
    }

    private void SliderOnScroll (object? sender, EventArgs<int> e)
    {
        if (ViewportDimension == 0)
        {
            return;
        }
    }

    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, within the Viewport.
    /// </summary>
    public int GetSliderPosition () => CalculateSliderPosition (_contentPosition);

    private void RaiseSliderPositionChangeEvents (int calculatedSliderPosition, int newSliderPosition)
    {
        if (/*newSliderPosition > Size - ViewportDimension ||*/ calculatedSliderPosition == newSliderPosition)
        {
            return;
        }

        // This sets the slider position and clamps the value
        _slider.Position = newSliderPosition;

        OnSliderPositionChanged (newSliderPosition);
        SliderPositionChanged?.Invoke (this, new (in newSliderPosition));
    }

    /// <summary>Called when the slider position has changed.</summary>
    protected virtual void OnSliderPositionChanged (int position) { }

    /// <summary>Raised when the slider position has changed.</summary>
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
    ///     Gets or sets the position of the slider relative to <see cref="Size"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The content position is clamped to 0 and <see cref="Size"/> minus <see cref="ViewportDimension"/>.
    ///     </para>
    ///     <para>
    ///         Setting will result in the <see cref="ContentPositionChanging"/> and <see cref="ContentPositionChanged"/>
    ///         events being raised.
    ///     </para>
    /// </remarks>
    public int ContentPosition
    {
        get => _contentPosition;
        set
        {
            if (value == _contentPosition)
            {
                return;
            }

            // Clamp the value between 0 and Size - ViewportDimension
            int newContentPosition = (int)Math.Clamp (value, 0, Math.Max (0, Size - ViewportDimension));

            RaiseContentPositionChangeEvents (newContentPosition);

            _slider.SetPosition (CalculateSliderPosition (_contentPosition));
        }
    }

    private void RaiseContentPositionChangeEvents (int newContentPosition)
    {

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
        // Check if the mouse click is a single click
        if (!args.IsSingleClicked)
        {
            return false;
        }

        int sliderCenter;
        int distanceFromCenter;

        if (Orientation == Orientation.Vertical)
        {
            sliderCenter = _slider.Frame.Y + _slider.Frame.Height / 2;
            distanceFromCenter = args.Position.Y - sliderCenter;
        }
        else
        {
            sliderCenter = _slider.Frame.X + _slider.Frame.Width / 2;
            distanceFromCenter = args.Position.X - sliderCenter;
        }

        // Ratio of the distance to the viewport dimension
        double ratio = (double)Math.Abs (distanceFromCenter) / ViewportDimension;
        // Jump size based on the ratio and the total content size
        int jump = (int)Math.Ceiling (ratio * Size);

        // Adjust the content position based on the distance
        ContentPosition += distanceFromCenter < 0 ? -jump : jump;

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
        Size = 250;

        return true;
    }
}
