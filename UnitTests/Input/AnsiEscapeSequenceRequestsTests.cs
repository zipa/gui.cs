namespace Terminal.Gui.InputTests;

public class AnsiEscapeSequenceRequestsTests
{
    [Fact]
    public void Add_Tests ()
    {
        AnsiEscapeSequenceRequests.Clear ();
        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        Assert.Single (AnsiEscapeSequenceRequests.Statuses);
        Assert.Equal ("t", AnsiEscapeSequenceRequests.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        Assert.Equal (2, AnsiEscapeSequenceRequests.Statuses.Count);
        Assert.Equal ("t", AnsiEscapeSequenceRequests.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        AnsiEscapeSequenceRequests.Clear ();
        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        Assert.Equal (2, AnsiEscapeSequenceRequests.Statuses.Count);
        Assert.Equal ("t", AnsiEscapeSequenceRequests.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        Assert.Equal (3, AnsiEscapeSequenceRequests.Statuses.Count);
        Assert.Equal ("t", AnsiEscapeSequenceRequests.Statuses.ToArray () [^1].AnsiRequest.Terminator);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        AnsiEscapeSequenceRequests.Clear ();
        Assert.NotNull (AnsiEscapeSequenceRequests.Statuses);
        Assert.Empty (AnsiEscapeSequenceRequests.Statuses);
    }

    [Fact]
    public void Remove_Tests ()
    {
        AnsiEscapeSequenceRequests.Clear ();
        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        AnsiEscapeSequenceRequests.HasResponse ("t", out AnsiEscapeSequenceRequestStatus seqReqStatus);
        AnsiEscapeSequenceRequests.Remove (seqReqStatus);
        Assert.Empty (AnsiEscapeSequenceRequests.Statuses);

        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        AnsiEscapeSequenceRequests.HasResponse ("t", out seqReqStatus);
        AnsiEscapeSequenceRequests.Remove (seqReqStatus);
        Assert.Single (AnsiEscapeSequenceRequests.Statuses);
        Assert.Equal ("t", AnsiEscapeSequenceRequests.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        AnsiEscapeSequenceRequests.HasResponse ("t", out seqReqStatus);
        AnsiEscapeSequenceRequests.Remove (seqReqStatus);
        Assert.Empty (AnsiEscapeSequenceRequests.Statuses);
    }

    [Fact]
    public void Requested_Tests ()
    {
        AnsiEscapeSequenceRequests.Clear ();
        Assert.False (AnsiEscapeSequenceRequests.HasResponse ("t", out AnsiEscapeSequenceRequestStatus seqReqStatus));
        Assert.Null (seqReqStatus);

        AnsiEscapeSequenceRequests.Add (new () { Request = "", Terminator = "t" });
        Assert.False (AnsiEscapeSequenceRequests.HasResponse ("r", out seqReqStatus));
        Assert.NotNull (seqReqStatus);
        Assert.Equal ("t", seqReqStatus.AnsiRequest.Terminator);
        Assert.True (AnsiEscapeSequenceRequests.HasResponse ("t", out seqReqStatus));
        Assert.NotNull (seqReqStatus);
        Assert.Equal ("t", seqReqStatus.AnsiRequest.Terminator);
    }
}
