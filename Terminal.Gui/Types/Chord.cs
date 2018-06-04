//
// Chord.cs: sequences of key sequences that trigger the execution of a command
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// A chord represents a series of key combinations that when entered
	/// in order will trigger the execution of a command.   
	/// </summary>
	/// <remarks>
	/// <para>
	///   Chords are created either by passing a string describing the chord,
	///   or by passing an array of SimpleKeyChord that precisely describe the
	///   key combinations to activate the action.
	/// </para>
	/// <para>
	///   When passing a string, the input takes prefixes followed by the 
	///   letter.   For example the "Control-" an "C-" are used to define 
	///   control sequences (like, Control-C), "Alt-", "A-", "M-" are all
	///   used to define alt-sequences, the letter F followed by a number
	///   is used for function keys, any other scenarios are used as regular 
	///   letters
	/// </para>
	/// </remarks>
	public class Chord {
		// "Control-x" - control x
		// "q-Control" - the string "control"
		// "alt-x" alt x
		// "meta-x" alt x
		// "M-x" alt x
		// "A-x" Alt x
		// "Fn" Function key
		// 
		static KeyEvent [] Parse (ustring [] sequences)
		{
			var list = new List<KeyEvent> ();

			foreach (var sequence in sequences) {
				if (sequence.Length == 0)
					Err (sequence, 0);

				var lower = sequence.ToLower ();
				if (lower.StartsWith ("c-"))
					Add (sequence, 2, control: true);
				else if (lower.StartsWith ("control-"))
					Add (sequence, 8, control: true);
				else if (lower.StartsWith ("m-"))
					Add (sequence, 2, alt: true);
				else if (lower.StartsWith ("meta-"))
					Add (sequence, 5, alt: true);
				else if (lower.StartsWith ("alt-"))
					Add (sequence, 4, alt: true);
				else if (lower.StartsWith ("a-"))
					Add (sequence, 2, alt: true);
				else if (sequence.StartsWith ("F")) {
					if (sequence.Length < 2)
						Err (sequence, 2);
				} else if (uint.TryParse (sequence.Substring (1).ToString (), out var fkey) && fkey > 0) {
					list.Add (new KeyEvent ((Key)(Key.F1 + fkey - 1)));
				} else {
					list.Add (new KeyEvent ((Key)(uint)GetRune (sequence, 0)));
					           
					//list.Add (new SimpleKeyChord ((uint)1);
				}

			}

			return list.ToArray ();

			Rune GetRune (ustring text, int index)
			{
				(var rune, var size) = Utf8.DecodeRune (text, index);
				if (rune == Rune.Error)
					Err (text, index);
				return rune;
			}

			void Add (ustring sequence, int p, bool control = false, bool alt = false)
			{
				if (p >= sequence.Length)
					Err (sequence, p);
				var rune = GetRune (sequence, p);
				if (control) {
					// Rune must be within a..z range
					if (rune >= 'a' || rune <= 'z')
						rune = rune - 'a' + 'A';
					if (rune < 'A' || rune > 'Z')
						throw new ArgumentException ("control must be followed by  a letter" + sequence.ToString ());
					list.Add (new KeyEvent ((Key)(rune - 'A' + 1)));
				} else {
					rune = rune | (uint)(alt ? Key.AltMask : 0);
					list.Add (new KeyEvent ((Key)(uint)rune));
				}
			}

			void Err (ustring text, int pos) =>
				throw new ArgumentException ($"Could not decode sequence in {text} at {pos} ");
		}

		// This is enforced to contains at least one element
		KeyEvent [] description;
		internal Action action;
		internal KeyEvent [] Description => description;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Chord"/> 
		/// class from an array of KeyDescriptions
		/// </summary>
		/// <param name="description">Array of KeyEvents.</param>
		/// <param name="action">Action to perform when there is a sequence match.</param>
		public Chord (KeyEvent [] description, Action action)
		{
			if (description == null) 
				throw new ArgumentException ("Invalid description, either null or it was not possible to parse a useful result out of the provided sequence");
			if (description.Length == 0)
				throw new ArgumentException ("Invalid description, it contains no elements");
			this.description = description;
			this.action = action;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Chord"/> 
		/// class from a string that contains a space-separated list of matching
		/// keys.
		/// </summary>
		/// <param name="sequence">Sequence string, values must be space separated.</param>
		/// <param name="action">Action to perform when there is a sequence match.</param>
		public Chord (string sequence, Action action) : this (ustring.Make (sequence), action)
		{
		}

		static ustring space = ustring.Make (' ');
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Chord"/> 
		/// class from a string that contains a space-separated list of matching
		/// keys.
		/// </summary>
		/// <param name="sequence">Sequence string, values must be space separated.</param>
		/// <param name="action">Action to perform when there is a sequence match.</param>
		public Chord (ustring sequence, Action action) : this (Parse (sequence.Split (space)), action)
		{
		}
	}

	/// <summary>
	/// Chords is used to track one or more chords and to track their execution
	/// </summary>
	/// <remarks>
	/// <code>
	/// // Binds the sequence control-x control-f to run the OpenFile command
	/// var x = new Chords ();
	/// x.Add ("C-x c-f", OpenFile);
	/// </code>
	/// <para>
	/// If there are conflicting definitions, the first definition that matches
	/// is executed.
	/// </para>
	/// </remarks>
	public class Chords {
		List<Chord> chords;

		// The sequence so far.
		List<KeyEvent> sequence;

		public Chords ()
		{
		}

		List<Chord> ChordStorage {
			get {
				if (chords == null)
					chords = new List<Chord> ();
				return chords;
			}
		}

		/// <summary>
		/// Adds the specified chord to track
		/// </summary>
		/// <param name="chord">Chord.</param>
		public void Add (Chord chord)
		{
			if (chord == null)
				return;
			ChordStorage.Add (chord);
		}

		/// <summary>
		/// Creates and adds a chord based on the provided string sequence, returns the created Chord.
		/// </summary>
		/// <param name="sequence">Text sequence (as described in the Chord documentation).</param>
		/// <param name="action">Code to invoke when the user enters the specified sequence.</param>
		public Chord Add (ustring sequence, Action action)
		{
			var chord = new Chord (sequence, action);
			Add (chord);
			return chord;
		}

		/// <summary>
		/// Creates and adds a chord based on the provided string sequence, returns the created Chord.
		/// </summary>
		/// <param name="sequence">Text sequence (as described in the Chord documentation).</param>
		/// <param name="action">Code to invoke when the user enters the specified sequence.</param>
		public Chord Add (string sequence, Action action)
		{
			var chord = new Chord (sequence, action);
			Add (chord);
			return chord;
		}

		/// <summary>
		/// Remove the specified chord and stops tracking it.
		/// </summary>
		/// <param name="chord">Chord.</param>
		public void Remove (Chord chord)
		{
			if (chord == null)
				return;
			ChordStorage.Remove (chord);
		}

		internal enum ChordStatus {
			// The event was not consumed
			NotConsumed,

			// The event was consumed
			Consumed,

			// The event was not consumed, and canceled a pending match, events must be flushed out
			NoMatch
		}

		/// <summary>
		/// Process the specified keyEvent, returns true if there was a match, 
		/// which means that the event should not be propagated further.
		/// </summary>
		/// <returns>One of the possible states of processing the event, a ChordStatus.</returns>
		/// <param name="keyEvent">Incoming key event.</param>
		/// <remarks>
		/// </remarks>
		internal ChordStatus ProcessKeyEvent (KeyEvent keyEvent, out List<KeyEvent> events)
		{
			events = null;
			if (chords == null)
				return ChordStatus.NotConsumed;
			// Avoid allocating the list, scan jsut the toplevel
			if (sequence == null) {
				bool match = false;
				foreach (var chord in chords) {
					if (chord.Description [0].Key == keyEvent.Key) {
						match = true;
						if (chord.Description.Length == 1) {
							chord.action ();
							return ChordStatus.NotConsumed;
						}
					}
				}
				if (match) {
					// allocate
					sequence = new List<KeyEvent> ();
					sequence.Add (keyEvent);
					return ChordStatus.Consumed;
				}
				return ChordStatus.NotConsumed;
			}

			var slen = sequence.Count;
			bool matches = false;
			foreach (var chord in chords) {
				if (chord.Description.Length < slen+1)
					continue;
				if (PrefixMatches (sequence, chord.Description)) {
					matches = true;
					if (chord.Description [slen].Key == keyEvent.Key) {
						if (chord.Description.Length == slen + 1) {
							chord.action ();
							return ChordStatus.Consumed;
						}
					}
				}
			}
			if (!matches)
				return ChordStatus.NotConsumed;
			else {
				events = sequence;
				sequence = null;
				return ChordStatus.NoMatch;
			}

			// Returns true if the chord has the given prefix
			bool PrefixMatches (List<KeyEvent> prefix, KeyEvent [] chord)
			{
				int pcount = prefix.Count;

				for (int i = 0; i < pcount; i++)
					if (prefix [i].Key != chord [i].Key)
						return false;
				return true;
			}
		}
	}
}
