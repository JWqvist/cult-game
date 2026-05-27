using Godot;

/// <summary>
/// Node2D child of World. Draws a procedural city grid via _Draw().
/// Roads at every 300px, 50px wide. Building blocks fill the spaces.
/// Rendered at z_index=-10 so it sits behind all game objects.
/// </summary>
public partial class WorldBackground : Node2D
{
    private const int GridSize     = 300;
    private const int RoadHalf     = 25;  // road is 50px wide
    private const int SidewalkBump = 8;   // sidewalk extends 8px beyond road edge
    private const int WorldExtent  = 2100;

    private static readonly Color Grass      = new Color(0.13f, 0.22f, 0.10f);
    private static readonly Color Road       = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color Sidewalk   = new Color(0.34f, 0.34f, 0.34f);
    private static readonly Color Marking    = new Color(1.00f, 1.00f, 1.00f, 0.35f);
    private static readonly Color Intersection = new Color(0.20f, 0.20f, 0.20f);

    private static readonly Color[] Buildings = {
        new Color(0.58f, 0.50f, 0.42f),
        new Color(0.48f, 0.48f, 0.60f),
        new Color(0.52f, 0.42f, 0.42f),
        new Color(0.38f, 0.54f, 0.50f),
        new Color(0.55f, 0.52f, 0.38f),
        new Color(0.42f, 0.40f, 0.58f),
    };

    public override void _Ready()
    {
        ZIndex = -10;

        // Place Kenney road intersection sprites at each grid intersection
        var roadTex = GD.Load<Texture2D>("res://assets/kenney/road-textures/Legacy/PNG/roadTile7.png");
        if (roadTex != null)
        {
            for (int xi = -7; xi <= 7; xi++)
            {
                for (int yi = -7; yi <= 7; yi++)
                {
                    var sp = new Sprite2D();
                    sp.Texture = roadTex;
                    sp.Position = new Vector2(xi * GridSize, yi * GridSize);
                    sp.Scale = new Vector2(3f, 3f);
                    sp.ZIndex = -9; // just above the _Draw() layer
                    AddChild(sp);
                }
            }
        }
        else
        {
            GD.PrintErr("[WorldBackground] Could not load roadTile7.png");
        }
    }

    public override void _Draw()
    {
        int ext = WorldExtent;
        int sw = RoadHalf + SidewalkBump;  // sidewalk half-width

        // Base grass
        DrawRect(new Rect2(-ext, -ext, ext * 2, ext * 2), Grass);

        // Building blocks (drawn first, below sidewalks/roads)
        for (int bxi = -ext / GridSize; bxi < ext / GridSize; bxi++)
        {
            for (int byi = -ext / GridSize; byi < ext / GridSize; byi++)
            {
                float bx = bxi * GridSize + sw;
                float by = byi * GridSize + sw;
                float bw = GridSize - sw * 2;
                float bh = GridSize - sw * 2;
                if (bw <= 0 || bh <= 0) continue;

                int ci = (Mathf.Abs(bxi * 3 + byi) + Mathf.Abs(bxi - byi * 2)) % Buildings.Length;
                Color col = Buildings[ci];

                DrawRect(new Rect2(bx, by, bw, bh), col);
                // Top/left edge highlight
                DrawRect(new Rect2(bx, by, bw, 4), col.Lightened(0.15f));
                DrawRect(new Rect2(bx, by, 4, bh), col.Lightened(0.15f));
                // Bottom/right shadow
                DrawRect(new Rect2(bx, by + bh - 3, bw, 3), col.Darkened(0.20f));
                DrawRect(new Rect2(bx + bw - 3, by, 3, bh), col.Darkened(0.20f));
            }
        }

        // Sidewalks (vertical strips)
        for (int xi = -ext / GridSize; xi <= ext / GridSize; xi++)
        {
            float cx = xi * GridSize;
            DrawRect(new Rect2(cx - sw, -ext, sw * 2, ext * 2), Sidewalk);
        }

        // Sidewalks (horizontal strips)
        for (int yi = -ext / GridSize; yi <= ext / GridSize; yi++)
        {
            float cy = yi * GridSize;
            DrawRect(new Rect2(-ext, cy - sw, ext * 2, sw * 2), Sidewalk);
        }

        // Roads (vertical)
        for (int xi = -ext / GridSize; xi <= ext / GridSize; xi++)
        {
            float cx = xi * GridSize;
            DrawRect(new Rect2(cx - RoadHalf, -ext, RoadHalf * 2, ext * 2), Road);
            // Center dashes
            for (int dy = -ext; dy < ext; dy += 40)
                DrawRect(new Rect2(cx - 1, dy, 2, 22), Marking);
        }

        // Roads (horizontal)
        for (int yi = -ext / GridSize; yi <= ext / GridSize; yi++)
        {
            float cy = yi * GridSize;
            DrawRect(new Rect2(-ext, cy - RoadHalf, ext * 2, RoadHalf * 2), Road);
            // Center dashes
            for (int dx = -ext; dx < ext; dx += 40)
                DrawRect(new Rect2(dx, cy - 1, 22, 2), Marking);
        }

        // Intersections (smooth corners)
        for (int xi = -ext / GridSize; xi <= ext / GridSize; xi++)
        {
            for (int yi = -ext / GridSize; yi <= ext / GridSize; yi++)
            {
                float cx = xi * GridSize;
                float cy = yi * GridSize;
                DrawRect(new Rect2(cx - RoadHalf, cy - RoadHalf, RoadHalf * 2, RoadHalf * 2), Intersection);
            }
        }
    }
}
