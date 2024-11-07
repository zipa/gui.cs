#nullable enable
using System;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class AdornmentsEditor : EditorBase
{
    public AdornmentsEditor ()
    {
        Title = "AdornmentsEditor";

        TabStop = TabBehavior.TabGroup;

        ExpanderButton.Orientation = Orientation.Horizontal;

        Initialized += AdornmentsEditor_Initialized;
    }

    public MarginEditor? MarginEditor { get; set; }
    public BorderEditor? BorderEditor { get; private set; }
    public PaddingEditor? PaddingEditor { get; private set; }

    /// <inheritdoc/>
    protected override void OnViewToEditChanged ()
    {
        Enabled = ViewToEdit is not Adornment;

        if (MarginEditor is { })
        {
            MarginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        }

        if (BorderEditor is { })
        {
            BorderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        }

        if (PaddingEditor is { })
        {
            PaddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
        }

        if (Padding is { })
        {
            Padding.Text = $"View: {GetIdentifyingString (ViewToEdit)}";
        }
    }

    private string GetIdentifyingString (View? view)
    {
        if (view is null)
        {
            return "null";
        }

        if (!string.IsNullOrEmpty (view.Id))
        {
            return view.Id;
        }

        if (!string.IsNullOrEmpty (view.Title))
        {
            return view.Title;
        }

        if (!string.IsNullOrEmpty (view.Text))
        {
            return view.Text;
        }

        return view.GetType ().Name;
    }

    public bool ShowViewIdentifier
    {
        get => Padding is { } && Padding.Thickness != Thickness.Empty;
        set
        {
            if (Padding is null)
            {
                return;
            }

            Padding.Thickness = value ? new (0, 2, 0, 0) : Thickness.Empty;
        }
    }

    private void AdornmentsEditor_Initialized (object? sender, EventArgs e)
    {
        MarginEditor = new ()
        {
            X = 0,
            Y = 0,
            SuperViewRendersLineCanvas = true
        };
        Add (MarginEditor);

        BorderEditor = new ()
        {
            X = Pos.Left (MarginEditor),
            Y = Pos.Bottom (MarginEditor),
            SuperViewRendersLineCanvas = true
        };
        Add (BorderEditor);

        PaddingEditor = new ()
        {
            X = Pos.Left (BorderEditor),
            Y = Pos.Bottom (BorderEditor),
            SuperViewRendersLineCanvas = true
        };
        Add (PaddingEditor);

        MarginEditor.ExpanderButton.Collapsed = true;
        BorderEditor.ExpanderButton.Collapsed = true;
        PaddingEditor.ExpanderButton.Collapsed = true;

        MarginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        BorderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        PaddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
    }
}
