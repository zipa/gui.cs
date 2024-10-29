#nullable enable
//#define HACK_DRAW_OVERLAPPED
using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

public partial class View // Drawing APIs
{
    /// <summary>
    ///     Draws the view if it needs to be drawn.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will only be drawn if it is visible, and has any of <see cref="NeedsDraw"/>,
    ///         <see cref="SubViewNeedsDraw"/>,
    ///         or <see cref="NeedsLayout"/> set.
    ///     </para>
    ///     <para>
    ///         // TODO: Add docs for the drawing process.
    ///     </para>
    /// </remarks>
    public void Draw ()
    {
        if (!CanBeVisible (this) || (!NeedsDraw && !SubViewNeedsDraw))
        {
            if (this is not Adornment)
            {
                Driver?.Clip.Exclude (FrameToScreen ());
            }

            return;
        }

        if (Border is { Diagnostics: ViewDiagnosticFlags.DrawIndicator })
        {
            if (Border.DrawIndicator is { })
            {
                Border.DrawIndicator.AdvanceAnimation (false);
                Border.DrawIndicator.DrawText ();

            }
        }

        // Frame/View-relative relative, thus the bounds location should be 0,0
        //Debug.Assert(clipRegion.GetBounds().X == 0 && clipRegion.GetBounds ().Y == 0);

        Region? saved = SetClipToFrame ();
        DoDrawAdornments ();
        DoSetAttribute ();
        if (saved is { })
        {
            Driver!.Clip = saved;
        }
        // By default, we clip to the viewport preventing drawing outside the viewport
        // We also clip to the content, but if a developer wants to draw outside the viewport, they can do
        // so via settings. SetClip honors the ViewportSettings.DisableVisibleContentClipping flag.
        // Get our Viewport in screen coordinates

        saved = SetClipToViewport ();

        DoClearViewport ();
        DoDrawText ();
        DoDrawContent ();

        DoDrawSubviews ();

        // Restore the clip before rendering the line canvas and adornment subviews
        // because they may draw outside the viewport.
        if (saved is { })
        {
            Driver!.Clip = saved;
        }

        saved = SetClipToFrame ();
        DoRenderLineCanvas ();
        DoDrawAdornmentSubViews ();
        ClearNeedsDraw ();

        // We're done
        DoDrawComplete ();
        if (saved is { })
        {
            Driver!.Clip = saved;
        }

        if (this is not Adornment)
        {
            Driver?.Clip.Exclude (FrameToScreen ());
        }

    }

    #region DrawAdornments

    private void DoDrawAdornmentSubViews ()
    {
        // This causes the Adornment's subviews to be REDRAWN
        // TODO: Figure out how to make this more efficient
        if (Margin?.Subviews is { } && Margin.Thickness != Thickness.Empty)
        {
            foreach (View subview in Margin.Subviews)
            {
                subview.SetNeedsDraw ();
            }

            Region? saved = Margin?.SetClipToFrame ();
            Margin?.DoDrawSubviews ();
            if (saved is { })
            {
                Driver!.Clip = saved;
            }
        }

        if (Border?.Subviews is { } && Border.Thickness != Thickness.Empty)
        {
            foreach (View subview in Border.Subviews)
            {
                subview.SetNeedsDraw ();
            }

            Border?.DoDrawSubviews ();
        }

        if (Padding?.Subviews is { } && Padding.Thickness != Thickness.Empty)
        {
            foreach (View subview in Padding.Subviews)
            {
                subview.SetNeedsDraw ();
            }

            Region? saved = Padding?.SetClipToFrame ();
            Padding?.DoDrawSubviews ();
            if (saved is { })
            {
                Driver!.Clip = saved;
            }

        }
    }

    private void DoDrawAdornments ()
    {
        if (OnDrawingAdornments ())
        {
            return;
        }

        // TODO: add event.

        DrawAdornments ();
    }

