#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked. <see cref="CommandContext{TBindingType}"/> inherits from this interface.
///     When a <see cref="Command"/> is invoked,
///     a context object is passed to Command handlers as an <see cref="ICommandContext"/> reference.
/// </summary>
/// <seealso cref="View.AddCommand(Command, CommandImplementation)"/>.
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }
}
