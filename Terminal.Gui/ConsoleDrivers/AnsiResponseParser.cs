#nullable enable

using System.Runtime.ConstrainedExecution;

namespace Terminal.Gui;

internal abstract class AnsiResponseParserBase : IAnsiResponseParser
{
    /// <summary>
    /// Responses we are expecting to come in.
    /// </summary>
    protected readonly List<(string terminator, Action<string> response)> expectedResponses = new ();

    /// <summary>
    /// Collection of responses that we <see cref="StopExpecting"/>.
    /// </summary>
    protected readonly List<(string terminator, Action<string> response)> lateResponses = new ();

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

    /// <summary>
    ///     When <see cref="State"/> was last changed.
    /// </summary>
    public DateTime StateChangedAt { get; private set; } = DateTime.Now;

    protected readonly HashSet<char> _knownTerminators = new ();

    public AnsiResponseParserBase ()
    {
        // These all are valid terminators on ansi responses,
        // see CSI in https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s
        _knownTerminators.Add ('@');
        _knownTerminators.Add ('A');
        _knownTerminators.Add ('B');
        _knownTerminators.Add ('C');
        _knownTerminators.Add ('D');
        _knownTerminators.Add ('E');
        _knownTerminators.Add ('F');
        _knownTerminators.Add ('G');
        _knownTerminators.Add ('G');
        _knownTerminators.Add ('H');
        _knownTerminators.Add ('I');
        _knownTerminators.Add ('J');
        _knownTerminators.Add ('K');
        _knownTerminators.Add ('L');
        _knownTerminators.Add ('M');

        // No - N or O
        _knownTerminators.Add ('P');
        _knownTerminators.Add ('Q');
        _knownTerminators.Add ('R');
        _knownTerminators.Add ('S');
        _knownTerminators.Add ('T');
        _knownTerminators.Add ('W');
        _knownTerminators.Add ('X');
        _knownTerminators.Add ('Z');

        _knownTerminators.Add ('^');
        _knownTerminators.Add ('`');
        _knownTerminators.Add ('~');

        _knownTerminators.Add ('a');
        _knownTerminators.Add ('b');
        _knownTerminators.Add ('c');
        _knownTerminators.Add ('d');
        _knownTerminators.Add ('e');
        _knownTerminators.Add ('f');
        _knownTerminators.Add ('g');
        _knownTerminators.Add ('h');
        _knownTerminators.Add ('i');

        _knownTerminators.Add ('l');
        _knownTerminators.Add ('m');
        _knownTerminators.Add ('n');

        _knownTerminators.Add ('p');
        _knownTerminators.Add ('q');
        _knownTerminators.Add ('r');
        _knownTerminators.Add ('s');
        _knownTerminators.Add ('t');
        _knownTerminators.Add ('u');
        _knownTerminators.Add ('v');
        _knownTerminators.Add ('w');
        _knownTerminators.Add ('x');
        _knownTerminators.Add ('y');
        _knownTerminators.Add ('z');
    }

    protected void ResetState ()
    {
        State = AnsiResponseParserState.Normal;
        ClearHeld ();
    }

