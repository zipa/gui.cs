#nullable enable
namespace Terminal.Gui;

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
/// <summary>
///     Provides context for a <see cref="Command"/> that is being invoked.
/// </summary>
/// <remarks>
///     <para>
///         To define a <see cref="Command"/> that is invoked with context,
///         use <see cref="View.AddCommand(Command,Func{CommandContext,System.Nullable{bool}})"/>.
///     </para>
/// </remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
public record struct CommandContext<TBindingType>
{
    /// <summary>
    ///     Initializes a new instance of <see cref="CommandContext"/> with the specified <see cref="Command"/>,
    /// </summary>
    /// <param name="command"></param>
    /// <param name="key"></param>
    /// <param name="binding"></param>
    /// <param name="data"></param>
    public CommandContext (Command command, TBindingType? binding, object? data = null)
    {
        Command = command;
        Binding = binding;
        Data = data;
    }

    /// <summary>
    ///     The <see cref="Command"/> that is being invoked.
    /// </summary>
    public Command Command { get; set; }

    /// <summary>
    /// The keyboard or mouse minding that was used to invoke the <see cref="Command"/>, if any.
    /// </summary>
    public TBindingType? Binding { get; set; }

    /// <summary>
    ///     Arbitrary data.
    /// </summary>
    public object? Data { get; set; }
}
