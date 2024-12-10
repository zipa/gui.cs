#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a collection of <see cref="KeyBinding"/> objects bound to a <see cref="Key"/>.
/// </summary>
/// <seealso cref="Application.KeyBindings"/>
/// <seealso cref="View.KeyBindings"/>
/// <seealso cref="Command"/>
public class KeyBindings : Bindings<Key, KeyBinding>
{
    /// <summary>Initializes a new instance bound to <paramref name="target"/>.</summary>
    public KeyBindings (View? target) : base (
                                              (commands, key) => new (commands),
                                              new KeyEqualityComparer ())
    {
        Target = target;
    }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> on the View
    ///         specified by <paramref name="target"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="target">
    ///     The View the commands will be invoked on. If <see langword="null"/>, the key will be bound to
    ///     <see cref="Application"/>.
    /// </param>
    /// <param name="commands">
    ///     The command to invoked on the <see paramref="target"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, View? target, params Command [] commands)
    {
        KeyBinding binding = new (commands, target);
        Add (key, binding);
    }

    /// <summary>
    ///     Gets the bindings.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<Key, KeyBinding>> GetBindings () { return _bindings; }

    /// <summary>
    ///     Gets the keys that are bound.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Key> GetBoundKeys () { return _bindings.Keys; }

    /// <summary>
    ///     The view that the <see cref="KeyBindings"/> are bound to.
    /// </summary>
    /// <remarks>
    ///     If <see langword="null"/> the KeyBindings object is being used for Application.KeyBindings.
    /// </remarks>
    public View? Target { get; init; }

    /// <summary>Removes all <see cref="KeyBinding"/> objects from the collection.</summary>
    public void Clear () { _bindings.Clear (); }

    /// <summary>
    ///     Removes all key bindings that trigger the given command set. Views can have multiple different keys bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<Key, KeyBinding> [] kvps = _bindings
                                                .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                .ToArray ();

        foreach (KeyValuePair<Key, KeyBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="KeyBinding"/> for the specified <see cref="Key"/>.</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public KeyBinding Get (Key key)
    {
        if (TryGet (key, out KeyBinding binding))
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
        if (TryGet (key, out KeyBinding bindings))
        {
            return bindings.Commands;
        }

        return [];
    }

    /// <summary>Gets the first Key bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The first <see cref="Key"/> bound to the set of commands specified by <paramref name="commands"/>.
    ///     <see langword="null"/> if the set of caommands was not found.
    /// </returns>
    public Key? GetKeyFromCommands (params Command [] commands) { return _bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key; }

    /// <summary>Gets Keys bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The <see cref="Key"/>s bound to the set of commands specified by <paramref name="commands"/>. An empty list if the
    ///     set of caommands was not found.
    /// </returns>
    public IEnumerable<Key> GetKeysFromCommands (params Command [] commands)
    {
        return _bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>Removes a <see cref="KeyBinding"/> from the collection.</summary>
    /// <param name="key"></param>
    public void Remove (Key key)
    {
        if (!TryGet (key, out KeyBinding _))
        {
            return;
        }

        _bindings.Remove (key);
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
        if (TryGet (key, out KeyBinding binding))
        {
            Remove (key);
            Add (key, newCommands);
        }
        else
        {
            Add (key, newCommands);
        }
    }

    /// <summary>Replaces a key combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldKey">The key to be replaced.</param>
    /// <param name="newKey">The new key to be used. If <see cref="Key.Empty"/> no action will be taken.</param>
    public void ReplaceKey (Key oldKey, Key newKey)
    {
        if (!newKey.IsValid)
        {
            throw new InvalidOperationException ($"Key {newKey} is is not valid.");
        }

        if (newKey == Key.Empty)
        {
            Remove (oldKey);

            return;
        }

        if (TryGet (oldKey, out KeyBinding binding))
        {
            Remove (oldKey);
            Add (newKey, binding);
        }
        else
        {
            Add (newKey, binding);
        }
    }

    /// <summary>Gets the commands bound with the specified Key.</summary>
    /// <remarks></remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified Key, if the Key is
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the Key is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (Key key, out KeyBinding binding)
    {
        binding = new ([], null);

        if (key.IsValid)
        {
            return _bindings.TryGetValue (key, out binding);
        }

        return false;
    }
}
