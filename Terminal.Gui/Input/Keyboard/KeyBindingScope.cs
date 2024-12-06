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
    Focused = 1
}
