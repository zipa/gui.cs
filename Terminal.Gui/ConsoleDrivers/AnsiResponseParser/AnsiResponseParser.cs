#nullable enable

using System.Runtime.ConstrainedExecution;

namespace Terminal.Gui;


internal abstract class AnsiResponseParserBase : IAnsiResponseParser
{
    /// <summary>
    /// Responses we are expecting to come in.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> expectedResponses = new ();

    /// <summary>
    /// Collection of responses that we <see cref="StopExpecting"/>.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> lateResponses = new ();

    /// <summary>
    /// Responses that you want to look out for that will come in continuously e.g. mouse events.
    /// Key is the terminator.
    /// </summary>
    protected readonly List<AnsiResponseExpectation> persistentExpectations = new ();

    private AnsiResponseParserState _state = AnsiResponseParserState.Normal;

    // Current state of the parser
    public AnsiResponseParserState State
    {
        get => _state;
        protected set
        {
            StateChangedAt = DateTime.Now;
            _state = value;
        }
    }

    protected readonly IHeld heldContent;

    /// <summary>
    ///     When <see cref="State"/> was last changed.
    /// </summary>
    public DateTime StateChangedAt { get; private set; } = DateTime.Now;

    // These all are valid terminators on ansi responses,
    // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
    // No - N or O
    protected readonly HashSet<char> _knownTerminators = new (new []
    {
        '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
        // No - N or O
        'P', 'Q', 'R', 'S', 'T', 'W', 'X', 'Z',
        '^', '`', '~',
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
        'l', 'm', 'n',
        'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
    });

    protected AnsiResponseParserBase (IHeld heldContent)
    {
        this.heldContent = heldContent;
    }

    protected void ResetState ()
    {
        State = AnsiResponseParserState.Normal;
        heldContent.ClearHeld ();
    }

    /// <summary>
    ///     Processes an input collection of objects <paramref name="inputLength"/> long.
    ///     You must provide the indexers to return the objects and the action to append
    ///     to output stream.
    /// </summary>
    /// <param name="getCharAtIndex">The character representation of element i of your input collection</param>
    /// <param name="getObjectAtIndex">The actual element in the collection (e.g. char or Tuple&lt;char,T&gt;)</param>
    /// <param name="appendOutput">
    ///     Action to invoke when parser confirms an element of the current collection or a previous
    ///     call's collection should be appended to the current output (i.e. append to your output List/StringBuilder).
    /// </param>
    /// <param name="inputLength">The total number of elements in your collection</param>
    protected void ProcessInputBase (
        Func<int, char> getCharAtIndex,
        Func<int, object> getObjectAtIndex,
        Action<object> appendOutput,
        int inputLength
    )
    {
        var index = 0; // Tracks position in the input string

        while (index < inputLength)
        {
            char currentChar = getCharAtIndex (index);
            object currentObj = getObjectAtIndex (index);

            bool isEscape = currentChar == '\x1B';

            switch (State)
            {
                case AnsiResponseParserState.Normal:
                    if (isEscape)
                    {
                        // Escape character detected, move to ExpectingBracket state
                        State = AnsiResponseParserState.ExpectingBracket;
                        heldContent.AddToHeld (currentObj); // Hold the escape character
                    }
                    else
                    {
                        // Normal character, append to output
                        appendOutput (currentObj);
                    }

                    break;

                case AnsiResponseParserState.ExpectingBracket:
                    if (isEscape)
                    {
                        // Second escape so we must release first
                        ReleaseHeld (appendOutput, AnsiResponseParserState.ExpectingBracket);
                        heldContent.AddToHeld (currentObj); // Hold the new escape
                    }
                    else if (currentChar == '[')
                    {
                        // Detected '[', transition to InResponse state
                        State = AnsiResponseParserState.InResponse;
                        heldContent.AddToHeld (currentObj); // Hold the '['
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        ReleaseHeld (appendOutput);
                        appendOutput (currentObj); // Add current character
                    }

                    break;

                case AnsiResponseParserState.InResponse:
                    heldContent.AddToHeld (currentObj);

                    // Check if the held content should be released
                    if (ShouldReleaseHeldContent ())
                    {
                        ReleaseHeld (appendOutput);
                    }

                    break;
            }

            index++;
        }
    }

    private void ReleaseHeld (Action<object> appendOutput, AnsiResponseParserState newState = AnsiResponseParserState.Normal)
    {
        foreach (object o in heldContent.HeldToObjects ())
        {
            appendOutput (o);
        }

        State = newState;
        heldContent.ClearHeld ();
    }

