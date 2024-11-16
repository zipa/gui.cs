#nullable enable
namespace Terminal.Gui;

public partial class View
{
    private Lazy<ScrollBar> _horizontalScrollBar;
    private Lazy<ScrollBar> _verticalScrollBar;

    /// <summary>
    ///     Initializes the ScrollBars of the View. Called by the constructor.
    /// </summary>
    private void SetupScrollBars ()
    {
        if (this is Adornment)
        {
            return;
        }

        _verticalScrollBar = new (() => ScrollBarFactory (Orientation.Vertical));
        _horizontalScrollBar = new (() => ScrollBarFactory (Orientation.Horizontal));

        ViewportChanged += (_, _) =>
                           {
                               if (_verticalScrollBar.IsValueCreated)
                               {
                                   _verticalScrollBar.Value.VisibleContentSize = Viewport.Height;
                                   _verticalScrollBar.Value.Position = Viewport.Y;
                               }

                               if (_horizontalScrollBar.IsValueCreated)
                               {
                                   _horizontalScrollBar.Value.VisibleContentSize = Viewport.Width;
                                   _horizontalScrollBar.Value.Position = Viewport.X;
                               }
                           };

        ContentSizeChanged += (_, _) =>
                              {
                                  if (_verticalScrollBar.IsValueCreated)
                                  {
                                      _verticalScrollBar.Value.ScrollableContentSize = GetContentSize ().Height;
                                  }

                                  if (_horizontalScrollBar.IsValueCreated)
                                  {
                                      _horizontalScrollBar.Value.ScrollableContentSize = GetContentSize ().Width;
                                  }
                              };
    }

    private ScrollBar ScrollBarFactory (Orientation orientation)
    {
        var scrollBar = new ScrollBar
        {
            Orientation = orientation,
            AutoHide = true
        };

        if (orientation == Orientation.Vertical)
        {
            scrollBar.X = Pos.AnchorEnd ();

            // Ensure the scrollbar's length accomodates for the opposite scrollbar's visibility
            scrollBar.Height = Dim.Fill (
                                         Dim.Func (
                                                   () =>
                                                   {
                                                       if (_horizontalScrollBar.IsValueCreated)
                                                       {
                                                           return _horizontalScrollBar.Value.Visible ? 1 : 0;
                                                       }

                                                       return 0;
                                                   }));
            scrollBar.ScrollableContentSize = GetContentSize ().Height;
        }
        else
        {
            scrollBar.Y = Pos.AnchorEnd ();

            // Ensure the scrollbar's length accomodates for the opposite scrollbar's visibility
            scrollBar.Width = Dim.Fill (
                                        Dim.Func (
                                                  () =>
                                                  {
                                                      if (_verticalScrollBar.IsValueCreated)
                                                      {
                                                          return _verticalScrollBar.Value.Visible ? 1 : 0;
                                                      }

                                                      return 0;
                                                  }));
            scrollBar.ScrollableContentSize = GetContentSize ().Width;
        }

        Padding?.Add (scrollBar);

        scrollBar.Initialized += OnScrollBarOnInitialized;

        return scrollBar;

        void OnScrollBarOnInitialized (object? o, EventArgs eventArgs)
        {
            if (orientation == Orientation.Vertical)
            {
                Padding!.Thickness = Padding.Thickness with { Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : 0 };

                scrollBar.PositionChanged += (_, args) =>
                                             {
                                                 Viewport = Viewport with
                                                 {
                                                     Y = Math.Min (
                                                                   args.CurrentValue,
                                                                   GetContentSize ().Height - Viewport.Height)
                                                 };
                                             };

                scrollBar.VisibleChanged += (_, _) =>
                                            {
                                                Padding.Thickness = Padding.Thickness with
                                                {
                                                    Right = scrollBar.Visible
                                                                ? Padding.Thickness.Right + 1
                                                                : Padding.Thickness.Right - 1
                                                };
                                            };
            }
            else
            {
                Padding!.Thickness = Padding.Thickness with { Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : 0 };

                scrollBar.PositionChanged += (_, args) =>
                                             {
                                                 Viewport = Viewport with
                                                 {
                                                     X = Math.Min (
                                                                   args.CurrentValue,
                                                                   GetContentSize ().Width - Viewport.Width)
                                                 };
                                             };

                scrollBar.VisibleChanged += (_, _) =>
                                            {
                                                Padding.Thickness = Padding.Thickness with
                                                {
                                                    Bottom = scrollBar.Visible
                                                                 ? Padding.Thickness.Bottom + 1
                                                                 : Padding.Thickness.Bottom - 1
                                                };
                                            };
            }
        }
    }

    /// <summary>
    /// </summary>
    public ScrollBar HorizontalScrollBar => _horizontalScrollBar.Value;

    /// <summary>
    /// </summary>
    public ScrollBar VerticalScrollBar => _verticalScrollBar.Value;

    /// <summary>
    ///     Clean up the ScrollBars of the View. Called by View.Dispose.
    /// </summary>
    private void DisposeScrollBars ()
    {
        if (this is Adornment)
        {
            return;
        }

        if (_horizontalScrollBar.IsValueCreated)
        {
            Padding?.Remove (_horizontalScrollBar.Value);
            _horizontalScrollBar.Value.Dispose ();
        }

        if (_verticalScrollBar.IsValueCreated)
        {
            Padding?.Remove (_verticalScrollBar.Value);
            _verticalScrollBar.Value.Dispose ();
        }
    }

    private void SetScrollBarsKeepContentInAllViewport (ViewportSettings viewportSettings)
    {
        if (viewportSettings == ViewportSettings.None)
        {
            _horizontalScrollBar.Value.KeepContentInAllViewport = true;
            _verticalScrollBar.Value.KeepContentInAllViewport = true;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowNegativeX))
        {
            _horizontalScrollBar.Value.AutoHide = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowNegativeY))
        {
            _verticalScrollBar.Value.AutoHide = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowNegativeLocation))
        {
            _horizontalScrollBar.Value.AutoHide = false;
            _verticalScrollBar.Value.AutoHide = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowXGreaterThanContentWidth))
        {
            _horizontalScrollBar.Value.KeepContentInAllViewport = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowYGreaterThanContentHeight))
        {
            _verticalScrollBar.Value.KeepContentInAllViewport = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowLocationGreaterThanContentSize))
        {
            _horizontalScrollBar.Value.KeepContentInAllViewport = false;
            _verticalScrollBar.Value.KeepContentInAllViewport = false;
        }
    }
}
