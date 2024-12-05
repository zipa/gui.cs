#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes the context in which a <see cref="Command"/> is being invoked.
/// </summary>
public interface ICommandContext
{
    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }

    // TODO: Remove this property. With CommandContext<TBindingType> being a generic type, there should be no need for arbitrary data.
    /// <summary>
    ///     Arbitrary data.
    /// </summary>
    public object? Data { get; set; }
}
