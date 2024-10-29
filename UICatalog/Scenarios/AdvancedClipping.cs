using System.Text;
using System.Timers;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("AdvancedClipping", "AdvancedClipping Tester")]
[ScenarioCategory ("AdvancedClipping")]
public class AdvancedClipping : Scenario
{
    private int _hotkeyCount;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        //var arrangementEditor = new ArrangementEditor()
        //{
        //    X = Pos.AnchorEnd (),
        //    Y = 0,
        //    AutoSelectViewToEdit = true,
        //};
        //app.Add (arrangementEditor);

        View tiledView1 = CreateTiledView (1, 0, 0);


        ProgressBar tiledProgressBar = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Id = "tiledProgressBar",
           // BorderStyle = LineStyle.Rounded
        };
        tiledView1.Add (tiledProgressBar);

        View tiledView2 = CreateTiledView (2, 2, 2);

        app.Add (tiledView1);
        app.Add (tiledView2);

        //View tiledView3 = CreateTiledView (3, 6, 6);
        //app.Add (tiledView3);

        //using View overlappedView1 = CreateOverlappedView (1, 30, 2);

        //ProgressBar progressBar = new ()
        //{
        //    X = Pos.AnchorEnd (),
        //    Y = Pos.AnchorEnd (),
        //    Width = Dim.Fill (),
        //    Id = "progressBar",
        //    BorderStyle = LineStyle.Rounded
        //};
        //overlappedView1.Add (progressBar);


        //View overlappedView2 = CreateOverlappedView (2, 32, 4);
        //View overlappedView3 = CreateOverlappedView (3, 34, 6);

        //app.Add (overlappedView1);
        //app.Add (overlappedView2);
        //app.Add (overlappedView3);

        Timer progressTimer = new Timer (250)
        {
            AutoReset = true
        };

        progressTimer.Elapsed += (s, e) =>
                                 {

                                     if (tiledProgressBar.Fraction == 1.0)
                                     {
                                         tiledProgressBar.Fraction = 0;
                                     }

                                     Application.Wakeup ();

                                     tiledProgressBar.Fraction += 0.1f;
                                    // tiledProgressBar.SetNeedsDraw ();
                                 };

        progressTimer.Start ();
        Application.Run (app);
        progressTimer.Stop ();
        app.Dispose ();
        Application.Shutdown ();

        return;
    }

    private View CreateOverlappedView (int id, Pos x, Pos y)
    {
        var overlapped = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (minimumContentDim: 4),
            Width = Dim.Auto (minimumContentDim: 14),
            Title = $"Overlapped{id} _{GetNextHotKey ()}",
            ColorScheme = Colors.ColorSchemes ["Toplevel"],
            Id = $"Overlapped{id}",
            ShadowStyle = ShadowStyle.Transparent,
            BorderStyle = LineStyle.Double,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabGroup,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped | ViewArrangement.Resizable
        };
        return overlapped;
    }

    private View CreateTiledView (int id, Pos x, Pos y)
    {
        var tiled = new View
        {
            X = x,
            Y = y,
            Height = Dim.Auto (minimumContentDim: 4),
            Width = Dim.Auto (minimumContentDim: 14),
            Title = $"Tiled{id} _{GetNextHotKey ()}",
            Id = $"Tiled{id}",
            Text = $"Tiled{id}",
            BorderStyle = LineStyle.Single,
            CanFocus = true, // Can't drag without this? BUGBUG
            TabStop = TabBehavior.TabStop,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable
        };
        tiled.Padding.Thickness = new (1);
        tiled.Padding.Diagnostics =  ViewDiagnosticFlags.Thickness;

        FrameView fv = new ()
        {
            Title = "FrameView",
            Width = 15,
            Height = 1,
        };
        tiled.Add (fv);
        
        return tiled;
    }

    private char GetNextHotKey () { return (char)('A' + _hotkeyCount++); }
}
