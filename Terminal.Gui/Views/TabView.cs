#nullable enable
namespace Terminal.Gui;

/// <summary>Control that hosts multiple sub views, presenting a single one at once.</summary>
public class TabView : View
{
    /// <summary>The default <see cref="MaxTabTextWidth"/> to set on new <see cref="TabView"/> controls.</summary>
    public const uint DefaultMaxTabTextWidth = 30;

    /// <summary>
    ///     This sub view is the main client area of the current tab.  It hosts the <see cref="Tab.View"/> of the tab, the
    ///     <see cref="SelectedTab"/>.
    /// </summary>
    private readonly View _containerView;

    private readonly List<Tab> _tabs = new ();

    /// <summary>This sub view is the 2 or 3 line control that represents the actual tabs themselves.</summary>
    private readonly TabRowView _tabsBar;

    private Tab? _selectedTab;
    private TabToRender []? _tabLocations;
    private int _tabScrollOffset;

    /// <summary>Initializes a <see cref="TabView"/> class.</summary>
    public TabView ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabStop; // Because TabView has focusable subviews, it must be a TabGroup
        _tabsBar = new TabRowView (this);
        _containerView = new ();
        ApplyStyleChanges ();

        base.Add (_tabsBar);
        base.Add (_containerView);

        // Things this view knows how to do
        AddCommand (Command.Left, () => SwitchTabBy (-1));

        AddCommand (Command.Right, () => SwitchTabBy (1));

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        TabScrollOffset = 0;
                        SelectedTab = Tabs.FirstOrDefault ()!;

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        TabScrollOffset = Tabs.Count - 1;
                        SelectedTab = Tabs.LastOrDefault ()!;

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        TabScrollOffset += _tabLocations!.Length;
                        SelectedTab = Tabs.ElementAt (TabScrollOffset);

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        TabScrollOffset -= _tabLocations!.Length;
                        SelectedTab = Tabs.ElementAt (TabScrollOffset);

