using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scrolling", "Content scrolling, IScrollBars, etc...")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Scrolling")]
[ScenarioCategory ("Tests")]
public class Scrolling : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        var app = new Window
        {
            Title = GetQuitKeyAndName (),
        };

        var label = new Label { X = 0, Y = 0 };
        app.Add (label);

        var scrollView = new IScrollView
        {
            Id = "scrollView",
            X = 2,
            Y = Pos.Bottom (label) + 1,
            Width = 60,
            Height = 20,
            ColorScheme = Colors.ColorSchemes ["TopLevel"],
            CanFocus = true,
            BorderStyle = LineStyle.Heavy,
            Arrangement = ViewArrangement.Resizable
        };
        scrollView.SetContentSize (new (80, 25));
        //scrollView.Padding.Thickness = new (1);
        //scrollView.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;

        View rulerView = new View ()
        {
            Height = Dim.Fill (),
            Width = Dim.Fill (),
        };
        rulerView.Border.Thickness = new (1);
        rulerView.Border.LineStyle = LineStyle.None;
        rulerView.Border.Diagnostics = ViewDiagnosticFlags.Ruler;
        rulerView.Border.ColorScheme = Colors.ColorSchemes ["Error"];

        scrollView.Add (rulerView);
        label.Text =
            $"{scrollView}\nContentSize: {scrollView.GetContentSize ()}\nViewport.Location: {scrollView.Viewport.Location}";

        scrollView.ViewportChanged += (_, _) =>
                                      {
                                          label.Text =
                                              $"{scrollView}\nContentSize: {scrollView.GetContentSize ()}\nViewport.Location: {scrollView.Viewport.Location}";
                                      };

        var pressMeButton = new Button
        {
            X = 1,
            Y = 1,
            Text = "Press me!"
        };
        pressMeButton.Accepting += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        scrollView.Add (pressMeButton);

        var aLongButton = new Button
        {
            X = Pos.Right (pressMeButton),
            Y = Pos.Bottom (pressMeButton),

            Text = "A very long button. Should be wide enough to demo clipping!"
        };
        aLongButton.Accepting += (s, e) => MessageBox.Query (20, 7, "MessageBox", "Neat?", "Yes", "No");
        scrollView.Add (aLongButton);

        scrollView.Add (
                        new TextField
                        {
                            X = Pos.Left (pressMeButton),
                            Y = Pos.Bottom (aLongButton) + 1,
                            Width = 50,
                            ColorScheme = Colors.ColorSchemes ["Dialog"],
                            Text = "This is a test of..."
                        }
                       );

        scrollView.Add (
                        new TextField
                        {
                            X = Pos.Left (pressMeButton),
                            Y = Pos.Bottom (aLongButton) + 3,
                            Width = 50,
                            ColorScheme = Colors.ColorSchemes ["Dialog"],
                            Text = "... the emergency broadcast system."
                        }
                       );

        scrollView.Add (
                        new TextField
                        {
                            X = Pos.Left (pressMeButton),
                            Y = 40,
                            Width = 50,
                            ColorScheme = Colors.ColorSchemes ["Error"],
                            Text = "Last line"
                        }
                       );

        // Demonstrate AnchorEnd - Button is anchored to bottom/right
        var anchorButton = new Button
        {
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd (),
            Text = "Bottom Right"
        };

        anchorButton.Accepting += (s, e) =>
                               {
                                   // This demonstrates how to have a dynamically sized button
                                   // Each time the button is clicked the button's text gets longer
                                   anchorButton.Text += "!";

                               };
        scrollView.Add (anchorButton);

        app.Add (scrollView);

        scrollView.HorizontalScrollBar.Visible = true;
        var hCheckBox = new CheckBox
        {
            X = Pos.X (scrollView),
            Y = Pos.Bottom (scrollView),
            Text = "Horizontal Scrollbar",
            CheckedState = scrollView.HorizontalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (hCheckBox);
        hCheckBox.CheckedStateChanged += (sender, args) =>
                                         {
                                             scrollView.HorizontalScrollBar.Visible = args.CurrentValue == CheckState.Checked;
                                         };

        scrollView.VerticalScrollBar.Visible = true;
        var vCheckBox = new CheckBox
        {
            X = Pos.Right (hCheckBox) + 3,
            Y = Pos.Bottom (scrollView),
            Text = "Vertical Scrollbar",
            CheckedState = scrollView.VerticalScrollBar.Visible ? CheckState.Checked : CheckState.UnChecked
        };
        app.Add (vCheckBox);
        vCheckBox.CheckedStateChanged += (sender, args) =>
                                         {
                                             scrollView.VerticalScrollBar.Visible = args.CurrentValue == CheckState.Checked;
                                         };

        var t = "Auto Hide Scrollbars";

        var ahCheckBox = new CheckBox
        {
            X = Pos.Left (scrollView), Y = Pos.Bottom (hCheckBox), Text = t,
            CheckedState = scrollView.HorizontalScrollBar.AutoHide ? CheckState.Checked : CheckState.UnChecked
        };

        ahCheckBox.CheckedStateChanging += (s, e) =>
                              {
                                  scrollView.HorizontalScrollBar.AutoHide = e.NewValue == CheckState.Checked;
                                  scrollView.VerticalScrollBar.AutoHide = e.NewValue == CheckState.Checked;
                                  hCheckBox.CheckedState = CheckState.Checked;
                                  vCheckBox.CheckedState = CheckState.Checked;
                              };
        app.Add (ahCheckBox);

        var count = 0;

        var mousePos = new Label
        {
            X = Pos.Right (scrollView) + 1,
            Y = Pos.AnchorEnd (1),

            Width = 50,
            Text = "Mouse: "
        };
        app.Add (mousePos);
        Application.MouseEvent += (sender, a) => { mousePos.Text = $"Mouse: ({a.Position}) - {a.Flags} {count++}"; };

        // Add a progress bar to cause constant redraws
        var progress = new ProgressBar { X = Pos.Right (scrollView) + 1, Y = Pos.AnchorEnd (2), Width = 50 };

        app.Add (progress);

        var pulsing = true;

        bool timer ()
        {
            progress.Pulse ();

            return pulsing;
        }

        Application.AddTimeout (TimeSpan.FromMilliseconds (300), timer);

        app.Unloaded += app_Unloaded;


        Application.Run (app);
        app.Unloaded -= app_Unloaded;
        app.Dispose ();
        Application.Shutdown ();

        return;

        void app_Unloaded (object sender, EventArgs args)
        {
            pulsing = false;
        }
    }
}

public class IScrollView : View
{

}
