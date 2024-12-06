namespace Terminal.Gui;

/// <summary>
///     Defines the scope of a <see cref="Command"/> that has been bound to a key with
///     <see cref="KeyBindings.Add(Key, Terminal.Gui.Command[])"/>.
/// </summary>
/// <remarks>
///     <para>Key bindings are scoped to the most-focused view (<see cref="Focused"/>) by default.</para>
/// </remarks>
/// <seealso cref="Application.KeyBindings"/>
/// <seealso cref="View.KeyBindings"/>
/// <seealso cref="Command"/>
[Flags]
public enum KeyBindingScope
{
    /// <summary>The key binding is disabled.</summary>
    Disabled = 0,

    /// <summary>
    ///     The key binding is scoped to just the view that has focus.
    ///     <para>
    ///     </para>
    /// </summary>
    /// <seealso cref="View.KeyBindings"/>
    Focused = 1,

    /// <summary>
    ///     The key binding is scoped to the View's Superview hierarchy and the bound <see cref="Command"/>s will be invoked
    ///     even when the View does not have
    ///     focus, as
    ///     long as some View up the SuperView hierachy does have focus. This is typically used for <see cref="View.HotKey"/>s.
    ///     <para>
    ///         The View must be visible.
    ///     </para>
    ///     <para>
    ///         HotKey-scoped key bindings are only invoked if the key down event was not handled by the focused view or
    ///         any of its subviews.
    ///     </para>
    /// </summary>
    /// <seealso cref="View.KeyBindings"/>
    /// <seeals cref="View.HotKey"/>
    HotKey = 2
}
