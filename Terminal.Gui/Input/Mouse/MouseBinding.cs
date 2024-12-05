#nullable enable

namespace Terminal.Gui;

/// <summary>
/// Provides a collection of <see cref="Command"/> objects for mouse events.
/// </summary>
/// <seealso cref="Command"/>
public record struct MouseBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this mouse binding will invoke.</param>
    /// <param name="mouseEventArgs">The mouse event arguments, to be passed as context.</param>
    public MouseBinding (Command [] commands, MouseEventArgs? mouseEventArgs)
    {
        Commands = commands;
        MouseEventArgs = mouseEventArgs;
    }

    /// <summary>The commands this key binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <summary>
    ///     The mouse event arguments.
    /// </summary>
    public MouseEventArgs? MouseEventArgs { get; set; }
}
