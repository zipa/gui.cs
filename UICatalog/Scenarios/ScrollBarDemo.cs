using System;
using System.Collections.ObjectModel;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBar Demo", "Demonstrates ScrollBar.")]
[ScenarioCategory ("Scrolling")]
public class ScrollBarDemo : Scenario
{
    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            Arrangement = ViewArrangement.Fixed
        };

        var demoFrame = new FrameView ()
        {
            Title = "Demo View",
            X = 0,
            Width = 75,
            Height = 25 + 4,
            ColorScheme = Colors.ColorSchemes ["Base"],
            Arrangement = ViewArrangement.Resizable
        };
        demoFrame!.Padding!.Thickness = new (1);
        demoFrame.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;
        app.Add (demoFrame);

        var scrollBar = new ScrollBar
        {
            X = Pos.AnchorEnd () - 5,
            AutoHide = false,
            ScrollableContentSize = 100,
            //ShowPercent = true
        };
        demoFrame.Add (scrollBar);

        ListView controlledList = new ()
        {
            X = Pos.AnchorEnd (),
            Width = 5,
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Error"],
        };

        demoFrame.Add (controlledList);

        // populate the list box with Size items of the form "{n:00000}"
        controlledList.SetSource (new ObservableCollection<string> (Enumerable.Range (0, scrollBar.ScrollableContentSize).Select (n => $"{n:00000}")));

        int GetMaxLabelWidth (int groupId)
        {
            return demoFrame.Subviews.Max (
                                           v =>
                                           {
                                               if (v.Y.Has<PosAlign> (out var pos) && pos.GroupId == groupId)
                                               {
                                                   return v.Text.GetColumns ();
                                               }

                                               return 0;
                                           });
        }

        var lblWidthHeight = new Label
        {
            Text = "_Width/Height:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblWidthHeight);

        NumericUpDown<int> scrollWidthHeight = new ()
        {
            Value = scrollBar.Frame.Width,
            X = Pos.Right (lblWidthHeight) + 1,
            Y = Pos.Top (lblWidthHeight)
        };
        demoFrame.Add (scrollWidthHeight);

        scrollWidthHeight.ValueChanging += (s, e) =>
                                           {
                                               if (e.NewValue < 1
                                                   || (e.NewValue
                                                       > (scrollBar.Orientation == Orientation.Vertical
                                                              ? scrollBar.SuperView?.GetContentSize ().Width
                                                              : scrollBar.SuperView?.GetContentSize ().Height)))
                                               {
                                                   // TODO: This must be handled in the ScrollSlider if Width and Height being virtual
                                                   e.Cancel = true;

                                                   return;
                                               }

                                               if (scrollBar.Orientation == Orientation.Vertical)
                                               {
                                                   scrollBar.Width = e.NewValue;
                                               }
                                               else
                                               {
                                                   scrollBar.Height = e.NewValue;
                                               }
                                           };


        var lblOrientationabel = new Label
        {
            Text = "_Orientation:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblOrientationabel);

        var rgOrientation = new RadioGroup
        {
            X = Pos.Right (lblOrientationabel) + 1,
            Y = Pos.Top (lblOrientationabel),
            RadioLabels = ["Vertical", "Horizontal"],
            Orientation = Orientation.Horizontal
        };
        demoFrame.Add (rgOrientation);

        rgOrientation.SelectedItemChanged += (s, e) =>
                                             {
                                                 if (e.SelectedItem == e.PreviousSelectedItem)
                                                 {
                                                     return;
                                                 }

                                                 if (rgOrientation.SelectedItem == 0)
                                                 {
                                                     scrollBar.Orientation = Orientation.Vertical;
                                                     scrollBar.X = Pos.AnchorEnd () - 5;
                                                     scrollBar.Y = 0;
                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scrollBar.SuperView!.GetContentSize ().Width);
                                                     scrollBar.Width = scrollWidthHeight.Value;
                                                     controlledList.Visible = true;
                                                 }
                                                 else
                                                 {
                                                     scrollBar.Orientation = Orientation.Horizontal;
                                                     scrollBar.X = 0;
                                                     scrollBar.Y = Pos.AnchorEnd ();
                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scrollBar.SuperView!.GetContentSize ().Height);
                                                     scrollBar.Height = scrollWidthHeight.Value;
                                                     controlledList.Visible = false;

                                                 }
                                             };

        var lblSize = new Label
        {
            Text = "_Size:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblSize);

        NumericUpDown<int> scrollSize = new ()
        {
            Value = scrollBar.ScrollableContentSize,
            X = Pos.Right (lblSize) + 1,
            Y = Pos.Top (lblSize)
        };
        demoFrame.Add (scrollSize);

        scrollSize.ValueChanging += (s, e) =>
                                    {
                                        if (e.NewValue < 0)
                                        {
                                            e.Cancel = true;

                                            return;
                                        }

                                        if (scrollBar.ScrollableContentSize != e.NewValue)
                                        {
                                            scrollBar.ScrollableContentSize = e.NewValue;
                                        }
                                    };

        var lblVisibleContentSize = new Label
        {
            Text = "_VisibleContentSize::",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblVisibleContentSize);

        NumericUpDown<int> VisibleContentSize = new ()
        {
            Value = scrollBar.VisibleContentSize,
            X = Pos.Right (lblVisibleContentSize) + 1,
            Y = Pos.Top (lblVisibleContentSize)
        };
        demoFrame.Add (VisibleContentSize);

        VisibleContentSize.ValueChanging += (s, e) =>
                                           {
                                               if (e.NewValue < 0)
                                               {
                                                   e.Cancel = true;

                                                   return;
                                               }

                                               if (scrollBar.VisibleContentSize != e.NewValue)
                                               {
                                                   scrollBar.VisibleContentSize = e.NewValue;
                                               }
                                           };

        var lblSliderPosition = new Label
        {
            Text = "_SliderPosition:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))

        };
        demoFrame.Add (lblSliderPosition);

        Label scrollSliderPosition = new ()
        {
            Text = scrollBar.GetSliderPosition ().ToString (),
            X = Pos.Right (lblSliderPosition) + 1,
            Y = Pos.Top (lblSliderPosition)
        };
        demoFrame.Add (scrollSliderPosition);

        var lblScrolled = new Label
        {
            Text = "_Scrolled:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))

        };
        demoFrame.Add (lblScrolled);
        Label scrolled = new ()
        {
            X = Pos.Right (lblScrolled) + 1,
            Y = Pos.Top (lblScrolled)
        };
        demoFrame.Add (scrolled);

        var lblContentPosition = new Label
        {
            Text = "_ContentPosition:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))

        };
        demoFrame.Add (lblContentPosition);

        NumericUpDown<int> scrollContentPosition = new ()
        {
            Value = scrollBar.GetSliderPosition (),
            X = Pos.Right (lblContentPosition) + 1,
            Y = Pos.Top (lblContentPosition)
        };
        demoFrame.Add (scrollContentPosition);

        scrollContentPosition.ValueChanging += (s, e) =>
                                               {
                                                   if (e.NewValue < 0)
                                                   {
                                                       e.Cancel = true;

                                                       return;
                                                   }

                                                   if (scrollBar.Position != e.NewValue)
                                                   {
                                                       scrollBar.Position = e.NewValue;
                                                   }

                                                   if (scrollBar.Position != e.NewValue)
                                                   {
                                                       e.Cancel = true;
                                                   }
                                               };

        var lblOptions = new Label
        {
            Text = "_Options:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))
        };
        demoFrame.Add (lblOptions);
        var ckbAutoHide = new CheckBox
        {
            Y = Pos.Top (lblOptions),
            X = Pos.Right (lblOptions) + 1,
            Text = "Auto_HideScrollBar",
            CheckedState = scrollBar.AutoHide ? CheckState.Checked : CheckState.UnChecked
        };
        ckbAutoHide.CheckedStateChanging += (s, e) => scrollBar.AutoHide = e.NewValue == CheckState.Checked;
        demoFrame.Add (ckbAutoHide);

        var ckbShowPercent = new CheckBox
        {
            Y = Pos.Top (lblOptions),
            X = Pos.Right (ckbAutoHide) + 1,
            Text = "Sho_wPercent",
            CheckedState = scrollBar.ShowPercent ? CheckState.Checked : CheckState.UnChecked
        };
        ckbShowPercent.CheckedStateChanging += (s, e) => scrollBar.ShowPercent = e.NewValue == CheckState.Checked;
        demoFrame.Add (ckbShowPercent);

        //var ckbKeepContentInAllViewport = new CheckBox
        //{
        //    X = Pos.Right (ckbShowScrollIndicator) + 1, Y = Pos.Bottom (scrollPosition), Text = "KeepContentInAllViewport",
        //    CheckedState = scrollBar.KeepContentInAllViewport ? CheckState.Checked : CheckState.UnChecked
        //};
        //ckbKeepContentInAllViewport.CheckedStateChanging += (s, e) => scrollBar.KeepContentInAllViewport = e.NewValue == CheckState.Checked;
        //view.Add (ckbKeepContentInAllViewport);

        var lblScrollFrame = new Label
        {
            Y = Pos.Bottom (lblOptions) + 1
        };
        demoFrame.Add (lblScrollFrame);

        var lblScrollViewport = new Label
        {
            Y = Pos.Bottom (lblScrollFrame)
        };
        demoFrame.Add (lblScrollViewport);

        var lblScrollContentSize = new Label
        {
            Y = Pos.Bottom (lblScrollViewport)
        };
        demoFrame.Add (lblScrollContentSize);

        scrollBar.SubviewsLaidOut += (s, e) =>
                                     {
                                         lblScrollFrame.Text = $"Scroll Frame: {scrollBar.Frame.ToString ()}";
                                         lblScrollViewport.Text = $"Scroll Viewport: {scrollBar.Viewport.ToString ()}";
                                         lblScrollContentSize.Text = $"Scroll ContentSize: {scrollBar.GetContentSize ().ToString ()}";
                                         VisibleContentSize.Value = scrollBar.VisibleContentSize;
                                     };

        EventLog eventLog = new ()
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Height = Dim.Fill (),
            BorderStyle = LineStyle.Single,
            ViewToLog = scrollBar
        };
        app.Add (eventLog);

        app.Initialized += AppOnInitialized;

        void AppOnInitialized (object sender, EventArgs e)
        {
            scrollBar.ScrollableContentSizeChanged += (s, e) =>
                                  {
                                      eventLog.Log ($"SizeChanged: {e.CurrentValue}");

                                      if (scrollSize.Value != e.CurrentValue)
                                      {
                                          scrollSize.Value = e.CurrentValue;
                                      }
                                  };

            scrollBar.SliderPositionChanged += (s, e) =>
                                            {
                                                eventLog.Log ($"SliderPositionChanged: {e.CurrentValue}");
                                                eventLog.Log ($"  ContentPosition: {scrollBar.Position}");
                                                scrollSliderPosition.Text = e.CurrentValue.ToString ();
                                            };

            scrollBar.Scrolled += (s, e) =>
                               {
                                   eventLog.Log ($"Scrolled: {e.CurrentValue}");
                                   eventLog.Log ($"  SliderPosition: {scrollBar.GetSliderPosition ()}");
                                   scrolled.Text = e.CurrentValue.ToString ();
                               };

            scrollBar.PositionChanged += (s, e) =>
                                             {
                                                 eventLog.Log ($"ContentPositionChanged: {e.CurrentValue}");
                                                 scrollContentPosition.Value = e.CurrentValue;
                                                 controlledList.Viewport = controlledList.Viewport with { Y = e.CurrentValue };
                                             };


            controlledList.ViewportChanged += (s, e) =>
                                              {
                                                  eventLog.Log ($"ViewportChanged: {e.NewViewport.Y}");
                                                  scrollBar.Position = e.NewViewport.Y;
                                              };

        }

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
