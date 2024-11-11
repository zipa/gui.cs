using Xunit.Abstractions;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui.ViewsTests;

public class ScrollBarTests
{
    public ScrollBarTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Fact]
    [AutoInitShutdown]
    public void AutoHideScrollBar_CheckScrollBarVisibility ()
    {
        var scrollBar = new ScrollBar { Width = 2, Height = Dim.Fill (), Size = 30 };
        View scrollBarSuperView = ScrollBarSuperView ();
        scrollBarSuperView.Add (scrollBar);
        Application.Begin ((scrollBarSuperView.SuperView as Toplevel)!);

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);
        Assert.Equal ("Absolute(2)", scrollBar.Width!.ToString ());
        Assert.Equal (2, scrollBar.Viewport.Width);
        Assert.Equal ("Fill(Absolute(0))", scrollBar.Height!.ToString ());
        Assert.Equal (25, scrollBar.Viewport.Height);

        scrollBar.Size = 10;
        //Assert.False (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.Visible);

        scrollBar.Size = 30;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoHide = false;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.Size = 10;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBarSuperView.SuperView!.Dispose ();
    }


    [Fact]
    public void Constructor_Defaults ()
    {
        var scrollBar = new ScrollBar ();
        Assert.False (scrollBar.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (0, scrollBar.Size);
        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.True (scrollBar.AutoHide);
    }


    [Fact]
    public void OnOrientationChanged_Keeps_Size ()
    {
        var scroll = new Scroll ();
        scroll.Layout ();
        scroll.Size = 1;

        scroll.Orientation = Orientation.Horizontal;
        Assert.Equal (1, scroll.Size);
    }

    [Fact]
    public void OnOrientationChanged_Sets_Position_To_0 ()
    {
        View super = new View ()
        {
            Id = "super",
            Width = 10,
            Height = 10
        };
        var scrollBar = new ScrollBar ()
        {
        };
        super.Add (scrollBar);
        scrollBar.Layout ();
        scrollBar.ContentPosition = 1;
        scrollBar.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollBar.GetSliderPosition ());
    }


    [Fact]
    public void ContentPosition_Event_Cancelables ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar { };
        scrollBar.Layout ();
        scrollBar.Size = scrollBar.Viewport.Height * 2;
        scrollBar.Layout ();

        scrollBar.ContentPositionChanging += (s, e) =>
                                            {
                                                if (changingCount == 0)
                                                {
                                                    e.Cancel = true;
                                                }

                                                changingCount++;
                                            };
        scrollBar.ContentPositionChanged += (s, e) => changedCount++;

        scrollBar.ContentPosition = 1;
        Assert.Equal (0, scrollBar.ContentPosition);
        Assert.Equal (1, changingCount);
        Assert.Equal (0, changedCount);

        scrollBar.ContentPosition = 1;
        Assert.Equal (1, scrollBar.ContentPosition);
        Assert.Equal (2, changingCount);
        Assert.Equal (1, changedCount);
    }



    [Fact (Skip = "Disabled - Will put this feature in View")]
    [AutoInitShutdown]
    public void KeepContentInAllViewport_True_False ()
    {
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        view.Padding.Thickness = new (0, 0, 2, 0);
        view.SetContentSize (new (view.Viewport.Width, 30));
        var scrollBar = new ScrollBar { Width = 2, Height = Dim.Fill (), Size = view.GetContentSize ().Height };
        scrollBar.SliderPositionChanged += (_, e) => view.Viewport = view.Viewport with { Y = e.CurrentValue };
        view.Padding.Add (scrollBar);
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);

        Assert.False (scrollBar.KeepContentInAllViewport);
        scrollBar.KeepContentInAllViewport = true;
        Assert.Equal (80, view.Padding.Viewport.Width);
        Assert.Equal (25, view.Padding.Viewport.Height);
        Assert.Equal (2, scrollBar.Viewport.Width);
        Assert.Equal (25, scrollBar.Viewport.Height);
        Assert.Equal (30, scrollBar.Size);

        scrollBar.KeepContentInAllViewport = false;
        scrollBar.ContentPosition = 50;
        Assert.Equal (scrollBar.GetSliderPosition (), scrollBar.Size - 1);
        Assert.Equal (scrollBar.GetSliderPosition (), view.Viewport.Y);
        Assert.Equal (29, scrollBar.GetSliderPosition ());
        Assert.Equal (29, view.Viewport.Y);

        top.Dispose ();
    }


    [Theory (Skip = "Disabled - Will put this feature in View")]
    [AutoInitShutdown]
    [InlineData (Orientation.Vertical)]
    [InlineData (Orientation.Horizontal)]
    public void Moving_Mouse_Outside_Host_Ensures_Correct_Location_KeepContentInAllViewport_True (Orientation orientation)
    {
        var scrollBar = new ScrollBar
        {
            X = 10, Y = 10, Width = orientation == Orientation.Vertical ? 1 : 10, Height = orientation == Orientation.Vertical ? 10 : 1, Size = 20,
            ContentPosition = 5, Orientation = orientation, KeepContentInAllViewport = true
        };
        var top = new Toplevel ();
        top.Add (scrollBar);
        RunState rs = Application.Begin (top);

        var scroll = (Scroll)scrollBar.Subviews.FirstOrDefault (x => x is Scroll);
        Rectangle scrollSliderFrame = scroll!.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame;
        Assert.Equal (scrollSliderFrame, orientation == Orientation.Vertical ? new (0, 2, 1, 4) : new (2, 0, 4, 1));

        Application.RaiseMouseEvent (new () { ScreenPosition = orientation == Orientation.Vertical ? new (10, 14) : new (14, 10), Flags = MouseFlags.Button1Pressed });
        Application.RunIteration (ref rs);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = orientation == Orientation.Vertical ? new (10, 0) : new (0, 10),
                                         Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });
        Application.RunIteration (ref rs);
        Assert.Equal (new (0, 0), scroll.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame.Location);

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = orientation == Orientation.Vertical ? new (0, 25) : new (80, 0),
                                         Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                     });

        Application.RunIteration (ref rs);
        Assert.Equal (
                      orientation == Orientation.Vertical ? new (0, 4) : new (4, 0),
                      scroll.Subviews.FirstOrDefault (x => x is ScrollSlider)!.Frame.Location);
    }

    [Fact]
    public void Size_Cannot_Be_Negative ()
    {
        var scrollBar = new ScrollBar { Height = 10, Size = -1 };
        Assert.Equal (0, scrollBar.Size);
        scrollBar.Size = -10;
        Assert.Equal (0, scrollBar.Size);
    }

    [Fact]
    public void SizeChanged_Event ()
    {
        var count = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.SizeChanged += (s, e) => count++;

        scrollBar.Size = 10;
        Assert.Equal (10, scrollBar.Size);
        Assert.Equal (1, count);
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData (
                    10,
                    1,
                    20,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄███░░░░░►│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    20,
                    1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│ ░███░░░░ │
│◄░███░░░░►│
│ ░███░░░░ │
└──────────┘")]

    [InlineData (
                    3,
                    10,
                    20,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│ ▲ │
│███│
│███│
│███│
│░░░│
│░░░│
│░░░│
│░░░│
│░░░│
│ ▼ │
└───┘")]
    [InlineData (
                    6,
                    10,
                    20,
                    1,
                    Orientation.Vertical,
                    @"
┌──────┐
│  ▲   │
│░░░░░░│
│██████│
│██████│
│██████│
│░░░░░░│
│░░░░░░│
│░░░░░░│
│░░░░░░│
│  ▼   │
└──────┘")]


    public void Draws_Correctly (int superViewportWidth, int superViewportHeight, int sliderSize, int sliderPosition, Orientation orientation, string expected)
    {
        var super = new Window
        {
            Id = "super",
            Width = superViewportWidth + 2,
            Height = superViewportHeight + 2
        };

        var scrollBar = new ScrollBar
        {
            Orientation = orientation,
        };

        if (orientation == Orientation.Vertical)
        {
            scrollBar.Width = Dim.Fill ();
        }
        else
        {
            scrollBar.Height = Dim.Fill ();
        }
        super.Add (scrollBar);

        scrollBar.Size = sliderSize;
        scrollBar.Layout ();
        scrollBar.ContentPosition = sliderPosition;

        super.BeginInit ();
        super.EndInit ();
        super.Layout ();
        super.Draw ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    private View ScrollBarSuperView ()
    {
        var view = new View
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var top = new Toplevel ();
        top.Add (view);

        return view;
    }


    [Theory]
    [CombinatorialData]
    [AutoInitShutdown]
    public void Mouse_Click_DecrementButton_Decrements ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
    {
        var top = new Toplevel ()
        {
            Id = "top",
            Width = 10,
            Height = 10
        };
        var scrollBar = new ScrollBar
        {
            Id = "scrollBar",
            Orientation = orientation,
            Size = 20,
            Increment = increment
        };

        top.Add (scrollBar);
        RunState rs = Application.Begin (top);
        scrollBar.ContentPosition = 5;
        Application.RunIteration (ref rs);

        Assert.Equal (5, scrollBar.GetSliderPosition ());
        Assert.Equal (12, scrollBar.ContentPosition);
        int initialPos = scrollBar.ContentPosition;

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = new (0, 0),
            Flags = MouseFlags.Button1Clicked
        });
        Application.RunIteration (ref rs);

        Assert.Equal (initialPos - increment, scrollBar.ContentPosition);

        Application.ResetState (true);
    }


    [Theory]
    [CombinatorialData]
    [AutoInitShutdown]
    public void Mouse_Click_IncrementButton_Increments ([CombinatorialRange (1, 3, 1)] int increment, Orientation orientation)
    {
        var top = new Toplevel ()
        {
            Id = "top",
            Width = 10,
            Height = 10
        };
        var scrollBar = new ScrollBar
        {
            Id = "scrollBar",
            Orientation = orientation,
            Size = 20,
            Increment = increment
        };

        top.Add (scrollBar);
        RunState rs = Application.Begin (top);
        scrollBar.ContentPosition = 0;
        Application.RunIteration (ref rs);

        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.ContentPosition);
        int initialPos = scrollBar.ContentPosition;

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = orientation == Orientation.Vertical ? new (0, scrollBar.Frame.Height - 1) : new (scrollBar.Frame.Width - 1, 0),
            Flags = MouseFlags.Button1Clicked
        });
        Application.RunIteration (ref rs);

        Assert.Equal (initialPos + increment, scrollBar.ContentPosition);

        Application.ResetState (true);
    }
}
