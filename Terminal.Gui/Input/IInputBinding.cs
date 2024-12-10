#nullable enable
namespace Terminal.Gui;

public interface IInputBinding
{
    Command [] Commands { get; set; }
}
