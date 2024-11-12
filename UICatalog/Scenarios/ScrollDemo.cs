//using System;
//using System.Collections.ObjectModel;
//using System.Linq;
//using Terminal.Gui;

//namespace UICatalog.Scenarios;

//[ScenarioMetadata ("Scroll Demo", "Demonstrates Scroll.")]
//[ScenarioCategory ("Scrolling")]
//public class ScrollDemo : Scenario
//{
//    public override void Main ()
//    {
//        Application.Init ();

//        Window app = new ()
//        {
//            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
//            Arrangement = ViewArrangement.Fixed
//        };

//        var demoFrame = new FrameView ()
//        {
//            Title = "Demo View",
//            X = 0,
//            Width = 75,
//            Height = 25 + 4,
//            ColorScheme = Colors.ColorSchemes ["Base"],
//            Arrangement = ViewArrangement.Resizable
//        };
//        demoFrame.Padding.Thickness = new (1);
//        demoFrame.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;

//        app.Add (demoFrame);

//        var scroll = new Scroll
//        {
//            X = Pos.AnchorEnd () - 5,
//            Size = 1000,
//        };
//        demoFrame.Add (scroll);

//        ListView controlledList = new ()
//        {
//            X = Pos.AnchorEnd (),
//            Width = 5,
//            Height = Dim.Fill (),
//            ColorScheme = Colors.ColorSchemes ["Error"],
//        };
//        demoFrame.Add (controlledList);

//        // populate the list box with Size items of the form "{n:00000}"
//        controlledList.SetSource (new ObservableCollection<string> (Enumerable.Range (0, scroll.Size).Select (n => $"{n:00000}")));

//        int GetMaxLabelWidth (int groupId)
//        {
//            return demoFrame.Subviews.Max (
//                                           v =>
//                                           {
//                                               if (v.Y.Has<PosAlign> (out var pos) && pos.GroupId == groupId)
//                                               {
//                                                   return v.Text.GetColumns ();
//                                               }

//                                               return 0;
//                                           });
//        }

//        var lblWidthHeight = new Label
//        {
//            Text = "_Width/Height:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))
//        };
//        demoFrame.Add (lblWidthHeight);

//        NumericUpDown<int> scrollWidthHeight = new ()
//        {
//            Value = scroll.Frame.Width,
//            X = Pos.Right (lblWidthHeight) + 1,
//            Y = Pos.Top (lblWidthHeight)
//        };
//        demoFrame.Add (scrollWidthHeight);

//        scrollWidthHeight.ValueChanging += (s, e) =>
//        {
//            if (e.NewValue < 1
//                || (e.NewValue
//                    > (scroll.Orientation == Orientation.Vertical
//                           ? scroll.SuperView?.GetContentSize ().Width
//                           : scroll.SuperView?.GetContentSize ().Height)))
//            {
//                // TODO: This must be handled in the ScrollSlider if Width and Height being virtual
//                e.Cancel = true;

//                return;
//            }

//            if (scroll.Orientation == Orientation.Vertical)
//            {
//                scroll.Width = e.NewValue;
//            }
//            else
//            {
//                scroll.Height = e.NewValue;
//            }
//        };


//        var lblOrientationabel = new Label
//        {
//            Text = "_Orientation:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))
//        };
//        demoFrame.Add (lblOrientationabel);

//        var rgOrientation = new RadioGroup
//        {
//            X = Pos.Right (lblOrientationabel) + 1,
//            Y = Pos.Top (lblOrientationabel),
//            RadioLabels = ["Vertical", "Horizontal"],
//            Orientation = Orientation.Horizontal
//        };
//        demoFrame.Add (rgOrientation);

//        rgOrientation.SelectedItemChanged += (s, e) =>
//        {
//            if (e.SelectedItem == e.PreviousSelectedItem)
//            {
//                return;
//            }

//            if (rgOrientation.SelectedItem == 0)
//            {
//                scroll.Orientation = Orientation.Vertical;
//                scroll.X = Pos.AnchorEnd ();
//                scroll.Y = 0;
//                scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scroll.SuperView.GetContentSize ().Width);
//                scroll.Width = scrollWidthHeight.Value;
//                controlledList.Visible = true;
//            }
//            else
//            {
//                scroll.Orientation = Orientation.Horizontal;
//                scroll.X = 0;
//                scroll.Y = Pos.AnchorEnd ();
//                scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scroll.SuperView.GetContentSize ().Height);
//                scroll.Height = scrollWidthHeight.Value;
//                controlledList.Visible = false;
//            }
//        };

//        var lblSize = new Label
//        {
//            Text = "_Size:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))
//        };
//        demoFrame.Add (lblSize);

//        NumericUpDown<int> scrollSize = new ()
//        {
//            Value = scroll.Size,
//            X = Pos.Right (lblSize) + 1,
//            Y = Pos.Top (lblSize)
//        };
//        demoFrame.Add (scrollSize);

//        scrollSize.ValueChanging += (s, e) =>
//        {
//            if (e.NewValue < 0)
//            {
//                e.Cancel = true;

//                return;
//            }

//            if (scroll.Size != e.NewValue)
//            {
//                scroll.Size = e.NewValue;
//            }
//        };

//        var lblSliderPosition = new Label
//        {
//            Text = "_SliderPosition:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))

//        };
//        demoFrame.Add (lblSliderPosition);
//        Label scrollSliderPosition = new ()
//        {
//            X = Pos.Right (lblSliderPosition) + 1,
//            Y = Pos.Top (lblSliderPosition)
//        };
//        demoFrame.Add (scrollSliderPosition);

