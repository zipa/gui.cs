using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("AnsiEscapeSequenceRequest", "Ansi Escape Sequence Request")]
[ScenarioCategory ("Tests")]
public sealed class AnsiEscapeSequenceRequests : Scenario
{
    private readonly Dictionary<DateTime, AnsiEscapeSequenceRequest> _sends = new ();

    private readonly object _lockAnswers = new ();
    private readonly Dictionary<DateTime, AnsiEscapeSequenceRequest> _answers = new ();
    private readonly Dictionary<DateTime, AnsiEscapeSequenceRequest> _errors = new ();

    private GraphView _graphView;

    private ScatterSeries _sentSeries;
    private ScatterSeries _answeredSeries;
    private Label _lblSummary;
    private Label _lblErrorSummary;

    public override void Main ()
    {
        // Init
        Application.Init ();

        var tv = new TabView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var single = new Tab ();
        single.DisplayText = "_Single";
        single.View = BuildSingleTab ();

        Tab bulk = new ();
        bulk.DisplayText = "_Multi";
        bulk.View = BuildBulkTab ();

        tv.AddTab (single, true);
        tv.AddTab (bulk, false);

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
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

    private View BuildBulkTab ()
    {
        var w = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        var lbl = new Label
        {
            Text =
                "_This scenario tests Ansi request/response processing. Use the TextView to ensure regular user interaction continues as normal during sends. Responses are in red, queued messages are in green.",
            Height = 2,
            Width = Dim.Fill ()
        };

        Application.AddTimeout (
                                TimeSpan.FromMilliseconds (1000),
                                () =>
                                {
                                    lock (_lockAnswers)
                                    {
                                        UpdateGraph ();

                                        UpdateResponses ();
                                    }

                                    return true;
                                });

        var tv = new TextView
        {
            Y = Pos.Bottom (lbl),
            Width = Dim.Percent (50),
            Height = Dim.Fill ()
        };

        var lblDar = new Label
        {
            Y = Pos.Bottom (lbl),
            X = Pos.Right (tv) + 1,
            Text = "_DAR per second: "
        };

        var cbDar = new NumericUpDown
        {
            X = Pos.Right (lblDar),
            Y = Pos.Bottom (lbl),
            Value = 0
        };

        cbDar.ValueChanging += (s, e) =>
                               {
                                   if (e.NewValue is < 0 or > 20)
                                   {
                                       e.Cancel = true;
                                   }
                               };
        w.Add (cbDar);

        int lastSendTime = Environment.TickCount;
        var lockObj = new object ();
        int interval = 50;

        Application.AddTimeout (
                                TimeSpan.FromMilliseconds ((double)interval / (cbDar.Value > 0 ? cbDar.Value : 1)),
                                () =>
                                {
                                    lock (lockObj)
                                    {
                                        if (cbDar.Value > 0)
                                        {
                                            interval = 1000 / cbDar.Value; // Calculate the desired interval in milliseconds
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

        _graphView = new ()
        {
            Y = Pos.Bottom (cbDar),
            X = Pos.Right (tv),
            Width = Dim.Fill (),
            Height = Dim.Fill (2)
        };

        _lblSummary = new ()
        {
            Y = Pos.Bottom (_graphView),
            X = Pos.Right (tv),
            Width = Dim.Fill ()
        };

        _lblErrorSummary = new ()
        {
            Y = Pos.Bottom (_lblSummary),
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
        w.Add (_lblErrorSummary);

        return w;
    }

    private View BuildSingleTab ()
    {
        var w = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            CanFocus = true
        };

        w.Padding.Thickness = new (1);

        // TODO: This hackery is why I think the EscSeqUtils class should be refactored and the CSI's made type safe.
        List<string> scrRequests = new ()
        {
            "CSI_SendDeviceAttributes",
            "CSI_ReportTerminalSizeInChars",
            "CSI_RequestCursorPositionReport",
            "CSI_SendDeviceAttributes2"
        };

        var cbRequests = new ComboBox { Width = 40, Height = 5, ReadOnly = true, Source = new ListWrapper<string> (new (scrRequests)) };
        w.Add (cbRequests);

        // TODO: Use Pos.Align and Dim.Func so these hardcoded widths aren't needed.
        var label = new Label { Y = Pos.Bottom (cbRequests) + 1, Text = "_Request:" };
        var tfRequest = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 20 };
        w.Add (label, tfRequest);

        label = new () { X = Pos.Right (tfRequest) + 1, Y = Pos.Top (tfRequest) - 1, Text = "E_xpectedResponseValue:" };
        var tfValue = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6 };
        w.Add (label, tfValue);

        label = new () { X = Pos.Left (tfValue) + label.Text.Length, Y = Pos.Top (tfValue) - 1, Text = "_Terminator:" };
        var tfTerminator = new TextField { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4 };
        w.Add (label, tfTerminator);

        cbRequests.SelectedItemChanged += (s, e) =>
                                          {
                                              if (cbRequests.SelectedItem == -1)
                                              {
                                                  return;
                                              }

                                              string selAnsiEscapeSequenceRequestName = scrRequests [cbRequests.SelectedItem];
                                              AnsiEscapeSequenceRequest selAnsiEscapeSequenceRequest = null;

                                              switch (selAnsiEscapeSequenceRequestName)
                                              {
                                                  case "CSI_SendDeviceAttributes":
                                                      selAnsiEscapeSequenceRequest = AnsiEscapeSequenceRequestUtils.CSI_SendDeviceAttributes;

                                                      break;
                                                  case "CSI_ReportTerminalSizeInChars":
                                                      selAnsiEscapeSequenceRequest = AnsiEscapeSequenceRequestUtils.CSI_ReportTerminalSizeInChars;

                                                      break;
                                                  case "CSI_RequestCursorPositionReport":
                                                      selAnsiEscapeSequenceRequest = AnsiEscapeSequenceRequestUtils.CSI_RequestCursorPositionReport;

                                                      break;
                                                  case "CSI_SendDeviceAttributes2":
                                                      selAnsiEscapeSequenceRequest = AnsiEscapeSequenceRequestUtils.CSI_SendDeviceAttributes2;

                                                      break;
                                              }

                                              tfRequest.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Request : "";

                                              tfValue.Text = selAnsiEscapeSequenceRequest is { }
                                                                 ? selAnsiEscapeSequenceRequest.ExpectedResponseValue ?? ""
                                                                 : "";
                                              tfTerminator.Text = selAnsiEscapeSequenceRequest is { } ? selAnsiEscapeSequenceRequest.Terminator : "";
                                          };

        // Forces raise cbRequests.SelectedItemChanged to update TextFields
        cbRequests.SelectedItem = 0;

        label = new () { Y = Pos.Bottom (tfRequest) + 2, Text = "_Response:" };
        var tvResponse = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 40, Height = 4, ReadOnly = true };
        w.Add (label, tvResponse);

        label = new () { X = Pos.Right (tvResponse) + 1, Y = Pos.Top (tvResponse) - 1, Text = "_Error:" };
        var tvError = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 40, Height = 4, ReadOnly = true };
        w.Add (label, tvError);

        label = new () { X = Pos.Right (tvError) + 1, Y = Pos.Top (tvError) - 1, Text = "_Value:" };
        var tvValue = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 6, Height = 4, ReadOnly = true };
        w.Add (label, tvValue);

        label = new () { X = Pos.Right (tvValue) + 1, Y = Pos.Top (tvValue) - 1, Text = "_Terminator:" };
        var tvTerminator = new TextView { X = Pos.Left (label), Y = Pos.Bottom (label), Width = 4, Height = 4, ReadOnly = true };
        w.Add (label, tvTerminator);

        var btnResponse = new Button { X = Pos.Center (), Y = Pos.Bottom (tvResponse) + 2, Text = "_Send Request", IsDefault = true };

        var lblSuccess = new Label { X = Pos.Center (), Y = Pos.Bottom (btnResponse) + 1 };
        w.Add (lblSuccess);

        btnResponse.Accepting += (s, e) =>
                                 {
                                     var ansiEscapeSequenceRequest = new AnsiEscapeSequenceRequest
                                     {
                                         Request = tfRequest.Text,
                                         Terminator = tfTerminator.Text,
                                         ExpectedResponseValue = string.IsNullOrEmpty (tfValue.Text) ? null : tfValue.Text
                                     };

                                     bool success = Application.Driver!.TryWriteAnsiRequest (
                                                                                             Application.MainLoop!.MainLoopDriver,
                                                                                             ref ansiEscapeSequenceRequest
                                                                                            );

                                     tvResponse.Text = ansiEscapeSequenceRequest.AnsiEscapeSequenceResponse?.Response ?? "";
                                     tvError.Text = ansiEscapeSequenceRequest.AnsiEscapeSequenceResponse?.Error ?? "";
                                     tvValue.Text = ansiEscapeSequenceRequest.AnsiEscapeSequenceResponse?.ExpectedResponseValue ?? "";
                                     tvTerminator.Text = ansiEscapeSequenceRequest.AnsiEscapeSequenceResponse?.Terminator ?? "";

                                     if (success)
                                     {
                                         lblSuccess.ColorScheme = Colors.ColorSchemes ["Base"];
                                         lblSuccess.Text = "Success";
                                     }
                                     else
                                     {
                                         lblSuccess.ColorScheme = Colors.ColorSchemes ["Error"];
                                         lblSuccess.Text = "Error";
                                     }
                                 };
        w.Add (btnResponse);

        w.Add (new Label { Y = Pos.Bottom (lblSuccess) + 2, Text = "Send other requests by editing the TextFields." });

        return w;
    }

