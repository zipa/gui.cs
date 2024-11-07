namespace Terminal.Gui.InputTests;

public class AnsiEscapeSequenceRequestsTests
{
    [Fact]
    public void Add_Tests ()
    {
        var escSeqReq = new AnsiEscapeSequenceRequests ();
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Equal (2, escSeqReq.Statuses.Count);
        Assert.Equal ("t", escSeqReq.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        escSeqReq = new ();
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Equal (2, escSeqReq.Statuses.Count);
        Assert.Equal ("t", escSeqReq.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.Equal (3, escSeqReq.Statuses.Count);
        Assert.Equal ("t", escSeqReq.Statuses.ToArray () [^1].AnsiRequest.Terminator);
    }

    [Fact]
    public void Constructor_Defaults ()
    {
        var escSeqReq = new AnsiEscapeSequenceRequests ();
        Assert.NotNull (escSeqReq.Statuses);
        Assert.Empty (escSeqReq.Statuses);
    }

    [Fact]
    public void Remove_Tests ()
    {
        var escSeqReq = new AnsiEscapeSequenceRequests ();
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.HasResponse ("t", out AnsiEscapeSequenceRequestStatus seqReqStatus);
        escSeqReq.Remove (seqReqStatus);
        Assert.Empty (escSeqReq.Statuses);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        escSeqReq.HasResponse ("t", out seqReqStatus);
        escSeqReq.Remove (seqReqStatus);
        Assert.Single (escSeqReq.Statuses);
        Assert.Equal ("t", escSeqReq.Statuses.ToArray () [^1].AnsiRequest.Terminator);

        escSeqReq.HasResponse ("t", out seqReqStatus);
        escSeqReq.Remove (seqReqStatus);
        Assert.Empty (escSeqReq.Statuses);
    }

    [Fact]
    public void Requested_Tests ()
    {
        var escSeqReq = new AnsiEscapeSequenceRequests ();
        Assert.False (escSeqReq.HasResponse ("t", out AnsiEscapeSequenceRequestStatus seqReqStatus));
        Assert.Null (seqReqStatus);

        escSeqReq.Add (new () { Request = "", Terminator = "t" });
        Assert.False (escSeqReq.HasResponse ("r", out seqReqStatus));
        Assert.Null (seqReqStatus);
        Assert.True (escSeqReq.HasResponse ("t", out seqReqStatus));
        Assert.NotNull (seqReqStatus);
    }
}
