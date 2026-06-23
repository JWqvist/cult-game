using Godot;

/// <summary>
/// Small HUD minimap rendered in the bottom-right corner.
/// Reads the static WorldGenerator.Map array to draw the tile layout,
/// then overlays a green dot for the player position.
/// The map image is pre-rendered to an ImageTexture once; only the player
/// dot is redrawn each frame via QueueRedraw().
/// </summary>
public partial class Minimap : Control
{
    // World extents that the minimap represents.
    private const float WorldWidth  = WorldGenerator.MapWidth  * WorldGenerator.TileSize;
    private const float WorldHeight = WorldGenerator.MapHeight * WorldGenerator.TileSize;

    // Minimap panel dimensions (pixels on screen).
    private const int DrawW = 160;
    private const int DrawH = 128;
    private const int Pad   = 2;   // padding inside the background rect

    private static readonly Color ColBg       = new Color(0f,    0f,    0f,    0.75f);
    private static readonly Color ColGrass    = new Color(0.25f, 0.55f, 0.15f, 1f);
    private static readonly Color ColRoad     = new Color(0.28f, 0.28f, 0.28f, 1f);
    private static readonly Color ColSidewalk = new Color(0.72f, 0.72f, 0.68f, 1f);
    private static readonly Color ColBuilding = new Color(0.42f, 0.32f, 0.22f, 1f);
    private static readonly Color ColPlayer   = new Color(0.1f,  1f,    0.3f,  1f);

    private Player _player;
    private ImageTexture _mapTex;

    public override void _Ready()
    {
        // Anchor to bottom-right of the viewport (1280×720 fixed canvas).
        Position = new Vector2(1280 - DrawW - Pad - 8, 720 - DrawH - Pad - 8);
        CustomMinimumSize = new Vector2(DrawW + Pad * 2, DrawH + Pad * 2);
    }

    public override void _Process(double delta)
    {
        // Lazily locate player.
        if (_player == null)
        {
            Node n = GetTree().GetFirstNodeInGroup("player");
            if (n is Player p) _player = p;
        }

        // Build map texture once WorldGenerator has finished.
        if (_mapTex == null && WorldGenerator.Map != null)
            _mapTex = BuildMapTexture();

        QueueRedraw();
    }

    public override void _Draw()
    {
        // Dark background panel.
        DrawRect(new Rect2(0, 0, DrawW + Pad * 2, DrawH + Pad * 2), ColBg);

        // Tile map image.
        if (_mapTex != null)
            DrawTextureRect(_mapTex, new Rect2(Pad, Pad, DrawW, DrawH), false);

        // Player dot.
        if (_player != null)
        {
            float px = Pad + (_player.GlobalPosition.X / WorldWidth)  * DrawW;
            float py = Pad + (_player.GlobalPosition.Y / WorldHeight) * DrawH;
            DrawCircle(
                new Vector2(Mathf.Clamp(px, Pad, Pad + DrawW), Mathf.Clamp(py, Pad, Pad + DrawH)),
                3f, ColPlayer);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static ImageTexture BuildMapTexture()
    {
        var map = WorldGenerator.Map;
        var img = Image.Create(WorldGenerator.MapWidth, WorldGenerator.MapHeight,
                               false, Image.Format.Rgb8);

        for (int x = 0; x < WorldGenerator.MapWidth; x++)
        {
            for (int y = 0; y < WorldGenerator.MapHeight; y++)
            {
                Color c = map[x, y] switch
                {
                    WorldGenerator.TileRoad     => ColRoad,
                    WorldGenerator.TileBuilding => ColBuilding,
                    WorldGenerator.TileSidewalk => ColSidewalk,
                    _                           => ColGrass,
                };
                img.SetPixel(x, y, c);
            }
        }

        return ImageTexture.CreateFromImage(img);
    }
}