                        return true;
                    }
                   );

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
    }

    /// <summary>
    ///     The maximum number of characters to render in a Tab header.  This prevents one long tab from pushing out all
    ///     the others.
    /// </summary>
    public uint MaxTabTextWidth { get; set; } = DefaultMaxTabTextWidth;

    // This is needed to hold initial value because it may change during the setter process
    private bool _selectedTabHasFocus;

    /// <summary>The currently selected member of <see cref="Tabs"/> chosen by the user.</summary>
    /// <value></value>
    public Tab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            Tab? old = _selectedTab;
            _selectedTabHasFocus = old is { } && (old.HasFocus == true || !_containerView.CanFocus);

            if (_selectedTab is { })
            {
                if (_selectedTab.View is { })
                {
                    _selectedTab.View.CanFocusChanged -= ContainerViewCanFocus!;
                    // remove old content
                    _containerView.Remove (_selectedTab.View);
                }
            }

            _selectedTab = value;

            // add new content
            if (_selectedTab?.View != null)
            {
                _selectedTab.View.CanFocusChanged += ContainerViewCanFocus!;
                _containerView.Add (_selectedTab.View);
            }

            ContainerViewCanFocus (null!, null!);

            EnsureSelectedTabIsVisible ();

            if (old != _selectedTab)
            {
                if (_selectedTabHasFocus || !_containerView.CanFocus)
                {
                    SelectedTab?.SetFocus ();
                }

                OnSelectedTabChanged (old!, _selectedTab!);
            }
            SetNeedsLayout ();
        }
    }

    private void ContainerViewCanFocus (object sender, EventArgs eventArgs)
    {
        _containerView.CanFocus = _containerView.Subviews.Count (v => v.CanFocus) > 0;
    }

    private TabStyle _style = new ();

    /// <summary>Render choices for how to display tabs.  After making changes, call <see cref="ApplyStyleChanges()"/>.</summary>
    /// <value></value>
    public TabStyle Style
    {
        get => _style;
        set
        {
            if (_style == value)
            {
                return;
            }
            _style = value;
            SetNeedsLayout ();
        }
    }

    /// <summary>All tabs currently hosted by the control.</summary>
    /// <value></value>
    public IReadOnlyCollection<Tab> Tabs => _tabs.AsReadOnly ();

    /// <summary>When there are too many tabs to render, this indicates the first tab to render on the screen.</summary>
    /// <value></value>
    public int TabScrollOffset
    {
        get => _tabScrollOffset;
        set
        {
            _tabScrollOffset = EnsureValidScrollOffsets (value);
            SetNeedsLayout ();
        }
    }

    /// <summary>Adds the given <paramref name="tab"/> to <see cref="Tabs"/>.</summary>
    /// <param name="tab"></param>
    /// <param name="andSelect">True to make the newly added Tab the <see cref="SelectedTab"/>.</param>
    public void AddTab (Tab tab, bool andSelect)
    {
        if (_tabs.Contains (tab))
        {
            return;
        }

        _tabs.Add (tab);
        _tabsBar.Add (tab);

        if (SelectedTab is null || andSelect)
        {
            SelectedTab = tab;

            EnsureSelectedTabIsVisible ();

            tab.View?.SetFocus ();
        }

        SetNeedsLayout ();
    }

    /// <summary>
    ///     Updates the control to use the latest state settings in <see cref="Style"/>. This can change the size of the
    ///     client area of the tab (for rendering the selected tab's content).  This method includes a call to
    ///     <see cref="View.SetNeedsDraw()"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        _containerView.BorderStyle = Style.ShowBorder ? LineStyle.Single : LineStyle.None;
        _containerView.Width = Dim.Fill ();

        if (Style.TabsOnBottom)
        {
            // Tabs are along the bottom so just dodge the border
            if (Style.ShowBorder)
            {
                _containerView.Border!.Thickness = new Thickness (1, 1, 1, 0);
            }

            _containerView.Y = 0;

            int tabHeight = GetTabHeight (false);

            // Fill client area leaving space at bottom for tabs
            _containerView.Height = Dim.Fill (tabHeight);

            _tabsBar.Height = tabHeight;

            _tabsBar.Y = Pos.Bottom (_containerView);
        }
        else
        {
            // Tabs are along the top
            if (Style.ShowBorder)
            {
                _containerView.Border!.Thickness = new Thickness (1, 0, 1, 1);
            }

            _tabsBar.Y = 0;

            int tabHeight = GetTabHeight (true);

            //move content down to make space for tabs
            _containerView.Y = Pos.Bottom (_tabsBar);

            // Fill client area leaving space at bottom for border
            _containerView.Height = Dim.Fill ();

            // The top tab should be 2 or 3 rows high and on the top

            _tabsBar.Height = tabHeight;

            // Should be able to just use 0 but switching between top/bottom tabs repeatedly breaks in ValidatePosDim if just using the absolute value 0
        }

        SetNeedsLayout ();
    }

    /// <inheritdoc />
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        _tabLocations = CalculateViewport (Viewport).ToArray ();

        base.OnViewportChanged (e);
    }

    /// <summary>Updates <see cref="TabScrollOffset"/> to ensure that <see cref="SelectedTab"/> is visible.</summary>
    public void EnsureSelectedTabIsVisible ()
    {
        if (!IsInitialized || SelectedTab is null)
        {
            return;
        }

        // if current viewport does not include the selected tab
        if (!CalculateViewport (Viewport).Any (r => Equals (SelectedTab, r.Tab)))
        {
            // Set scroll offset so the first tab rendered is the
            TabScrollOffset = Math.Max (0, Tabs.IndexOf (SelectedTab));
        }
    }

    /// <summary>Updates <see cref="TabScrollOffset"/> to be a valid index of <see cref="Tabs"/>.</summary>
    /// <param name="value">The value to validate.</param>
    /// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>.</remarks>
    /// <returns>The valid <see cref="TabScrollOffset"/> for the given value.</returns>
    public int EnsureValidScrollOffsets (int value) { return Math.Max (Math.Min (value, Tabs.Count - 1), 0); }

    /// <inheritdoc />
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (SelectedTab is { HasFocus: false } && !_containerView.CanFocus && focusedView == this)
        {
            SelectedTab?.SetFocus ();

            return;
        }

        base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);
    }

    /// <summary>
    ///     Removes the given <paramref name="tab"/> from <see cref="Tabs"/>. Caller is responsible for disposing the
    ///     tab's hosted <see cref="Tab.View"/> if appropriate.
    /// </summary>
    /// <param name="tab"></param>
    public void RemoveTab (Tab? tab)
    {
        if (tab is null || !_tabs.Contains (tab))
        {
            return;
        }

        // what tab was selected before closing
        int idx = _tabs.IndexOf (tab);

        _tabs.Remove (tab);

        // if the currently selected tab is no longer a member of Tabs
        if (SelectedTab is null || !Tabs.Contains (SelectedTab))
        {
            // select the tab closest to the one that disappeared
            int toSelect = Math.Max (idx - 1, 0);

            if (toSelect < Tabs.Count)
            {
                SelectedTab = Tabs.ElementAt (toSelect);
            }
            else
            {
                SelectedTab = Tabs.LastOrDefault ();
            }
        }

        EnsureSelectedTabIsVisible ();
        SetNeedsLayout ();
    }

    /// <summary>Event for when <see cref="SelectedTab"/> changes.</summary>
    public event EventHandler<TabChangedEventArgs>? SelectedTabChanged;

    /// <summary>
    ///     Changes the <see cref="SelectedTab"/> by the given <paramref name="amount"/>. Positive for right, negative for
    ///     left.  If no tab is currently selected then the first tab will become selected.
    /// </summary>
    /// <param name="amount"></param>
    public bool SwitchTabBy (int amount)
    {
        if (Tabs.Count == 0)
        {
            return false;
        }

        // if there is only one tab anyway or nothing is selected
        if (Tabs.Count == 1 || SelectedTab is null)
        {
            SelectedTab = Tabs.ElementAt (0);

            return SelectedTab is { };
        }

        int currentIdx = Tabs.IndexOf (SelectedTab);

        // Currently selected tab has vanished!
        if (currentIdx == -1)
        {
            SelectedTab = Tabs.ElementAt (0);
            return true;
        }

        int newIdx = Math.Max (0, Math.Min (currentIdx + amount, Tabs.Count - 1));

        if (newIdx == currentIdx)
        {
            return false;
        }

        SelectedTab = _tabs [newIdx];

        EnsureSelectedTabIsVisible ();

        return true;
    }

    /// <summary>
    ///     Event fired when a <see cref="Tab"/> is clicked.  Can be used to cancel navigation, show context menu (e.g. on
    ///     right click) etc.
    /// </summary>
    public event EventHandler<TabMouseEventArgs>? TabClicked;

    /// <summary>Disposes the control and all <see cref="Tabs"/>.</summary>
    /// <param name="disposing"></param>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        // The selected tab will automatically be disposed but
        // any tabs not visible will need to be manually disposed

        foreach (Tab tab in Tabs)
        {
            if (!Equals (SelectedTab, tab))
            {
                tab.View?.Dispose ();
            }
        }
    }

    /// <summary>Raises the <see cref="SelectedTabChanged"/> event.</summary>
    protected virtual void OnSelectedTabChanged (Tab oldTab, Tab newTab)
    {
        SelectedTabChanged?.Invoke (this, new TabChangedEventArgs (oldTab, newTab));
    }

    /// <summary>Returns which tabs to render at each x location.</summary>
    /// <returns></returns>
    private IEnumerable<TabToRender> CalculateViewport (Rectangle bounds)
    {
        UnSetCurrentTabs ();

        var i = 1;
        View? prevTab = null;

        // Starting at the first or scrolled to tab
        foreach (Tab tab in Tabs.Skip (TabScrollOffset))
        {
            if (prevTab is { })
            {
                tab.X = Pos.Right (prevTab) - 1;
            }
            else
            {
                tab.X = 0;
            }

            tab.Y = 0;

            // while there is space for the tab
            int tabTextWidth = tab.DisplayText.EnumerateRunes ().Sum (c => c.GetColumns ());

            string text = tab.DisplayText;

            // The maximum number of characters to use for the tab name as specified
            // by the user (MaxTabTextWidth).  But not more than the width of the view
            // or we won't even be able to render a single tab!
            long maxWidth = Math.Max (0, Math.Min (bounds.Width - 3, MaxTabTextWidth));

            tab.Width = 2;
            tab.Height = Style.ShowTopLine ? 3 : 2;

            // if tab view is width <= 3 don't render any tabs
            if (maxWidth == 0)
            {
                tab.Visible = true;
                tab.MouseClick += Tab_MouseClick!;
                tab.Border!.MouseClick += Tab_MouseClick!;

                yield return new (tab, string.Empty, Equals (SelectedTab, tab));

                break;
            }

            if (tabTextWidth > maxWidth)
            {
                text = tab.Text = tab.DisplayText.Substring (0, (int)maxWidth);
                tabTextWidth = (int)maxWidth;
            }
            else
            {
                tab.Text = text;
            }

            tab.Width = Math.Max (tabTextWidth + 2, 1);
            tab.Height = Style.ShowTopLine ? 3 : 2;

            // if there is not enough space for this tab
            if (i + tabTextWidth >= bounds.Width)
            {
                tab.Visible = false;

                break;
            }

            // there is enough space!
            tab.Visible = true;
            tab.MouseClick += Tab_MouseClick!;
            tab.Border!.MouseClick += Tab_MouseClick!;

            yield return new (tab, text, Equals (SelectedTab, tab));

            prevTab = tab;

            i += tabTextWidth + 1;
        }

        if (_selectedTabHasFocus)
        {
            SelectedTab?.SetFocus ();
        }
    }

    /// <summary>
    ///     Returns the number of rows occupied by rendering the tabs, this depends on <see cref="TabStyle.ShowTopLine"/>
    ///     and can be 0 (e.g. if <see cref="TabStyle.TabsOnBottom"/> and you ask for <paramref name="top"/>).
    /// </summary>
    /// <param name="top">True to measure the space required at the top of the control, false to measure space at the bottom.</param>
    /// .
    /// <returns></returns>
    private int GetTabHeight (bool top)
    {
        if (top && Style.TabsOnBottom)
        {
            return 0;
        }

        if (!top && !Style.TabsOnBottom)
        {
            return 0;
        }

        return Style.ShowTopLine ? 3 : 2;
    }

    private void Tab_MouseClick (object sender, MouseEventArgs e)
    {
        e.Handled = _tabsBar.NewMouseEvent (e) == true;
    }

    private void UnSetCurrentTabs ()
    {
        if (_tabLocations is null)
        {
            // Ensures unset any visible tab prior to TabScrollOffset
            for (int i = 0; i < TabScrollOffset; i++)
            {
                Tab tab = Tabs.ElementAt (i);

                if (tab.Visible)
                {
                    tab.MouseClick -= Tab_MouseClick!;
                    tab.Border!.MouseClick -= Tab_MouseClick!;
                    tab.Visible = false;
                }
            }
        }
        else if (_tabLocations is { })
        {
            foreach (TabToRender tabToRender in _tabLocations)
            {
                tabToRender.Tab.MouseClick -= Tab_MouseClick!;
                tabToRender.Tab.Border!.MouseClick -= Tab_MouseClick!;
                tabToRender.Tab.Visible = false;
            }

            _tabLocations = null;
        }
    }

    /// <summary>Raises the <see cref="TabClicked"/> event.</summary>
    /// <param name="tabMouseEventArgs"></param>
    private protected virtual void OnTabClicked (TabMouseEventArgs tabMouseEventArgs) { TabClicked?.Invoke (this, tabMouseEventArgs); }

    private class TabRowView : View
    {
        private readonly TabView _host;
        private readonly View _leftScrollIndicator;
        private readonly View _rightScrollIndicator;

        public TabRowView (TabView host)
        {
            _host = host;
            Id = "tabRowView";

            CanFocus = true;
            Width = Dim.Fill ();

            _rightScrollIndicator = new View
            {
                Id = "rightScrollIndicator",
                Width = 1,
                Height = 1,
                Visible = false,
                Text = Glyphs.RightArrow.ToString ()
            };
            _rightScrollIndicator.MouseClick += _host.Tab_MouseClick!;

            _leftScrollIndicator = new View
            {
                Id = "leftScrollIndicator",
                Width = 1,
                Height = 1,
                Visible = false,
                Text = Glyphs.LeftArrow.ToString ()
            };
            _leftScrollIndicator.MouseClick += _host.Tab_MouseClick!;

            Add (_rightScrollIndicator, _leftScrollIndicator);
        }

        protected override bool OnMouseEvent (MouseEventArgs me)
        {
            View? parent = me.View is Adornment adornment ? adornment.Parent : me.View;
            Tab? hit = parent as Tab;

            if (me.IsSingleClicked)
            {
                _host.OnTabClicked (new TabMouseEventArgs (hit, me));

                // user canceled click
                if (me.Handled)
                {
                    return true;
                }

                if (parent == _host.SelectedTab)
                {
                    _host.SelectedTab?.SetFocus ();
                }
            }

            if (!me.IsSingleDoubleOrTripleClicked)
            {
                return false;
            }

            if (!HasFocus && CanFocus)
            {
                SetFocus ();
            }

            if (me.IsSingleDoubleOrTripleClicked)
            {
                var scrollIndicatorHit = 0;

                if (me.View is { Id: "rightScrollIndicator" })
                {
                    scrollIndicatorHit = 1;
                }
                else if (me.View is { Id: "leftScrollIndicator" })
                {
                    scrollIndicatorHit = -1;
                }

                if (scrollIndicatorHit != 0)
                {
                    _host.SwitchTabBy (scrollIndicatorHit);

                    SetNeedsLayout ();

                    return true;
                }

                if (hit is { })
                {
                    _host.SelectedTab = hit;
                    SetNeedsLayout ();

                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
        {
            if (_host.SelectedTab is { HasFocus: false, CanFocus: true } && focusedView == this)
            {
                _host.SelectedTab?.SetFocus ();

                return;
            }

            base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);
        }

        /// <inheritdoc />
        protected override void OnSubviewLayout (LayoutEventArgs args)
        {
            _host._tabLocations = _host.CalculateViewport (Viewport).ToArray ();

            RenderTabLine ();

            RenderUnderline ();

            base.OnSubviewLayout (args);
        }

        /// <inheritdoc />
        protected override bool OnRenderingLineCanvas ()
        {
            RenderTabLineCanvas ();

            return false;
        }

        private void RenderTabLineCanvas ()
        {
            if (_host._tabLocations is null)
            {
                return;
            }

            TabToRender [] tabLocations = _host._tabLocations;
            int selectedTab = -1;
            var lc = new LineCanvas ();

            for (var i = 0; i < tabLocations.Length; i++)
            {
                View tab = tabLocations [i].Tab;
                Rectangle vts = tab.ViewportToScreen (tab.Viewport);
                int selectedOffset = _host.Style.ShowTopLine && tabLocations [i].IsSelected ? 0 : 1;

                if (tabLocations [i].IsSelected)
                {
                    selectedTab = i;

                    if (i == 0 && _host.TabScrollOffset == 0)
                    {
                        if (_host.Style.TabsOnBottom)
                        {
                            // Upper left vertical line
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );
                        }
                        else
                        {
                            // Lower left vertical line
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );
                        }
                    }
                    else if (i > 0 && i <= tabLocations.Length - 1)
                    {
                        if (_host.Style.TabsOnBottom)
                        {
                            // URCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }
                        else
                        {
                            // LRCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

                        if (_host.Style.ShowTopLine)
                        {
                            if (_host.Style.TabsOnBottom)
                            {
                                // Lower left tee
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }
                            else
                            {
                                // Upper left tee
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }
                        }
                    }

                    if (i < tabLocations.Length - 1)
                    {
                        if (_host.Style.ShowTopLine)
                        {
                            if (_host.Style.TabsOnBottom)
                            {
                                // Lower right tee
                                lc.AddLine (
                                            new Point (vts.Right, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.Right, vts.Bottom),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }
                            else
                            {
                                // Upper right tee
                                lc.AddLine (
                                            new Point (vts.Right, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.Right, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }
                        }
                    }

                    if (_host.Style.TabsOnBottom)
                    {
                        //URCorner
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                    else
                    {
                        //LLCorner
                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom - selectedOffset),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom - selectedOffset),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                }
                else if (selectedTab == -1)
                {
                    if (i == 0 && string.IsNullOrEmpty (tab.Text))
                    {
                        if (_host.Style.TabsOnBottom)
                        {
                            if (_host.Style.ShowTopLine)
                            {
                                // LLCorner
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // ULCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }
                        else
                        {
                            if (_host.Style.ShowTopLine)
                            {
                                // ULCorner
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // LLCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }
                    }
                    else if (i > 0)
                    {
                        if (_host.Style.ShowTopLine || _host.Style.TabsOnBottom)
                        {
                            // Upper left tee
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        0,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

                        // Lower left tee
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                }
                else if (i < tabLocations.Length - 1)
                {
                    if (_host.Style.ShowTopLine)
                    {
                        // Upper right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }

                    if (_host.Style.ShowTopLine || !_host.Style.TabsOnBottom)
                    {
                        // Lower right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                    else
                    {
                        // Upper right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                }

                if (i == 0 && i != selectedTab && _host is { TabScrollOffset: 0, Style.ShowBorder: true })
                {
                    if (_host.Style.TabsOnBottom)
                    {
                        // Upper left vertical line
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    0,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                    else
                    {
                        // Lower left vertical line
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom),
                                    0,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                }

                if (i == tabLocations.Length - 1 && i != selectedTab)
                {
                    if (_host.Style.TabsOnBottom)
                    {
                        // Upper right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                    else
                    {
                        // Lower right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                }

                if (i == tabLocations.Length - 1)
                {
                    var arrowOffset = 1;

                    int lastSelectedTab = !_host.Style.ShowTopLine && i == selectedTab ? 1 :
                                          _host.Style.TabsOnBottom ? 1 : 0;
                    Rectangle tabsBarVts = ViewportToScreen (Viewport);
                    int lineLength = tabsBarVts.Right - vts.Right;

                    // Right horizontal line
                    if (ShouldDrawRightScrollIndicator ())
                    {
                        if (lineLength - arrowOffset > 0)
                        {
                            if (_host.Style.TabsOnBottom)
                            {
                                lc.AddLine (
                                            new Point (vts.Right, vts.Y - lastSelectedTab),
                                            lineLength - arrowOffset,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }
                            else
                            {
                                lc.AddLine (
                                            new Point (
                                                       vts.Right,
                                                       vts.Bottom - lastSelectedTab
                                                      ),
                                            lineLength - arrowOffset,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }
                        }
                    }
                    else
                    {
                        // Right corner
                        if (_host.Style.TabsOnBottom)
                        {
                            lc.AddLine (
                                        new Point (vts.Right, vts.Y - lastSelectedTab),
                                        lineLength,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }
                        else
                        {
                            lc.AddLine (
                                        new Point (vts.Right, vts.Bottom - lastSelectedTab),
                                        lineLength,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

                        if (_host.Style.ShowBorder)
                        {
                            if (_host.Style.TabsOnBottom)
                            {
                                // More LRCorner
                                lc.AddLine (
                                            new Point (
                                                       tabsBarVts.Right - 1,
                                                       vts.Y - lastSelectedTab
                                                      ),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );
                            }
                            else
                            {
                                // More URCorner
                                lc.AddLine (
                                            new Point (
                                                       tabsBarVts.Right - 1,
                                                       vts.Bottom - lastSelectedTab
                                                      ),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );
                            }
                        }
                    }
                }
            }

            _host.LineCanvas.Merge (lc);
        }

        private int GetUnderlineYPosition ()
        {
            if (_host.Style.TabsOnBottom)
            {
                return 0;
            }

            return _host.Style.ShowTopLine ? 2 : 1;
        }

        /// <summary>Renders the line with the tab names in it.</summary>
        private void RenderTabLine ()
        {
            if (_host._tabLocations is null)
            {
                return;
            }

            View? selected = null;
            int topLine = _host.Style.ShowTopLine ? 1 : 0;

            foreach (TabToRender toRender in _host._tabLocations)
            {
                Tab tab = toRender.Tab;

                if (toRender.IsSelected)
                {
                    selected = tab;

                    if (_host.Style.TabsOnBottom)
                    {
                        tab.Border!.Thickness = new (1, 0, 1, topLine);
                        tab.Margin!.Thickness = new (0, 1, 0, 0);
                    }
                    else
                    {
                        tab.Border!.Thickness = new (1, topLine, 1, 0);
                        tab.Margin!.Thickness = new (0, 0, 0, topLine);
                    }
                }
                else if (selected is null)
                {
                    if (_host.Style.TabsOnBottom)
                    {
                        tab.Border!.Thickness = new (1, 1, 1, topLine);
                        tab.Margin!.Thickness = new (0, 0, 0, 0);
                    }
                    else
                    {
                        tab.Border!.Thickness = new (1, topLine, 1, 1);
                        tab.Margin!.Thickness = new (0, 0, 0, 0);
                    }
                }
                else
                {
                    if (_host.Style.TabsOnBottom)
                    {
                        tab.Border!.Thickness = new (1, 1, 1, topLine);
                        tab.Margin!.Thickness = new (0, 0, 0, 0);
                    }
                    else
                    {
                        tab.Border!.Thickness = new (1, topLine, 1, 1);
                        tab.Margin!.Thickness = new (0, 0, 0, 0);
                    }
                }

                // Ensures updating TextFormatter constrains
                tab.TextFormatter.ConstrainToWidth = tab.GetContentSize ().Width;
                tab.TextFormatter.ConstrainToHeight = tab.GetContentSize ().Height;
            }
        }

        /// <summary>Renders the line of the tab that adjoins the content of the tab.</summary>
        private void RenderUnderline ()
        {
            int y = GetUnderlineYPosition ();

            TabToRender? selected = _host._tabLocations?.FirstOrDefault (t => t.IsSelected);

            if (selected is null)
            {
                return;
            }

            // draw scroll indicators

            // if there are more tabs to the left not visible
            if (_host.TabScrollOffset > 0)
            {
                _leftScrollIndicator.X = 0;
                _leftScrollIndicator.Y = y;

                // indicate that
                _leftScrollIndicator.Visible = true;

                // Ensures this is clicked instead of the first tab
                MoveSubviewToEnd (_leftScrollIndicator);
            }
            else
            {
                _leftScrollIndicator.Visible = false;
            }

            // if there are more tabs to the right not visible
            if (ShouldDrawRightScrollIndicator ())
            {
                _rightScrollIndicator.X = Viewport.Width - 1;
                _rightScrollIndicator.Y = y;

                // indicate that
                _rightScrollIndicator.Visible = true;

                // Ensures this is clicked instead of the last tab if under this
                MoveSubviewToStart (_rightScrollIndicator);
            }
            else
            {
                _rightScrollIndicator.Visible = false;
            }
        }

        private bool ShouldDrawRightScrollIndicator () { return _host._tabLocations!.LastOrDefault ()?.Tab != _host.Tabs.LastOrDefault (); }
    }

    private class TabToRender
    {
        public TabToRender (Tab tab, string textToRender, bool isSelected)
        {
            Tab = tab;
            IsSelected = isSelected;
            TextToRender = textToRender;
        }

        /// <summary>True if the tab that is being rendered is the selected one.</summary>
        /// <value></value>
        public bool IsSelected { get; }

        public Tab Tab { get; }
        public string TextToRender { get; }
    }
}
