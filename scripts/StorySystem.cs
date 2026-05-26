using Godot;
using System.Collections.Generic;

public enum CultPath { None, Waco, Scientology, HeavensGate }

/// <summary>
/// Autoload singleton. Fires story events at CultSize milestones 10/30/75/150.
/// At 75 members shows a path choice dialogue. Each path has mechanical effects.
/// </summary>
public partial class StorySystem : Node
{
    public static StorySystem Instance { get; private set; }

    public CultPath CurrentPath { get; private set; } = CultPath.None;

    private HashSet<int> _firedMilestones = new HashSet<int>();
    private static readonly int[] Milestones = { 10, 30, 75, 150 };

    // Popup UI
    private CanvasLayer _canvas;
    private Panel _popupPanel;
    private Label _popupText;
    private Button _dismissBtn;
    private VBoxContainer _choiceBox;
    private bool _popupOpen = false;

    public override void _Ready()
    {
        Instance = this;
        BuildUI();
        GD.Print("[StorySystem] Initialized.");
    }

    public override void _Process(double delta)
    {
        if (_popupOpen) return;
        if (GameManager.Instance == null) return;

        int cultSize = GameManager.Instance.CultSize;
        foreach (int milestone in Milestones)
        {
            if (!_firedMilestones.Contains(milestone) && cultSize >= milestone)
            {
                _firedMilestones.Add(milestone);
                FireMilestone(milestone);
                break; // one popup at a time
            }
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (!_popupOpen) return;
        if (ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Enter || key.Keycode == Key.Space)
            {
                // Only dismiss if path choice is not showing
                if (!_choiceBox.Visible)
                {
                    DismissPopup();
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void FireMilestone(int milestone)
    {
        GD.Print($"[StorySystem] Milestone {milestone} reached.");
        switch (milestone)
        {
            case 10:
                ShowPopup($"Word spreads. You have {GameManager.Instance.CultSize} believers.",
                    showDismiss: true, showChoices: false);
                break;
            case 30:
                ShowPopup("The compound expands. Authorities are watching.",
                    showDismiss: true, showChoices: false);
                break;
            case 75:
                ShowPopup("Your cult stands at a crossroads. Choose your path:",
                    showDismiss: false, showChoices: true);
                break;
            case 150:
                string msg = CurrentPath switch
                {
                    CultPath.Waco        => "You have built something unstoppable. The flames will not be forgotten.",
                    CultPath.Scientology => "You have built something unstoppable. The lawyers will see to that.",
                    CultPath.HeavensGate => "You have built something unstoppable. The stars await.",
                    _                    => "You have built something unstoppable."
                };
                ShowPopup(msg, showDismiss: true, showChoices: false);
                break;
        }
    }

    private void ShowPopup(string text, bool showDismiss, bool showChoices)
    {
        _popupOpen = true;
        _popupText.Text = text;
        _dismissBtn.Visible = showDismiss;
        _choiceBox.Visible = showChoices;
        _popupPanel.Visible = true;
    }

    private void DismissPopup()
    {
        _popupPanel.Visible = false;
        _popupOpen = false;
    }

    private void ChoosePath(CultPath path)
    {
        CurrentPath = path;
        GD.Print($"[StorySystem] Path chosen: {path}");
        ApplyPathEffects(path);
        DismissPopup();
    }

    private void ApplyPathEffects(CultPath path)
    {
        switch (path)
        {
            case CultPath.Waco:
                HeatSystem.Instance?.SetDecayMultiplier(0.5f);
                GD.Print("[StorySystem] Waco: heat decay halved (police more aggressive).");
                break;
            case CultPath.Scientology:
                InnerCircle.Instance?.SetFinancierBonus(2.5f);
                GD.Print("[StorySystem] Scientology: financier donation bonus → 2.5x.");
                break;
            case CultPath.HeavensGate:
                InnerCircle.Instance?.SetRecruiterInterval(30f);
                GD.Print("[StorySystem] HeavensGate: recruiter auto-recruits every 30s.");
                break;
        }
    }

    private void BuildUI()
    {
        _canvas = new CanvasLayer();
        _canvas.Layer = 50;
        AddChild(_canvas);

        // Centered panel 600×300
        _popupPanel = new Panel();
        _popupPanel.AnchorLeft   = 0.5f;
        _popupPanel.AnchorRight  = 0.5f;
        _popupPanel.AnchorTop    = 0.5f;
        _popupPanel.AnchorBottom = 0.5f;
        _popupPanel.OffsetLeft   = -300f;
        _popupPanel.OffsetRight  =  300f;
        _popupPanel.OffsetTop    = -150f;
        _popupPanel.OffsetBottom =  150f;
        _popupPanel.Visible = false;
        _canvas.AddChild(_popupPanel);

        var vbox = new VBoxContainer();
        vbox.AnchorLeft   = 0f;
        vbox.AnchorRight  = 1f;
        vbox.AnchorTop    = 0f;
        vbox.AnchorBottom = 1f;
        vbox.OffsetLeft   =  20f;
        vbox.OffsetRight  = -20f;
        vbox.OffsetTop    =  20f;
        vbox.OffsetBottom = -20f;
        _popupPanel.AddChild(vbox);

        _popupText = new Label();
        _popupText.AutowrapMode = TextServer.AutowrapMode.Word;
        _popupText.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(_popupText);

        // Path choice buttons (visible only at milestone 75)
        _choiceBox = new VBoxContainer();
        _choiceBox.Visible = false;
        vbox.AddChild(_choiceBox);

        var wacoBtn = new Button();
        wacoBtn.Text = "\"We are above the law\"  →  Waco/Jonestown path";
        wacoBtn.Pressed += () => ChoosePath(CultPath.Waco);
        _choiceBox.AddChild(wacoBtn);

        var sciBtn = new Button();
        sciBtn.Text = "\"We need legitimacy\"  →  Scientology/NXIVM path";
        sciBtn.Pressed += () => ChoosePath(CultPath.Scientology);
        _choiceBox.AddChild(sciBtn);

        var hgBtn = new Button();
        hgBtn.Text = "\"The end is near\"  →  Heaven's Gate path";
        hgBtn.Pressed += () => ChoosePath(CultPath.HeavensGate);
        _choiceBox.AddChild(hgBtn);

        // Dismiss button (visible for non-choice popups)
        _dismissBtn = new Button();
        _dismissBtn.Text = "Continue  [Enter / Space]";
        _dismissBtn.Pressed += () => DismissPopup();
        vbox.AddChild(_dismissBtn);
    }
}
