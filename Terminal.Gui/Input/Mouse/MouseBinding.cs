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
    public MouseBinding (Command [] commands)
    {
        Commands = commands;
    }

    /// <summary>The commands this key binding will invoke.</summary>
    public Command [] Commands { get; set; }
}
