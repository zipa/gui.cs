#nullable enable

namespace Terminal.Gui;

/// <summary>
/// Provides a collection of <see cref="Command"/> objects for mouse events.
/// </summary>
/// <seealso cref="Command"/>
public record struct MouseBinding : IInputBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this mouse binding will invoke.</param>
    /// <param name="mouseFlags">The mouse flags that trigger this binding.</param>
    public MouseBinding (Command [] commands, MouseFlags mouseFlags)
    {
        Commands = commands;

        MouseEventArgs = new MouseEventArgs()
        {
            Flags = mouseFlags
        };
    }

    /// <summary>The commands this key binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <summary>
    ///     The mouse event arguments.
    /// </summary>
    public MouseEventArgs? MouseEventArgs { get; set; }
}
