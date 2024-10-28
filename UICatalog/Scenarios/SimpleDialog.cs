#nullable enable
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("SimpleDialog", "SimpleDialog ")]
public sealed class SimpleDialog : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        Dialog dialog = new () { Id = "dialog", Width = 20, Height = 4, Title = "Dialog" };
        dialog.Arrangement |= ViewArrangement.Resizable;

        var button = new Button
        {
            Id = "button", X = Pos.Center (), Y = 1, Text = "_Press me!",
            WantContinuousButtonPressed = false,
            HighlightStyle = HighlightStyle.None,
            ShadowStyle = ShadowStyle.None,
        };

        button.Accepting += (s, e) =>
                            {
                                Application.Run (dialog);
                                e.Cancel = true;
                            };
        appWindow.Add (button);
        
        // Run - Start the application.
        Application.Run (appWindow);
        dialog.Dispose ();
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
