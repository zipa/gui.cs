namespace Terminal.Gui.InputTests;

public class MouseBindingsTests
{
    [Fact]
    public void Add_Adds ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];

        var flags = MouseFlags.AllEvents;
        mouseBindings.Add (flags, commands);
        MouseBinding binding = mouseBindings.Get (flags);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = mouseBindings.Get (flags);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        Command [] resultCommands = mouseBindings.GetCommands (flags);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_Invalid_Flag_Throws ()
    {
        var mouseBindings = new MouseBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => mouseBindings.Add (MouseFlags.None, Command.Accept));
    }

    [Fact]
    public void Add_Multiple_Commands_Adds ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];

        mouseBindings.Add (MouseFlags.Button1Clicked, commands);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);

        mouseBindings.Add (MouseFlags.Button2Clicked, commands);
        resultCommands = mouseBindings.GetCommands (MouseFlags.Button2Clicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void Add_No_Commands_Throws ()
    {
        var mouseBindings = new MouseBindings ();
        List<Command> commands = new ();
        Assert.Throws<ArgumentException> (() => mouseBindings.Add (MouseFlags.Button1Clicked, commands.ToArray ()));
    }

    [Fact]
    public void Add_Single_Command_Adds ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings.Add (MouseFlags.Button2Clicked, Command.HotKey);
        resultCommands = mouseBindings.GetCommands (MouseFlags.Button2Clicked);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Add should not allow duplicates
    [Fact]
    public void Add_Throws_If_Exists ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept));

        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept));

        resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept));

        resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.HotKey, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept);
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Add (MouseFlags.Button1Clicked, Command.ScrollDown));

        resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.Accept, resultCommands);

        mouseBindings = new ();
        mouseBindings.Add (MouseFlags.Button1Clicked, new MouseBinding ([Command.HotKey], MouseFlags.Button1Clicked));

        Assert.Throws<InvalidOperationException> (
                                                  () => mouseBindings.Add (
                                                                           MouseFlags.Button1Clicked,
                                                                           new MouseBinding ([Command.Accept], MouseFlags.Button1Clicked)));

        resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    // Clear
    [Fact]
    public void Clear_Clears ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        mouseBindings.Clear ();
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Empty (resultCommands);
        resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void Defaults ()
    {
        var mouseBindings = new MouseBindings ();
        Assert.Empty (mouseBindings.GetBindings ());
        Assert.Equal (MouseFlags.None, mouseBindings.GetMouseFlagsFromCommands (Command.Accept));
    }

    [Fact]
    public void Get_Binding_Not_Found_Throws ()
    {
        var mouseBindings = new MouseBindings ();
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Get (MouseFlags.Button1Clicked));
        Assert.Throws<InvalidOperationException> (() => mouseBindings.Get (MouseFlags.AllEvents));
    }

    // GetCommands
    [Fact]
    public void GetCommands_Unknown_ReturnsEmpty ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Empty (resultCommands);
    }

    [Fact]
    public void GetCommands_WithCommands_ReturnsCommands ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.HotKey, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleBindings_ReturnsCommands ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];
        mouseBindings.Add (MouseFlags.Button1Clicked, commands);
        mouseBindings.Add (MouseFlags.Button2Clicked, commands);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
        resultCommands = mouseBindings.GetCommands (MouseFlags.Button2Clicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetCommands_WithMultipleCommands_ReturnsCommands ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];
        mouseBindings.Add (MouseFlags.Button1Clicked, commands);
        Command [] resultCommands = mouseBindings.GetCommands (MouseFlags.Button1Clicked);
        Assert.Contains (Command.Right, resultCommands);
        Assert.Contains (Command.Left, resultCommands);
    }

    [Fact]
    public void GetMouseFlagsFromCommands_MultipleCommands ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands1 = [Command.Right, Command.Left];
        mouseBindings.Add (MouseFlags.Button1Clicked, commands1);

        Command [] commands2 = { Command.Up, Command.Down };
        mouseBindings.Add (MouseFlags.Button2Clicked, commands2);

        MouseFlags mouseFlags = mouseBindings.GetMouseFlagsFromCommands (commands1);
        Assert.Equal (MouseFlags.Button1Clicked, mouseFlags);

        mouseFlags = mouseBindings.GetMouseFlagsFromCommands (commands2);
        Assert.Equal (MouseFlags.Button2Clicked, mouseFlags);
    }

    [Fact]
    public void GetMouseFlagsFromCommands_OneCommand ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.Right);

        MouseFlags mouseFlags = mouseBindings.GetMouseFlagsFromCommands (Command.Right);
        Assert.Equal (MouseFlags.Button1Clicked, mouseFlags);
    }

    // GetMouseFlagsFromCommands
    [Fact]
    public void GetMouseFlagsFromCommands_Unknown_Returns_Key_Empty ()
    {
        var mouseBindings = new MouseBindings ();
        Assert.Equal (MouseFlags.None, mouseBindings.GetMouseFlagsFromCommands (Command.Accept));
    }

    [Fact]
    public void GetMouseFlagsFromCommands_WithCommands_ReturnsKey ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        MouseFlags mouseFlags = mouseBindings.GetMouseFlagsFromCommands (Command.HotKey);
        Assert.Equal (MouseFlags.Button1Clicked, mouseFlags);
    }

    [Fact]
    public void ReplaceMouseFlags_Replaces ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        mouseBindings.Add (MouseFlags.Button2Clicked, Command.HotKey);
        mouseBindings.Add (MouseFlags.Button3Clicked, Command.HotKey);
        mouseBindings.Add (MouseFlags.Button4Clicked, Command.HotKey);

        mouseBindings.ReplaceMouseFlag (MouseFlags.Button1Clicked, MouseFlags.Button1DoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.Button1Clicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.Button1DoubleClicked));

        mouseBindings.ReplaceMouseFlag (MouseFlags.Button2Clicked, MouseFlags.Button2DoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.Button2Clicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.Button2DoubleClicked));

        mouseBindings.ReplaceMouseFlag (MouseFlags.Button3Clicked, MouseFlags.Button3DoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.Button3Clicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.Button3DoubleClicked));

        mouseBindings.ReplaceMouseFlag (MouseFlags.Button4Clicked, MouseFlags.Button4DoubleClicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.Button4Clicked));
        Assert.Contains (Command.HotKey, mouseBindings.GetCommands (MouseFlags.Button4DoubleClicked));
    }

    [Fact]
    public void ReplaceMouseFlags_Replaces_Leaves_Old_Binding ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept);
        mouseBindings.Add (MouseFlags.Button2Clicked, Command.HotKey);

        mouseBindings.ReplaceMouseFlag (mouseBindings.GetMouseFlagsFromCommands (Command.Accept), MouseFlags.Button3Clicked);
        Assert.Empty (mouseBindings.GetCommands (MouseFlags.Button1Clicked));
        Assert.Contains (Command.Accept, mouseBindings.GetCommands (MouseFlags.Button3Clicked));
    }

    [Fact]
    public void ReplaceMouseFlags_Adds_If_DoesNotContain_Old ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.ReplaceMouseFlag (MouseFlags.Button1Clicked, MouseFlags.Button2Clicked);
        Assert.True (mouseBindings.TryGet (MouseFlags.Button2Clicked, out _));
    }

    [Fact]
    public void ReplaceMouseFlags_Throws_If_New_Is_None ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        Assert.Throws<ArgumentException> (() => mouseBindings.ReplaceMouseFlag (MouseFlags.Button1Clicked, MouseFlags.None));
    }

    [Fact]
    public void Get_Gets ()
    {
        var mouseBindings = new MouseBindings ();
        Command [] commands = [Command.Right, Command.Left];

        mouseBindings.Add (MouseFlags.Button1Clicked, commands);
        MouseBinding binding = mouseBindings.Get (MouseFlags.Button1Clicked);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);

        binding = mouseBindings.Get (MouseFlags.Button1Clicked);
        Assert.Contains (Command.Right, binding.Commands);
        Assert.Contains (Command.Left, binding.Commands);
    }

    // TryGet
    [Fact]
    public void TryGet_Succeeds ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        bool result = mouseBindings.TryGet (MouseFlags.Button1Clicked, out MouseBinding _);
        Assert.True (result);
        ;
    }

    [Fact]
    public void TryGet_Unknown_ReturnsFalse ()
    {
        var mouseBindings = new MouseBindings ();
        bool result = mouseBindings.TryGet (MouseFlags.Button1Clicked, out MouseBinding _);
        Assert.False (result);
    }

    [Fact]
    public void TryGet_WithCommands_ReturnsTrue ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.HotKey);
        bool result = mouseBindings.TryGet (MouseFlags.Button1Clicked, out MouseBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.HotKey, bindings.Commands);
    }

    [Fact]
    public void ReplaceCommands_Replaces ()
    {
        var mouseBindings = new MouseBindings ();
        mouseBindings.Add (MouseFlags.Button1Clicked, Command.Accept);

        mouseBindings.ReplaceCommands (MouseFlags.Button1Clicked, Command.Refresh);

        bool result = mouseBindings.TryGet (MouseFlags.Button1Clicked, out MouseBinding bindings);
        Assert.True (result);
        Assert.Contains (Command.Refresh, bindings.Commands);
    }
}
