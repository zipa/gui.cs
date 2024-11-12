//using Microsoft.VisualStudio.TestPlatform.Utilities;
//using Xunit.Abstractions;

//namespace Terminal.Gui.ViewsTests;

//public class ScrollTests
//{
//    public ScrollTests (ITestOutputHelper output) { _output = output; }
//    private readonly ITestOutputHelper _output;


//    [Fact]
//    public void OnOrientationChanged_Keeps_Size ()
//    {
//        var scroll = new Scroll ();
//        scroll.Layout ();
//        scroll.Size = 1;

//        scroll.Orientation = Orientation.Horizontal;
//        Assert.Equal (1, scroll.Size);
//    }

//    [Fact]
//    public void OnOrientationChanged_Sets_ContentPosition_To_0 ()
//    {
//        View super = new View ()
//        {
//            Id = "super",
//            Width = 10,
//            Height = 10
//        };
//        var scroll = new Scroll ()
//        {
//        };
//        super.Add (scroll);
//        scroll.Layout ();
//        scroll.ContentPosition = 1;
//        scroll.Orientation = Orientation.Horizontal;

//        Assert.Equal (0, scroll.ContentPosition);
//    }


//    //    [Theory]
//    //    [AutoInitShutdown]
//    //    [InlineData (
//    //                    20,
//    //                    @"
//    //█
//    //█
//    //█
//    //█
//    //█
//    //░
//    //░
//    //░
//    //░
//    //░",
//    //                    @"
//    //░
//    //░
//    //█
//    //█
//    //█
//    //█
//    //█
//    //░
//    //░
//    //░",
//    //                    @"
//    //░
//    //░
//    //░
//    //░
//    //░
//    //█
//    //█
//    //█
//    //█
//    //█",
//    //                    @"
//    //░
//    //░
//    //█
//    //█
//    //█
//    //░
//    //░
//    //░
//    //░
//    //░",
//    //                    @"
//    //█████░░░░░",
//    //                    @"
//    //░░█████░░░",
//    //                    @"
//    //░░░░░█████",
//    //                    @"
//    //░░███░░░░░")]
//    //    [InlineData (
//    //                    40,
//    //                    @"
//    //█
//    //█
//    //█
//    //░
//    //░
//    //░
//    //░
//    //░
//    //░
//    //░",
//    //                    @"
//    //░
//    //█
//    //█
//    //█
//    //░
//    //░
//    //░
//    //░
//    //░
//    //░",
//    //                    @"
//    //░
//    //░
//    //█
//    //█
//    //█
//    //░
//    //░
//    //░
//    //░
//    //░",
//    //                    @"
//    //░
//    //█
//    //█
//    //░
//    //░
//    //░
//    //░
//    //░
//    //░
//    //░",
//    //                    @"
//    //███░░░░░░░",
//    //                    @"
//    //░███░░░░░░",
//    //                    @"
//    //░░███░░░░░",
//    //                    @"
//    //░██░░░░░░░")]
//    //    public void Changing_Position_Size_Orientation_Draws_Correctly_KeepContentInAllViewport_True (
//    //        int size,
//    //        string firstVertExpected,
//    //        string middleVertExpected,
//    //        string endVertExpected,
//    //        string sizeVertExpected,
//    //        string firstHoriExpected,
//    //        string middleHoriExpected,
//    //        string endHoriExpected,
//    //        string sizeHoriExpected
//    //    )
//    //    {
//    //        var scroll = new Scroll
//    //        {
//    //            Orientation = Orientation.Vertical,
//    //            Size = size,
//    //            Height = 10,
//    //            KeepContentInAllViewport = true
//    //        };
//    //        var top = new Toplevel ();
//    //        top.Add (scroll);
//    //        RunState rs = Application.Begin (top);
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (firstVertExpected, _output);

//    //        scroll.Position = 4;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (middleVertExpected, _output);

//    //        scroll.Position = 10;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (endVertExpected, _output);

//    //        scroll.Size = size * 2;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (sizeVertExpected, _output);

//    //        scroll.Orientation = Orientation.Horizontal;
//    //        scroll.Width = 10;
//    //        scroll.Height = 1;
//    //        scroll.Position = 0;
//    //        scroll.Size = size;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (firstHoriExpected, _output);

