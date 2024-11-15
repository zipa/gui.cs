using Xunit.Abstractions;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui.ViewsTests;

public class ScrollBarTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var scrollBar = new ScrollBar ();
        Assert.False (scrollBar.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        Assert.Equal (0, scrollBar.VisibleContentSize);
        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Position);
        Assert.True (scrollBar.AutoHide);
    }

    #region AutoHide
    [Fact]
    [AutoInitShutdown]
    public void AutoHide_True_Is_Default_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
        };
        super.Add (scrollBar);
        Assert.True (scrollBar.AutoHide);
        Assert.True (scrollBar.Visible); // Before Init

        RunState rs = Application.Begin (super);

        // Should Show
        scrollBar.ScrollableContentSize = 21;
        Application.RunIteration (ref rs);
        Assert.True (scrollBar.Visible);

        // Should Hide
        scrollBar.ScrollableContentSize = 10;
        Assert.False (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoHide_False_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
            AutoHide = false
        };
        super.Add (scrollBar);
        Assert.False (scrollBar.AutoHide);
        Assert.True (scrollBar.Visible);

        RunState rs = Application.Begin (super);

        // Should Hide if AutoSize = true, but should not hide if AutoSize = false
        scrollBar.ScrollableContentSize = 10;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoHide_Change_AutoSize_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
        };
        super.Add (scrollBar);
        Assert.True (scrollBar.AutoHide);
        Assert.True (scrollBar.Visible); // Before Init

        RunState rs = Application.Begin (super);

        Assert.False (scrollBar.Visible);
        Assert.Equal (1, scrollBar.Frame.Width);
        Assert.Equal (20, scrollBar.Frame.Height);

        scrollBar.ScrollableContentSize = 10;
        Application.RunIteration (ref rs);
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 30;
        Assert.True (scrollBar.Visible);

        scrollBar.AutoHide = false;
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoHide_Change_Size_CorrectlyHidesAndShows ()
    {
        var super = new Toplevel ()
        {
            Id = "super",
            Width = 1,
            Height = 20
        };

        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = 20,
        };
        super.Add (scrollBar);

        RunState rs = Application.Begin (super);

        Assert.Equal (Orientation.Vertical, scrollBar.Orientation);
        Assert.Equal (20, scrollBar.VisibleContentSize);
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.Visible);
        Assert.Equal (1, scrollBar.Frame.Width);
        Assert.Equal (20, scrollBar.Frame.Height);

        scrollBar.ScrollableContentSize = 10;
        Application.RunIteration (ref rs);
        //Assert.False (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 30;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        Application.RunIteration (ref rs);
        //Assert.False (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 21;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.AutoHide = false;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        scrollBar.ScrollableContentSize = 10;
        //Assert.True (scrollBar.ShowScrollIndicator);
        Assert.True (scrollBar.Visible);

        super.Dispose ();
    }

    #endregion AutoHide

    #region Orientation
    [Fact]
    public void OnOrientationChanged_Keeps_Size ()
    {
        var scroll = new ScrollBar ();
        scroll.Layout ();
        scroll.ScrollableContentSize = 1;

        scroll.Orientation = Orientation.Horizontal;
        Assert.Equal (1, scroll.ScrollableContentSize);
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
        scrollBar.Position = 1;
        scrollBar.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollBar.GetSliderPosition ());
    }

    #endregion Orientation

    #region Slider

    [Theory]
    [InlineData (-1, 10, 1)]
    [InlineData (0, 10, 1)]
    [InlineData (10, 15, 5)]
    [InlineData (10, 5, 8)]
    [InlineData (10, 3, 8)]
    [InlineData (10, 2, 8)]
    [InlineData (10, 1, 8)]
    [InlineData (10, 0, 8)]
    [InlineData (10, 10, 8)]
    [InlineData (10, 20, 4)]
    [InlineData (10, 100, 1)]
    [InlineData (15, 0, 13)]
    [InlineData (15, 1, 13)]
    [InlineData (15, 2, 13)]
    [InlineData (15, 3, 13)]
    [InlineData (15, 5, 13)]
    [InlineData (15, 10, 13)]
    [InlineData (15, 14, 13)]
    [InlineData (15, 15, 13)]
    [InlineData (15, 16, 12)]
    [InlineData (20, 10, 18)]
    [InlineData (100, 10, 98)]
    public void CalculateSliderSize_Width_Is_VisibleContentSize_CalculatesCorrectly (int visibleContentSize, int scrollableContentSize, int expectedSliderSize)
    {
        // Arrange
        var scrollBar = new ScrollBar
        {
            VisibleContentSize = visibleContentSize,
            ScrollableContentSize = scrollableContentSize,
            Orientation = Orientation.Horizontal // Assuming horizontal for simplicity
        };
        scrollBar.Width = visibleContentSize;

        // Act
        var sliderSize = scrollBar.CalculateSliderSize ();

        // Assert
        Assert.Equal (expectedSliderSize, sliderSize);
    }

    [Theory]
    // 0123456789
    //  -
    // **********
    // ◄███►
    [InlineData (5, 10, 1, 3)]

    // 01234567890
    //  ----------
    // **********
    // ◄██░►
    [InlineData (5, 10, 11, 2)]


    [InlineData (20, 10, 1, 18)]

    //// ◄█░░░░░░░►
    //[InlineData (1, 10, 1)]

    ////  ---------
    //// ◄████░░░░►
    //[InlineData (5, 10, 4)]

    ////  ----------
    //// ◄███░░░░░►
    //[InlineData (5, 11, 3)]
    //[InlineData (5, 12, 3)]
    //[InlineData (5, 13, 3)]

    //// 012345678901234
    ////  --------------
    //// ◄██░░░░░░►
    //[InlineData (5, 14, 2)]
    //[InlineData (5, 15, 2)]
    //[InlineData (5, 16, 2)]

    //// 012345678901234567890
    ////  ----------------
    //// ◄██░░░░░░►
    //[InlineData (5, 18, 2)]
    //[InlineData (5, 19, 2)]
    //[InlineData (5, 20, 2)]


    //// 012345678901234567890
    ////  --------------------
    //// ◄█░░░░░░░►
    //[InlineData (5, 21, 1)]
    //[InlineData (5, 22, 1)]
    //[InlineData (5, 23, 1)]
    public void CalculateSliderSize_Width_Is_LT_VisibleContentSize_CalculatesCorrectly (int width, int visibleContentSize, int scrollableContentSize, int expectedSliderSize)
    {
        // Arrange
        var scrollBar = new ScrollBar
        {
            VisibleContentSize = visibleContentSize,
            ScrollableContentSize = scrollableContentSize,
            Orientation = Orientation.Horizontal // Assuming horizontal for simplicity
        };
        scrollBar.Width = width;

        // Act
        var sliderSize = scrollBar.CalculateSliderSize ();

        // Assert
        Assert.Equal (expectedSliderSize, sliderSize);
    }


    [Theory]
    // 0123456789
    //  ---------
    // ◄█░░░░░░░►
    [InlineData (0, 10, 1)]
    // ◄█░░░░░░░►
    [InlineData (1, 10, 1)]

    //  ---------
    // ◄████░░░░►
    [InlineData (5, 10, 4)]

    //  ----------
    // ◄███░░░░░►
    [InlineData (5, 11, 3)]
    [InlineData (5, 12, 3)]
    [InlineData (5, 13, 3)]

    // 012345678901234
    //  --------------
    // ◄██░░░░░░►
    [InlineData (5, 14, 2)]
    [InlineData (5, 15, 2)]
    [InlineData (5, 16, 2)]

    // 012345678901234567890
    //  ----------------
    // ◄██░░░░░░►
    [InlineData (5, 18, 2)]
    [InlineData (5, 19, 2)]
    [InlineData (5, 20, 2)]


    // 012345678901234567890
    //  --------------------
    // ◄█░░░░░░░►
    [InlineData (5, 21, 1)]
    [InlineData (5, 22, 1)]
    [InlineData (5, 23, 1)]

    public void CalculateSliderSize_Width_Is_GT_VisibleContentSize_CalculatesCorrectly (int visibleContentSize, int scrollableContentSize, int expectedSliderSize)
    {
        // Arrange
        var scrollBar = new ScrollBar
        {
            VisibleContentSize = visibleContentSize,
            ScrollableContentSize = scrollableContentSize,
            Orientation = Orientation.Horizontal // Assuming horizontal for simplicity
        };
        scrollBar.Width = 10;

        // Act
        var sliderSize = scrollBar.CalculateSliderSize ();

        // Assert
        Assert.Equal (expectedSliderSize, sliderSize);
    }

    [Theory]
    // 0123456789
    //  ---------
    // ◄█►
    [InlineData (3, 3, 0, 0)]
    [InlineData (3, 3, 1, 0)]
    [InlineData (3, 3, 2, 0)]

    // 0123456789
    //  ---------
    // ◄██►
    [InlineData (4, 4, 0, 0)]
    [InlineData (4, 4, 1, 0)]
    [InlineData (4, 4, 2, 0)]
    [InlineData (4, 4, 3, 0)]
    [InlineData (4, 4, 4, 0)]


    // 012345
    //  ^----
    // ◄█░►
    [InlineData (4, 5, 0, 0)]
    //  -^---
    // ◄█░►
    [InlineData (4, 5, 1, 0)]
    //  --^--
    // ◄░█►
    [InlineData (4, 5, 2, 1)]
    //  ---^-
    // ◄░█►
    [InlineData (4, 5, 3, 1)]
    //  ----^
    // ◄░█►
    [InlineData (4, 5, 4, 1)]

    // 01234
    // ^---------
    // ◄█░░►
    [InlineData (5, 10, 0, 0)]
    // -^--------
    // ◄█░░►
    [InlineData (5, 10, 1, 0)]
    // --^-------
    // ◄█░░►
    [InlineData (5, 10, 2, 0)]
    // ---^------
    // ◄█░░►
    [InlineData (5, 10, 3, 0)]
    // ----^----
    // ◄░█░►
    [InlineData (5, 10, 4, 1)]
    // -----^---
    // ◄░█░►
    [InlineData (5, 10, 5, 1)]
    // ------^--
    // ◄░░█►
    [InlineData (5, 10, 6, 2)]
    // ------^--
    // ◄░░█►
    [InlineData (5, 10, 7, 2)]
    // -------^-
    // ◄░░█►
    [InlineData (5, 10, 8, 2)]
    // --------^
    // ◄░░█►
    [InlineData (5, 10, 9, 2)]


    [InlineData (10, 20, 0, 0)]
    [InlineData (10, 20, 1, 0)]
    [InlineData (10, 20, 2, 0)]
    [InlineData (10, 20, 3, 1)]
    [InlineData (10, 20, 4, 2)]
    [InlineData (10, 20, 5, 2)]
    [InlineData (10, 20, 6, 3)]
    [InlineData (10, 20, 7, 4)]
    [InlineData (10, 20, 8, 4)]

    public void CalculateSliderPosition_Calculates_Correctly (int visibleContentSize, int scrollableContentSize,  int contentPosition, int expectedSliderPosition)
    {
        // Arrange
        var scrollBar = new ScrollBar
        {
            ScrollableContentSize = scrollableContentSize,
            VisibleContentSize = visibleContentSize,
            Orientation = Orientation.Horizontal // Assuming horizontal for simplicity
        };
        scrollBar.Width = visibleContentSize;

        // Act
        var sliderPosition= scrollBar.CalculateSliderPositionFromContentPosition (contentPosition, NavigationDirection.Forward);

        // Assert
        Assert.Equal (expectedSliderPosition, sliderPosition);
    }


    #endregion Slider

    #region Size

    // TODO: Add tests.

    #endregion Size

    #region Position

    // 012345678901
    // ◄█░░░░░░░░░►
    [Theory]
    // ◄█►
    [InlineData (3, 3, -1, 0)]
    [InlineData (3, 3, 0, 0)]
    // 012
    // ---
    // ◄█►
    [InlineData (3, 3, 1, 0)]
    [InlineData (3, 3, 2, 0)]

    // ◄██►
    [InlineData (4, 2, 1, 0)]
    [InlineData (4, 2, 2, 0)]

    // 0123
    //  ---
    // ◄██►
    [InlineData (4, 3, 0, 0)] // scrollBarWidth/VisibleContentSize > size - scrolling doesn't make sense. Size should clamp to scrollSlider.Size.
    // ◄██►
    [InlineData (4, 3, 1, 0)]
    // ◄██►
    [InlineData (4, 3, 2, 0)]


    // 01234
    //  ----
    // ◄██►
    [InlineData (4, 4, 0, 0)] // scrollBarWidth/VisibleContentSize == size - scrolling doesn't make sense. Size should clamp to scrollSlider.Size.
    // ◄██►
    [InlineData (4, 4, 1, 0)]
    // ◄██►
    [InlineData (4, 4, 2, 0)]

    // 012345
    // ◄███►
    //  -----
    [InlineData (5, 5, 3, 0)]
    [InlineData (5, 5, 4, 0)]

    // 0123456
    // ◄██░►
    //  ^-----
    [InlineData (5, 6, 0, 0)]
    // ◄░██►
    //  -^----
    [InlineData (5, 6, 1, 1)]
    [InlineData (5, 6, 2, 1)]

    // 012346789
    // ◄█░░►
    //  ^--------
    [InlineData (5, 10, -1, 0)]
    [InlineData (5, 10, 0, 0)]

    // 0123456789
    // ◄░█░►
    //  --^-------
    [InlineData (5, 10, 1, 3)]

    // ◄░░█►
    //  ----^----
    [InlineData (5, 10, 2, 5)]

    // ◄░░█►
    //  ------^---
    [InlineData (5, 10, 4, 5)]

    // ◄░████░░░►
    //  --------------------
    [InlineData (10, 20, 0, 0)]

    public void CalculatePosition_Calculates (int visibleContentSize, int scrollableContentSize, int sliderPosition, int expectedContentPosition)
    {
        // Arrange
        var scrollBar = new ScrollBar
        {
            VisibleContentSize = visibleContentSize,
            ScrollableContentSize = scrollableContentSize,
            Orientation = Orientation.Horizontal // Use Horizontal because it's easier to visualize
        };
        scrollBar.Frame = new (0, 0, visibleContentSize, 0);

        // Act
        var contentPosition = scrollBar.CalculatePositionFromSliderPosition (sliderPosition);

        // Assert
        Assert.Equal (expectedContentPosition, contentPosition);
    }
    [Fact]
    public void Position_Event_Cancelables ()
    {
        var changingCount = 0;
        var changedCount = 0;
        var scrollBar = new ScrollBar { };
        scrollBar.ScrollableContentSize = 5;
        scrollBar.Frame = new Rectangle (0, 0, 1, 4); // Needs to be at least 4 for slider to move

        scrollBar.PositionChanging += (s, e) =>
                                            {
                                                if (changingCount == 0)
                                                {
                                                    e.Cancel = true;
                                                }

                                                changingCount++;
                                            };
        scrollBar.PositionChanged += (s, e) => changedCount++;

        scrollBar.Position = 1;
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (1, changingCount);
        Assert.Equal (0, changedCount);

        scrollBar.Position = 1;
        Assert.Equal (1, scrollBar.Position);
        Assert.Equal (2, changingCount);
        Assert.Equal (1, changedCount);
    }
    #endregion Position


    [Fact]
    public void ScrollableContentSize_Cannot_Be_Negative ()
    {
        var scrollBar = new ScrollBar { Height = 10, ScrollableContentSize = -1 };
        Assert.Equal (0, scrollBar.ScrollableContentSize);
        scrollBar.ScrollableContentSize = -10;
        Assert.Equal (0, scrollBar.ScrollableContentSize);
    }

    [Fact]
    public void ScrollableContentSizeChanged_Event ()
    {
        var count = 0;
        var scrollBar = new ScrollBar ();
        scrollBar.ScrollableContentSizeChanged += (s, e) => count++;

        scrollBar.ScrollableContentSize = 10;
        Assert.Equal (10, scrollBar.ScrollableContentSize);
        Assert.Equal (1, count);
    }

    [Theory]
    [SetupFakeDriver]

    #region Draw


    #region Horizontal

    #region Super 10 - ScrollBar 8
    [InlineData (
                    10,
                    1,
                    10,
                    -1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████████►│
└──────────┘")]

    [InlineData (
                    10,
                    1,
                    20,
                    -1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘")]
    [InlineData (
                    10,
                    1,
                    20,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘")]

    [InlineData (
                    10,
                    1,
                    20,
                    1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘")]

    [InlineData (
                    10,
                    1,
                    20,
                    2,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄████░░░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    3,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░████░░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    4,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░████░░►│
└──────────┘
")]
    [InlineData (
                    10,
                    1,
                    20,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░████░░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    6,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░████░►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    7,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░████░►│
└──────────┘
")]


    [InlineData (
                    10,
                    1,
                    20,
                    8,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    9,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]

    [InlineData (
                    10,
                    1,
                    20,
                    10,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]


    [InlineData (
                    10,
                    1,
                    20,
                    19,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]


    [InlineData (
                    10,
                    1,
                    20,
                    20,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│◄░░░░████►│
└──────────┘
")]
    #endregion  Super 10 - ScrollBar 8

    #region  Super 12 - ScrollBar 10
    [InlineData (
                    12,
                    1,
                    10,
                    -1,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████████►│
└────────────┘")]

    [InlineData (
                    12,
                    1,
                    20,
                    -1,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████░░░░►│
└────────────┘")]
    [InlineData (
                    12,
                    1,
                    20,
                    0,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████░░░░►│
└────────────┘")]

    [InlineData (
                    12,
                    1,
                    20,
                    1,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄██████░░░░►│
└────────────┘")]

    [InlineData (
                    12,
                    1,
                    20,
                    2,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░██████░░░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    3,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░██████░░░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    4,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░██████░░►│
└────────────┘
")]
    [InlineData (
                    12,
                    1,
                    20,
                    5,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░██████░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    6,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░██████░►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    7,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]


    [InlineData (
                    12,
                    1,
                    20,
                    8,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    9,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]

    [InlineData (
                    12,
                    1,
                    20,
                    10,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]


    [InlineData (
                    12,
                    1,
                    20,
                    19,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]


    [InlineData (
                    12,
                    1,
                    20,
                    20,
                    Orientation.Horizontal,
                    @"
┌────────────┐
│◄░░░░██████►│
└────────────┘
")]
    #endregion Super 12 - ScrollBar 10
    [InlineData (
                    10,
                    3,
                    20,
                    2,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│ ░████░░░ │
│◄░████░░░►│
│ ░████░░░ │
└──────────┘
")]
    #endregion Horizontal

    #region Vertical

    [InlineData (
                    1,
                    10,
                    10,
                    -1,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│▼│
└─┘")]

    [InlineData (
                    1,
                    10,
                    10,
                    5,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│█│
│▼│
└─┘")]

    [InlineData (
                    1,
                    10,
                    20,
                    5,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│░│
│░│
│█│
│█│
│█│
│█│
│░│
│░│
│▼│
└─┘")]

    [InlineData (
                    1,
                    12,
                    20,
                    5,
                    Orientation.Vertical,
                    @"
┌─┐
│▲│
│░│
│░│
│░│
│█│
│█│
│█│
│█│
│█│
│█│
│░│
│▼│
└─┘")]

    [InlineData (
                    3,
                    10,
                    20,
                    2,
                    Orientation.Vertical,
                    @"
┌───┐
│ ▲ │
│░░░│
│███│
│███│
│███│
│███│
│░░░│
│░░░│
│░░░│
│ ▼ │
└───┘
")]
    #endregion Vertical


    public void Draws_Correctly (int width, int height, int contentSize, int contentPosition, Orientation orientation, string expected)
    {
        var super = new Window
        {
            Id = "super",
            Width = width + 2,
            Height = height + 2
        };

        var scrollBar = new ScrollBar
        {
            Orientation = orientation,
        };

        if (orientation == Orientation.Vertical)
        {
            scrollBar.Width = 1;
            scrollBar.Height = height;
        }
        else
        {
            scrollBar.Width = width;
            scrollBar.Height = 1;
        }
        super.Add (scrollBar);

        scrollBar.ScrollableContentSize = contentSize;
        scrollBar.Position = contentPosition;

        int sliderPos = scrollBar.CalculateSliderPositionFromContentPosition (contentPosition, NavigationDirection.Forward);

        super.BeginInit ();
        super.EndInit ();
        super.Layout ();
        super.Draw ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }
    #endregion Draw

    #region Mouse



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
            ScrollableContentSize = 20,
            Increment = increment
        };

        top.Add (scrollBar);
        RunState rs = Application.Begin (top);

        // Scroll to end
        scrollBar.Position = 20;
        Assert.Equal (10, scrollBar.Position);
        Application.RunIteration (ref rs);

        Assert.Equal (4, scrollBar.GetSliderPosition ());
        Assert.Equal (10, scrollBar.Position);
        int initialPos = scrollBar.Position;

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = new (0, 0),
            Flags = MouseFlags.Button1Clicked
        });
        Application.RunIteration (ref rs);

        Assert.Equal (initialPos - increment, scrollBar.Position);

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
            ScrollableContentSize = 20,
            Increment = increment
        };

        top.Add (scrollBar);
        RunState rs = Application.Begin (top);

        // Scroll to top
        scrollBar.Position = 0;
        Application.RunIteration (ref rs);

        Assert.Equal (0, scrollBar.GetSliderPosition ());
        Assert.Equal (0, scrollBar.Position);
        int initialPos = scrollBar.Position;

        Application.RaiseMouseEvent (new ()
        {
            ScreenPosition = orientation == Orientation.Vertical ? new (0, scrollBar.Frame.Height - 1) : new (scrollBar.Frame.Width - 1, 0),
            Flags = MouseFlags.Button1Clicked
        });
        Application.RunIteration (ref rs);

        Assert.Equal (initialPos + increment, scrollBar.Position);

        Application.ResetState (true);
    }
    #endregion Mouse



    [Fact (Skip = "Disabled - Will put this feature in View")]
    [AutoInitShutdown]
    public void KeepContentInAllViewport_True_False ()
    {
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        view.Padding.Thickness = new (0, 0, 2, 0);
        view.SetContentSize (new (view.Viewport.Width, 30));
        var scrollBar = new ScrollBar { Width = 2, Height = Dim.Fill (), ScrollableContentSize = view.GetContentSize ().Height };
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
        Assert.Equal (30, scrollBar.ScrollableContentSize);

        scrollBar.KeepContentInAllViewport = false;
        scrollBar.Position = 50;
        Assert.Equal (scrollBar.GetSliderPosition (), scrollBar.ScrollableContentSize - 1);
        Assert.Equal (scrollBar.GetSliderPosition (), view.Viewport.Y);
        Assert.Equal (29, scrollBar.GetSliderPosition ());
        Assert.Equal (29, view.Viewport.Y);

        top.Dispose ();
    }

}
