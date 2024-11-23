namespace Terminal.Gui.InputTests;

public class EscSeqRequestsTests
{
    [Fact]
    public void Add_Tests ()
    {
        EscSeqRequests.Clear ();
        EscSeqRequests.Add ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Add ("t", 2);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Clear ();
        EscSeqRequests.Add ("t", 2);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Add ("t", 3);
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumOutstanding);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        EscSeqRequests.Clear ();
        Assert.NotNull (EscSeqRequests.Statuses);
        Assert.Empty (EscSeqRequests.Statuses);
    }

    [Fact]
    public void Remove_Tests ()
    {
        EscSeqRequests.Clear ();
        EscSeqRequests.Add ("t");
        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);

        EscSeqRequests.Add ("t", 2);
        EscSeqRequests.Remove ("t");
        Assert.Single (EscSeqRequests.Statuses);
        Assert.Equal ("t", EscSeqRequests.Statuses [^1].Terminator);
        Assert.Equal (2, EscSeqRequests.Statuses [^1].NumRequests);
        Assert.Equal (1, EscSeqRequests.Statuses [^1].NumOutstanding);

        EscSeqRequests.Remove ("t");
        Assert.Empty (EscSeqRequests.Statuses);
    }

    [Fact]
    public void Requested_Tests ()
    {
        EscSeqRequests.Clear ();
        Assert.False (EscSeqRequests.HasResponse ("t"));

        EscSeqRequests.Add ("t");
        Assert.False (EscSeqRequests.HasResponse ("r"));
        Assert.True (EscSeqRequests.HasResponse ("t"));
    }
}
