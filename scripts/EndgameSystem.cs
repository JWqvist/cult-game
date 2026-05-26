using Godot;

public enum EndgameOutcome { None, Win, LosePlayerDead, LoseCultCollapsed }

/// <summary>
/// Autoload singleton — monitors win and loss conditions, shows the endgame screen.
///
/// Win condition  : CultSize >= 200 (triggered from StorySystem milestone 200).
/// Loss condition : Player health drops to 0 (triggered from Player.TakeDamage).
///                  -or- cult collapses (followers reach 0 after having grown past 5).
///
/// On endgame, shows a full-screen overlay with outcome + path summary + Restart/Quit buttons.
/// </summary>
public partial class EndgameSystem : Node
{
    public static EndgameSystem Instance { get; private set; }

    public EndgameOutcome Outcome { get; private set; } = EndgameOutcome.None;
    public bool IsOver => Outcome != EndgameOutcome.None;

    // Track whether cult ever grew past 5 (to distinguish "never started" from "collapsed")
    private bool _cultEverGrew = false;
    private bool _collapseChecked = false;

    // UI
    private CanvasLayer _canvas;
    private Panel _panel;
    private Label _titleLabel;
    private Label _bodyLabel;

    [Signal]
    public delegate void GameEndedEventHandler(int outcome);

    public override void _Ready()
    {
        Instance = this;
        BuildUI();
        GD.Print("[EndgameSystem] Initialized.");
    }

