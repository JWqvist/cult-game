using Godot;

public partial class HUD : CanvasLayer
{
    private ProgressBar _healthBar;
    private Label _equippedLabel;
    private Label _cultStatsLabel;
    private Label _wantedLabel;
    private Label _icLabel;
    private Label _missionLabel;
    private Player _player;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("VBoxContainer/HealthBar");
        _equippedLabel = GetNode<Label>("VBoxContainer/EquippedLabel");
        _cultStatsLabel = GetNode<Label>("VBoxContainer/CultStatsLabel");
        _wantedLabel = GetNode<Label>("VBoxContainer/WantedLabel");
        _icLabel = GetNode<Label>("VBoxContainer/ICLabel");
        // MissionLabel may not exist in older scene files; create it if missing
        _missionLabel = GetNodeOrNull<Label>("VBoxContainer/MissionLabel");
        if (_missionLabel == null)
        {
            _missionLabel = new Label();
            _missionLabel.Name = "MissionLabel";
            GetNode<VBoxContainer>("VBoxContainer").AddChild(_missionLabel);
        }
    }

    public override void _Process(double delta)
    {
        if (_player == null)
        {
            Node node = GetTree().GetFirstNodeInGroup("player");
            if (node is Player p)
                _player = p;
            return;
        }

        _healthBar.Value = _player.Health;
        _equippedLabel.Text = "Equipped: " + (_player.Inventory?.EquippedItem ?? "Fists");

        if (GameManager.Instance != null)
        {
            int followers = GameManager.Instance.CultSize;
            int money = (int)GameManager.Instance.Money;
            _cultStatsLabel.Text = $"Followers: {followers} | ${money}";
        }

        if (HeatSystem.Instance != null)
        {
            int stars = HeatSystem.Instance.WantedStars;
            _wantedLabel.Text = stars > 0 ? $"Wanted: {new string('*', stars)}" : "";
            _wantedLabel.Visible = stars > 0;
        }

        if (InnerCircle.Instance != null)
        {
            int r = InnerCircle.Instance.RecruiterCount;
            int e = InnerCircle.Instance.EnforcerCount;
            int f = InnerCircle.Instance.FinancierCount;
            _icLabel.Text = $"IC: R:{r} E:{e} F:{f}";
        }

        if (MissionSystem.Instance != null && _missionLabel != null)
        {
            string status = MissionSystem.Instance.MissionStatusText;
            _missionLabel.Text = status;
            _missionLabel.Visible = !string.IsNullOrEmpty(status);
        }
    }
}
