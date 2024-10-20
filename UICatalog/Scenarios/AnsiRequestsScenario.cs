using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColorHelper;
using Terminal.Gui;

namespace UICatalog.Scenarios;



[ScenarioMetadata ("Ansi Requests", "Demonstration of how to send ansi requests.")]
[ScenarioCategory ("Colors")]
[ScenarioCategory ("Drawing")]
public class AnsiRequestsScenario : Scenario
{
    private GraphView _graphView;
    private Window _win;

    private DateTime start = DateTime.Now;
    private ScatterSeries _sentSeries;
    private ScatterSeries _answeredSeries;

    private List<DateTime> sends = new  ();
    private Dictionary<DateTime,string> answers = new ();
    private Label _lblSummary;

    public override void Main ()
    {
        Application.Init ();
        _win = new Window { Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}" };

        var lbl = new Label ()
        {
            Text = "This scenario tests Ansi request/response processing. Use the TextView to ensure regular user interaction continues as normal during sends",
            Height = 2,
            Width = Dim.Fill()
        };

        Application.AddTimeout (
                                TimeSpan.FromMilliseconds (1000),
                                () =>
                                {
                                    UpdateGraph ();

                                    UpdateResponses ();

                                    return _win.DisposedCount == 0;
                                });

        var tv = new TextView ()
        {
            Y = Pos.Bottom (lbl),
            Width = Dim.Percent (50),
            Height = Dim.Fill()
        };


        var lblDar = new Label ()
        {
            Y = Pos.Bottom (lbl),
            X = Pos.Right (tv) + 1,
            Text = "DAR per second",
        };
        var cbDar = new NumericUpDown ()
        {
            X = Pos.Right (lblDar),
            Y = Pos.Bottom (lbl),
            Value = 0,
        };

        cbDar.ValueChanging += (s, e) =>
                               {
                                   if (e.NewValue < 0 || e.NewValue > 20)
                                   {
                                       e.Cancel = true;
                                   }
                               };
        _win.Add (cbDar);

        int lastSendTime = Environment.TickCount;
        Application.AddTimeout (
                                TimeSpan.FromMilliseconds (50),
                                () =>
                                {
                                    if (cbDar.Value > 0)
                                    {
                                        int interval = 1000 / cbDar.Value; // Calculate the desired interval in milliseconds
                                        int currentTime = Environment.TickCount; // Current system time in milliseconds

                                        // Check if the time elapsed since the last send is greater than the interval
                                        if (currentTime - lastSendTime >= interval)
                                        {
                                            SendDar (); // Send the request
                                            lastSendTime = currentTime; // Update the last send time
                                        }
                                    }

                                    return _win.DisposedCount == 0;
                                });


        _graphView = new GraphView ()
        {
            Y = Pos.Bottom (cbDar),
            X = Pos.Right (tv),
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        _lblSummary = new Label ()
        {
            Y = Pos.Bottom (_graphView),
            X = Pos.Right (tv),
            Width = Dim.Fill()
        };

        SetupGraph ();

        _win.Add (lbl);
        _win.Add (lblDar);
        _win.Add (cbDar);
        _win.Add (tv);
        _win.Add (_graphView);
        _win.Add (_lblSummary);

        Application.Run (_win);
        _win.Dispose ();
        Application.Shutdown ();
    }
    
    private void UpdateResponses ()
    {
        _lblSummary.Text = GetSummary ();
        _lblSummary.SetNeedsDisplay();
    }

    private string GetSummary ()
    {
        if (answers.Count == 0)
        {
            return "No requests sent yet";
        }

        var last = answers.Last ().Value;

        var unique = answers.Values.Distinct ().Count ();
        var total = answers.Count; 
        
        return $"Last:{last} U:{unique} T:{total}";
    }

    private void SetupGraph ()
    {

        _graphView.Series.Add (_sentSeries = new ScatterSeries ());
        _graphView.Series.Add (_answeredSeries = new ScatterSeries ());

        _sentSeries.Fill = new GraphCellToRender (new Rune ('.'), new Attribute (ColorName16.BrightGreen, ColorName16.Black));
        _answeredSeries.Fill = new GraphCellToRender (new Rune ('.'), new Attribute (ColorName16.BrightCyan, ColorName16.Black));

        // Todo:
        // _graphView.Annotations.Add (_sentSeries new PathAnnotation {});

        _graphView.CellSize = new PointF (1, 1);
        _graphView.MarginBottom = 2;
        _graphView.AxisX.Increment = 1;
        _graphView.AxisX.Text = "Seconds";
        _graphView.GraphColor = new Attribute (Color.Green, Color.Black);
    }

    private void UpdateGraph ()
    {
        _sentSeries.Points = sends
                             .GroupBy (ToSeconds)
                             .Select (g => new PointF (g.Key, g.Count ()))
                             .ToList ();

        _answeredSeries.Points = answers.Keys
                                        .GroupBy (ToSeconds)
                                        .Select (g => new PointF (g.Key, g.Count ()))
                                        .ToList ();
        //  _graphView.ScrollOffset  = new PointF(,0);
        _graphView.SetNeedsDisplay();

    }

    private int ToSeconds (DateTime t)
    {
        return (int)(DateTime.Now - t).TotalSeconds;
    }

    private void SendDar ()
    {
        // Ask for device attributes (DAR)
        var p = Application.Driver.GetParser ();
        p.ExpectResponse ("c", HandleResponse);
        Application.Driver.RawWrite (EscSeqUtils.CSI_SendDeviceAttributes);
        sends.Add (DateTime.Now);
    }

    private void HandleResponse (string response)
    {
        answers.Add (DateTime.Now,response);
    }


}