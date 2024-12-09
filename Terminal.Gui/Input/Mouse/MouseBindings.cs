#nullable enable
using System.Collections.Generic;

namespace Terminal.Gui;

public abstract class Bindings<TKey, TBind>  where TBind : IInputBinding, new()
{
    protected readonly Dictionary<TKey, TBind> _bindings = new ();
    private readonly Func<Command [], TKey, TBind> _constructBinding;

    protected Bindings (Func<Command [], TKey, TBind> constructBinding)
    {
        _constructBinding = constructBinding;
    }

    /// <summary>Adds a <see cref="MouseBinding"/> to the collection.</summary>
    /// <param name="mouseEventArgs"></param>
    /// <param name="binding"></param>
    public void Add (TKey mouseEventArgs, TBind binding)
    {
        if (TryGet (mouseEventArgs, out TBind _))
        {
            throw new InvalidOperationException (@$"A binding for {mouseEventArgs} exists ({binding}).");
        }

        // IMPORTANT: Add a COPY of the mouseEventArgs. This is needed because ConfigurationManager.Apply uses DeepMemberWiseCopy 
        // IMPORTANT: update the memory referenced by the key, and Dictionary uses caching for performance, and thus 
        // IMPORTANT: Apply will update the Dictionary with the new mouseEventArgs, but the old mouseEventArgs will still be in the dictionary.
        // IMPORTANT: See the ConfigurationManager.Illustrate_DeepMemberWiseCopy_Breaks_Dictionary test for details.
        _bindings.Add (mouseEventArgs, binding);
    }


    /// <summary>Gets the commands bound with the specified <see cref="MouseFlags"/>.</summary>
    /// <remarks></remarks>
    /// <param name="mouseEventArgs">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified mouse flags, if the mouse flags are
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the mouse flags are bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (TKey mouseEventArgs, out TBind? binding)
    {
        return _bindings.TryGetValue (mouseEventArgs, out binding);
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
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="mouseFlags"/> event
    ///     will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (TKey mouseFlags, params Command [] commands)
    {
        if (EqualityComparer<TKey>.Default.Equals (mouseFlags, default))
        {
            throw new ArgumentException (@"Invalid MouseFlag", nameof (mouseFlags));
        }

        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (mouseFlags, out var binding))
        {
            throw new InvalidOperationException (@$"A binding for {mouseFlags} exists ({binding}).");
        }

        Add (mouseFlags, _constructBinding(commands,mouseFlags));
    }

    /// <summary>
    ///     Gets the bindings.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TKey, TBind>> GetBindings ()
    {
        return _bindings;
    }

    /// <summary>Removes all <see cref="MouseBinding"/> objects from the collection.</summary>
    public void Clear () { _bindings.Clear (); }

    /// <summary>
    ///     Removes all bindings that trigger the given command set. Views can have multiple different events bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<TKey, TBind> [] kvps = _bindings
                                                         .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                         .ToArray ();

        foreach (KeyValuePair<TKey, TBind> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="MouseBinding"/> for the specified combination of <see cref="MouseFlags"/>.</summary>
    /// <param name="mouseEventArgs"></param>
    /// <returns></returns>
    public TBind? Get (TKey mouseEventArgs)
    {
        if (TryGet (mouseEventArgs, out var binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"{mouseEventArgs} is not bound.");
    }


    /// <summary>Removes a <see cref="MouseBinding"/> from the collection.</summary>
    /// <param name="mouseEventArgs"></param>
    public void Remove (TKey mouseEventArgs)
    {
        if (!TryGet (mouseEventArgs, out var _))
        {
            return;
        }

        _bindings.Remove (mouseEventArgs);
    }
}

/// <summary>
///     Provides a collection of <see cref="MouseBinding"/> objects bound to a combination of <see cref="MouseFlags"/>.
/// </summary>
/// <seealso cref="View.MouseBindings"/>
/// <seealso cref="Command"/>
public class MouseBindings : Bindings<MouseFlags,MouseBinding>
{
    /// <summary>
    ///     Initializes a new instance. This constructor is used when the <see cref="MouseBindings"/> are not bound to a
    ///     <see cref="View"/>. This is used for Application.MouseBindings and unit tests.
    /// </summary>
    public MouseBindings ():base(
                                 (commands, flags)=> new MouseBinding (commands, flags)) { }

    

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