    /// <summary>
    ///     Causes each of the View's Adornments to be drawn. This includes the <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>.
    /// </summary>
    public void DrawAdornments ()
    {
        // Each of these renders lines to either this View's LineCanvas 
        // Those lines will be finally rendered in OnRenderLineCanvas
        if (Margin is { } && Margin.Thickness != Thickness.Empty)
        {
            Margin?.Draw ();
        }

        if (Border is { } && Border.Thickness != Thickness.Empty)
        {
            Border?.Draw ();
        }

        if (Padding is { } && Padding.Thickness != Thickness.Empty)
        {
            Padding?.Draw ();
        }
    }

    /// <summary>
    ///     Called when the View's Adornments are to be drawn. Prepares <see cref="View.LineCanvas"/>. If
    ///     <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <param name="clipRegion"></param>
    /// <returns><see langword="true"/> to stop further drawing of the Adornments.</returns>
    protected virtual bool OnDrawingAdornments () { return false; }

    #endregion DrawAdornments

    #region SetAttribute

    private void DoSetAttribute ()
    {
        if (OnSettingAttribute ())
        {
            return;
        }

        var args = new CancelEventArgs ();
        SettingAttribute?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        SetNormalAttribute ();
    }


    /// <summary>
    ///     Called when the normal attribute for the View is to be set. This is called before the View is drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnSettingAttribute () { return false; }

    /// <summary>Raised  when the normal attribute for the View is to be set. This is raised before the View is drawn.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? SettingAttribute;

    /// <summary>
    ///     Sets the attribute for the View. This is called before the View is drawn.
    /// </summary>
    public void SetNormalAttribute ()
    {
        if (ColorScheme is { })
        {
            SetAttribute (GetNormalColor ());
        }
    }


    #endregion
    #region ClearViewport

    private void DoClearViewport ()
    {

        if (OnClearingViewport ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        ClearingViewport?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        if (!NeedsDraw)
        {
            return;
        }

        ClearViewport ();
    }

    /// <summary>
    ///     Called when the <see cref="Viewport"/> is to be cleared.
    /// </summary>
    /// <returns><see langword="true"/> to stop further clearing.</returns>
    protected virtual bool OnClearingViewport () { return false; }

    /// <summary>Event invoked when the content area of the View is to be drawn.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rect provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? ClearingViewport;

    /// <summary>Clears <see cref="Viewport"/> with the normal background.</summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="ViewportSettings"/> has <see cref="Gui.ViewportSettings.ClearContentOnly"/> only
    ///         the portion of the content
    ///         area that is visible within the <see cref="View.Viewport"/> will be cleared. This is useful for views that have
    ///         a
    ///         content area larger than the Viewport (e.g. when <see cref="ViewportSettings.AllowNegativeLocation"/> is
    ///         enabled) and want
    ///         the area outside the content to be visually distinct.
    ///     </para>
    /// </remarks>
    public void ClearViewport ()
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = ViewportToScreen (Viewport with { Location = new (0, 0) });

        if (ViewportSettings.HasFlag (ViewportSettings.ClearContentOnly))
        {
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            toClear = Rectangle.Intersect (toClear, visibleContent);
        }

        Attribute prev = SetAttribute (GetNormalColor ());
        Driver.FillRect (toClear);
        SetAttribute (prev);
        SetNeedsDraw ();
    }

    #endregion ClearViewport

    #region DrawText

