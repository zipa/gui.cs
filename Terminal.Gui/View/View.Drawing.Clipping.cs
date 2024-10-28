#nullable enable
namespace Terminal.Gui;

public partial class View
{
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
    public Region? SetClip ()
    {
        if (Driver is null)
        {
            return null;
        }

        Region previous = Driver.Clip ?? new (Application.Screen);

        // Clamp the Clip to the entire visible area
        Rectangle clip = Rectangle.Intersect (ViewportToScreen (Viewport with { Location = Point.Empty }), previous.GetBounds());

        if (ViewportSettings.HasFlag (ViewportSettings.ClipContentOnly))
        {
            // Clamp the Clip to the just content area that is within the viewport
            Rectangle visibleContent = ViewportToScreen (new Rectangle (new (-Viewport.X, -Viewport.Y), GetContentSize ()));
            clip = Rectangle.Intersect (clip, visibleContent);
        }

        Driver.Clip = new (clip);// !.Complement(clip);

        return previous;
    }

}
