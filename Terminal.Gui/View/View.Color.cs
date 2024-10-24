namespace Terminal.Gui;

public partial class View
{
    private ColorScheme _colorScheme;

    /// <summary>The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s color scheme.</summary>
    public virtual ColorScheme ColorScheme
    {
        get
        {
            if (_colorScheme is null)
            {
                return SuperView?.ColorScheme;
            }

            return _colorScheme;
        }
        set
        {
            if (_colorScheme != value)
            {
                _colorScheme = value;

                if (Border is { } && Border.LineStyle != LineStyle.None && Border.ColorScheme is { })
                {
                    Border.ColorScheme = _colorScheme;
                }

                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetFocusColor ()
    {
        ColorScheme cs = ColorScheme;

        if (cs is null)
        {
            cs = new ();
        }

        return Enabled ? GetColor (cs.Focus) : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotFocusColor ()
    {
        ColorScheme cs = ColorScheme ?? new ();

        return Enabled ? GetColor (cs.HotFocus) : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Terminal.Gui.ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetHotNormalColor ()
    {
        ColorScheme cs = ColorScheme;

        if (cs is null)
        {
            cs = new ();
        }

        return Enabled ? GetColor (cs.HotNormal) : cs.Disabled;
    }

    /// <summary>Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.</summary>
    /// <returns>
    ///     <see cref="Terminal.Gui.ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/> or
    ///     <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>. If it's
    ///     overridden can return other values.
    /// </returns>
    public virtual Attribute GetNormalColor ()
    {
        ColorScheme cs = ColorScheme;

        if (cs is null)
        {
            cs = new ();
        }

        Attribute disabled = new (cs.Disabled.Foreground, cs.Disabled.Background);

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        {
            disabled = new (disabled.Foreground.GetDarkerColor (), disabled.Background.GetDarkerColor ());
        }

        return Enabled ? GetColor (cs.Normal) : disabled;
    }

    private Attribute GetColor (Attribute inputAttribute)
    {
        Attribute attr = inputAttribute;

        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Hover) && _hovering)
        {
            attr = new (attr.Foreground.GetDarkerColor (), attr.Background.GetDarkerColor ());
        }

        return attr;
    }
}