    private void DoDrawText ()
    {

        if (OnDrawingText ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawingText?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        if (!NeedsDraw)
        {
            return;
        }

        DrawText ();
    }

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View is to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of  <see cref="Text"/>.</returns>
    protected virtual bool OnDrawingText () { return false; }

    /// <summary>Raised when the <see cref="Text"/> of the View is to be drawn.</summary>
    /// <returns>
    ///     Set <see cref="DrawEventArgs.Cancel"/> to <see langword="true"/> to stop further drawing of
    ///     <see cref="Text"/>.
    /// </returns>
    public event EventHandler<DrawEventArgs>? DrawingText;

    /// <summary>
    ///     Draws the <see cref="Text"/> of the View using the <see cref="TextFormatter"/>.
    /// </summary>
    public void DrawText ()
    {
        if (!string.IsNullOrEmpty (TextFormatter.Text))
        {
            TextFormatter.NeedsFormat = true;
        }

        // This should NOT clear 
        // TODO: If the output is not in the Viewport, do nothing
        var drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

        TextFormatter?.Draw (
                             drawRect,
                             HasFocus ? GetFocusColor () : GetNormalColor (),
                             HasFocus ? GetHotFocusColor () : GetHotNormalColor (),
                             Rectangle.Empty
                            );

        // We assume that the text has been drawn over the entire area; ensure that the subviews are redrawn.
        SetSubViewNeedsDraw ();
    }

    #endregion DrawText

    #region DrawContent

    private void DoDrawContent ()
    {
        if (OnDrawingContent ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawingContent?.Invoke (this, dev);

        if (dev.Cancel)
        { }

        // Do nothing.
    }

    /// <summary>
    ///     Called when the View's content is to be drawn. The default implementation does nothing.
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <returns><see langword="true"/> to stop further drawing content.</returns>
    protected virtual bool OnDrawingContent () { return false; }

    /// <summary>Raised when  the View's content is to be drawn.</summary>
    /// <remarks>
    ///     <para>Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.</para>
    ///     <para>
    ///         Rect provides the view-relative rectangle describing the currently visible viewport into the
    ///         <see cref="View"/> .
    ///     </para>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? DrawingContent;

    #endregion DrawContent

    #region DrawSubviews

    private void DoDrawSubviews ()
    {
        if (OnDrawingSubviews ())
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawingSubviews?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        if (!SubViewNeedsDraw)
        {
            return;
        }

        DrawSubviews ();
    }

    /// <summary>
    ///     Called when the <see cref="Subviews"/> are to be drawn.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="Subviews"/>.</returns>
    protected virtual bool OnDrawingSubviews () { return false; }

    /// <summary>Raised when the <see cref="Subviews"/> are to be drawn.</summary>
    /// <remarks>
    /// </remarks>
    /// <returns>
    ///     Set <see cref="DrawEventArgs.Cancel"/> to <see langword="true"/> to stop further drawing of
    ///     <see cref="Subviews"/>.
    /// </returns>
    public event EventHandler<DrawEventArgs>? DrawingSubviews;

    /// <summary>
    ///     Draws the <see cref="Subviews"/>.
    /// </summary>
    public void DrawSubviews ()
    {
        if (_subviews is null)
        {
            return;
        }

#if HACK_DRAW_OVERLAPPED
        IEnumerable<View> subviewsNeedingDraw = _subviews.Where (view => (view.Visible && (view.NeedsDraw || view.SubViewNeedsDraw))
                                                                      || view.Arrangement.HasFlag (ViewArrangement.Overlapped));
#else
        IEnumerable<View> subviewsNeedingDraw = _subviews.Where (view => (view.Visible));
#endif

        foreach (View view in subviewsNeedingDraw.Reverse())
        {
#if HACK_DRAW_OVERLAPPED
            if (view.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {

                view.SetNeedsDraw ();
            }
#endif
            view.Draw ();
        }

    }

    #endregion DrawSubviews

    #region DrawLineCanvas

    private void DoRenderLineCanvas ()
    {
        if (OnRenderingLineCanvas ())
        {
            return;
        }

        // TODO: Add event

        RenderLineCanvas ();
    }

    /// <summary>
    ///     Called when the <see cref="View.LineCanvas"/> is to be rendered. See <see cref="RenderLineCanvas"/>.
    /// </summary>
    /// <param name="clipRegion"></param>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="LineCanvas"/>.</returns>
    protected virtual bool OnRenderingLineCanvas () { return false; }

    /// <summary>The canvas that any line drawing that is to be shared by subviews of this view should add lines to.</summary>
    /// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
    public LineCanvas LineCanvas { get; } = new ();

    /// <summary>
    ///     Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for rendering any
    ///     lines. If <see langword="true"/> the rendering of any borders drawn by this Frame will be done by its parent's
    ///     SuperView. If <see langword="false"/> (the default) this View's <see cref="OnDrawingAdornments"/> method will be
    ///     called to render the borders.
    /// </summary>
    public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

    /// <summary>
    ///     Causes the contents of <see cref="LineCanvas"/> to be drawn. 
    ///      If <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> to be rendered.
    /// </summary>
    public void RenderLineCanvas ()
    {
        // TODO: This is super confusing and needs to be refactored.

        if (Driver is null)
        {
            return;
        }

        // If we have a SuperView, it'll render our frames.
        if (!SuperViewRendersLineCanvas && LineCanvas.Viewport != Rectangle.Empty)
        {
            foreach (KeyValuePair<Point, Cell?> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                if (p.Value is { })
                {
                    SetAttribute (p.Value.Value.Attribute ?? ColorScheme!.Normal);
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    Driver.AddRune (p.Value.Value.Rune);
                }
            }

            LineCanvas.Clear ();
        }

        if (Subviews.Any (s => s.SuperViewRendersLineCanvas))
        {
            foreach (View subview in Subviews.Where (s => s.SuperViewRendersLineCanvas))
            {
                // Combine the LineCanvas'
                LineCanvas.Merge (subview.LineCanvas);
                subview.LineCanvas.Clear ();
            }

            foreach (KeyValuePair<Point, Cell?> p in LineCanvas.GetCellMap ())
            {
                // Get the entire map
                if (p.Value is { })
                {
                    SetAttribute (p.Value.Value.Attribute ?? ColorScheme!.Normal);
                    Driver.Move (p.Key.X, p.Key.Y);

                    // TODO: #2616 - Support combining sequences that don't normalize
                    Driver.AddRune (p.Value.Value.Rune);
                }
            }

            LineCanvas.Clear ();
        }
    }
    #endregion DrawLineCanvas

    #region DrawComplete

    private void DoDrawComplete ()
    {
        OnDrawComplete ();

        DrawComplete?.Invoke (this, new (Viewport, Viewport));

        // Default implementation does nothing.
    }

    /// <summary>
    ///     Called when the View is completed drawing.
    /// </summary>
    protected virtual void OnDrawComplete () { }

    /// <summary>Raised when the View is completed drawing.</summary>
    /// <remarks>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? DrawComplete;

    #endregion DrawComplete

    #region NeedsDraw

    // TODO: Make _needsDrawRect nullable instead of relying on Empty
    // TODO: If null, it means ?
    // TODO: If Empty, it means no need to redraw
    // TODO: If not Empty, it means the region that needs to be redrawn
    // The viewport-relative region that needs to be redrawn. Marked internal for unit tests.
    internal Rectangle _needsDrawRect = Rectangle.Empty;

    /// <summary>Gets or sets whether the view needs to be redrawn.</summary>
    /// <remarks>
    ///     <para>
    ///         Will be <see langword="true"/> if the <see cref="NeedsLayout"/> property is <see langword="true"/> or if
    ///         any part of the view's <see cref="Viewport"/> needs to be redrawn.
    ///     </para>
    ///     <para>
    ///         Setting has no effect on <see cref="NeedsLayout"/>.
    ///     </para>
    /// </remarks>
    public bool NeedsDraw
    {
        // TODO: Figure out if we can decouple NeedsDraw from NeedsLayout. This is a temporary fix.
        get => _needsDrawRect != Rectangle.Empty || NeedsLayout;
        set
        {
            if (value)
            {
                SetNeedsDraw ();
            }
            else
            {
                ClearNeedsDraw ();
            }
        }
    }

    /// <summary>Gets whether any Subviews need to be redrawn.</summary>
    public bool SubViewNeedsDraw { get; private set; }

    /// <summary>Sets that the <see cref="Viewport"/> of this View needs to be redrawn.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDraw ()
    {
        Rectangle viewport = Viewport;

        if (_needsDrawRect != Rectangle.Empty && viewport.IsEmpty)
        {
            // This handles the case where the view has not been initialized yet
            return;
        }

        SetNeedsDraw (viewport);
    }

    /// <summary>Expands the area of this view needing to be redrawn to include <paramref name="viewPortRelativeRegion"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         The location of <paramref name="viewPortRelativeRegion"/> is relative to the View's <see cref="Viewport"/>.
    ///     </para>
    ///     <para>
    ///         If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), the area to be
    ///         redrawn will be the <paramref name="viewPortRelativeRegion"/>.
    ///     </para>
    /// </remarks>
    /// <param name="viewPortRelativeRegion">The <see cref="Viewport"/>relative region that needs to be redrawn.</param>
    public void SetNeedsDraw (Rectangle viewPortRelativeRegion)
    {
        if (_needsDrawRect.IsEmpty)
        {
            _needsDrawRect = viewPortRelativeRegion;
        }
        else
        {
            int x = Math.Min (Viewport.X, viewPortRelativeRegion.X);
            int y = Math.Min (Viewport.Y, viewPortRelativeRegion.Y);
            int w = Math.Max (Viewport.Width, viewPortRelativeRegion.Width);
            int h = Math.Max (Viewport.Height, viewPortRelativeRegion.Height);
            _needsDrawRect = new (x, y, w, h);
        }

        if (Margin is { } && Margin.Thickness != Thickness.Empty)
        {
            Margin?.SetNeedsDraw ();
        }

        if (Border is { } && Border.Thickness != Thickness.Empty)
        {
            Border?.SetNeedsDraw ();
        }

        if (Padding is { } && Padding.Thickness != Thickness.Empty)
        {
            Padding?.SetNeedsDraw ();
        }

        SuperView?.SetSubViewNeedsDraw ();

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDraw ();
        }

        foreach (View subview in Subviews)
        {
            if (subview.Frame.IntersectsWith (viewPortRelativeRegion))
            {
                Rectangle subviewRegion = Rectangle.Intersect (subview.Frame, viewPortRelativeRegion);
                subviewRegion.X -= subview.Frame.X;
                subviewRegion.Y -= subview.Frame.Y;
                subview.SetNeedsDraw (subviewRegion);
            }
        }
    }

    /// <summary>Sets <see cref="SubViewNeedsDraw"/> to <see langword="true"/> for this View and all Superviews.</summary>
    public void SetSubViewNeedsDraw ()
    {
        SubViewNeedsDraw = true;

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDraw ();
        }

        if (SuperView is { SubViewNeedsDraw: false })
        {
            SuperView.SetSubViewNeedsDraw ();
        }
    }

    /// <summary>Clears <see cref="NeedsDraw"/> and <see cref="SubViewNeedsDraw"/>.</summary>
    protected void ClearNeedsDraw ()
    {
        _needsDrawRect = Rectangle.Empty;
        SubViewNeedsDraw = false;


        if (Margin is { } && Margin.Thickness != Thickness.Empty)
        {
            Margin?.ClearNeedsDraw ();
        }

        if (Border is { } && Border.Thickness != Thickness.Empty)
        {
            Border?.ClearNeedsDraw ();
        }

        if (Padding is { } && Padding.Thickness != Thickness.Empty)
        {
            Padding?.ClearNeedsDraw ();
        }
        foreach (View subview in Subviews)
        {
            subview.ClearNeedsDraw ();
        }
    }

    #endregion NeedsDraw
}