    public override void _Process(double delta)
    {
        if (IsOver) return;
        if (GameManager.Instance == null) return;

        // Track growth
        if (GameManager.Instance.CultSize > 5)
            _cultEverGrew = true;

        // Cult collapse check (run once per frame only when cult has shrunk to 0)
        if (_cultEverGrew && GameManager.Instance.Followers.Count == 0 && !_collapseChecked)
        {
            _collapseChecked = true;
            // Give one frame grace — if still 0, trigger loss
            CallDeferred(MethodName.CheckCollapse);
        }
        else if (GameManager.Instance.Followers.Count > 0)
        {
            _collapseChecked = false;
        }
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>Called by StorySystem when CultSize milestone 200 is reached.</summary>
    public void TriggerWin()
    {
        if (IsOver) return;
        Outcome = EndgameOutcome.Win;
        ShowScreen();
        EmitSignal(SignalName.GameEnded, (int)Outcome);
        GD.Print("[EndgameSystem] WIN triggered.");
    }

    /// <summary>Called by Player.TakeDamage when health reaches 0.</summary>
    public void TriggerPlayerDead()
    {
        if (IsOver) return;
        Outcome = EndgameOutcome.LosePlayerDead;
        ShowScreen();
        EmitSignal(SignalName.GameEnded, (int)Outcome);
        GD.Print("[EndgameSystem] LOSS — player dead.");
    }

    /// <summary>Called internally when cult collapses to 0 followers.</summary>
    public void TriggerCultCollapse()
    {
        if (IsOver) return;
        Outcome = EndgameOutcome.LoseCultCollapsed;
        ShowScreen();
        EmitSignal(SignalName.GameEnded, (int)Outcome);
        GD.Print("[EndgameSystem] LOSS — cult collapsed.");
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private void CheckCollapse()
    {
        if (GameManager.Instance != null && GameManager.Instance.Followers.Count == 0)
            TriggerCultCollapse();
    }

    private void ShowScreen()
    {
        CultPath path = StorySystem.Instance?.CurrentPath ?? CultPath.None;
        int cultSize = GameManager.Instance?.CultSize ?? 0;
        int money = (int)(GameManager.Instance?.Money ?? 0);
        int missions = MissionSystem.Instance?.CompletedMissions ?? 0;

        string title;
        string body;

        switch (Outcome)
        {
            case EndgameOutcome.Win:
                title = "✦  YOU HAVE ASCENDED  ✦";
                body = BuildWinBody(path, cultSize, money, missions);
                break;
            case EndgameOutcome.LosePlayerDead:
                title = "✞  THE LEADER HAS FALLEN  ✞";
                body = $"Your cult of {cultSize} followers is leaderless.\n"
                     + $"Money gathered: ${money}\n"
                     + $"Missions completed: {missions}\n\n"
                     + "Without its prophet, the movement crumbles to ash.";
                break;
            default:
                title = "☠  THE CULT IS NO MORE  ☠";
                body = $"All followers have defected or been lost.\n"
                     + $"Money remaining: ${money}\n"
                     + $"Missions completed: {missions}\n\n"
                     + "A cult without believers is just a lonely person with ideas.";
                break;
        }

        _titleLabel.Text = title;
        _bodyLabel.Text = body;
        _panel.Visible = true;

        // Pause the game tree
        GetTree().Paused = true;
        _canvas.ProcessMode = ProcessModeEnum.Always;
    }

    private static string BuildWinBody(CultPath path, int cultSize, int money, int missions)
    {
        string pathLine = path switch
        {
            CultPath.Waco        => "PATH: Waco/Jonestown — You chose fire and fury.\nThe authorities couldn't stop what you had become.",
            CultPath.Scientology => "PATH: Scientology/NXIVM — You chose lawyers over guns.\nYour empire is untouchable; your followers, loyal.",
            CultPath.HeavensGate => "PATH: Heaven's Gate — You chose transcendence.\nThe stars called, and your people followed.",
            _                    => "PATH: Undecided — You kept all options open.\nPerhaps ambiguity was your greatest weapon.",
        };

        return $"Cult size reached: {cultSize} believers\n"
             + $"Money accumulated: ${money}\n"
             + $"Missions completed: {missions}\n\n"
             + pathLine + "\n\n"
             + "The world will remember your name.\n"
             + "Whether that is a good thing remains to be seen.";
    }

    private void BuildUI()
    {
        _canvas = new CanvasLayer();
        _canvas.Layer = 100;
        _canvas.ProcessMode = ProcessModeEnum.Always;
        AddChild(_canvas);

        // Full-screen dark background
        var bg = new ColorRect();
        bg.Color = new Color(0f, 0f, 0f, 0.88f);
        bg.AnchorLeft   = 0f;
        bg.AnchorRight  = 1f;
        bg.AnchorTop    = 0f;
        bg.AnchorBottom = 1f;
        bg.Visible = false;

        // Central panel
        _panel = new Panel();
        _panel.AnchorLeft   = 0.5f;
        _panel.AnchorRight  = 0.5f;
        _panel.AnchorTop    = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.OffsetLeft   = -340f;
        _panel.OffsetRight  =  340f;
        _panel.OffsetTop    = -220f;
        _panel.OffsetBottom =  220f;
        _panel.Visible = false;
        _canvas.AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AnchorLeft   = 0f;
        vbox.AnchorRight  = 1f;
        vbox.AnchorTop    = 0f;
        vbox.AnchorBottom = 1f;
        vbox.OffsetLeft   =  24f;
        vbox.OffsetRight  = -24f;
        vbox.OffsetTop    =  24f;
        vbox.OffsetBottom = -24f;
        _panel.AddChild(vbox);

        _titleLabel = new Label();
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 22);
        vbox.AddChild(_titleLabel);

        vbox.AddChild(new HSeparator());

        _bodyLabel = new Label();
        _bodyLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _bodyLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(_bodyLabel);

        vbox.AddChild(new HSeparator());

        var btnRow = new HBoxContainer();
        btnRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(btnRow);

        var restartBtn = new Button();
        restartBtn.Text = "Restart";
        restartBtn.ProcessMode = ProcessModeEnum.Always;
        restartBtn.Pressed += () =>
        {
            GetTree().Paused = false;
            GetTree().ReloadCurrentScene();
        };
        btnRow.AddChild(restartBtn);

        var quitBtn = new Button();
        quitBtn.Text = "Quit";
        quitBtn.ProcessMode = ProcessModeEnum.Always;
        quitBtn.Pressed += () => GetTree().Quit();
        btnRow.AddChild(quitBtn);
    }
}
