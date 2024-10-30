using Moq;

namespace UnitTests.ConsoleDrivers;


public class AnsiRequestSchedulerTests
{
    private readonly Mock<IAnsiResponseParser> _parserMock;
    private readonly AnsiRequestScheduler _scheduler;

    private static DateTime _staticNow; // Static value to hold the current time

    public AnsiRequestSchedulerTests ()
    {
        _parserMock = new Mock<IAnsiResponseParser> (MockBehavior.Strict);
        _staticNow = DateTime.UtcNow; // Initialize static time
        _scheduler = new AnsiRequestScheduler (_parserMock.Object, () => _staticNow);
    }

    [Fact]
    public void SendOrSchedule_SendsDeviceAttributeRequest_WhenNoOutstandingRequests ()
    {
        // Arrange
        var request = new AnsiEscapeSequenceRequest
        {
            Request = "\u001b[0c", // ESC [ c
            Terminator = "c",
            ResponseReceived = r => { }
        };

        // we have no outstanding for c already
        _parserMock.Setup (p => p.IsExpecting ("c")).Returns (false).Verifiable(Times.Once);

        // then we should execute our request
        _parserMock.Setup (p => p.ExpectResponse ("c", It.IsAny<Action<string>> (), false)).Verifiable (Times.Once);

        // Act
        bool result = _scheduler.SendOrSchedule (request);


        // Assert
        Assert.Empty (_scheduler.QueuedRequests); // We sent it i.e. we did not queue it for later
        Assert.True (result); // Should send immediately
        _parserMock.Verify ();
    }
    [Fact]
    public void SendOrSchedule_QueuesRequest_WhenOutstandingRequestExists ()
    {
        // Arrange
        var request1 = new AnsiEscapeSequenceRequest
        {
            Request = "\u001b[0c", // ESC [ 0 c
            Terminator = "c",
            ResponseReceived = r => { }
        };

        // Parser already has an ongoing request for "c"
        _parserMock.Setup (p => p.IsExpecting ("c")).Returns (true).Verifiable (Times.Once);

        // Act
        var result = _scheduler.SendOrSchedule (request1);

        // Assert
        Assert.Single (_scheduler.QueuedRequests); // Ensure only one request is in the queue
        Assert.False (result); // Should be queued
        _parserMock.Verify ();
    }
    private void SetTime (int milliseconds)
    {
        // This simulates the passing of time by setting the Now function to return a specific time.
        var newNow = _staticNow.AddMilliseconds (milliseconds);
        _scheduler.Now = () => newNow;
    }
}