//    //        scroll.Position = 4;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (middleHoriExpected, _output);

//    //        scroll.Position = 10;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (endHoriExpected, _output);

//    //        scroll.Size = size * 2;
//    //        Application.RunIteration (ref rs);

//    //        _ = TestHelpers.AssertDriverContentsWithFrameAre (sizeHoriExpected, _output);
//    //    }

//    [Fact]
//    public void Constructor_Defaults ()
//    {
//        var scroll = new Scroll ();
//        Assert.False (scroll.CanFocus);
//        Assert.Equal (Orientation.Vertical, scroll.Orientation);
//        Assert.Equal (0, scroll.Size);
//        Assert.Equal (0, scroll.GetSliderPosition ());
//    }

//    //[Fact]
//    //[AutoInitShutdown]
//    //public void KeepContentInAllViewport_True_False_KeepContentInAllViewport_True ()
//    //{
//    //    var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
//    //    view.Padding.Thickness = new (0, 0, 2, 0);
//    //    view.SetContentSize (new (view.Viewport.Width, 30));
//    //    var scroll = new Scroll { Width = 2, Height = Dim.Fill (), Size = view.GetContentSize ().Height, KeepContentInAllViewport = true };
//    //    scroll.PositionChanged += (_, e) => view.Viewport = view.Viewport with { Y = e.CurrentValue };
//    //    view.Padding.Add (scroll);
//    //    var top = new Toplevel ();
//    //    top.Add (view);
//    //    Application.Begin (top);

//    //    Assert.True (scroll.KeepContentInAllViewport);
//    //    Assert.Equal (80, view.Padding.Viewport.Width);
//    //    Assert.Equal (25, view.Padding.Viewport.Height);
//    //    Assert.Equal (2, scroll.Viewport.Width);
//    //    Assert.Equal (25, scroll.Viewport.Height);
//    //    Assert.Equal (30, scroll.Size);

//    //    scroll.KeepContentInAllViewport = false;
//    //    scroll.Position = 50;
//    //    Assert.Equal (scroll.Position, scroll.Size - 1);
//    //    Assert.Equal (scroll.Position, view.Viewport.Y);
//    //    Assert.Equal (29, scroll.Position);
//    //    Assert.Equal (29, view.Viewport.Y);

//    //    top.Dispose ();
//    //}


//    //[Theory]
//    //[AutoInitShutdown]
//    //[InlineData (Orientation.Vertical)]
//    //[InlineData (Orientation.Horizontal)]
//    //public void Moving_Mouse_Outside_Host_Ensures_Correct_Location_KeepContentInAllViewport_True (Orientation orientation)
//    //{
//    //    var scroll = new Scroll
//    //    {
//    //        X = 10, Y = 10, Width = orientation == Orientation.Vertical ? 1 : 10, Height = orientation == Orientation.Vertical ? 10 : 1, Size = 20,
//    //        SliderPosition = 5, Orientation = orientation
//    //    };
//    //    var top = new Toplevel ();
//    //    top.Add (scroll);
//    //    RunState rs = Application.Begin (top);

//    //    Rectangle scrollSliderFrame = scroll.Subviews.FirstOrDefault (x => x.Id == "scrollSlider")!.Frame;
//    //    Assert.Equal (scrollSliderFrame, orientation == Orientation.Vertical ? new (0, 2, 1, 5) : new (2, 0, 5, 1));

//    //    Application.RaiseMouseEvent (new () { ScreenPosition = orientation == Orientation.Vertical ? new (10, 12) : new (12, 10), Flags = MouseFlags.Button1Pressed });
//    //    Application.RunIteration (ref rs);

//    //    Application.RaiseMouseEvent (
//    //                              new ()
//    //                              {
//    //                                  ScreenPosition = orientation == Orientation.Vertical ? new (10, 0) : new (0, 10),
//    //                                  Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
//    //                              });
//    //    Application.RunIteration (ref rs);
//    //    Assert.Equal (new (0, 0), scroll.Subviews.FirstOrDefault (x => x.Id == "scrollSlider")!.Frame.Location);

//    //    Application.RaiseMouseEvent (
//    //                              new ()
//    //                              {
//    //                                  ScreenPosition = orientation == Orientation.Vertical ? new (0, 25) : new (80, 0),
//    //                                  Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
//    //                              });
//    //    Application.RunIteration (ref rs);

