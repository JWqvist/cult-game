using Godot;

/// <summary>
/// Autoload CanvasLayer. Shows slide-in toast notifications.
/// Usage: ToastManager.Show("Message", color)
/// Colors: green = good news, red = heat/danger, gold = money.
/// Stacks up to 3 toasts; older ones are pushed down.
/// </summary>
public partial class ToastManager : CanvasLayer
{
    public static ToastManager Instance { get; private set; }

    private const int MaxToasts = 3;
    private const float ShowDuration = 2.0f;
    private const float FadeDuration = 0.5f;

    private VBoxContainer _container;

    public static readonly Color ColorGood   = new Color(0.30f, 0.90f, 0.30f);
    public static readonly Color ColorDanger = new Color(0.95f, 0.30f, 0.10f);
    public static readonly Color ColorMoney  = new Color(1.00f, 0.84f, 0.00f);

    public override void _Ready()
    {
        Instance = this;
        Layer = 100;
        ProcessMode = Node.ProcessModeEnum.Always;

        _container = new VBoxContainer();
        _container.AnchorLeft   = 0.5f;
        _container.AnchorRight  = 0.5f;
        _container.AnchorTop    = 0f;
        _container.AnchorBottom = 0f;
        _container.OffsetLeft   = -160f;
        _container.OffsetRight  =  160f;
        _container.OffsetTop    =  16f;
        _container.OffsetBottom =  220f;
        _container.AddThemeConstantOverride("separation", 4);
        AddChild(_container);
    }

    public static void Show(string message, Color color)
    {
        Instance?.ShowToast(message, color);
    }

    public void ShowToast(string message, Color color)
    {
        if (_container.GetChildCount() >= MaxToasts)
            return;

        var panel = new PanelContainer();
        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0f, 0f, 0f, 0.78f);
        bg.BorderColor = color;
        bg.BorderWidthLeft = bg.BorderWidthRight = bg.BorderWidthTop = bg.BorderWidthBottom = 2;
        bg.CornerRadiusTopLeft = bg.CornerRadiusTopRight = bg.CornerRadiusBottomLeft = bg.CornerRadiusBottomRight = 4;
        bg.ContentMarginLeft = bg.ContentMarginRight = 14f;
        bg.ContentMarginTop = bg.ContentMarginBottom = 6f;
        panel.AddThemeStyleboxOverride("panel", bg);

        var label = new Label();
        label.Text = message;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeColorOverride("font_color", color);
        panel.AddChild(label);

        _container.AddChild(panel);

        // Fade in → wait → fade out → free
        panel.Modulate = new Color(1f, 1f, 1f, 0f);
        var tween = CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Idle);
        tween.TweenProperty(panel, "modulate:a", 1.0f, 0.25f);
        tween.TweenInterval(ShowDuration);
        tween.TweenProperty(panel, "modulate:a", 0.0f, FadeDuration);
        tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(panel)) panel.QueueFree(); }));
    }
}