    // Common response handler logic
    protected bool ShouldReleaseHeldContent ()
    {
        string cur = heldContent.HeldToString ();

        // Look for an expected response for what is accumulated so far (since Esc)
        if (MatchResponse (cur,
                           expectedResponses,
                           invokeCallback: true,
                           removeExpectation: true))
        {
            return false;
        }

        // Also try looking for late requests - in which case we do not invoke but still swallow content to avoid corrupting downstream
        if (MatchResponse (cur,
                           lateResponses,
                           invokeCallback: false,
                           removeExpectation: true))
        {
            return false;
        }

        // Look for persistent requests
        if (MatchResponse (cur,
                           persistentExpectations,
                           invokeCallback: true,
                           removeExpectation: false))
        {
            return false;
        }

        // Finally if it is a valid ansi response but not one we are expect (e.g. its mouse activity)
        // then we can release it back to input processing stream
        if (_knownTerminators.Contains (cur.Last ()) && cur.StartsWith (EscSeqUtils.CSI))
        {
            // Detected a response that was not expected
            return true;
        }

        return false; // Continue accumulating
    }


    private bool MatchResponse (string cur, List<AnsiResponseExpectation> collection, bool invokeCallback, bool removeExpectation)
    {
        // Check for expected responses
        var matchingResponse = collection.FirstOrDefault (r => r.Matches (cur));

        if (matchingResponse?.Response != null)
        {
            if (invokeCallback)
            {
                matchingResponse.Response?.Invoke (heldContent.HeldToString ());
            }
            ResetState ();

            if (removeExpectation)
            {
                collection.Remove (matchingResponse);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void ExpectResponse (string terminator, Action<string> response, bool persistent)
    {
        if (persistent)
        {
            persistentExpectations.Add (new (terminator, response));
        }
        else
        {
            expectedResponses.Add (new (terminator, response));
        }
    }

    /// <inheritdoc />
    public bool IsExpecting (string terminator)
    {
        // If any of the new terminator matches any existing terminators characters it's a collision so true.
        return expectedResponses.Any (r => r.Terminator.Intersect (terminator).Any ());
    }

    /// <inheritdoc />
    public void StopExpecting (string terminator, bool persistent)
    {
        if (persistent)
        {
            persistentExpectations.RemoveAll (r => r.Matches (terminator));
        }
        else
        {
            var removed = expectedResponses.Where (r => r.Terminator == terminator).ToArray ();

            foreach (var r in removed)
            {
                expectedResponses.Remove (r);
                lateResponses.Add (r);
            }
        }
    }
}

internal interface IHeld
{
    void ClearHeld ();
    string HeldToString ();
    IEnumerable<object> HeldToObjects ();
    void AddToHeld (object o);
}

internal class StringHeld : IHeld
{
    private readonly StringBuilder held = new ();

    public void ClearHeld () => held.Clear ();
    public string HeldToString () => held.ToString ();
    public IEnumerable<object> HeldToObjects () => held.ToString ().Select (c => (object)c);
    public void AddToHeld (object o) => held.Append ((char)o);
}

internal class GenericHeld<T> : IHeld
{
    private readonly List<Tuple<char, T>> held = new ();

    public void ClearHeld () => held.Clear ();
    public string HeldToString () => new (held.Select (h => h.Item1).ToArray ());
    public IEnumerable<object> HeldToObjects () => held;
    public void AddToHeld (object o) => held.Add ((Tuple<char, T>)o);
}

internal class AnsiResponseParser<T> : AnsiResponseParserBase
{
    public AnsiResponseParser () : base (new GenericHeld<T> ()) { }

    public IEnumerable<Tuple<char, T>> ProcessInput (params Tuple<char, T> [] input)
    {
        List<Tuple<char, T>> output = new List<Tuple<char, T>> ();

        ProcessInputBase (
                          i => input [i].Item1,
                          i => input [i],
                          c => output.Add ((Tuple<char, T>)c),
                          input.Length);

        return output;
    }

    public IEnumerable<Tuple<char, T>> Release ()
    {
        foreach (Tuple<char, T> h in (IEnumerable<Tuple<char, T>>)heldContent.HeldToObjects ())
        {
            yield return h;
        }

        ResetState ();
    }
}

internal class AnsiResponseParser : AnsiResponseParserBase
{
    public AnsiResponseParser () : base (new StringHeld ()) { }

    public string ProcessInput (string input)
    {
        var output = new StringBuilder ();

        ProcessInputBase (
                          i => input [i],
                          i => input [i], // For string there is no T so object is same as char
                          c => output.Append ((char)c),
                          input.Length);

        return output.ToString ();
    }

    public string Release ()
    {
        var output = heldContent.HeldToString ();
        ResetState ();

        return output;
    }
}