//    //    Assert.Equal (
//    //                  orientation == Orientation.Vertical ? new (0, 5) : new (5, 0),
//    //                  scroll.Subviews.FirstOrDefault (x => x.Id == "scrollSlider")!.Frame.Location);
//    //}

//    [Theory]
//    [CombinatorialData]
//    [AutoInitShutdown]
//    public void Mouse_Wheel_Increments_Position ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
//    {
//        var top = new Toplevel ()
//        {
//            Id = "top",
//            Width = 10,
//            Height = 10
//        };
//        var scroll = new Scroll
//        {
//            Id = "scroll",
//            Orientation = orientation,
//            Size = 20,
//            Increment = increment
//        };

//        top.Add (scroll);
//        RunState rs = Application.Begin (top);

//        Assert.Equal (0, scroll.GetSliderPosition ());
//        Assert.Equal (0, scroll.ContentPosition);

//        Application.RaiseMouseEvent (new ()
//        {
//            ScreenPosition = new (0, 0),
//            Flags = orientation == Orientation.Vertical ? MouseFlags.WheeledDown : MouseFlags.WheeledRight
//        });
//        Application.RunIteration (ref rs);

//        Assert.Equal (increment, scroll.ContentPosition);

//        Application.RaiseMouseEvent (new ()
//        {
//            ScreenPosition = new (0, 0),
//            Flags = orientation == Orientation.Vertical ? MouseFlags.WheeledDown : MouseFlags.WheeledRight
//        });
//        Application.RunIteration (ref rs);

//        Assert.Equal (increment * 2, scroll.ContentPosition);

//        Application.RaiseMouseEvent (new ()
//        {
//            ScreenPosition = new (0, 0),
//            Flags = orientation == Orientation.Vertical ? MouseFlags.WheeledUp : MouseFlags.WheeledLeft
//        });
//        Application.RunIteration (ref rs);

//        Assert.Equal (increment, scroll.ContentPosition);

//        Application.ResetState (true);
//    }

//    [Theory]
//    [CombinatorialData]
//    [AutoInitShutdown]
//    public void Mouse_Click_Outside_Slider_Moves (Orientation orientation)
//    {
//        var top = new Toplevel ()
//        {
//            Id = "top",
//            Width = 10,
//            Height = 10
//        };
//        var scroll = new Scroll
//        {
//            Id = "scroll",
//            Orientation = orientation,
//            Size = 20,
//        };

//        top.Add (scroll);
//        RunState rs = Application.Begin (top);
//        scroll.ContentPosition = 5;
//        Application.RunIteration (ref rs);

//        Assert.Equal (5, scroll.GetSliderPosition ());
//        Assert.Equal (10, scroll.ContentPosition);

//        Application.RaiseMouseEvent (new ()
//        {
//            ScreenPosition = new (0, 0),
//            Flags = MouseFlags.Button1Clicked
//        });
//        Application.RunIteration (ref rs);

//        Assert.Equal (0, scroll.GetSliderPosition ());
//        Assert.Equal (0, scroll.ContentPosition);

//        Application.ResetState (true);
//    }

//    [Theory]
//    [CombinatorialData]
//    public void Position_Clamps_To_Size (
//        [CombinatorialRange (1, 6, 1)] int scrollSize,
//        [CombinatorialRange (-1, 6, 1)] int scrollPosition,
//        Orientation orientation
//    )
//    {
//        var super = new View
//        {
//            Id = "super",
//            Width = 5,
//            Height = 5
//        };

//        var scroll = new Scroll
//        {
//            Orientation = orientation,
//            Width = Dim.Fill (),
//            Height = Dim.Fill ()
//        };
//        super.Add (scroll);
//        scroll.Size = scrollSize;
//        super.Layout ();

//        scroll.ContentPosition = scrollPosition;
//        super.Layout ();

//        Assert.True (scroll.GetSliderPosition () <= scrollSize);
//    }

//    [Fact]
//    public void PositionChanging_PositionChanged_Events_Only_Raises_Once_If_Position_Was_Really_Changed ()
//    {
//        var changing = 0;
//        var cancel = false;
//        var changed = 0;
//        var scroll = new Scroll { Height = 10, Size = 20 };
//        scroll.SliderPositionChanged += Scroll_PositionChanged;

