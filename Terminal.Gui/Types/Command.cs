//
// Command.cs: defines a user-triggered command, which can be added to menus
// or status bars.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
namespace Terminal.Gui {
	public class Command {
		public string Title { get; set; }
		public string Help { get; set; }
		public Action Callback { get; }

		Command (string title, string help, Action callback)
		{
			Title = title;
			Help = help;
			Callback = callback;
		}

		public static Command Create (string title, string help, Action callback)
		{
			return new Command (title, help, callback);
		}

		public static Command Create (string title, Action callback)
		{
			return new Command (title, null, callback);
		}
	}
}
