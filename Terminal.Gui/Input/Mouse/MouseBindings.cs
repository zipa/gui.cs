#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a collection of <see cref="MouseBinding"/> objects bound to a combination of <see cref="MouseFlags"/>.
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
    /// <param name="mouseFlag"></param>
    /// <param name="binding"></param>
    public void Add (MouseFlags mouseFlag, MouseBinding binding)
    {
        if (TryGet (mouseFlag, out MouseBinding _))
        {
            throw new InvalidOperationException (@$"A binding for {mouseFlag} exists ({binding}).");

            //Bindings [key] = binding;
        }


        // IMPORTANT: Add a COPY of the key. This is needed because ConfigurationManager.Apply uses DeepMemberWiseCopy 
        // IMPORTANT: update the memory referenced by the key, and Dictionary uses caching for performance, and thus 
        // IMPORTANT: Apply will update the Dictionary with the new key, but the old key will still be in the dictionary.
        // IMPORTANT: See the ConfigurationManager.Illustrate_DeepMemberWiseCopy_Breaks_Dictionary test for details.
        Bindings.Add (mouseFlag, binding);
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
    /// <param name="mouseFlags">The mouse flags to check.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="mouseFlags"/> is received. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="mouseFlags"/> event will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (MouseFlags mouseFlags, params Command [] commands)
    {
        if (mouseFlags == MouseFlags.None)
        {
            throw new ArgumentException (@"Invalid MouseFlag", nameof (commands));
        }

        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (mouseFlags, out MouseBinding binding))
        {
            throw new InvalidOperationException (@$"A binding for {mouseFlags} exists ({binding}).");
        }

        Add (mouseFlags, new MouseBinding (commands));
    }

    // TODO: Add a dictionary comparer that ignores Scope
    // TODO: This should not be public!
    /// <summary>The collection of <see cref="MouseBinding"/> objects.</summary>
    public Dictionary<MouseFlags, MouseBinding> Bindings { get; } = new ();

    /// <summary>
    ///     Gets the <see cref="MouseFlags"/> that are bound.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<MouseFlags> GetBoundMouseFlags ()
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
        KeyValuePair<MouseFlags, MouseBinding> [] kvps = Bindings
                                                .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                .ToArray ();

        foreach (KeyValuePair<MouseFlags, MouseBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="MouseBinding"/> for the specified combination of <see cref="MouseFlags"/>.</summary>
    /// <param name="mouseFlags"></param>
    /// <returns></returns>
    public MouseBinding Get (MouseFlags mouseFlags)
    {
        if (TryGet (mouseFlags, out MouseBinding binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"{mouseFlags} is not bound.");
    }

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="mouseFlags"/> if it exists.</summary>
    /// <param name="mouseFlags">The key to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="mouseFlags"/> is bound. An empty <see cref="Command"/> array
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

    /// <summary>Gets the first combination of <see cref="MouseFlags"/> bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The first combination of <see cref="MouseFlags"/> bound to the set of commands specified by <paramref name="commands"/>. <see langword="null"/> if the set of caommands was not found.</returns>
    public MouseFlags? GetMouseFlagsFromCommands (params Command [] commands)
    {
        return Bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key;
    }

    /// <summary>Gets combination of <see cref="MouseFlags"/> bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The combination of <see cref="MouseFlags"/> bound to the set of commands specified by <paramref name="commands"/>. An empty list if the set of caommands was not found.</returns>
    public IEnumerable<MouseFlags> GetAllMouseFlagsFromCommands (params Command [] commands)
    {
        return Bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>Removes a <see cref="MouseBinding"/> from the collection.</summary>
    /// <param name="mouseFlags"></param>
    public void Remove (MouseFlags mouseFlags)
    {
        if (!TryGet (mouseFlags, out MouseBinding _))
        {
            return;
        }

        Bindings.Remove (mouseFlags);
    }

    /// <summary>Replaces the commands already bound to a combination of <see cref="MouseFlags"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the combination of <see cref="MouseFlags"/> is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="mouseFlags">The combination of <see cref="MouseFlags"/> bound to the command to be replaced.</param>
    /// <param name="commands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (MouseFlags mouseFlags, params Command [] commands)
    {
        if (TryGet (mouseFlags, out MouseBinding binding))
        {
            binding.Commands = commands;
        }
        else
        {
            Add (mouseFlags, commands);
        }
    }

    /// <summary>Replaces a <see cref="MouseFlags"/> combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldMouseFlags">The <see cref="MouseFlags"/> to be replaced.</param>
    /// <param name="newMouseFlags">The new <see cref="MouseFlags"/> to be used. If <see cref="Key.Empty"/> no action will be taken.</param>
    public void ReplaceKey (MouseFlags oldMouseFlags, MouseFlags newMouseFlags)
    {
        if (!TryGet (oldMouseFlags, out MouseBinding _))
        {
            throw new InvalidOperationException ($"Key {oldMouseFlags} is not bound.");
        }

        MouseBinding value = Bindings [oldMouseFlags];
        Remove (oldMouseFlags);
        Add (newMouseFlags, value);
    }

    /// <summary>Gets the commands bound with the specified <see cref="MouseFlags"/>.</summary>
    /// <remarks></remarks>
    /// <param name="mouseFlags">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified mouse flags, if the mouse flags are
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the mouse flags are bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (MouseFlags mouseFlags, out MouseBinding binding)
    {

        binding = new ([]);

        return Bindings.TryGetValue (mouseFlags, out binding);
    }
}
