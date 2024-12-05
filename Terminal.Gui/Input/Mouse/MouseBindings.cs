#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a collection of <see cref="MouseBinding"/> objects bound to a combination of <see cref="MouseEventArgs"/>.
/// </summary>
/// <seealso cref="View.MouseBindings"/>
/// <seealso cref="Command"/>
public class MouseBindings
{
    /// <summary>
    ///     Initializes a new instance. This constructor is used when the <see cref="MouseBindings"/> are not bound to a
    ///     <see cref="View"/>. This is used for Application.MouseBindings and unit tests.
    /// </summary>
    public MouseBindings () { }

    /// <summary>Adds a <see cref="MouseBinding"/> to the collection.</summary>
    /// <param name="mouseEvent"></param>
    /// <param name="binding"></param>
    public void Add (MouseEventArgs mouseEvent, MouseBinding binding)
    {
        if (TryGet (mouseEvent, out MouseBinding _))
        {
            throw new InvalidOperationException (@$"A binding for {mouseEvent} exists ({binding}).");
        }


        // IMPORTANT: Add a COPY of the key. This is needed because ConfigurationManager.Apply uses DeepMemberWiseCopy 
        // IMPORTANT: update the memory referenced by the key, and Dictionary uses caching for performance, and thus 
        // IMPORTANT: Apply will update the Dictionary with the new key, but the old key will still be in the dictionary.
        // IMPORTANT: See the ConfigurationManager.Illustrate_DeepMemberWiseCopy_Breaks_Dictionary test for details.
        Bindings.Add (mouseEvent, binding);
    }

    /// <summary>
    ///     <para>Adds a new mouse flag combination that will trigger the commands in <paramref name="commands"/>.</para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Commands are only ever applied to the current <see cref="View"/> (i.e. this feature cannot be used to switch
    ///     focus to another view and perform multiple commands there).
    /// </remarks>
    /// <param name="mouseEvents">The mouse flags to check.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="mouseEvents"/> is received. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="mouseEvents"/> event will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (MouseEventArgs mouseEvents, params Command [] commands)
    {
        if (mouseEvents.Flags == MouseFlags.None)
        {
            throw new ArgumentException (@"Invalid MouseFlag", nameof (commands));
        }

        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (mouseEvents, out MouseBinding binding))
        {
            throw new InvalidOperationException (@$"A binding for {mouseEvents} exists ({binding}).");
        }

        Add (mouseEvents, new MouseBinding (commands));
    }

    // TODO: Add a dictionary comparer that ignores Scope
    // TODO: This should not be public!
    /// <summary>The collection of <see cref="MouseBinding"/> objects.</summary>
    public Dictionary<MouseEventArgs, MouseBinding> Bindings { get; } = new ();

    /// <summary>
    ///     Gets the <see cref="MouseEventArgs"/> that are bound.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<MouseEventArgs> GetBoundMouseEventArgs ()
    {
        return Bindings.Keys;
    }

    /// <summary>Removes all <see cref="MouseBinding"/> objects from the collection.</summary>
    public void Clear () { Bindings.Clear (); }

    /// <summary>
    ///     Removes all bindings that trigger the given command set. Views can have multiple different events bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<MouseEventArgs, MouseBinding> [] kvps = Bindings
                                                .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                .ToArray ();

        foreach (KeyValuePair<MouseEventArgs, MouseBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="MouseBinding"/> for the specified combination of <see cref="MouseEventArgs"/>.</summary>
    /// <param name="mouseEvents"></param>
    /// <returns></returns>
    public MouseBinding Get (MouseEventArgs mouseEvents)
    {
        if (TryGet (mouseEvents, out MouseBinding binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"{mouseEvents} is not bound.");
    }

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="mouseEvents"/> if it exists.</summary>
    /// <param name="mouseEvents">The key to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="mouseEvents"/> is bound. An empty <see cref="Command"/> array
    ///     if not.
    /// </returns>
    public Command [] GetCommands (MouseEventArgs mouseEvents)
    {
        if (TryGet (mouseEvents, out MouseBinding bindings))
        {
            return bindings.Commands;
        }

        return [];
    }

    /// <summary>Gets the first combination of <see cref="MouseEventArgs"/> bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The first combination of <see cref="MouseEventArgs"/> bound to the set of commands specified by <paramref name="commands"/>. <see langword="null"/> if the set of caommands was not found.</returns>
    public MouseEventArgs? GetMouseEventArgsFromCommands (params Command [] commands)
    {
        return Bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key;
    }

    /// <summary>Gets combination of <see cref="MouseEventArgs"/> bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The combination of <see cref="MouseEventArgs"/> bound to the set of commands specified by <paramref name="commands"/>. An empty list if the set of caommands was not found.</returns>
    public IEnumerable<MouseEventArgs> GetAllMouseEventArgsFromCommands (params Command [] commands)
    {
        return Bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>Removes a <see cref="MouseBinding"/> from the collection.</summary>
    /// <param name="mouseEvents"></param>
    public void Remove (MouseEventArgs mouseEvents)
    {
        if (!TryGet (mouseEvents, out MouseBinding _))
        {
            return;
        }

        Bindings.Remove (mouseEvents);
    }

    /// <summary>Replaces the commands already bound to a combination of <see cref="MouseEventArgs"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the combination of <see cref="MouseEventArgs"/> is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="mouseEvents">The combination of <see cref="MouseEventArgs"/> bound to the command to be replaced.</param>
    /// <param name="commands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (MouseEventArgs mouseEvents, params Command [] commands)
    {
        if (TryGet (mouseEvents, out MouseBinding binding))
        {
            binding.Commands = commands;
        }
        else
        {
            Add (mouseEvents, commands);
        }
    }

    /// <summary>Replaces a <see cref="MouseEventArgs"/> combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldMouseEventArgs">The <see cref="MouseEventArgs"/> to be replaced.</param>
    /// <param name="newMouseEventArgs">The new <see cref="MouseEventArgs"/> to be used. If <see cref="Key.Empty"/> no action will be taken.</param>
    public void ReplaceKey (MouseEventArgs oldMouseEventArgs, MouseEventArgs newMouseEventArgs)
    {
        if (!TryGet (oldMouseEventArgs, out MouseBinding _))
        {
            throw new InvalidOperationException ($"Key {oldMouseEventArgs} is not bound.");
        }

        MouseBinding value = Bindings [oldMouseEventArgs];
        Remove (oldMouseEventArgs);
        Add (newMouseEventArgs, value);
    }

    /// <summary>Gets the commands bound with the specified <see cref="MouseEventArgs"/>.</summary>
    /// <remarks></remarks>
    /// <param name="mouseEvents">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified mouse flags, if the mouse flags are
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the mouse flags are bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (MouseEventArgs mouseEvents, out MouseBinding binding)
    {

        binding = new ([]);

        return Bindings.TryGetValue (mouseEvents, out binding);
    }
}