    public abstract void ClearHeld ();
    protected abstract string HeldToString ();
    protected abstract IEnumerable<object> HeldToObjects ();
    protected abstract void AddToHeld (object o);

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
                        AddToHeld (currentObj); // Hold the escape character
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
                        AddToHeld (currentObj); // Hold the new escape
                    }
                    else if (currentChar == '[')
                    {
                        // Detected '[', transition to InResponse state
                        State = AnsiResponseParserState.InResponse;
                        AddToHeld (currentObj); // Hold the '['
                    }
                    else
                    {
                        // Invalid sequence, release held characters and reset to Normal
                        ReleaseHeld (appendOutput);
                        appendOutput (currentObj); // Add current character
                    }

                    break;

                case AnsiResponseParserState.InResponse:
                    AddToHeld (currentObj);

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
        foreach (object o in HeldToObjects ())
        {
            appendOutput (o);
        }

        State = newState;
        ClearHeld ();
    }

    // Common response handler logic
    protected bool ShouldReleaseHeldContent ()
    {
        string cur = HeldToString ();

        // Look for an expected response for what is accumulated so far (since Esc)
        if (MatchResponse (cur, expectedResponses))
        {
            return false;
        }

        // Also try looking for late requests
        if (MatchResponse (cur, lateResponses))
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

    private bool MatchResponse (string cur, List<(string terminator, Action<string> response)> valueTuples)
    {
        // Check for expected responses
        var matchingResponse = valueTuples.FirstOrDefault (r => cur.EndsWith (r.terminator));

        if (matchingResponse.response != null)
        {
            DispatchResponse (matchingResponse.response);
            expectedResponses.Remove (matchingResponse);

            return true;
        }

        return false;
    }

    protected void DispatchResponse (Action<string> response)
    {
        response?.Invoke (HeldToString ());
        ResetState ();
    }

    /// <inheritdoc />
    public void ExpectResponse (string terminator, Action<string> response) { expectedResponses.Add ((terminator, response)); }

    /// <inheritdoc />
    public bool IsExpecting (string requestTerminator)
    {
        // If any of the new terminator matches any existing terminators characters it's a collision so true.
        return expectedResponses.Any (r => r.terminator.Intersect (requestTerminator).Any());
    }

    /// <inheritdoc />
    public void StopExpecting (string requestTerminator)
    {
        var removed = expectedResponses.Where (r => r.terminator == requestTerminator).ToArray ();

        foreach (var r in removed)
        {
            expectedResponses.Remove (r);
            lateResponses.Add (r);
        }
    }
}

internal class AnsiResponseParser<T> : AnsiResponseParserBase
{
    private readonly List<Tuple<char, T>> held = new ();

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
        foreach (Tuple<char, T> h in held.ToArray ())
        {
            yield return h;
        }

        ResetState ();
    }

    public override void ClearHeld () { held.Clear (); }

    protected override string HeldToString () { return new (held.Select (h => h.Item1).ToArray ()); }

    protected override IEnumerable<object> HeldToObjects () { return held; }

    protected override void AddToHeld (object o) { held.Add ((Tuple<char, T>)o); }
}

internal class AnsiResponseParser : AnsiResponseParserBase
{
    private readonly StringBuilder held = new ();

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
        var output = held.ToString ();
        ResetState ();

        return output;
    }

    public override void ClearHeld () { held.Clear (); }

    protected override string HeldToString () { return held.ToString (); }

    protected override IEnumerable<object> HeldToObjects () { return held.ToString ().Select (c => (object)c).ToArray (); }

    protected override void AddToHeld (object o) { held.Append ((char)o); }
}


/// <summary>
///     Describes an ongoing ANSI request sent to the console.
///     Use <see cref="ResponseReceived"/> to handle the response
///     when console answers the request.
/// </summary>
public class AnsiEscapeSequenceRequest
{
    /// <summary>
    ///     Request to send e.g. see
    ///     <see>
    ///         <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///     </see>
    /// </summary>
    public required string Request { get; init; }

    /// <summary>
    ///     Invoked when the console responds with an ANSI response code that matches the
    ///     <see cref="Terminator"/>
    /// </summary>
    public Action<string> ResponseReceived;

    /// <summary>
    ///     <para>
    ///         The terminator that uniquely identifies the type of response as responded
    ///         by the console. e.g. for
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Request</cref>
    ///         </see>
    ///         the terminator is
    ///         <see>
    ///             <cref>EscSeqUtils.CSI_SendDeviceAttributes.Terminator</cref>
    ///         </see>
    ///         .
    ///     </para>
    ///     <para>
    ///         After sending a request, the first response with matching terminator will be matched
    ///         to the oldest outstanding request.
    ///     </para>
    /// </summary>
    public required string Terminator { get; init; }

    /// <summary>
    /// Sends the <see cref="Request"/> to the raw output stream of the current <see cref="ConsoleDriver"/>.
    /// Only call this method from the main UI thread. You should use <see cref="AnsiRequestScheduler"/> if
    /// sending many requests.
    /// </summary>
    public void Send ()
    {
        Application.Driver?.RawWrite (Request);
    }
}
