using Godot;

/// <summary>
/// Handles NPC recruitment and crimes. Attach as child Node of Player.
/// Press interact (E) near a Pedestrian to show options.
/// Non-recruited: "Recruit [Y] | Mug [M] | Theft [T] | Scam [S] | Cancel [N]".
/// Mug:   $10-30,  heat +0.5
/// Theft: $30-60,  heat +1.0  (pickpocket)
/// Scam:  $5-15,   heat +0.1  (low risk, low reward)
/// </summary>
public partial class RecruitSystem : Node
{
    private const float InteractRange = 80f;
    private const float MugMinMoney = 10f;
    private const float MugMaxMoney = 30f;
    private const float MugHeat = 0.5f;
    private const float TheftMinMoney = 30f;
    private const float TheftMaxMoney = 60f;
    private const float TheftHeat = 1.0f;
    private const float ScamMinMoney = 5f;
    private const float ScamMaxMoney = 15f;
    private const float ScamHeat = 0.1f;

    private static readonly Color Gold   = new Color(1.00f, 0.84f, 0.00f);
    private static readonly Color DarkBg = new Color(0.00f, 0.00f, 0.00f, 0.80f);

    private PanelContainer _dialoguePanel;
    private Label _npcNameLabel;
    private Label _optionsLabel;
    private bool _dialogueActive = false;
    private bool _targetIsRecruited = false;
    private Pedestrian _targetPedestrian = null;
    private Node2D _player = null;

