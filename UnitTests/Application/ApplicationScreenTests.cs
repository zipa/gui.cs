using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests.NavigationTests;

public class ApplicationScreenTests (ITestOutputHelper output)
{
    [Fact]
    public void ClearScreenNextIteration_Resets_To_False_After_LayoutAndDraw ()
    {
        // Arrange
        Application.Init ();

        // Act
        Application.ClearScreenNextIteration = true;
        Application.LayoutAndDraw ();

        // Assert
        Assert.False (Application.ClearScreenNextIteration);

        // Cleanup
        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeDriver]
    public void ClearContents_Called_When_Top_Frame_Changes ()
    {
        // Arrange
        Application.Init ();
        Application.Top = new Toplevel ();
        Application.TopLevels.Push (Application.Top);

        int clearedContentsRaised = 0;

        Application.Driver!.ClearedContents += (e, a) => clearedContentsRaised++;

        // Act
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (0, clearedContentsRaised);

        // Act
        Application.Top.SetNeedsLayout ();
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (0, clearedContentsRaised);

        // Act
        Application.Top.X = 1;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (1, clearedContentsRaised);

        // Act
        Application.Top.Width = 10;
        Application.LayoutAndDraw ();

        // Assert
        Assert.Equal (2, clearedContentsRaised);

        // Cleanup
        Application.ResetState (true);
    }
}
