using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("ScrollBar Demo", "Demonstrates using ScrollBar view.")]
[ScenarioCategory ("Scrolling")]
public class ScrollBarDemo : Scenario
{
    private ViewDiagnosticFlags _diagnosticFlags;

    public override void Main ()
    {
        Application.Init ();

        Window app = new ()
        {
            Title = $"{Application.QuitKey} to Quit - Scenario: {GetName ()}",
            Arrangement = ViewArrangement.Fixed
        };

        var editor = new AdornmentsEditor ();
        app.Add (editor);

        var frameView = new FrameView
        {
            Title = "Demo View",
            X = Pos.Right (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            ColorScheme = Colors.ColorSchemes ["Base"]
        };
        app.Add (frameView);

        var scrollBar = new ScrollBar
        {
            X = Pos.AnchorEnd (),
            AutoHide = false
            //ShowPercent = true
        };
        frameView.Add (scrollBar);

        app.Loaded += (s, e) =>
                      {
                          scrollBar.Size = scrollBar.Viewport.Height;
                      };

        int GetMaxLabelWidth (int groupId)
        {
            return frameView.Subviews.Max (
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
        frameView.Add (lblWidthHeight);

        NumericUpDown<int> scrollWidthHeight = new ()
        {
            Value = scrollBar.Frame.Width,
            X = Pos.Right (lblWidthHeight) + 1,
            Y = Pos.Top (lblWidthHeight)
        };
        frameView.Add (scrollWidthHeight);

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
        frameView.Add (lblOrientationabel);

        var rgOrientation = new RadioGroup
        {
            X = Pos.Right (lblOrientationabel) + 1,
            Y = Pos.Top (lblOrientationabel),
            RadioLabels = ["Vertical", "Horizontal"],
            Orientation = Orientation.Horizontal
        };
        frameView.Add (rgOrientation);

        rgOrientation.SelectedItemChanged += (s, e) =>
                                             {
                                                 if (e.SelectedItem == e.PreviousSelectedItem)
                                                 {
                                                     return;
                                                 }

                                                 if (rgOrientation.SelectedItem == 0)
                                                 {
                                                     scrollBar.Orientation = Orientation.Vertical;
                                                     scrollBar.X = Pos.AnchorEnd ();
                                                     scrollBar.Y = 0;
                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scrollBar.SuperView.GetContentSize ().Width);
                                                     scrollBar.Width = scrollWidthHeight.Value;
                                                     scrollBar.Size /= 3;
                                                 }
                                                 else
                                                 {
                                                     scrollBar.Orientation = Orientation.Horizontal;
                                                     scrollBar.X = 0;
                                                     scrollBar.Y = Pos.AnchorEnd ();
                                                     scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scrollBar.SuperView.GetContentSize ().Height);
                                                     scrollBar.Height = scrollWidthHeight.Value;
                                                     scrollBar.Size *= 3;
                                                 }
                                             };

        var lblSize = new Label
        {
            Text = "_Size:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))
        };
        frameView.Add (lblSize);

        NumericUpDown<int> scrollSize = new ()
        {
            Value = scrollBar.Size,
            X = Pos.Right (lblSize) + 1,
            Y = Pos.Top (lblSize)
        };
        frameView.Add (scrollSize);

        scrollSize.ValueChanging += (s, e) =>
                                    {
                                        if (e.NewValue < 0)
                                        {
                                            e.Cancel = true;

                                            return;
                                        }

                                        if (scrollBar.Size != e.NewValue)
                                        {
                                            scrollBar.Size = e.NewValue;
                                        }
                                    };

        var lblSliderPosition = new Label
        {
            Text = "_SliderPosition:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))

        };
        frameView.Add (lblSliderPosition);

        NumericUpDown<int> scrollSliderPosition = new ()
        {
            Value = scrollBar.SliderPosition,
            X = Pos.Right (lblSliderPosition) + 1,
            Y = Pos.Top (lblSliderPosition)
        };
        frameView.Add (scrollSliderPosition);

        scrollSliderPosition.ValueChanging += (s, e) =>
                                              {
                                                  if (e.NewValue < 0)
                                                  {
                                                      e.Cancel = true;

                                                      return;
                                                  }

                                                  if (scrollBar.SliderPosition != e.NewValue)
                                                  {
                                                      scrollBar.SliderPosition = e.NewValue;
                                                  }

                                                  if (scrollBar.SliderPosition != e.NewValue)
                                                  {
                                                      e.Cancel = true;
                                                  }
                                              };

        var lblContentPosition = new Label
        {
            Text = "_ContentPosition:",
            TextAlignment = Alignment.End,
            Y = Pos.Align (Alignment.Start, groupId: 1),
            Width = Dim.Func (() => GetMaxLabelWidth (1))

        };
        frameView.Add (lblContentPosition);

        NumericUpDown<int> scrollContentPosition = new ()
        {
            Value = scrollBar.SliderPosition,
            X = Pos.Right (lblContentPosition) + 1,
            Y = Pos.Top (lblContentPosition)
        };
        frameView.Add (scrollContentPosition);

        scrollContentPosition.ValueChanging += (s, e) =>
                                               {
                                                   if (e.NewValue < 0)
                                                   {
                                                       e.Cancel = true;

                                                       return;
                                                   }

                                                   if (scrollBar.ContentPosition != e.NewValue)
                                                   {
                                                       scrollBar.ContentPosition = e.NewValue;
                                                   }

                                                   if (scrollBar.ContentPosition != e.NewValue)
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
        frameView.Add (lblOptions);
        var ckbAutoHide = new CheckBox
        {
            Y = Pos.Top (lblOptions),
            X = Pos.Right (lblOptions) + 1,
            Text = "Auto_HideScrollBar",
            CheckedState = scrollBar.AutoHide ? CheckState.Checked : CheckState.UnChecked
        };
        ckbAutoHide.CheckedStateChanging += (s, e) => scrollBar.AutoHide = e.NewValue == CheckState.Checked;
        frameView.Add (ckbAutoHide);

        var ckbShowPercent = new CheckBox
        {
            Y = Pos.Top (lblOptions),
            X = Pos.Right (ckbAutoHide) + 1,
            Text = "Sho_wPercent",
            CheckedState = scrollBar.ShowPercent ? CheckState.Checked : CheckState.UnChecked
        };
        ckbShowPercent.CheckedStateChanging += (s, e) => scrollBar.ShowPercent = e.NewValue == CheckState.Checked;
        frameView.Add (ckbShowPercent);

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
        frameView.Add (lblScrollFrame);

        var lblScrollViewport = new Label
        {
            Y = Pos.Bottom (lblScrollFrame)
        };
        frameView.Add (lblScrollViewport);

        var lblScrollContentSize = new Label
        {
            Y = Pos.Bottom (lblScrollViewport)
        };
        frameView.Add (lblScrollContentSize);

        scrollBar.SubviewsLaidOut += (s, e) =>
                                     {
                                         lblScrollFrame.Text = $"Scroll Frame: {scrollBar.Frame.ToString ()}";
                                         lblScrollViewport.Text = $"Scroll Viewport: {scrollBar.Viewport.ToString ()}";
                                         lblScrollContentSize.Text = $"Scroll ContentSize: {scrollBar.GetContentSize ().ToString ()}";
                                     };

        EventLog eventLog = new ()
        {
            X = Pos.AnchorEnd () - 1,
            Y = 0,
            Height = Dim.Height (frameView),
            BorderStyle = LineStyle.Single,
            ViewToLog = scrollBar
        };
        app.Add (eventLog);
        frameView.Width = Dim.Fill (Dim.Func (() => Math.Max (28, eventLog.Frame.Width + 1)));

        app.Initialized += AppOnInitialized;


        void AppOnInitialized (object sender, EventArgs e)
        {
            scrollBar.SizeChanged += (s, e) =>
                                     {
                                         eventLog.Log ($"SizeChanged: {e.CurrentValue}");

                                         if (scrollSize.Value != e.CurrentValue)
                                         {
                                             scrollSize.Value = e.CurrentValue;
                                         }
                                     };

            scrollBar.SliderPositionChanging += (s, e) =>
                                          {
                                              eventLog.Log ($"SliderPositionChanging: {e.CurrentValue}");
                                              eventLog.Log ($"  NewValue: {e.NewValue}");
                                          };

            scrollBar.SliderPositionChanged += (s, e) =>
                                         {
                                             eventLog.Log ($"SliderPositionChanged: {e.CurrentValue}");
                                             eventLog.Log ($"  ContentPosition: {scrollBar.ContentPosition}");
                                             scrollSliderPosition.Value = e.CurrentValue;
                                         };

            editor.Initialized += (s, e) =>
                                  {
                                      scrollBar.Size = int.Max (app.GetContentSize ().Height * 2, app.GetContentSize ().Width * 2);
                                      editor.ViewToEdit = scrollBar;
                                  };

        }

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
