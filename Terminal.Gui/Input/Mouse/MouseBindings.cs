#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a collection of <see cref="MouseBinding"/> objects bound to a combination of <see cref="MouseFlags"/>.
/// </summary>
/// <seealso cref="View.MouseBindings"/>
/// <seealso cref="Command"/>
public class MouseBindings : Bindings<MouseFlags, MouseBinding>
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public MouseBindings () : base (
                                    (commands, flags) => new (commands, flags),
                                    EqualityComparer<MouseFlags>.Default)
    { }

    /// <summary>
    ///     Gets combination of <see cref="MouseFlags"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>.
    /// </summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The combination of <see cref="MouseFlags"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>. An empty list if the set of caommands was not found.
    /// </returns>
    public IEnumerable<MouseFlags> GetAllMouseFlagsFromCommands (params Command [] commands)
    {
        return _bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>
    ///     Gets the <see cref="MouseFlags"/> that are bound.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<MouseFlags> GetBoundMouseFlags () { return _bindings.Keys; }

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="mouseFlags"/> if it exists.</summary>
    /// <param name="mouseFlags">The key to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="mouseFlags"/> is bound. An empty <see cref="Command"/>
    ///     array
    ///     if not.
    /// </returns>
    public Command [] GetCommands (MouseFlags mouseFlags)
    {
        if (TryGet (mouseFlags, out MouseBinding bindings))
        {
            return bindings.Commands;
        }

        return [];
    }

    /// <summary>
    ///     Gets the first combination of <see cref="MouseFlags"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>.
    /// </summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The first combination of <see cref="MouseFlags"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>. <see langword="null"/> if the set of caommands was not found.
    /// </returns>
    public MouseFlags GetMouseFlagsFromCommands (params Command [] commands)
    {
        return _bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key;
    }

    /// <summary>Replaces the commands already bound to a combination of <see cref="MouseFlags"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the combination of <see cref="MouseFlags"/> is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEventArgs">The combination of <see cref="MouseFlags"/> bound to the command to be replaced.</param>
    /// <param name="newCommands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (MouseFlags mouseEventArgs, params Command [] newCommands)
    {
        if (TryGet (mouseEventArgs, out MouseBinding binding))
        {
            Remove (mouseEventArgs);
            Add (mouseEventArgs, newCommands);
        }
        else
        {
            Add (mouseEventArgs, newCommands);
        }
    }

    /// <summary>Replaces a <see cref="MouseFlags"/> combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldMouseFlags">The <see cref="MouseFlags"/> to be replaced.</param>
    /// <param name="newMouseFlags">
    ///     The new <see cref="MouseFlags"/> to be used. If <see cref="Key.Empty"/> no action
    ///     will be taken.
    /// </param>
    public void ReplaceMouseFlag (MouseFlags oldMouseFlags, MouseFlags newMouseFlags)
    {
        if (newMouseFlags == MouseFlags.None)
        {
            throw new ArgumentException (@"Invalid MouseFlag", nameof (newMouseFlags));
        }

        if (TryGet (oldMouseFlags, out MouseBinding binding))
        {
            Remove (oldMouseFlags);
            Add (newMouseFlags, binding);
        }
        else
        {
            Add (newMouseFlags, binding);
        }
    }

    /// <summary>Gets the commands bound with the specified <see cref="MouseFlags"/>.</summary>
    /// <remarks></remarks>
    /// <param name="mouseEventArgs">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified mouse flags, if the mouse flags are
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the mouse flags are bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (MouseFlags mouseEventArgs, out MouseBinding binding)
    {
        binding = new ([], mouseEventArgs);

        return _bindings.TryGetValue (mouseEventArgs, out binding);
    }
}
