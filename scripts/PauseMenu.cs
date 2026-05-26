using Godot;

/// <summary>
/// Autoload CanvasLayer. Shows a styled pause/status menu on ESC.
/// Displays cult stats while paused. ProcessMode=Always so it works while paused.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    private static readonly Color Gold   = new Color(1.00f, 0.84f, 0.00f);
    private static readonly Color DimBg  = new Color(0.00f, 0.00f, 0.00f, 0.80f);

    private Control _overlay;
    private Label _moneyVal;
    private Label _followersVal;
    private Label _heatVal;
    private Label _icVal;
    private bool _paused;

    public override void _Ready()
    {
        Layer = 50;
        ProcessMode = Node.ProcessModeEnum.Always;
        BuildUI();
        _overlay.Visible = false;
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        if (ev.IsActionPressed("ui_cancel"))
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }

    private void TogglePause()
    {
        _paused = !_paused;
        _overlay.Visible = _paused;
        GetTree().Paused = _paused;
        if (_paused)
            RefreshStats();
    }

    private void RefreshStats()
    {
        if (GameManager.Instance == null) return;
        _moneyVal.Text    = "$" + (int)GameManager.Instance.Money;
        _followersVal.Text = GameManager.Instance.CultSize.ToString();
        _heatVal.Text     = (HeatSystem.Instance?.WantedStars ?? 0) + " / 3";

        if (InnerCircle.Instance != null)
        {
            int r = InnerCircle.Instance.RecruiterCount;
            int e = InnerCircle.Instance.EnforcerCount;
            int f = InnerCircle.Instance.FinancierCount;
            _icVal.Text = $"R:{r} E:{e} F:{f}";
        }
    }

    // ── UI construction ──────────────────────────────────────────────────────

    private void BuildUI()
    {
        _overlay = new Control();
        _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.ProcessMode = Node.ProcessModeEnum.Always;
        AddChild(_overlay);

        // Dark backdrop
        var bg = new ColorRect();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        bg.Color = DimBg;
        _overlay.AddChild(bg);

        // Center panel
        var panelContainer = new PanelContainer();
        panelContainer.AnchorLeft   = 0.5f;
        panelContainer.AnchorRight  = 0.5f;
        panelContainer.AnchorTop    = 0.5f;
        panelContainer.AnchorBottom = 0.5f;
        panelContainer.OffsetLeft   = -200f;
        panelContainer.OffsetRight  =  200f;
        panelContainer.OffsetTop    = -190f;
        panelContainer.OffsetBottom =  190f;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.06f, 0.06f, 0.06f, 0.96f);
        panelStyle.BorderColor = Gold;
        panelStyle.BorderWidthLeft = panelStyle.BorderWidthRight = panelStyle.BorderWidthTop = panelStyle.BorderWidthBottom = 2;
        panelStyle.CornerRadiusTopLeft = panelStyle.CornerRadiusTopRight = panelStyle.CornerRadiusBottomLeft = panelStyle.CornerRadiusBottomRight = 6;
        panelStyle.ContentMarginLeft = panelStyle.ContentMarginRight = 20f;
        panelStyle.ContentMarginTop = panelStyle.ContentMarginBottom = 16f;
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
        _overlay.AddChild(panelContainer);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        panelContainer.AddChild(vbox);

        // Title
        var title = new Label();
        title.Text = "CULT RISING";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", Gold);
        title.AddThemeFontSizeOverride("font_size", 26);
        vbox.AddChild(title);

        var subtitle = new Label();
        subtitle.Text = "— PAUSED —";
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
        subtitle.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(subtitle);

        vbox.AddChild(new HSeparator());

        // Stats grid
        var grid = new GridContainer();
        grid.Columns = 2;
        grid.AddThemeConstantOverride("h_separation", 24);
        grid.AddThemeConstantOverride("v_separation", 10);
        vbox.AddChild(grid);

        _moneyVal    = AddStatRow(grid, "Money");
        _followersVal = AddStatRow(grid, "Followers");
        _heatVal     = AddStatRow(grid, "Heat");
        _icVal       = AddStatRow(grid, "Inner Circle");

        vbox.AddChild(new HSeparator());

        var hint = new Label();
        hint.Text = "TAB — Inner Circle    B — Bribe police";
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        hint.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
        hint.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(hint);

        var resumeBtn = new Button();
        resumeBtn.Text = "RESUME  [ESC]";
        resumeBtn.AddThemeColorOverride("font_color", Gold);
        resumeBtn.Pressed += TogglePause;
        vbox.AddChild(resumeBtn);
    }

    private Label AddStatRow(GridContainer grid, string labelText)
    {
        var lbl = new Label();
        lbl.Text = labelText + ":";
        lbl.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
        grid.AddChild(lbl);

        var val = new Label();
        val.Text = "—";
        val.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f));
        grid.AddChild(val);

        return val;
    }
}
