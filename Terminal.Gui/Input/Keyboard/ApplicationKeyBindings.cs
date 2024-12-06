#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a collection of <see cref="ApplicationKeyBinding"/> objects bound to a <see cref="Key"/>.
/// </summary>
/// <remarks>
///     This is used for <see cref="Application.KeyBindings"/>.
/// </remarks>
/// <seealso cref="Application.KeyBindings"/>
/// <seealso cref="Command"/>
public class ApplicationKeyBindings
{
    /// <summary>
    ///     Initializes a new instance. This constructor is used when the <see cref="ApplicationKeyBindings"/> are not bound to a
    ///     <see cref="View"/>. This is used for <see cref="Application.KeyBindings"/>.
    /// </summary>
    public ApplicationKeyBindings () { }

    /// <summary>Adds a <see cref="ApplicationKeyBinding"/> to the collection.</summary>
    /// <param name="key"></param>
    /// <param name="binding"></param>
    public void Add (Key key, ApplicationKeyBinding binding)
    {
        if (TryGet (key, out ApplicationKeyBinding _))
        {
            throw new InvalidOperationException (@$"A key binding for {key} exists ({binding}).");
        }

        // IMPORTANT: Add a COPY of the key. This is needed because ConfigurationManager.Apply uses DeepMemberWiseCopy 
        // IMPORTANT: update the memory referenced by the key, and Dictionary uses caching for performance, and thus 
        // IMPORTANT: Apply will update the Dictionary with the new key, but the old key will still be in the dictionary.
        // IMPORTANT: See the ConfigurationManager.Illustrate_DeepMemberWiseCopy_Breaks_Dictionary test for details.
        Bindings.Add (new (key), binding);
    }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> on the View
    ///         specified by <paramref name="boundView"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="boundView">The View the commands will be invoked on. If <see langword="null"/>, the key will be bound to <see cref="Application"/>.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see paramref="boundView"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect. 
    /// </param>
    public void Add (Key key, View? boundView, params Command [] commands)
    {
        ApplicationKeyBinding binding = new (commands, boundView);
        Add (key, binding);
    }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> on <see cref="Application"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="commands">
    ///     The commands to invoke on <see cref="Application"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, params Command [] commands)
    {
        ApplicationKeyBinding binding = new (commands, null);
        Add (key, binding);
    }

    // TODO: This should not be public!
    /// <summary>The collection of <see cref="ApplicationKeyBinding"/> objects.</summary>
    public Dictionary<Key, ApplicationKeyBinding> Bindings { get; } = new (new KeyEqualityComparer ());

    /// <summary>
    ///     Gets the keys that are bound.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Key> GetBoundKeys ()
    {
        return Bindings.Keys;
    }

    /// <summary>Removes all <see cref="ApplicationKeyBinding"/> objects from the collection.</summary>
    public void Clear () { Bindings.Clear (); }

    /// <summary>
    ///     Removes all key bindings that trigger the given command set. Views can have multiple different keys bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<Key, ApplicationKeyBinding> [] kvps = Bindings
                                                .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                .ToArray ();

        foreach (KeyValuePair<Key, ApplicationKeyBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="ApplicationKeyBinding"/> for the specified <see cref="Key"/>.</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public ApplicationKeyBinding Get (Key key)
    {
        if (TryGet (key, out ApplicationKeyBinding binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"Key {key} is not bound.");
    }

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="key"/> if it exists.</summary>
    /// <param name="key">The key to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="key"/> is bound. An empty <see cref="Command"/> array
    ///     if not.
    /// </returns>
    public Command [] GetCommands (Key key)
    {
        if (TryGet (key, out ApplicationKeyBinding bindings))
        {
            return bindings.Commands;
        }

        return [];
    }

    /// <summary>Gets the first Key bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The first <see cref="Key"/> bound to the set of commands specified by <paramref name="commands"/>. <see langword="null"/> if the set of caommands was not found.</returns>
    public Key? GetKeyFromCommands (params Command [] commands)
    {
        return Bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key;
    }

    /// <summary>Gets Keys bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The <see cref="Key"/>s bound to the set of commands specified by <paramref name="commands"/>. An empty list if the set of caommands was not found.</returns>
    public IEnumerable<Key> GetKeysFromCommands (params Command [] commands)
    {
        return Bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>Removes a <see cref="ApplicationKeyBinding"/> from the collection.</summary>
    /// <param name="key"></param>
    public void Remove (Key key)
    {
        if (!TryGet (key, out ApplicationKeyBinding _))
        {
            return;
        }

        Bindings.Remove (key);
    }

    /// <summary>Replaces the commands already bound to a key.</summary>
    /// <remarks>
    ///     <para>
    ///         If the key is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="key">The key bound to the command to be replaced.</param>
    /// <param name="newCommands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (Key key, params Command [] newCommands)
    {
        if (TryGet (key, out ApplicationKeyBinding binding))
        {
            Remove (key);
            Add (key, binding.Target, newCommands);

            return;
        }

        throw new InvalidOperationException ($"Key {key} is not bound.");
    }

    /// <summary>Replaces a key combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldKey">The key to be replaced.</param>
    /// <param name="newKey">The new key to be used. If <see cref="Key.Empty"/> no action will be taken.</param>
    public void ReplaceKey (Key oldKey, Key newKey)
    {
        if (!TryGet (oldKey, out ApplicationKeyBinding _))
        {
            throw new InvalidOperationException ($"Key {oldKey} is not bound.");
        }

        if (!newKey.IsValid)
        {
            throw new InvalidOperationException ($"Key {newKey} is is not valid.");
        }

        ApplicationKeyBinding binding = Bindings [oldKey];
        Remove (oldKey);
        Add (newKey, binding);
    }

    /// <summary>Gets the commands bound with the specified Key.</summary>
    /// <remarks></remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified Key, if the Key is
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the Key is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (Key key, out ApplicationKeyBinding binding)
    {
        binding = new ([], null);

        if (key.IsValid)
        {
            return Bindings.TryGetValue (key, out binding);
        }

        return false;
    }
}