    public override void _Ready()
    {
        var canvas = new CanvasLayer();
        canvas.Layer = 20;
        AddChild(canvas);

        // Styled dark panel, slightly above screen center
        _dialoguePanel = new PanelContainer();
        _dialoguePanel.AnchorLeft   = 0.5f;
        _dialoguePanel.AnchorRight  = 0.5f;
        _dialoguePanel.AnchorTop    = 0.5f;
        _dialoguePanel.AnchorBottom = 0.5f;
        _dialoguePanel.OffsetLeft   = -160f;
        _dialoguePanel.OffsetRight  =  160f;
        _dialoguePanel.OffsetTop    = -100f;
        _dialoguePanel.OffsetBottom =  -4f;
        _dialoguePanel.Visible = false;

        var style = new StyleBoxFlat();
        style.BgColor = DarkBg;
        style.BorderColor = Gold;
        style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = style.BorderWidthBottom = 2;
        style.CornerRadiusTopLeft = style.CornerRadiusTopRight = style.CornerRadiusBottomLeft = style.CornerRadiusBottomRight = 6;
        style.ContentMarginLeft = style.ContentMarginRight = 14f;
        style.ContentMarginTop = style.ContentMarginBottom = 10f;
        _dialoguePanel.AddThemeStyleboxOverride("panel", style);
        canvas.AddChild(_dialoguePanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        _dialoguePanel.AddChild(vbox);

        _npcNameLabel = new Label();
        _npcNameLabel.Text = "Stranger";
        _npcNameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _npcNameLabel.AddThemeColorOverride("font_color", Gold);
        _npcNameLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_npcNameLabel);

        _optionsLabel = new Label();
        _optionsLabel.Text = "[Y] Recruit  [M] Mug  [T] Theft  [S] Scam  [N] Cancel";
        _optionsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _optionsLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.88f, 0.88f));
        _optionsLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_optionsLabel);
    }

    public override void _Process(double delta)
    {
        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Node2D n2d)
                _player = n2d;
        }

        if (!_dialogueActive && Input.IsActionJustPressed("interact"))
        {
            if (_player is Player playerNode && playerNode.IsInVehicle)
                return;
            TryOpenDialogue();
        }
    }

    public override void _Input(InputEvent ev)
    {
        if (!_dialogueActive) return;

        if (ev is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Y)
            {
                AttemptRecruit();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.M && !_targetIsRecruited)
            {
                AttemptMug();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.T && !_targetIsRecruited)
            {
                AttemptTheft();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.S && !_targetIsRecruited)
            {
                AttemptScam();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.N)
            {
                HideDialogue();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void TryOpenDialogue()
    {
        if (_player == null) return;

        Pedestrian nearest = null;
        float nearestDist = InteractRange;

        foreach (Node node in GetTree().GetNodesInGroup("pedestrians"))
        {
            if (node is Pedestrian ped)
            {
                float dist = _player.GlobalPosition.DistanceTo(ped.GlobalPosition);
                if (dist < nearestDist)
                {
                    nearest = ped;
                    nearestDist = dist;
                }
            }
        }

        if (nearest != null)
        {
            _targetPedestrian = nearest;
            _targetIsRecruited = nearest.IsRecruited;

            int nameNum = Mathf.Abs((int)(nearest.GlobalPosition.X + nearest.GlobalPosition.Y)) % 99 + 1;
            _npcNameLabel.Text = "Stranger #" + nameNum;

            _optionsLabel.Text = _targetIsRecruited
                ? "Already a follower."
                : "[Y] Recruit  [M] Mug  [T] Theft  [S] Scam  [N] Cancel";

            _dialoguePanel.Visible = true;
            _dialogueActive = true;
        }
    }

    private void AttemptRecruit()
    {
        HideDialogue();

        if (_targetPedestrian == null || !IsInstanceValid(_targetPedestrian)) return;
        if (_targetIsRecruited)
        {
            _targetPedestrian = null;
            return;
        }

        bool success = GD.Randf() < 0.4f;
        if (success)
        {
            _targetPedestrian.StartFollowing();
            GameManager.Instance.AddFollower();
            int total = GameManager.Instance.CultSize;
            ToastManager.Show("New follower! (" + total + " total)", ToastManager.ColorGood);
            GD.Print("[RecruitSystem] Recruited! CultSize=", total);
        }
        else
        {
            ToastManager.Show("Recruitment failed.", new Color(0.70f, 0.70f, 0.70f));
            GD.Print("[RecruitSystem] Recruitment failed.");
        }

        _targetPedestrian = null;
    }

    private void AttemptMug()
    {
        HideDialogue();

        if (_targetPedestrian == null || !IsInstanceValid(_targetPedestrian)) return;

        float stolen = MugMinMoney + GD.Randf() * (MugMaxMoney - MugMinMoney);
        GameManager.Instance.AddMoney(stolen);
        HeatSystem.Instance?.AddHeat(MugHeat);
        MissionSystem.Instance?.ReportMug();   // mug missions count real muggings, not just kills
        ToastManager.Show("Mugged! +$" + (int)stolen, ToastManager.ColorMoney);
        GD.Print("[RecruitSystem] Mugged for $", stolen.ToString("F0"));

        _targetPedestrian = null;
    }

    private void AttemptTheft()
    {
        HideDialogue();

        if (_targetPedestrian == null || !IsInstanceValid(_targetPedestrian)) return;

        float stolen = TheftMinMoney + GD.Randf() * (TheftMaxMoney - TheftMinMoney);
        GameManager.Instance.AddMoney(stolen);
        HeatSystem.Instance?.AddHeat(TheftHeat);
        ToastManager.Show("Theft! +$" + (int)stolen, ToastManager.ColorMoney);
        GD.Print("[RecruitSystem] Theft for $", stolen.ToString("F0"));

        _targetPedestrian = null;
    }

    private void AttemptScam()
    {
        HideDialogue();

        if (_targetPedestrian == null || !IsInstanceValid(_targetPedestrian)) return;

        float earned = ScamMinMoney + GD.Randf() * (ScamMaxMoney - ScamMinMoney);
        GameManager.Instance.AddMoney(earned);
        HeatSystem.Instance?.AddHeat(ScamHeat);
        ToastManager.Show("Scam! +$" + (int)earned, ToastManager.ColorMoney);
        GD.Print("[RecruitSystem] Scam for $", earned.ToString("F0"));

        _targetPedestrian = null;
    }

    private void HideDialogue()
    {
        _dialoguePanel.Visible = false;
        _dialogueActive = false;
    }
}
