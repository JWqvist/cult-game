using Godot;

public partial class HUD : CanvasLayer
{
    private ProgressBar _healthBar;
    private Label _equippedLabel;
    private Label _cultStatsLabel;
    private Label _wantedLabel;
    private Player _player;

    public override void _Ready()
    {
        _healthBar = GetNode<ProgressBar>("VBoxContainer/HealthBar");
        _equippedLabel = GetNode<Label>("VBoxContainer/EquippedLabel");
        _cultStatsLabel = GetNode<Label>("VBoxContainer/CultStatsLabel");
        _wantedLabel = GetNode<Label>("VBoxContainer/WantedLabel");
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
    }
}
