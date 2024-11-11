using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Scroll Demo", "Demonstrates Scroll.")]
[ScenarioCategory ("Scrolling")]
public class ScrollDemo : Scenario
{
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
            ColorScheme = Colors.ColorSchemes ["Base"],
            Arrangement = ViewArrangement.Resizable
        };
        frameView.Padding.Thickness = new (1);
        frameView.Padding.Diagnostics = ViewDiagnosticFlags.Ruler;
        app.Add (frameView);

        var scroll = new Scroll
        {
            X = Pos.AnchorEnd (),
            Size = 1000,
        };
        frameView.Add (scroll);

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
            Value = scroll.Frame.Width,
            X = Pos.Right (lblWidthHeight) + 1,
            Y = Pos.Top (lblWidthHeight)
        };
        frameView.Add (scrollWidthHeight);

        scrollWidthHeight.ValueChanging += (s, e) =>
        {
            if (e.NewValue < 1
                || (e.NewValue
                    > (scroll.Orientation == Orientation.Vertical
                           ? scroll.SuperView?.GetContentSize ().Width
                           : scroll.SuperView?.GetContentSize ().Height)))
            {
                // TODO: This must be handled in the ScrollSlider if Width and Height being virtual
                e.Cancel = true;

                return;
            }

            if (scroll.Orientation == Orientation.Vertical)
            {
                scroll.Width = e.NewValue;
            }
            else
            {
                scroll.Height = e.NewValue;
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
                scroll.Orientation = Orientation.Vertical;
                scroll.X = Pos.AnchorEnd ();
                scroll.Y = 0;
                scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scroll.SuperView.GetContentSize ().Width);
                scroll.Width = scrollWidthHeight.Value;
            }
            else
            {
                scroll.Orientation = Orientation.Horizontal;
                scroll.X = 0;
                scroll.Y = Pos.AnchorEnd ();
                scrollWidthHeight.Value = Math.Min (scrollWidthHeight.Value, scroll.SuperView.GetContentSize ().Height);
                scroll.Height = scrollWidthHeight.Value;
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
            Value = scroll.Size,
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

            if (scroll.Size != e.NewValue)
            {
                scroll.Size = e.NewValue;
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

        Label scrollSliderPosition = new ()
        {
            Text = scroll.GetSliderPosition ().ToString (),
            X = Pos.Right (lblSliderPosition) + 1,
            Y = Pos.Top (lblSliderPosition)
        };
        frameView.Add (scrollSliderPosition);

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
            Value = scroll.GetSliderPosition (),
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

            if (scroll.ContentPosition != e.NewValue)
            {
                scroll.ContentPosition = e.NewValue;
            }

            if (scroll.ContentPosition != e.NewValue)
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

        var ckbShowPercent = new CheckBox
        {
            Y = Pos.Top (lblOptions),
            X = Pos.Right (lblOptions) + 1,
            Text = "Sho_wPercent",
            CheckedState = scroll.ShowPercent ? CheckState.Checked : CheckState.UnChecked
        };
        ckbShowPercent.CheckedStateChanging += (s, e) => scroll.ShowPercent = e.NewValue == CheckState.Checked;
        frameView.Add (ckbShowPercent);

        //var ckbKeepContentInAllViewport = new CheckBox
        //{
        //    X = Pos.Right (ckbShowScrollIndicator) + 1, Y = Pos.Bottom (scrollPosition), Text = "KeepContentInAllViewport",
        //    CheckedState = Scroll.KeepContentInAllViewport ? CheckState.Checked : CheckState.UnChecked
        //};
        //ckbKeepContentInAllViewport.CheckedStateChanging += (s, e) => Scroll.KeepContentInAllViewport = e.NewValue == CheckState.Checked;
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

        scroll.SubviewsLaidOut += (s, e) =>
        {
            lblScrollFrame.Text = $"Scroll Frame: {scroll.Frame.ToString ()}";
            lblScrollViewport.Text = $"Scroll Viewport: {scroll.Viewport.ToString ()}";
            lblScrollContentSize.Text = $"Scroll ContentSize: {scroll.GetContentSize ().ToString ()}";
        };

        EventLog eventLog = new ()
        {
            X = Pos.AnchorEnd () - 1,
            Y = 0,
            Height = Dim.Height (frameView),
            BorderStyle = LineStyle.Single,
            ViewToLog = scroll
        };
        app.Add (eventLog);
        frameView.Width = Dim.Fill (Dim.Func (() => Math.Max (28, eventLog.Frame.Width + 1)));

        app.Initialized += AppOnInitialized;


        void AppOnInitialized (object sender, EventArgs e)
        {
            scroll.SizeChanged += (s, e) =>
            {
                eventLog.Log ($"SizeChanged: {e.CurrentValue}");

                if (scrollSize.Value != e.CurrentValue)
                {
                    scrollSize.Value = e.CurrentValue;
                }
            };

            scroll.SliderPositionChanged += (s, e) =>
            {
                eventLog.Log ($"SliderPositionChanged: {e.CurrentValue}");
                eventLog.Log ($"  ContentPosition: {scroll.ContentPosition}");
                scrollSliderPosition.Text = e.CurrentValue.ToString ();
            };

            scroll.ContentPositionChanged += (s, e) =>
                                            {
                                                eventLog.Log ($"SliderPositionChanged: {e.CurrentValue}");
                                                eventLog.Log ($"  ContentPosition: {scroll.ContentPosition}");
                                                scrollContentPosition.Value = e.CurrentValue;
                                            };

            editor.Initialized += (s, e) =>
            {
                editor.ViewToEdit = scroll;
            };

        }

        Application.Run (app);
        app.Dispose ();
        Application.Shutdown ();
    }
}
