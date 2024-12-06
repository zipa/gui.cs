#nullable enable

// These classes use a key binding system based on the design implemented in Scintilla.Net which is an
// MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

namespace Terminal.Gui;

/// <summary>
/// Provides a collection of <see cref="Command"/> objects that are scoped to the <see cref="Application"/>.
/// </summary>
/// <seealso cref="Command"/>
public record struct ApplicationKeyBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    public ApplicationKeyBinding (Command [] commands)
    {
        Commands = commands;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    /// <param name="target">The view the Application-scoped key binding is bound to. If <see langword="null"/> the commands will be invoked on
    /// the <see cref="Application"/>.</param>
    public ApplicationKeyBinding (Command [] commands, View? target)
    {
        Commands = commands;
        Target = target;
    }

    /// <summary>The commands this binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <summary>
    ///     The Key that is bound to the <see cref="Commands"/>.
    /// </summary>
    public Key? Key { get; set; }

    /// <summary>The view the Application-scoped key binding is bound to.</summary>
    public View? Target { get; set; }
}
