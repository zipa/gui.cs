namespace Terminal.Gui;

public partial class View
{
    #region Drawing Primitives

    /// <summary>Moves the drawing cursor to the specified <see cref="Viewport"/>-relative location in the view.</summary>
    /// <remarks>
    ///     <para>
    ///         If the provided coordinates are outside the visible content area, this method does nothing.
    ///     </para>
    ///     <para>
    ///         The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    public bool Move (int col, int row)
    {
        if (Driver is null || Driver?.Rows == 0)
        {
            return false;
        }

        if (col < 0 || row < 0 || col >= Viewport.Width || row >= Viewport.Height)
        {
            return false;
        }

        Point screen = ViewportToScreen (new Point (col, row));
        Driver?.Move (screen.X, screen.Y);

        return true;
    }

    /// <summary>Draws the specified character in the specified viewport-relative column and row of the View.</summary>
    /// <para>
    ///     If the provided coordinates are outside the visible content area, this method does nothing.
    /// </para>
    /// <remarks>
    ///     The top-left corner of the visible content area is <c>ViewPort.Location</c>.
    /// </remarks>
    /// <param name="col">Column (viewport-relative).</param>
    /// <param name="row">Row (viewport-relative).</param>
    /// <param name="rune">The Rune.</param>
    public void AddRune (int col, int row, Rune rune)
    {
        if (Move (col, row))
        {
            Driver?.AddRune (rune);
        }
    }

    /// <summary>Utility function to draw strings that contain a hotkey.</summary>
    /// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
    /// <param name="hotColor">Hot color.</param>
    /// <param name="normalColor">Normal color.</param>
    /// <remarks>
    ///     <para>
    ///         The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by
    ///         default.
    ///     </para>
    ///     <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
    /// </remarks>
    public void DrawHotString (string text, Attribute hotColor, Attribute normalColor)
    {
        Rune hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
        SetAttribute (normalColor);

        foreach (Rune rune in text.EnumerateRunes ())
        {
            if (rune == new Rune (hotkeySpec.Value))
            {
                SetAttribute (hotColor);

                continue;
            }

            Application.Driver?.AddRune (rune);
            SetAttribute (normalColor);
        }
    }

    /// <summary>
    ///     Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused"
    ///     state.
    /// </summary>
    /// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
    /// <param name="focused">
    ///     If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise
    ///     the regular ones.
    /// </param>
    public void DrawHotString (string text, bool focused)
    {
        if (focused)
        {
            DrawHotString (text, GetHotFocusColor (), GetFocusColor ());
        }
        else
        {
            DrawHotString (
                           text,
                           Enabled ? GetHotNormalColor () : ColorScheme!.Disabled,
                           Enabled ? GetNormalColor () : ColorScheme!.Disabled
                          );
        }
    }

    /// <summary>Fills the specified <see cref="Viewport"/>-relative rectangle with the specified color.</summary>
    /// <param name="rect">The Viewport-relative rectangle to clear.</param>
    /// <param name="color">The color to use to fill the rectangle. If not provided, the Normal background color will be used.</param>
    public void FillRect (Rectangle rect, Color? color = null)
    {
        if (Driver is null)
        {
            return;
        }

        // Get screen-relative coords
        Rectangle toClear = ViewportToScreen (rect);

        Rectangle prevClip = Driver.Clip;

        Driver.Clip = Rectangle.Intersect (prevClip, ViewportToScreen (Viewport with { Location = new (0, 0) }));

        Attribute prev = SetAttribute (new (color ?? GetNormalColor ().Background));
        Driver.FillRect (toClear);
        SetAttribute (prev);

        Driver.Clip = prevClip;
    }

    #endregion Drawing Primitives
}
