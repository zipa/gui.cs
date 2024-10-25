#nullable enable
using System.Diagnostics;

namespace Terminal.Gui;

public partial class View // Drawing APIs
{
    /// <summary>
    ///     Draws the view if it needs to be drawn.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will only be drawn if it is visible, and has any of <see cref="NeedsDisplay"/>,
    ///         <see cref="SubViewNeedsDisplay"/>,
    ///         or <see cref="NeedsLayout"/> set.
    ///     </para>
    ///     <para>
    ///         // TODO: Add docs for the drawing process.
    ///     </para>
    /// </remarks>
    public void Draw ()
    {
        if (!CanBeVisible (this) || (!NeedsDisplay && !SubViewNeedsDisplay))
        {
            return;
        }

        DoDrawAdornments ();

        // Set the color scheme for the view after adornments have been drawn
        if (ColorScheme is { })
        {
            Driver?.SetAttribute (GetNormalColor ());
        }

        // By default, we clip to the viewport preventing drawing outside the viewport
        // We also clip to the content, but if a developer wants to draw outside the viewport, they can do
        // so via settings. SetClip honors the ViewportSettings.DisableVisibleContentClipping flag.
        Rectangle prevClip = SetClip ();

        DoClearViewport (Viewport);
        DoDrawText (Viewport);
        DoDrawContent (Viewport);
        DoDrawSubviews (Viewport);

        // Restore the clip before rendering the line canvas and adornment subviews
        // because they may draw outside the viewport.
        if (Driver is { })
        {
            Driver.Clip = prevClip;
        }

        DoRenderLineCanvas ();
        DoDrawAdornmentSubViews ();
        ClearNeedsDisplay ();

        // We're done
        DoDrawComplete (Viewport);
    }

    #region DrawAdornments

    private void DoDrawAdornmentSubViews ()
    {
        // This causes the Adornment's subviews to be REDRAWN
        // TODO: Figure out how to make this more efficient
        if (Margin?.Subviews is { })
        {
            foreach (View subview in Margin.Subviews)
            {
                subview.SetNeedsDisplay ();
            }

            Margin?.DoDrawSubviews (Margin.Viewport);
        }

        if (Border?.Subviews is { })
        {
            foreach (View subview in Border.Subviews)
            {
                subview.SetNeedsDisplay ();
            }

            Border?.DoDrawSubviews (Border.Viewport);
        }

        if (Padding?.Subviews is { })
        {
            foreach (View subview in Padding.Subviews)
            {
                subview.SetNeedsDisplay ();
            }

            Padding?.DoDrawSubviews (Padding.Viewport);
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
        Margin?.Draw ();
        Border?.Draw ();
        Padding?.Draw ();
    }

    /// <summary>
    ///     Called when the View's Adornments are to be drawn. Prepares <see cref="View.LineCanvas"/>. If
    ///     <see cref="SuperViewRendersLineCanvas"/> is true, only the
    ///     <see cref="LineCanvas"/> of this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is
    ///     false (the default), this method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
    /// </summary>
    /// <returns><see langword="true"/> to stop further drawing of the Adornments.</returns>
    protected virtual bool OnDrawingAdornments () { return false; }

    #endregion DrawAdornments

    #region ClearViewport

    private void DoClearViewport (Rectangle viewport)
    {
        Debug.Assert (viewport == Viewport);

        if (OnClearingViewport (Viewport))
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        ClearingViewport?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        ClearViewport ();
    }

    /// <summary>
    ///     Called when the <see cref="Viewport"/> is to be cleared.
    /// </summary>
    /// <param name="viewport"></param>
    /// <returns><see langword="true"/> to stop further clearing.</returns>
    protected virtual bool OnClearingViewport (Rectangle viewport) { return false; }

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

        Rectangle prevClip = Driver.Clip;

        if (ViewportSettings.HasFlag (ViewportSettings.ClearContentOnly))
        {
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            toClear = Rectangle.Intersect (toClear, visibleContent);
        }

        Attribute prev = Driver.SetAttribute (GetNormalColor ());
        Driver.FillRect (toClear);
        Driver.SetAttribute (prev);

        Driver.Clip = prevClip;
        SetNeedsDisplay ();
    }

    #endregion ClearViewport

    #region DrawText

    private void DoDrawText (Rectangle viewport)
    {
        Debug.Assert (viewport == Viewport);

        if (OnDrawingText (Viewport))
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawingText?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        DrawText ();
    }

    /// <summary>
    ///     Called when the <see cref="Text"/> of the View is to be drawn.
    /// </summary>
    /// <param name="viewport"></param>
    /// <returns><see langword="true"/> to stop further drawing of  <see cref="Text"/>.</returns>
    protected virtual bool OnDrawingText (Rectangle viewport) { return false; }

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
        SetSubViewNeedsDisplay ();
        
