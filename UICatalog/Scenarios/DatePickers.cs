using Terminal.Gui;

namespace UICatalog.Scenarios;

[Scenario.ScenarioMetadata ("Date Picker", "Demonstrates how to use DatePicker class")]
[Scenario.ScenarioCategory ("Controls")]
[Scenario.ScenarioCategory ("DateTime")]
public class DatePickers : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        var datePicker = new DatePicker { Y = Pos.Center (), X = Pos.Center () };

        app.Add (datePicker);

        Application.Run (app);
        app.Dispose ();

        Application.Shutdown ();
    }
}
