using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class AllViewsDrawTests (ITestOutputHelper _output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_Does_Not_Layout (Type viewType)
    {
        var view = (View)CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (view is IDesignable designable)
        {
            designable.EnableForDesign ();
        }

        var drawContentCount = 0;
        view.DrawContent += (s, e) => drawContentCount++;

        var layoutStartedCount = 0;
        view.LayoutStarted += (s, e) => layoutStartedCount++;

        var layoutCompleteCount = 0;
        view.LayoutComplete += (s, e) => layoutCompleteCount++;

        view.SetLayoutNeeded ();
        view.Layout ();

        Assert.Equal (0, drawContentCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);

        view.Draw ();

        Assert.Equal (1, drawContentCount);
        Assert.Equal (1, layoutStartedCount);
        Assert.Equal (1, layoutCompleteCount);
    }
}