//        var lblScrolled = new Label
//        {
//            Text = "_Scrolled:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))

//        };
//        demoFrame.Add (lblScrolled);
//        Label scrolled = new ()
//        {
//            X = Pos.Right (lblScrolled) + 1,
//            Y = Pos.Top (lblScrolled)
//        };
//        demoFrame.Add (scrolled);

//        var lblContentPosition = new Label
//        {
//            Text = "_ContentPosition:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))

//        };
//        demoFrame.Add (lblContentPosition);

//        NumericUpDown<int> scrollContentPosition = new ()
//        {
//            Value = scroll.GetSliderPosition (),
//            X = Pos.Right (lblContentPosition) + 1,
//            Y = Pos.Top (lblContentPosition)
//        };
//        demoFrame.Add (scrollContentPosition);

//        scrollContentPosition.ValueChanging += (s, e) =>
//        {
//            if (e.NewValue < 0)
//            {
//                e.Cancel = true;

//                return;
//            }

//            if (scroll.ContentPosition != e.NewValue)
//            {
//                scroll.ContentPosition = e.NewValue;
//            }

//            if (scroll.ContentPosition != e.NewValue)
//            {
//                e.Cancel = true;
//            }
//        };

//        var lblOptions = new Label
//        {
//            Text = "_Options:",
//            TextAlignment = Alignment.End,
//            Y = Pos.Align (Alignment.Start, groupId: 1),
//            Width = Dim.Func (() => GetMaxLabelWidth (1))
//        };
//        demoFrame.Add (lblOptions);

//        var ckbShowPercent = new CheckBox
//        {
//            Y = Pos.Top (lblOptions),
//            X = Pos.Right (lblOptions) + 1,
//            Text = "Sho_wPercent",
//            CheckedState = scroll.ShowPercent ? CheckState.Checked : CheckState.UnChecked
//        };
//        ckbShowPercent.CheckedStateChanging += (s, e) => scroll.ShowPercent = e.NewValue == CheckState.Checked;
//        demoFrame.Add (ckbShowPercent);

//        //var ckbKeepContentInAllViewport = new CheckBox
//        //{
//        //    X = Pos.Right (ckbShowScrollIndicator) + 1, Y = Pos.Bottom (scrollPosition), Text = "KeepContentInAllViewport",
//        //    CheckedState = Scroll.KeepContentInAllViewport ? CheckState.Checked : CheckState.UnChecked
//        //};
//        //ckbKeepContentInAllViewport.CheckedStateChanging += (s, e) => Scroll.KeepContentInAllViewport = e.NewValue == CheckState.Checked;
//        //view.Add (ckbKeepContentInAllViewport);

//        var lblScrollFrame = new Label
//        {
//            Y = Pos.Bottom (lblOptions) + 1
//        };
//        demoFrame.Add (lblScrollFrame);

//        var lblScrollViewport = new Label
//        {
//            Y = Pos.Bottom (lblScrollFrame)
//        };
//        demoFrame.Add (lblScrollViewport);

//        var lblScrollContentSize = new Label
//        {
//            Y = Pos.Bottom (lblScrollViewport)
//        };
//        demoFrame.Add (lblScrollContentSize);

//        scroll.SubviewsLaidOut += (s, e) =>
//        {
//            lblScrollFrame.Text = $"Scroll Frame: {scroll.Frame.ToString ()}";
//            lblScrollViewport.Text = $"Scroll Viewport: {scroll.Viewport.ToString ()}";
//            lblScrollContentSize.Text = $"Scroll ContentSize: {scroll.GetContentSize ().ToString ()}";
//        };

//        EventLog eventLog = new ()
//        {
//            X = Pos.AnchorEnd (),
//            Y = 0,
//            Height = Dim.Fill (),
//            BorderStyle = LineStyle.Single,
//            ViewToLog = scroll
//        };
//        app.Add (eventLog);

//        app.Initialized += AppOnInitialized;


//        void AppOnInitialized (object sender, EventArgs e)
//        {
//            scroll.SizeChanged += (s, e) =>
//            {
//                eventLog.Log ($"SizeChanged: {e.CurrentValue}");

//                if (scrollSize.Value != e.CurrentValue)
//                {
//                    scrollSize.Value = e.CurrentValue;
//                }
//            };

//            scroll.SliderPositionChanged += (s, e) =>
//            {
//                eventLog.Log ($"SliderPositionChanged: {e.CurrentValue}");
//                eventLog.Log ($"  ContentPosition: {scroll.ContentPosition}");
//                scrollSliderPosition.Text = e.CurrentValue.ToString ();
//            };

//            scroll.Scrolled += (s, e) =>
//                                            {
//                                                eventLog.Log ($"Scrolled: {e.CurrentValue}");
//                                                eventLog.Log ($"  SliderPosition: {scroll.GetSliderPosition ()}");
//                                                scrolled.Text = e.CurrentValue.ToString ();
//                                            };

//            scroll.ContentPositionChanged += (s, e) =>
//                                            {
//                                                eventLog.Log ($"ContentPositionChanged: {e.CurrentValue}");
//                                                scrollContentPosition.Value = e.CurrentValue;
//                                                controlledList.Viewport = controlledList.Viewport with { Y = e.CurrentValue };
//                                            };


//            controlledList.ViewportChanged += (s, e) =>
//                                              {
//                                                  eventLog.Log ($"ViewportChanged: {e.NewViewport.Y}");
//                                                  scroll.ContentPosition = e.NewViewport.Y;
//                                              };

//        }

//        Application.Run (app);
//        app.Dispose ();
//        Application.Shutdown ();
//    }
//}
