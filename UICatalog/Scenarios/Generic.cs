using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Generic", "Generic sample - A template for creating new Scenarios")]
[ScenarioCategory ("Controls")]
public sealed class MyScenario : Scenario
{
    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Id = "appWindow",
            Title = GetQuitKeyAndName (),
            Arrangement = ViewArrangement.Fixed
        };

        var button = new Button
        {
            Id = "button",
            X = Pos.AnchorEnd(), Y = 0, Text = "_Press me!"
        };
        button.Accepting += (s, e) => MessageBox.ErrorQuery ("Error", "You pressed the button!", "_Ok");

        button.HighlightStyle = HighlightStyle.None;
        button.ShadowStyle = ShadowStyle.None;

        appWindow.Border.Add (button);

        //appWindow.Border.LineStyle = LineStyle.None;

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
}