        Debug.WriteLine($"DrawText: {Id}");
    }

    #endregion DrawText

    #region DrawContent
    
    private void DoDrawContent (Rectangle viewport)
    {
        Debug.Assert (viewport == Viewport);

        if (OnDrawingContent (Viewport))
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
    protected virtual bool OnDrawingContent (Rectangle viewport) { return false; }

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

    private void DoDrawSubviews (Rectangle viewport)
    {
        Debug.Assert (viewport == Viewport);

        if (OnDrawingSubviews (Viewport))
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawingSubviews?.Invoke (this, dev);

        if (dev.Cancel)
        {
            return;
        }

        DrawSubviews ();
    }

    /// <summary>
    ///     Called when the <see cref="Subviews"/> are to be drawn.
    /// </summary>
    /// <param name="viewport"></param>
    /// <returns><see langword="true"/> to stop further drawing of <see cref="Subviews"/>.</returns>
    protected virtual bool OnDrawingSubviews (Rectangle viewport) { return false; }

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
        if (_subviews is null || !SubViewNeedsDisplay)
        {
            return;
        }

        IEnumerable<View> subviewsNeedingDraw = _subviews.Where (view => view.Visible && (view.NeedsDisplay || view.SubViewNeedsDisplay));

        foreach (View view in subviewsNeedingDraw)
        {
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
                    Driver.SetAttribute (p.Value.Value.Attribute ?? ColorScheme!.Normal);
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
                    Driver.SetAttribute (p.Value.Value.Attribute ?? ColorScheme!.Normal);
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

    private void DoDrawComplete (Rectangle viewport)
    {
        Debug.Assert (viewport == Viewport);

        if (OnDrawComplete (Viewport))
        {
            return;
        }

        var dev = new DrawEventArgs (Viewport, Rectangle.Empty);
        DrawComplete?.Invoke (this, dev);

        if (dev.Cancel)
        { }

        // Default implementation does nothing.
    }

    /// <summary>
    ///     Called when the View is completed drawing.
    /// </summary>
    /// <param name="viewport"></param>
    protected virtual bool OnDrawComplete (Rectangle viewport) { return false; }

    /// <summary>Raised when the View is completed drawing.</summary>
    /// <remarks>
    /// </remarks>
    public event EventHandler<DrawEventArgs>? DrawComplete;

    #endregion DrawComplete

    #region NeedsDisplay

    // TODO: Make _needsDisplayRect nullable instead of relying on Empty
    // TODO: If null, it means ?
    // TODO: If Empty, it means no need to redraw
    // TODO: If not Empty, it means the region that needs to be redrawn
    // The viewport-relative region that needs to be redrawn. Marked internal for unit tests.
    internal Rectangle _needsDisplayRect = Rectangle.Empty;

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
    public bool NeedsDisplay
    {
        // TODO: Figure out if we can decouple NeedsDisplay from NeedsLayout. This is a temporary fix.
        get => _needsDisplayRect != Rectangle.Empty || NeedsLayout;
        set
        {
            if (value)
            {
                SetNeedsDisplay ();
            }
            else
            {
                ClearNeedsDisplay ();
            }
        }
    }

    /// <summary>Gets whether any Subviews need to be redrawn.</summary>
    public bool SubViewNeedsDisplay { get; private set; }

    /// <summary>Sets that the <see cref="Viewport"/> of this View needs to be redrawn.</summary>
    /// <remarks>
    ///     If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>), this method
    ///     does nothing.
    /// </remarks>
    public void SetNeedsDisplay ()
    {
        Rectangle viewport = Viewport;

        if (_needsDisplayRect != Rectangle.Empty && viewport.IsEmpty)
        {
            // This handles the case where the view has not been initialized yet
            return;
        }

        SetNeedsDisplay (viewport);
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
    public void SetNeedsDisplay (Rectangle viewPortRelativeRegion)
    {
        if (_needsDisplayRect.IsEmpty)
        {
            _needsDisplayRect = viewPortRelativeRegion;
        }
        else
        {
            int x = Math.Min (Viewport.X, viewPortRelativeRegion.X);
            int y = Math.Min (Viewport.Y, viewPortRelativeRegion.Y);
            int w = Math.Max (Viewport.Width, viewPortRelativeRegion.Width);
            int h = Math.Max (Viewport.Height, viewPortRelativeRegion.Height);
            _needsDisplayRect = new (x, y, w, h);
        }

        Margin?.SetNeedsDisplay ();
        Border?.SetNeedsDisplay ();
        Padding?.SetNeedsDisplay ();

        SuperView?.SetSubViewNeedsDisplay ();

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDisplay ();
        }

        foreach (View subview in Subviews)
        {
            if (subview.Frame.IntersectsWith (viewPortRelativeRegion))
            {
                Rectangle subviewRegion = Rectangle.Intersect (subview.Frame, viewPortRelativeRegion);
                subviewRegion.X -= subview.Frame.X;
                subviewRegion.Y -= subview.Frame.Y;
                subview.SetNeedsDisplay (subviewRegion);
            }
        }
    }

    /// <summary>Sets <see cref="SubViewNeedsDisplay"/> to <see langword="true"/> for this View and all Superviews.</summary>
    public void SetSubViewNeedsDisplay ()
    {
        SubViewNeedsDisplay = true;

        if (this is Adornment adornment)
        {
            adornment.Parent?.SetSubViewNeedsDisplay ();
        }

        if (SuperView is { SubViewNeedsDisplay: false })
        {
            SuperView.SetSubViewNeedsDisplay ();
        }
    }

    /// <summary>Clears <see cref="NeedsDisplay"/> and <see cref="SubViewNeedsDisplay"/>.</summary>
    protected void ClearNeedsDisplay ()
    {
        _needsDisplayRect = Rectangle.Empty;
        SubViewNeedsDisplay = false;

        Margin?.ClearNeedsDisplay ();
        Border?.ClearNeedsDisplay ();
        Padding?.ClearNeedsDisplay ();

        foreach (View subview in Subviews)
        {
            subview.ClearNeedsDisplay ();
        }
    }

    #endregion NeedsDisplay
}
