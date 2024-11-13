using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui.ViewsTests;

public class ScrollSliderTests (ITestOutputHelper output)
{

    [Fact]
    public void Constructor_Initializes_Correctly ()
    {
        var scrollSlider = new ScrollSlider ();
        Assert.False (scrollSlider.CanFocus);
        Assert.Equal (Orientation.Vertical, scrollSlider.Orientation);
        Assert.Equal (TextDirection.TopBottom_LeftRight, scrollSlider.TextDirection);
        Assert.Equal (Alignment.Center, scrollSlider.TextAlignment);
        Assert.Equal (Alignment.Center, scrollSlider.VerticalTextAlignment);
        scrollSlider.Layout ();
        Assert.Equal (0, scrollSlider.Frame.X);
        Assert.Equal (0, scrollSlider.Frame.Y);
        Assert.Equal (1, scrollSlider.Size);
    }

    [Fact]
    public void OnOrientationChanged_Sets_Size_To_1 ()
    {
        var scrollSlider = new ScrollSlider ();
        scrollSlider.Orientation = Orientation.Horizontal;
        Assert.Equal (1, scrollSlider.Size);
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
        var scrollSlider = new ScrollSlider ()
        {
        };
        super.Add (scrollSlider);
        scrollSlider.Layout ();
        scrollSlider.Position = 1;
        scrollSlider.Orientation = Orientation.Horizontal;

        Assert.Equal (0, scrollSlider.Position);
    }

    [Fact]
    public void OnOrientationChanged_Updates_TextDirection_And_TextAlignment ()
    {
        var scrollSlider = new ScrollSlider ();
        scrollSlider.Orientation = Orientation.Horizontal;
        Assert.Equal (TextDirection.LeftRight_TopBottom, scrollSlider.TextDirection);
        Assert.Equal (Alignment.Center, scrollSlider.TextAlignment);
        Assert.Equal (Alignment.Center, scrollSlider.VerticalTextAlignment);
    }

    [Theory]
    [CombinatorialData]
    public void Size_Clamps_To_SuperView_Viewport ([CombinatorialRange (-1, 6, 1)] int sliderSize, Orientation orientation)
    {
        var super = new View
        {
            Id = "super",
            Width = 5,
            Height = 5
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
        };
        super.Add (scrollSlider);
        scrollSlider.Layout ();

        scrollSlider.Size = sliderSize;
        scrollSlider.Layout ();

        Assert.True (scrollSlider.Size > 0);

        Assert.True (scrollSlider.Size <= 5);
    }

    [Theory]
    [CombinatorialData]
    public void Size_Clamps_To_ViewportDimensions ([CombinatorialRange (10, 10, 1)] int dimension, [CombinatorialRange (-1, 12, 1)] int sliderSize, Orientation orientation)
    {

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            ViewportDimension = dimension,
            Size = sliderSize,
        };
        scrollSlider.Layout ();

        Assert.True (scrollSlider.Size > 0);