//        Assert.Equal (Orientation.Vertical, scroll.Orientation);
//        scroll.Layout ();
//        Assert.Equal (new (0, 0, 1, 10), scroll.Viewport);
//        Assert.Equal (0, scroll.GetSliderPosition ());
//        Assert.Equal (0, changing);
//        Assert.Equal (0, changed);

//        scroll.ContentPosition = 0;
//        Assert.Equal (0, scroll.GetSliderPosition ());
//        Assert.Equal (0, changing);
//        Assert.Equal (0, changed);

//        scroll.ContentPosition = 1;
//        Assert.Equal (1, scroll.GetSliderPosition ());
//        Assert.Equal (1, changing);
//        Assert.Equal (1, changed);

//        Reset ();
//        cancel = true;
//        scroll.ContentPosition = 2;
//        Assert.Equal (1, scroll.GetSliderPosition ());
//        Assert.Equal (1, changing);
//        Assert.Equal (0, changed);

//        Reset ();
//        scroll.ContentPosition = 10;
//        Assert.Equal (5, scroll.GetSliderPosition ());
//        Assert.Equal (1, changing);
//        Assert.Equal (1, changed);

//        Reset ();
//        scroll.ContentPosition = 11;
//        Assert.Equal (5, scroll.GetSliderPosition ());
//        Assert.Equal (1, changing);
//        Assert.Equal (1, changed);

//        Reset ();
//        scroll.ContentPosition = 0;
//        Assert.Equal (0, scroll.GetSliderPosition ());
//        Assert.Equal (1, changing);
//        Assert.Equal (1, changed);

//        scroll.SliderPositionChanged -= Scroll_PositionChanged;

//        void Scroll_PositionChanged (object sender, EventArgs<int> e) { changed++; }

//        void Reset ()
//        {
//            changing = 0;
//            cancel = false;
//            changed = 0;
//        }
//    }

//    [Fact]
//    public void Size_Cannot_Be_Negative ()
//    {
//        var scroll = new Scroll { Height = 10, Size = -1 };
//        Assert.Equal (0, scroll.Size);
//        scroll.Size = -10;
//        Assert.Equal (0, scroll.Size);
//    }

//    [Fact]
//    public void SizeChanged_Event ()
//    {
//        var count = 0;
//        var scroll = new Scroll ();
//        scroll.Layout ();
//        scroll.SizeChanged += (s, e) => count++;

//        scroll.Size = 10;
//        Assert.Equal (10, scroll.Size);
//        Assert.Equal (1, count);
//    }

//    [Theory]
//    [SetupFakeDriver]
//    [InlineData (
//                    10,
//                    1,
//                    20,
//                    0,
//                    Orientation.Horizontal,
//                    @"
//┌──────────┐
//│█████░░░░░│
//└──────────┘")]

//    [InlineData (
//                    10,
//                    3,
//                    20,
//                    1,
//                    Orientation.Horizontal,
//                    @"
//┌──────────┐
//│░█████░░░░│
//│░█████░░░░│
//│░█████░░░░│
//└──────────┘")]

//    [InlineData (
//                    3,
//                    10,
//                    20,
//                    0,
//                    Orientation.Vertical,
//                    @"
//┌───┐
//│███│
//│███│
//│███│
//│███│
//│███│
//│░░░│
//│░░░│
//│░░░│
//│░░░│
//│░░░│
//└───┘")]


//    public void Draws_Correctly (int superViewportWidth, int superViewportHeight, int sliderSize, int sliderPosition, Orientation orientation, string expected)
//    {
//        var super = new Window
//        {
//            Id = "super",
//            Width = superViewportWidth + 2,
//            Height = superViewportHeight + 2
//        };

//        var scroll = new Scroll
//        {
//            Orientation = orientation,
//        };

//        if (orientation == Orientation.Vertical)
//        {
//            scroll.Width = Dim.Fill ();
//        }
//        else
//        {
//            scroll.Height = Dim.Fill ();
//        }
//        super.Add (scroll);

//        scroll.Size = sliderSize;
//        scroll.Layout ();
//        scroll.ContentPosition = sliderPosition;

//        super.BeginInit ();
//        super.EndInit ();
//        super.Layout ();
//        super.Draw ();

//        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//    }
//}
