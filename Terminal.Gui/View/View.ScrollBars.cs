#nullable enable
namespace Terminal.Gui;

public partial class View
{
    private Lazy<ScrollBar> _horizontalScrollBar;
    private Lazy<ScrollBar> _verticalScrollBar;

    /// <summary>
    ///     Initializes the ScrollBars of the View. Called by the View constructor.
    /// </summary>
    private void SetupScrollBars ()
    {
        if (this is Adornment)
        {
            return;
        }

        _verticalScrollBar = new (() => CreateScrollBar (Orientation.Vertical));
        _horizontalScrollBar = new (() => CreateScrollBar (Orientation.Horizontal));
    }

    ///// <summary>
    /////     Causes the scrollbar associated with <paramref name="orientation"/> to be explicitly created.
    ///// </summary>
    ///// <remarks>
    /////     The built-in scrollbars are lazy-created internally. To enable them, the <see cref="VerticalScrollBar"/> or <see cref="HorizontalScrollBar"/>
    /////     need to be referenced. All this method does is reference the associated property.
    ///// </remarks>
    ///// <param name="orientation"></param>
    //public void EnableScrollBar (Orientation orientation)
    //{
    //    if (orientation == Orientation.Vertical)
    //    {
    //        _ = VerticalScrollBar; // Explicitly create the vertical scroll bar
    //    }
    //    else
    //    {
    //        _ = HorizontalScrollBar; // Explicitly create the horizontal scroll bar
    //    }
    //}

    private ScrollBar CreateScrollBar (Orientation orientation)
    {
        var scrollBar = new ScrollBar
        {
            Orientation = orientation,
            AutoHide = true
        };

        if (orientation == Orientation.Vertical)
        {
            ConfigureVerticalScrollBar (scrollBar);
        }
        else
        {
            ConfigureHorizontalScrollBar (scrollBar);
        }

        scrollBar.Initialized += OnScrollBarInitialized;

        Padding?.Add (scrollBar);
        return scrollBar;
    }

    private void ConfigureVerticalScrollBar (ScrollBar scrollBar)
    {
        scrollBar.X = Pos.AnchorEnd ();
        scrollBar.Height = Dim.Fill (Dim.Func (() =>
                                               {
                                                   if (_horizontalScrollBar.IsValueCreated)
                                                   {
                                                       return _horizontalScrollBar.Value.Visible ? 1 : 0;
                                                   }
                                                   return 0;
                                               }));
        scrollBar.ScrollableContentSize = GetContentSize ().Height;

        ViewportChanged += (_, _) =>
        {
            scrollBar.VisibleContentSize = Viewport.Height;
            scrollBar.Position = Viewport.Y;
        };

        ContentSizeChanged += (_, _) =>
        {
            scrollBar.ScrollableContentSize = GetContentSize ().Height;
        };
    }

    private void ConfigureHorizontalScrollBar (ScrollBar scrollBar)
    {
        scrollBar.Y = Pos.AnchorEnd ();
        scrollBar.Width = Dim.Fill (Dim.Func (() => {
                                                  if (_verticalScrollBar.IsValueCreated)
                                                  {
                                                      return _verticalScrollBar.Value.Visible ? 1 : 0;
                                                  }
                                                  return 0;
                                              }));
        scrollBar.ScrollableContentSize = GetContentSize ().Width;

        ViewportChanged += (_, _) =>
        {
            scrollBar.VisibleContentSize = Viewport.Width;
            scrollBar.Position = Viewport.X;
        };

        ContentSizeChanged += (_, _) =>
        {
            scrollBar.ScrollableContentSize = GetContentSize ().Width;
        };
    }

    private void OnScrollBarInitialized (object? sender, EventArgs e)
    {
        var scrollBar = (ScrollBar)sender!;
        if (scrollBar.Orientation == Orientation.Vertical)
        {
            ConfigureVerticalScrollBarEvents (scrollBar);
        }
        else
        {
            ConfigureHorizontalScrollBarEvents (scrollBar);
        }
    }

    private void ConfigureVerticalScrollBarEvents (ScrollBar scrollBar)
    {
        Padding!.Thickness = Padding.Thickness with { Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : 0 };

        scrollBar.PositionChanged += (_, args) =>
        {
            Viewport = Viewport with
            {
                Y = Math.Min (args.CurrentValue, GetContentSize ().Height - Viewport.Height)
            };
        };

        scrollBar.VisibleChanged += (_, _) =>
        {
            Padding.Thickness = Padding.Thickness with
            {
                Right = scrollBar.Visible ? Padding.Thickness.Right + 1 : Padding.Thickness.Right - 1
            };
        };
    }

    private void ConfigureHorizontalScrollBarEvents (ScrollBar scrollBar)
    {
        Padding!.Thickness = Padding.Thickness with { Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : 0 };

        scrollBar.PositionChanged += (_, args) =>
        {
            Viewport = Viewport with
            {
                X = Math.Min (args.CurrentValue, GetContentSize ().Width - Viewport.Width)
            };
        };

        scrollBar.VisibleChanged += (_, _) =>
        {
            Padding.Thickness = Padding.Thickness with
            {
                Bottom = scrollBar.Visible ? Padding.Thickness.Bottom + 1 : Padding.Thickness.Bottom - 1
            };
        };
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
            //_horizontalScrollBar.Value.KeepContentInAllViewport = true;
            //_verticalScrollBar.Value.KeepContentInAllViewport = true;
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
            //_horizontalScrollBar.Value.KeepContentInAllViewport = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowYGreaterThanContentHeight))
        {
            //_verticalScrollBar.Value.KeepContentInAllViewport = false;
        }
        else if (viewportSettings.HasFlag (ViewportSettings.AllowLocationGreaterThanContentSize))
        {
            //_horizontalScrollBar.Value.KeepContentInAllViewport = false;
            //_verticalScrollBar.Value.KeepContentInAllViewport = false;
        }
    }
}
