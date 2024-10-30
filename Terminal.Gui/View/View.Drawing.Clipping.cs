#nullable enable
namespace Terminal.Gui;

public partial class View
{
    internal Region? SetClipToFrame ()
    {
        if (Driver is null)
        {
            return null;
        }

        Region previous = Driver.Clip ?? new (Application.Screen);

        Region frameRegion = Driver.Clip!.Clone ();
        // Translate viewportRegion to screen-relative coords
        Rectangle screenRect = FrameToScreen ();
        frameRegion.Intersect (screenRect);

        if (this is Adornment adornment && adornment.Thickness != Thickness.Empty)
        {
            // Ensure adornments can't draw outside thier thickness
            frameRegion.Exclude (adornment.Thickness.GetInside (Frame));
        }

        Application.SetClip (frameRegion);

        return previous;

    }

    /// <summary>Sets the <see cref="ConsoleDriver"/>'s clip region to <see cref="Viewport"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         By default, the clip rectangle is set to the intersection of the current clip region and the
    ///         <see cref="Viewport"/>. This ensures that drawing is constrained to the viewport, but allows
    ///         content to be drawn beyond the viewport.
    ///     </para>
    ///     <para>
    ///         If <see cref="ViewportSettings"/> has <see cref="Gui.ViewportSettings.ClipContentOnly"/> set, clipping will be
    ///         applied to just the visible content area.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The current screen-relative clip region, which can be then re-applied by setting
    ///     <see cref="ConsoleDriver.Clip"/>.
    /// </returns>
    public Region? SetClipToViewport ()
    {
        if (Driver is null)
        {
            return null;
        }

        Region previous = Driver.Clip ?? new (Application.Screen);

        Region viewportRegion = Driver.Clip!.Clone ();

        Rectangle viewport = ViewportToScreen (new Rectangle (Point.Empty, Viewport.Size));
        viewportRegion?.Intersect (viewport);

        if (ViewportSettings.HasFlag (ViewportSettings.ClipContentOnly))
        {
            // Clamp the Clip to the just content area that is within the viewport
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            viewportRegion?.Intersect (visibleContent);
        }

        if (this is Adornment adornment && adornment.Thickness != Thickness.Empty)
        {
            // Ensure adornments can't draw outside their thickness
            viewportRegion?.Exclude (adornment.Thickness.GetInside (viewport));
        }

        Application.SetClip (viewportRegion);

        return previous;
    }

    /// <summary>Gets the view-relative clip region.</summary>
    public Region? GetClip ()
    {
        // get just the portion of the application clip that is within this view's Viewport
        if (Driver is null)
        {
            return null;
        }

        // Get our Viewport in screen coordinates
        Rectangle screen = ViewportToScreen (Viewport with { Location = Point.Empty });

        // Get the clip region in screen coordinates
        Region? clip = Driver.Clip;
        if (clip is null)
        {
            return null;
        }
        Region? previous = Driver.Clip;
        clip = clip.Clone ();
        clip.Intersect (screen);
        return clip;
    }
}
