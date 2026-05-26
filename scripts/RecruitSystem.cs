using Godot;

/// <summary>
/// Handles NPC recruitment. Attach as child Node of Player.
/// Press interact (E) near a Pedestrian to show "Join us? [Y/N]".
/// Roll < 0.4 succeeds: pedestrian enters FOLLOWING state, CultSize++.
/// </summary>
public partial class RecruitSystem : Node
{
    private const float InteractRange = 80f;

    private Label _dialogueLabel;
    private bool _dialogueActive = false;
    private Pedestrian _targetPedestrian = null;
    private Node2D _player = null;

    public override void _Ready()
    {
        var canvas = new CanvasLayer();
        canvas.Layer = 20;
        AddChild(canvas);

        _dialogueLabel = new Label();
        _dialogueLabel.Text = "Join us? [Y/N]";
        _dialogueLabel.Visible = false;
        _dialogueLabel.AnchorLeft = 0.5f;
        _dialogueLabel.AnchorRight = 0.5f;
        _dialogueLabel.AnchorTop = 0.5f;
        _dialogueLabel.AnchorBottom = 0.5f;
        _dialogueLabel.OffsetLeft = -80f;
        _dialogueLabel.OffsetRight = 80f;
        _dialogueLabel.OffsetTop = -16f;
        _dialogueLabel.OffsetBottom = 16f;
        canvas.AddChild(_dialogueLabel);
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
            TryOpenDialogue();
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
            _dialogueLabel.Visible = true;
            _dialogueActive = true;
        }
    }

    private void AttemptRecruit()
    {
        HideDialogue();

        if (_targetPedestrian == null || !IsInstanceValid(_targetPedestrian)) return;

        bool success = GD.Randf() < 0.4f;
        if (success)
        {
            _targetPedestrian.StartFollowing();
            GameManager.Instance.AddFollower();
            GD.Print("[RecruitSystem] Recruited! CultSize=", GameManager.Instance.CultSize);
        }
        else
        {
            GD.Print("[RecruitSystem] Recruitment failed.");
        }

        _targetPedestrian = null;
    }

    private void HideDialogue()
    {
        _dialogueLabel.Visible = false;
        _dialogueActive = false;
    }
}