    private string GetSummary ()
    {
        if (_answers.Count == 0)
        {
            return "No requests sent yet";
        }

        string last = _answers.Last ().Value.AnsiEscapeSequenceResponse!.Response;

        int unique = _answers.Values.Distinct ().Count ();
        int total = _answers.Count;

        return $"Last:{last} U:{unique} T:{total}";
    }

    private string GetSummaryErrors ()
    {
        if (_errors.Count == 0)
        {
            return "No errors received yet";
        }

        string last = _errors.Last ().Value.AnsiEscapeSequenceResponse!.Error;

        int unique = _errors.Values.Distinct ().Count ();
        int total = _errors.Count;

        return $"Last:{last} U:{unique} T:{total}";
    }

    private void HandleResponse (AnsiEscapeSequenceRequest ansiRequest)
    {
        lock (_lockAnswers)
        {
            _answers.Add (DateTime.Now, ansiRequest);
            KeyValuePair<DateTime, AnsiEscapeSequenceRequest> found = _sends.First (r => r.Value == ansiRequest);
            _sends.Remove (found.Key);
        }
    }

    private void HandleResponseError (AnsiEscapeSequenceRequest ansiRequest)
    {
        lock (_lockAnswers)
        {
            _errors.Add (DateTime.Now, ansiRequest);
            KeyValuePair<DateTime, AnsiEscapeSequenceRequest> found = _sends.First (r => r.Value == ansiRequest);
            _sends.Remove (found.Key);
        }
    }