        Assert.True (scrollSlider.Size <= dimension);
    }

    [Theory]
    [CombinatorialData]
    public void ViewportDimensions_Clamps_0_To_Dimension ([CombinatorialRange (0, 10, 1)] int dimension, Orientation orientation)
    {
        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            ViewportDimension = dimension,
        };

        Assert.InRange (scrollSlider.ViewportDimension, 1, 10);

        View super = new ()
        {
            Id = "super",
            Height = dimension,
            Width = dimension,
        };

        scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
        };
        super.Add (scrollSlider);
        super.Layout ();

        Assert.InRange (scrollSlider.ViewportDimension, 1, 10);

        scrollSlider.ViewportDimension = dimension;

        Assert.InRange (scrollSlider.ViewportDimension, 1, 10);
    }

    [Theory]
    [CombinatorialData]
    public void ClampPosition_Clamps_To_Viewport_Minus_Size ([CombinatorialRange (10, 10, 1)] int dimension, [CombinatorialRange (1, 5, 1)] int sliderSize, [CombinatorialRange (-2, 6, 2)] int sliderPosition, Orientation orientation)
    {
        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            ViewportDimension = dimension,
            Size = sliderSize,
        };

        int clampedPosition = scrollSlider.ClampPosition (sliderPosition);

        Assert.InRange (clampedPosition, 0, dimension - sliderSize);

        View super = new ()
        {
            Id = "super",
            Height = dimension,
            Width = dimension,
        };
        scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            Size = sliderSize,
        };
        super.Add (scrollSlider);
        super.Layout ();

        clampedPosition = scrollSlider.ClampPosition (sliderPosition);
        Assert.InRange (clampedPosition, 0, dimension - sliderSize);
    }

    [Theory]
    [CombinatorialData]
    public void Position_Clamps_To_SuperView_Viewport ([CombinatorialRange (0, 5, 1)] int sliderSize, [CombinatorialRange (-2, 6, 2)] int sliderPosition, Orientation orientation)
    {
        var super = new View
        {
            Id = "super",
            Width = 5,
            Height = 5
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
        };
        super.Add (scrollSlider);
        scrollSlider.Size = sliderSize;
        scrollSlider.Layout ();

        scrollSlider.Position = sliderPosition;

        Assert.True (scrollSlider.Position <= 5);
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData (
                    3,
                    10,
                    1,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
│   │
└───┘")]
    [InlineData (
                    10,
                    1,
                    3,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│███       │
└──────────┘")]
    [InlineData (
                    3,
                    10,
                    3,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│   │
│   │
│   │
│   │
│   │
│   │
│   │
└───┘")]



    [InlineData (
                    3,
                    10,
                    5,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│   │
│   │
│   │
│   │
│   │
└───┘")]

    [InlineData (
                    3,
                    10,
                    5,
                    1,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│███│
│███│
│███│
│███│
│███│
│   │
│   │
│   │
│   │
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    4,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
│   │
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    5,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
└───┘")]
    [InlineData (
                    3,
                    10,
                    5,
                    6,
                    Orientation.Vertical,
                    @"
┌───┐
│   │
│   │
│   │
│   │
│   │
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    3,
                    10,
                    10,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    3,
                    10,
                    10,
                    5,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]
    [InlineData (
                    3,
                    10,
                    11,
                    0,
                    Orientation.Vertical,
                    @"
┌───┐
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
│███│
└───┘")]

    [InlineData (
                    10,
                    3,
                    5,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│█████     │
│█████     │
│█████     │
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    5,
                    1,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│ █████    │
│ █████    │
│ █████    │
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    4,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│    █████ │
│    █████ │
│    █████ │
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│     █████│
│     █████│
│     █████│
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    5,
                    6,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│     █████│
│     █████│
│     █████│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    10,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]

    [InlineData (
                    10,
                    3,
                    10,
                    5,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]
    [InlineData (
                    10,
                    3,
                    11,
                    0,
                    Orientation.Horizontal,
                    @"
┌──────────┐
│██████████│
│██████████│
│██████████│
└──────────┘")]
    public void Draws_Correctly (int superViewportWidth, int superViewportHeight, int sliderSize, int position, Orientation orientation, string expected)
    {
        var super = new Window
        {
            Id = "super",
            Width = superViewportWidth + 2,
            Height = superViewportHeight + 2
        };

        var scrollSlider = new ScrollSlider
        {
            Orientation = orientation,
            Size = sliderSize,
            Position = position,
        };
        Assert.Equal (sliderSize, scrollSlider.Size);
        super.Add (scrollSlider);
 
        super.Layout ();
        super.Draw ();

        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    public void ShowPercent_True_ShowsPercentage ()
    {
        View super = new ()
        {
            Id = "super",
            Width = 10,
            Height = 10
        };
        ScrollSlider scrollSlider = new ()
        {
            Id = "scrollSlider",
            Height = 10,
            Width = 10,
        };
        super.Add (scrollSlider);
        scrollSlider.ShowPercent = true;
        Assert.True (scrollSlider.ShowPercent);
        super.Draw ();
        Assert.Contains ("0%", scrollSlider.Text);
    }
}
