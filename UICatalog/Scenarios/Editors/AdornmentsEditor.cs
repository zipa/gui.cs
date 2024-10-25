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

    private MarginEditor? _marginEditor;
    private BorderEditor? _borderEditor;
    private PaddingEditor? _paddingEditor;

    /// <inheritdoc />
    protected override void OnViewToEditChanged ()
    {

        if (_marginEditor is { })
        {
            _marginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        }

        if (_borderEditor is { })
        {
            _borderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        }

        if (_paddingEditor is { })
        {
            _paddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;
        }

        if (ViewToEdit is not Adornment)
        {
            Enabled = true;
        }
        else
        {
            Enabled = false;
        }

        Padding.Text = $"View: {GetIdentifyingString (ViewToEdit)}";
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
        get => Padding.Thickness != Thickness.Empty;
        set
        {
            if (value)
            {
                Padding.Thickness = new (0, 2, 0, 0);
            }
            else
            {
                Padding.Thickness = Thickness.Empty;
            }
        }
    }

    private void AdornmentsEditor_Initialized (object? sender, EventArgs e)
    {
        _marginEditor = new ()
        {
            X = 0,
            Y = 0,
            SuperViewRendersLineCanvas = true,
        };
        Add (_marginEditor);

        _borderEditor = new ()
        {
            X = Pos.Left (_marginEditor),
            Y = Pos.Bottom (_marginEditor),
            SuperViewRendersLineCanvas = true
        };
        Add (_borderEditor);

        _paddingEditor = new ()
        {
            X = Pos.Left (_borderEditor),
            Y = Pos.Bottom (_borderEditor),
            SuperViewRendersLineCanvas = true
        };
        Add (_paddingEditor);


        _marginEditor.AdornmentToEdit = ViewToEdit?.Margin ?? null;
        _borderEditor.AdornmentToEdit = ViewToEdit?.Border ?? null;
        _paddingEditor.AdornmentToEdit = ViewToEdit?.Padding ?? null;

    }
}