    private void SendDar ()
    {
        AnsiEscapeSequenceRequest ansiRequest = AnsiEscapeSequenceRequestUtils.CSI_SendDeviceAttributes;
        _sends.Add (DateTime.Now, ansiRequest);

        if (Application.Driver!.TryWriteAnsiRequest (Application.MainLoop!.MainLoopDriver, ref ansiRequest))
        {
            HandleResponse (ansiRequest);
        }
        else
        {
            HandleResponseError (ansiRequest);
        }
    }

    private void SetupGraph ()
    {
        _graphView.Series.Add (_sentSeries = new ());
        _graphView.Series.Add (_answeredSeries = new ());

        _sentSeries.Fill = new (new ('.'), new (ColorName16.BrightGreen, ColorName16.Black));
        _answeredSeries.Fill = new (new ('.'), new (ColorName16.BrightRed, ColorName16.Black));

        // Todo:
        // _graphView.Annotations.Add (_sentSeries new PathAnnotation {});

        _graphView.CellSize = new (1, 1);
        _graphView.MarginBottom = 2;
        _graphView.AxisX.Increment = 1;
        _graphView.AxisX.Text = "Seconds";
        _graphView.GraphColor = new Attribute (Color.Green, Color.Black);
    }

    private static Func<KeyValuePair<DateTime, AnsiEscapeSequenceRequest>, int> ToSeconds () { return t => (int)(DateTime.Now - t.Key).TotalSeconds; }

    private void UpdateGraph ()
    {
        System.Diagnostics.Debug.Assert (_sends.Count == 0);

        _sentSeries.Points = _sends
                             .GroupBy (ToSeconds ())
                             .Select (g => new PointF (g.Key, g.Count ()))
                             .ToList ();

        _answeredSeries.Points = _answers
                                 .Where (
                                         r => r.Value.AnsiEscapeSequenceResponse is { }
                                              && !string.IsNullOrEmpty (r.Value?.AnsiEscapeSequenceResponse.Response))
                                 .GroupBy (ToSeconds ())
                                 .Select (g => new PointF (g.Key, g.Count ()))
                                 .ToList ();

        //  _graphView.ScrollOffset  = new PointF(,0);
        if (_sentSeries.Points.Count > 0 || _answeredSeries.Points.Count > 0)
        {
            _graphView.SetNeedsDraw ();
        }
    }

    private void UpdateResponses ()
    {
        _lblSummary.Text = GetSummary ();
        _lblSummary.SetNeedsDraw ();

        _lblErrorSummary.Text = GetSummaryErrors ();
        _lblErrorSummary.SetNeedsDraw ();
    }
}
