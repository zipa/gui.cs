#nullable enable
using System;

namespace Terminal.Gui;

/// <summary>
///     Abstract base class for <see cref="KeyBindings"/> and <see cref="MouseBindings"/>.
/// </summary>
/// <typeparam name="TEvent">The type of the event (e.g. <see cref="Key"/> or <see cref="MouseEventArgs"/>).</typeparam>
/// <typeparam name="TBinding">The binding type (e.g. <see cref="KeyBinding"/>).</typeparam>
public abstract class Bindings<TEvent, TBinding>  where TBinding : IInputBinding, new() where TEvent : notnull
{
    /// <summary>
    ///     The bindings.
    /// </summary>
    protected readonly Dictionary<TEvent, TBinding> _bindings;

    private readonly Func<Command [], TEvent, TBinding> _constructBinding;

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    /// <param name="constructBinding"></param>
    /// <param name="equalityComparer"></param>
    protected Bindings (Func<Command [], TEvent, TBinding> constructBinding, IEqualityComparer<TEvent> equalityComparer)
    {
        _constructBinding = constructBinding;
        _bindings = new (equalityComparer);
    }

    /// <summary>Adds a <see cref="TEvent"/> bound to <see cref="TBinding"/> to the collection.</summary>
    /// <param name="eventArgs"></param>
    /// <param name="binding"></param>
    public void Add (TEvent eventArgs, TBinding binding)
    { 
        if (TryGet (eventArgs, out TBinding _))
        {
            throw new InvalidOperationException (@$"A binding for {eventArgs} exists ({binding}).");
        }

        // IMPORTANT: Add a COPY of the mouseEventArgs. This is needed because ConfigurationManager.Apply uses DeepMemberWiseCopy 
        // IMPORTANT: update the memory referenced by the key, and Dictionary uses caching for performance, and thus 
        // IMPORTANT: Apply will update the Dictionary with the new mouseEventArgs, but the old mouseEventArgs will still be in the dictionary.
        // IMPORTANT: See the ConfigurationManager.Illustrate_DeepMemberWiseCopy_Breaks_Dictionary test for details.
        _bindings.Add (eventArgs, binding);
    }


    /// <summary>Gets the commands bound with the specified <see cref="TEvent"/>.</summary>
    /// <remarks></remarks>
    /// <param name="eventArgs">The args to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified mouse flags, if the mouse flags are
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the mouse flags are bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (TEvent eventArgs, out TBinding? binding)
    {
         return _bindings.TryGetValue (eventArgs, out binding);
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
    /// <param name="eventArgs">The mouse flags to check.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="eventArgs"/> is received. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="eventArgs"/> event
    ///     will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (TEvent eventArgs, params Command [] commands)
    {
        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (eventArgs, out var binding))
        {
            throw new InvalidOperationException (@$"A binding for {eventArgs} exists ({binding}).");
        }

        Add (eventArgs, _constructBinding(commands,eventArgs));
    }

    /// <summary>
    ///     Gets the bindings.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TEvent, TBinding>> GetBindings ()
    {
        return _bindings;
    }

    /// <summary>Removes all <see cref="TEvent"/> objects from the collection.</summary>
    public void Clear () { _bindings.Clear (); }

    /// <summary>
    ///     Removes all bindings that trigger the given command set. Views can have multiple different events bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<TEvent, TBinding> [] kvps = _bindings
                                            .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                            .ToArray ();

        foreach (KeyValuePair<TEvent, TBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="TBinding"/> for the specified <see cref="TEvent"/>.</summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    public TBinding? Get (TEvent eventArgs)
    {
        if (TryGet (eventArgs, out var binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"{eventArgs} is not bound.");
    }


    /// <summary>Removes a <see cref="MouseBinding"/> from the collection.</summary>
    /// <param name="mouseEventArgs"></param>
    public void Remove (TEvent mouseEventArgs)
    {
        if (!TryGet (mouseEventArgs, out var _))
        {
            return;
        }

        _bindings.Remove (mouseEventArgs);
    }
}
