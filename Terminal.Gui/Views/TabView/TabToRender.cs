#nullable enable
namespace Terminal.Gui;

internal class TabToRender
{
    public TabToRender (Tab tab, bool isSelected)
    {
        Tab = tab;
        IsSelected = isSelected;
    }

    /// <summary>True if the tab that is being rendered is the selected one.</summary>
    /// <value></value>
    public bool IsSelected { get; }

    public Tab Tab { get; }
}
