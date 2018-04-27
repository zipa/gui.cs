// 
// FileDialog.cs: File system dialogs for open and save
//

using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	public class FileDialog : Dialog {
		Button prompt;
		Label directoryLabel, fileLabel;
		Label nameFieldLabel, message;
		TextField nameEntry, directoryEntry;

		class Filler : View {
			public Filler (int w, int h) : base (w, h) { }
			public Filler (Rect rect) : base (rect)
			{
			}
			public Filler () : base () { }

			public override void Redraw (Rect region)
			{
				Driver.SetAttribute (ColorScheme.Focus);
				var f = Frame;

				for (int y = 0; y < f.Height; y++) {
					Move (0, y);
					for (int x = 0; x < f.Width; x++) {
						Rune r;
						switch (x % 3) {
						case 0:
							r = '.';
							break;
						case 1:
							r = 'o';
							break;
						default:
							r = 'O';
							break;
						}
						Driver.AddRune (r);
					}
				}
			}
		}

		public FileDialog (ustring title, ustring prompt, ustring nameFieldLabel, ustring message) : base (title, Driver.Cols - 20, Driver.Rows - 6, null)
		{
			this.prompt = new Button (prompt);
			AddButton (this.prompt);

			directoryLabel = new Label ("Directory: ") {
				MarginLeft = 1,
			};
			directoryEntry = new TextField ("") {
				Grow = 1,
				AlignSelf = AlignSelf.Stretch,
				MarginRight = 2
			};

			this.nameFieldLabel = new Label (nameFieldLabel + ": ") {
				MarginLeft = 1,
				MarginTop = 1,
				// THIS BELOW TELLS ME THAT WE PROBABLY SHOULD NOT BE DOING SIZING BY DEFAULT, AS IT OVERRIDES THIS
				Width = 30,
			};
			nameEntry = new TextField ("") {
				Grow = 1,
				AlignSelf = AlignSelf.Stretch,
				MarginTop = 1,
				MarginRight = 2,
			};


			this.message = new Label ("MSG" + message) {
				Direction = Direction.Row
			};

			AddViews ();
		}

		void AddViews ()
		{
			ContentView.Direction = Direction.Row;
			ContentView.AlignItems = AlignItems.Start;
			ContentView.AlignContent = AlignContent.Start;
			ContentView.Wrap = Wrap.Wrap;

			AlignSelf = AlignSelf.Start;
			AlignItems = AlignItems.Start;

			//Add (this.nameFieldLabel);
			Add (directoryLabel);
			Add (directoryEntry);
			Add (new Filler () { Width = 500, Height = 0 });
			Add (nameFieldLabel);
			Add (nameEntry);
			Add (new Filler () { Width = 500, Height = 0 } );
			//Add (this.message);
			Add (new Filler () {
				AlignSelf = AlignSelf.Stretch,
				Grow = 1,
				Basis = Basis.Auto,
				Height = 3

			});
			Layout ();
		}

		/// <summary>
		/// Gets or sets the prompt label for the button displayed to the user
		/// </summary>
		/// <value>The prompt.</value>
		public ustring Prompt {
			get => prompt.Text;
			set {
				prompt.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the name field label.
		/// </summary>
		/// <value>The name field label.</value>
		public ustring NameFieldLabel {
			get => nameFieldLabel.Text;
			set {
				nameFieldLabel.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the message displayed to the user, defaults to nothing
		/// </summary>
		/// <value>The message.</value>
		public ustring Message { get; set; }


		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.FileDialog"/> can create directories.
		/// </summary>
		/// <value><c>true</c> if can create directories; otherwise, <c>false</c>.</value>
		public bool CanCreateDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.FileDialog"/> is extension hidden.
		/// </summary>
		/// <value><c>true</c> if is extension hidden; otherwise, <c>false</c>.</value>
		public bool IsExtensionHidden { get; set; }

		/// <summary>
		/// Gets or sets the directory path for this panel
		/// </summary>
		/// <value>The directory path.</value>
		public ustring DirectoryPath { get; set; }

		/// <summary>
		/// The array of filename extensions allowed, or null if all file extensions are allowed.
		/// </summary>
		/// <value>The allowed file types.</value>
		public ustring [] AllowedFileTypes { get; set; }


		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.FileDialog"/> allows the file to be saved with a different extension
		/// </summary>
		/// <value><c>true</c> if allows other file types; otherwise, <c>false</c>.</value>
		public bool AllowsOtherFileTypes { get; set; }

		/// <summary>
		/// The File path that is currently shown on the panel
		/// </summary>
		/// <value>The absolute file path for the file path entered.</value>
		public ustring FilePath { get; set; }
	}

	public class SaveDialog : FileDialog {
		public SaveDialog (ustring title, ustring message) : base (title, "Save", "Save as:", message)
		{
		}
	}

	public class OpenDialog : FileDialog {
		public OpenDialog (ustring title, ustring message) : base (title, "Open", "Open", message)
		{
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.OpenDialog"/> can choose files.
		/// </summary>
		/// <value><c>true</c> if can choose files; otherwise, <c>false</c>.</value>
		public bool CanChooseFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.OpenDialog"/> can choose directories.
		/// </summary>
		/// <value><c>true</c> if can choose directories; otherwise, <c>false</c>.</value>
		public bool CanChooseDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.OpenDialog"/> allows multiple selection.
		/// </summary>
		/// <value><c>true</c> if allows multiple selection; otherwise, <c>false</c>.</value>
		public bool AllowsMultipleSelection { get; set; }

		/// <summary>
		/// Gets the file paths selected
		/// </summary>
		/// <value>The file paths.</value>
		public IReadOnlyList<ustring> FilePaths { get; }
	}
}
