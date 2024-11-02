using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("AnsiEscapeSequenceRequest", "Ansi Escape Sequence Request")]
[ScenarioCategory ("Ansi Escape Sequence")]
public sealed class AnsiEscapeSequenceRequests : Scenario
{
    private GraphView _graphView;

    private DateTime start = DateTime.Now;
    private ScatterSeries _sentSeries;
    private ScatterSeries _answeredSeries;

    private List<DateTime> sends = new ();

    private object lockAnswers = new object ();
    private Dictionary<DateTime, string> answers = new ();
    private Label _lblSummary;

    public override void Main ()
    {
        // Init
        Application.Init ();

        TabView tv = new TabView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Tab single = new Tab ();
        single.DisplayText = "Single";
        single.View = BuildSingleTab ();

        Tab bulk = new ();
        bulk.DisplayText = "Multi";
        bulk.View = BuildBulkTab ();

        tv.AddTab (single, true);
        tv.AddTab (bulk, false);

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
        };

        appWindow.Add (tv);

        // Run - Start the application.
        Application.Run (appWindow);
        bulk.View.Dispose ();
        single.View.Dispose ();
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }
    private View BuildSingleTab ()
    {
        View w = new View ()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill ()
        };

        w.Padding.Thickness = new (1);

        var scrRequests = new List<string>
        {
            "CSI_SendDeviceAttributes",
            "CSI_ReportTerminalSizeInChars",
            "CSI_RequestCursorPositionReport",
            "CSI_SendDeviceAttributes2"
        };

        var cbRequests = new ComboBox () { Width = 40, Height = 5, ReadOnly = true, Source = new ListWrapper<string> (new (scrRequests)) };
        w.Add (cbRequests);

        var label = new Label { Y = Pos.Bottom (cbRequests) + 1, Text = "Request:" };
        var tfRequest = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 20 };
        w.Add (label, tfRequest);

        label = new Label { X = Pos.Right (tfRequest) + 1, Y = Pos.Top (tfRequest) - 1, Text = "Value:" };
        var tfValue = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6 };
        w.Add (label, tfValue);

        label = new Label { X = Pos.Right (tfValue) + 1, Y = Pos.Top (tfValue) - 1, Text = "Terminator:" };
        var tfTerminator = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4 };
        w.Add (label, tfTerminator);

        cbRequests.SelectedItemChanged += (s, e) =>
                                          {
                                              if (cbRequests.SelectedItem == -1)
                                              {
                                                  return;
                                              }

                                              var selAnsiEscapeSequenceRequestName = scrRequests [cbRequests.SelectedItem];
                                              AnsiEscapeSequenceRequest selAnsiEscapeSequenceRequest = null;
                                              switch (selAnsiEscapeSequenceRequestName)
                                              {
                                                  case "CSI_SendDeviceAttributes":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_SendDeviceAttributes;
                                                      break;
                                                  case "CSI_ReportTerminalSizeInChars":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_ReportTerminalSizeInChars;
                                                      break;
                                                  case "CSI_RequestCursorPositionReport":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_RequestCursorPositionReport;
                                                      break;
                                                  case "CSI_SendDeviceAttributes2":
                                                      selAnsiEscapeSequenceRequest = EscSeqUtils.CSI_SendDeviceAttributes2;
                                                      break;
                                              }

                                              tfRequest.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Request : "";
                                              tfValue.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Value ?? "" : "";
                                              tfTerminator.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Terminator : "";
                                          };
        // Forces raise cbRequests.SelectedItemChanged to update TextFields
        cbRequests.SelectedItem = 0;

        label = new Label { Y = Pos.Bottom (tfRequest) + 2, Text = "Response:" };
        var tvResponse = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 40, Height = 4, ReadOnly = true };
        w.Add (label, tvResponse);

        label = new Label { X = Pos.Right (tvResponse) + 1, Y = Pos.Top (tvResponse) - 1, Text = "Error:" };
        var tvError = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 40, Height = 4, ReadOnly = true };
        w.Add (label, tvError);

        label = new Label { X = Pos.Right (tvError) + 1, Y = Pos.Top (tvError) - 1, Text = "Value:" };
        var tvValue = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6, Height = 4, ReadOnly = true };
        w.Add (label, tvValue);

        label = new Label { X = Pos.Right (tvValue) + 1, Y = Pos.Top (tvValue) - 1, Text = "Terminator:" };
        var tvTerminator = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4, Height = 4, ReadOnly = true };
        w.Add (label, tvTerminator);

        var btnResponse = new Button { X = Pos.Center (), Y = Pos.Bottom (tvResponse) + 2, Text = "Send Request", IsDefault = true };

        var lblSuccess = new Label { X = Pos.Center (), Y = Pos.Bottom (btnResponse) + 1 };
        w.Add (lblSuccess);

        btnResponse.Accepting += (s, e) =>
                              {
                                  var ansiEscapeSequenceRequest = new AnsiEscapeSequenceRequest
                                  {
                                      Request = tfRequest.Text,
                                      Terminator = tfTerminator.Text,
                                      Value = string.IsNullOrEmpty (tfValue.Text) ? null : tfValue.Text
                                  };

                                  var success = AnsiEscapeSequenceRequest.TryExecuteAnsiRequest (
                                       ansiEscapeSequenceRequest,
                                       out AnsiEscapeSequenceResponse ansiEscapeSequenceResponse
                                      );

                                  tvResponse.Text = ansiEscapeSequenceResponse.Response;
                                  tvError.Text = ansiEscapeSequenceResponse.Error;
                                  tvValue.Text = ansiEscapeSequenceResponse.Value ?? "";
                                  tvTerminator.Text = ansiEscapeSequenceResponse.Terminator;

                                  if (success)
                                  {
                                      lblSuccess.ColorScheme = Colors.ColorSchemes ["Base"];
                                      lblSuccess.Text = "Successful";
                                  }
                                  else
                                  {
                                      lblSuccess.ColorScheme = Colors.ColorSchemes ["Error"];
                                      lblSuccess.Text = "Error";
                                  }
                              };
        w.Add (btnResponse);

        w.Add (new Label { Y = Pos.Bottom (lblSuccess) + 2, Text = "You can send other requests by editing the TextFields." });

        return w;
    }

    private View BuildBulkTab ()
    {
        View w = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var lbl = new Label ()
        {
            Text = "This scenario tests Ansi request/response processing. Use the TextView to ensure regular user interaction continues as normal during sends",
            Height = 2,
            Width = Dim.Fill ()
        };

        Application.AddTimeout (
                                TimeSpan.FromMilliseconds (1000),
                                () =>
                                {
                                    lock (lockAnswers)
                                    {
                                        UpdateGraph ();

                                        UpdateResponses ();
                                    }



                                    return true;
                                });

        var tv = new TextView ()
        {
            Y = Pos.Bottom (lbl),
            Width = Dim.Percent (50),
            Height = Dim.Fill ()
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
        w.Add (cbDar);

        int lastSendTime = Environment.TickCount;
        object lockObj = new object ();
        Application.AddTimeout (
                                TimeSpan.FromMilliseconds (50),
                                () =>
                                {
                                    lock (lockObj)
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
                                    }

                                    return true;
                                });


        _graphView = new GraphView ()
        {
            Y = Pos.Bottom (cbDar),
            X = Pos.Right (tv),
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };

        _lblSummary = new Label ()
        {
            Y = Pos.Bottom (_graphView),
            X = Pos.Right (tv),
            Width = Dim.Fill ()
        };

        SetupGraph ();

        w.Add (lbl);
        w.Add (lblDar);
        w.Add (cbDar);
        w.Add (tv);
        w.Add (_graphView);
        w.Add (_lblSummary);

        return w;
    }
    private void UpdateResponses ()
    {
        _lblSummary.Text = GetSummary ();
        _lblSummary.SetNeedsDisplay ();
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
        _answeredSeries.Fill = new GraphCellToRender (new Rune ('.'), new Attribute (ColorName16.BrightRed, ColorName16.Black));

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
        _graphView.SetNeedsDisplay ();

    }

    private int ToSeconds (DateTime t)
    {
        return (int)(DateTime.Now - t).TotalSeconds;
    }

    private void SendDar ()
    {
        sends.Add (DateTime.Now);
        var result = Application.Driver.WriteAnsiRequest (EscSeqUtils.CSI_SendDeviceAttributes);
        HandleResponse (result);
    }

    private void HandleResponse (string response)
    {
        lock (lockAnswers)
        {
            answers.Add (DateTime.Now, response);
        }
    }
}
