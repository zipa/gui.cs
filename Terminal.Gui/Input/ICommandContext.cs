#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked. When a <see cref="Command"/> is invoked,
///     a context object is passed to Command handlers. See <see cref="View.AddCommand(Command, CommandImplementation)"/>.
/// </summary>
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }
}
