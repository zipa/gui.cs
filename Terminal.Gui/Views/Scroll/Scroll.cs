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
public class ScrollBar : View, IOrientation, IDesignable
{
    private readonly Button _decreaseButton;
    internal readonly ScrollSlider _slider;
    private readonly Button _increaseButton;

    /// <inheritdoc/>
    public ScrollBar ()
    {
        _decreaseButton = new ()
        {
            CanFocus = false,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.None,
            WantContinuousButtonPressed = true
        };
        _decreaseButton.Accepting += OnDecreaseButtonOnAccept;

        _slider = new ()
        {
            ShrinkBy = 2, // For the buttons
        };
        _slider.Scrolled += SliderOnScroll;
        _slider.PositionChanged += SliderOnPositionChanged;

        _increaseButton = new ()
        {
            CanFocus = false,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.None,
            WantContinuousButtonPressed = true
        };
        _increaseButton.Accepting += OnIncreaseButtonOnAccept;
        base.Add (_decreaseButton, _slider, _increaseButton);

        CanFocus = false;

        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        // This sets the width/height etc...
        OnOrientationChanged (Orientation);

        void OnDecreaseButtonOnAccept (object? s, CommandEventArgs e)
        {
            ContentPosition -= Increment;
            e.Cancel = true;
        }

        void OnIncreaseButtonOnAccept (object? s, CommandEventArgs e)
        {
            ContentPosition += Increment;
            e.Cancel = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        ShowHide ();
    }

    private void ShowHide ()
    {
        if (!AutoHide || !IsInitialized)
        {
            return;
        }

        if (Orientation == Orientation.Vertical)
        {
            Visible = Frame.Height < Size;
        }
        else
        {
            Visible = Frame.Width < Size;
        }
    }

    /// <inheritdoc/>
    protected override void OnSubviewLayout (LayoutEventArgs args)
    {
        _slider.Size = CalculateSliderSize ();

        if (Orientation == Orientation.Vertical)
        {
            _slider.ViewportDimension = Viewport.Height - _slider.ShrinkBy;
        }
        else
        {
            _slider.ViewportDimension = Viewport.Width - _slider.ShrinkBy;
        }
    }

    private int CalculateSliderSize ()
    {
        if (Size == 0 || ViewportDimension == 0)
        {
            return 1;
        }
        return (int)Math.Clamp (Math.Floor ((double)ViewportDimension / Size * (Viewport.Height - 2)), 1, ViewportDimension);
    }

    private void PositionSubviews ()
    {
        if (Orientation == Orientation.Vertical)
        {
            _decreaseButton.Y = 0;
            _decreaseButton.X = 0;
            _decreaseButton.Width = Dim.Fill ();
            _decreaseButton.Height = 1;
            _decreaseButton.Title = Glyphs.UpArrow.ToString ();

            _slider.X = 0;
            _slider.Y = 1;
            _slider.Width = Dim.Fill ();

            _increaseButton.Y = Pos.AnchorEnd ();
            _increaseButton.X = 0;
            _increaseButton.Width = Dim.Fill ();
            _increaseButton.Height = 1;
            _increaseButton.Title = Glyphs.DownArrow.ToString ();
        }
        else
        {
            _decreaseButton.Y = 0;
            _decreaseButton.X = 0;
            _decreaseButton.Width = 1;
            _decreaseButton.Height = Dim.Fill ();
            _decreaseButton.Title = Glyphs.LeftArrow.ToString ();

            _slider.Y = 0;
            _slider.X = 1;
            _slider.Height = Dim.Fill ();

            _increaseButton.Y = 0;
            _increaseButton.X = Pos.AnchorEnd ();
            _increaseButton.Width = 1;
            _increaseButton.Height = Dim.Fill ();
            _increaseButton.Title = Glyphs.RightArrow.ToString ();
        }
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
        PositionSubviews ();
    }

    #endregion


    private bool _autoHide = true;

    /// <summary>
    ///     Gets or sets whether <see cref="View.Visible"/> will be set to <see langword="false"/> if the dimension of the
    ///     scroll bar is greater than or equal to <see cref="Size"/>.
    /// </summary>
    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide != value)
            {
                _autoHide = value;

                if (!AutoHide)
                {
                    Visible = true;
                }

                SetNeedsLayout ();
            }
        }
    }

    public bool KeepContentInAllViewport
    {
        //get => _scroll.KeepContentInAllViewport;
        //set => _scroll.KeepContentInAllViewport = value;
        get;
        set;
    }

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

        int pos = e.CurrentValue;

        RaiseSliderPositionChangeEvents (_slider.Position, pos);
    }

    private void SliderOnScroll (object? sender, EventArgs<int> e)
    {
        if (ViewportDimension == 0)
        {
            return;
        }

        int calculatedSliderPos = CalculateSliderPosition (_contentPosition, e.CurrentValue >= 0 ? NavigationDirection.Forward : NavigationDirection.Backward);
        int sliderScrolledAmount = e.CurrentValue;
        int scrolledAmount = CalculateContentPosition (sliderScrolledAmount);

        RaiseSliderPositionChangeEvents (calculatedSliderPos, _slider.Position);

        ContentPosition = _contentPosition + scrolledAmount;
    }

    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, within the Viewport.
    /// </summary>
    public int GetSliderPosition () => CalculateSliderPosition (_contentPosition);

    private void RaiseSliderPositionChangeEvents (int calculatedSliderPosition, int newSliderPosition)
    {
        if (calculatedSliderPosition == newSliderPosition)
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

    private int CalculateSliderPosition (int contentPosition, NavigationDirection direction = NavigationDirection.Forward)
    {
        if (Size - ViewportDimension == 0)
        {
            return 0;
        }

        int scrollBarSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;
        double newSliderPosition = (double)contentPosition / (Size - ViewportDimension) * (scrollBarSize - _slider.Size - _slider.ShrinkBy);

        return direction == NavigationDirection.Forward ? (int)Math.Floor (newSliderPosition) : (int)Math.Ceiling (newSliderPosition);
    }

    private int CalculateContentPosition (int sliderPosition)
    {
        int scrollBarSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;
        return (int)Math.Round ((double)(sliderPosition) / (scrollBarSize - _slider.Size - _slider.ShrinkBy) * (Size - ViewportDimension));
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
            NavigationDirection direction = newContentPosition >= _contentPosition ? NavigationDirection.Forward : NavigationDirection.Backward;

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

            int distance = newContentPosition - _contentPosition;

            _contentPosition = newContentPosition;

            OnContentPositionChanged (_contentPosition);
            ContentPositionChanged?.Invoke (this, new (in _contentPosition));

            OnScrolled (distance);
            Scrolled?.Invoke (this, new (in distance));

            int currentSliderPosition = _slider.Position;
            int calculatedSliderPosition = CalculateSliderPosition (_contentPosition, direction);

            _slider.MoveToPosition (calculatedSliderPosition);

            RaiseSliderPositionChangeEvents (currentSliderPosition, _slider.Position);

        }
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

    /// <summary>Called when <see cref="ContentPosition"/> has changed. Indicates how much to scroll.</summary>
    protected virtual void OnScrolled (int distance) { }

    /// <summary>Raised when the <see cref="ContentPosition"/> has changed. Indicates how much to scroll.</summary>
    public event EventHandler<EventArgs<int>>? Scrolled;

    #endregion ContentPosition

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (Orientation == Orientation.Vertical)
        {
            FillRect (Viewport with { Y = Viewport.Y + 1, Height = Viewport.Height - 2 }, Glyphs.Stipple);
        }
        else
        {
            FillRect (Viewport with { X = Viewport.X + 1, Width = Viewport.Width - 2 }, Glyphs.Stipple);
        }

        return true;
    }

    // TODO: Change this to work OnMouseEvent with continuouse press and grab so it's continous.
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
            sliderCenter = 1 + _slider.Frame.Y + _slider.Frame.Height / 2;
            distanceFromCenter = args.Position.Y - sliderCenter;
        }
        else
        {
            sliderCenter = 1 + _slider.Frame.X + _slider.Frame.Width / 2;
            distanceFromCenter = args.Position.X - sliderCenter;
        }

#if PROPORTIONAL_SCROLL_JUMP
        // BUGBUG: This logic mostly works to provide a proportional jump. However, the math
        // BUGBUG: falls apart in edge cases. Most other scroll bars (e.g. Windows) do not do prooportional
        // BUGBUG: Thus, this is disabled and we just jump a page each click.
        // Ratio of the distance to the viewport dimension
        double ratio = (double)Math.Abs (distanceFromCenter) / (ViewportDimension);
        // Jump size based on the ratio and the total content size
        int jump = (int)(ratio * (Size - ViewportDimension));
#else
        int jump = (ViewportDimension);
#endif
        // Adjust the content position based on the distance
        if (distanceFromCenter < 0)
        {
            ContentPosition = Math.Max (0, ContentPosition - jump);
        }
        else
        {
            ContentPosition = Math.Min (Size - _slider.ViewportDimension, ContentPosition + jump);
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
        Size = 250;

        return true;
    }
}
