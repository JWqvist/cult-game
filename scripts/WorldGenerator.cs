using Godot;
using System.Collections.Generic;

/// <summary>
/// Programmatically builds the TileMap with roads, sidewalks, open areas, and buildings.
/// Buildings get physics collision shapes on layer 0 of the TileSet.
/// The static Map array is read by Minimap.cs for the minimap overlay.
/// </summary>
public partial class WorldGenerator : Node
{
    public const int TileSize   = 32;
    public const int MapWidth   = 50;
    public const int MapHeight  = 40;

    public const int TileGrass    = 0;
    public const int TileRoad     = 1;
    public const int TileSidewalk = 2;
    public const int TileBuilding = 3;

    // Shared read-only map grid populated during _Ready.
    public static int[,] Map { get; private set; }

    private TileMap _tileMap;

    public override void _Ready()
    {
        _tileMap = GetNode<TileMap>("../TileMap");
        Map = new int[MapWidth, MapHeight];
        BuildTileSet();
        GenerateMap();
        PlaceTiles();
        GD.Print("[WorldGenerator] Map generated (", MapWidth, "x", MapHeight, " tiles).");
    }

    // -------------------------------------------------------------------------
    // TileSet construction
    // -------------------------------------------------------------------------

    private void BuildTileSet()
    {
        var tileSet = new TileSet();
        tileSet.TileSize = new Vector2I(TileSize, TileSize);

        // Physics layer 0 — wall/building collisions
        tileSet.AddPhysicsLayer();
        tileSet.SetPhysicsLayerCollisionLayer(0, 1); // bit 0 = layer 1
        tileSet.SetPhysicsLayerCollisionMask(0, 1);

        // Atlas source: white 128x32 image, four 32x32 tiles side-by-side.
        var source = new TileSetAtlasSource();
        source.Texture = CreateWhiteTexture(TileSize * 4, TileSize);
        source.TextureRegionSize = new Vector2I(TileSize, TileSize);

        // Grass tile
        source.CreateTile(new Vector2I(TileGrass, 0));
        source.GetTileData(new Vector2I(TileGrass, 0), 0).Modulate =
            new Color(0.25f, 0.55f, 0.15f);

        // Road tile
        source.CreateTile(new Vector2I(TileRoad, 0));
        source.GetTileData(new Vector2I(TileRoad, 0), 0).Modulate =
            new Color(0.28f, 0.28f, 0.28f);

        // Sidewalk tile
        source.CreateTile(new Vector2I(TileSidewalk, 0));
        source.GetTileData(new Vector2I(TileSidewalk, 0), 0).Modulate =
            new Color(0.72f, 0.72f, 0.68f);

        // Building tile — full-tile collision polygon
        source.CreateTile(new Vector2I(TileBuilding, 0));
        var bd = source.GetTileData(new Vector2I(TileBuilding, 0), 0);
        bd.Modulate = new Color(0.42f, 0.32f, 0.22f);
        bd.AddCollisionPolygon(0);
        float h = TileSize / 2f;
        bd.SetCollisionPolygonPoints(0, 0, new Vector2[]
        {
            new Vector2(-h, -h),
            new Vector2( h, -h),
            new Vector2( h,  h),
            new Vector2(-h,  h),
        });

        tileSet.AddSource(source, 0);
        _tileMap.TileSet = tileSet;
    }

    // Creates a fully-opaque white ImageTexture of given dimensions.
    private static ImageTexture CreateWhiteTexture(int width, int height)
    {
        var image = Image.Create(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        return ImageTexture.CreateFromImage(image);
    }

    // -------------------------------------------------------------------------
    // Map layout
    // -------------------------------------------------------------------------

    private void GenerateMap()
    {
        // Road grid — two-tile-wide corridors at fixed intervals.
        var roadRows = new HashSet<int> { 0, 1, 13, 14, 26, 27, 38, 39 };
        var roadCols = new HashSet<int> { 0, 1, 14, 15, 28, 29, 42, 43 };

        for (int x = 0; x < MapWidth; x++)
            for (int y = 0; y < MapHeight; y++)
                Map[x, y] = (roadRows.Contains(y) || roadCols.Contains(x))
                    ? TileRoad : TileGrass;

        // City blocks occupying the space between road corridors.
        // X extents: [2-13], [16-27], [30-41]
        // Y extents: [2-12], [15-25], [28-37]
        int[][] xBlocks = { new[] { 2, 13 }, new[] { 16, 27 }, new[] { 30, 41 } };
        int[][] yBlocks = { new[] { 2, 12 }, new[] { 15, 25 }, new[] { 28, 37 } };

        foreach (var xb in xBlocks)
            foreach (var yb in yBlocks)
                PlaceBlock(xb[0], yb[0], xb[1], yb[1]);
    }

    /// <summary>
    /// Fills a rectangular block with:
    ///   border row/col  → sidewalk
    ///   one tile inside → open grass (courtyard)
    ///   deeper inside   → building (with collision)
    /// </summary>
    private void PlaceBlock(int x0, int y0, int x1, int y1)
    {
        for (int x = x0; x <= x1; x++)
        {
            for (int y = y0; y <= y1; y++)
            {
                bool border   = (x == x0 || x == x1 || y == y0 || y == y1);
                bool building = (x > x0 + 1 && x < x1 - 1 && y > y0 + 1 && y < y1 - 1);

                if (building)
                    Map[x, y] = TileBuilding;
                else if (border)
                    Map[x, y] = TileSidewalk;
                else
                    Map[x, y] = TileGrass; // inner courtyard
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tile placement
    // -------------------------------------------------------------------------

    private void PlaceTiles()
    {
        for (int x = 0; x < MapWidth; x++)
            for (int y = 0; y < MapHeight; y++)
                _tileMap.SetCell(0, new Vector2I(x, y), 0, new Vector2I(Map[x, y], 0));
    }
}
