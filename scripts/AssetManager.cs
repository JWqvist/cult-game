using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload singleton. Generates pixel art sprites programmatically at startup.
/// Access textures via AssetManager.Instance.Get*Texture() methods.
/// Must be first in autoload order so textures are ready before scene scripts.
/// </summary>
public partial class AssetManager : Node
{
    public static AssetManager Instance { get; private set; }

    private readonly Dictionary<string, ImageTexture> _textures = new();

    // Palette
    private static readonly Color Transparent  = new Color(0f, 0f, 0f, 0f);
    private static readonly Color SkinTone     = new Color(0.90f, 0.72f, 0.52f);
    private static readonly Color PlayerCoat   = new Color(0.15f, 0.20f, 0.55f);
    private static readonly Color PlayerLegs   = new Color(0.10f, 0.10f, 0.35f);
    private static readonly Color NpcShirt     = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color NpcLegs      = new Color(0.30f, 0.30f, 0.45f);
    private static readonly Color FollowerShirt = new Color(0.20f, 0.55f, 0.20f);
    private static readonly Color WheelColor   = new Color(0.12f, 0.12f, 0.12f);
    private static readonly Color WindowColor  = new Color(0.50f, 0.70f, 0.90f, 0.80f);
    private static readonly Color HeadlightCol = new Color(1.00f, 1.00f, 0.70f);
    private static readonly Color TaillightCol = new Color(1.00f, 0.20f, 0.20f);

    public override void _Ready()
    {
        Instance = this;
        GenerateAll();
        GD.Print("[AssetManager] Sprites generated.");
    }

    private void GenerateAll()
    {
        // Characters: 16x24
        _textures["player_down"]  = MakePerson(PlayerCoat, PlayerLegs, SkinTone, Face.Front);
        _textures["player_up"]    = MakePerson(PlayerCoat, PlayerLegs, SkinTone, Face.Back);
        _textures["player_left"]  = MakePerson(PlayerCoat, PlayerLegs, SkinTone, Face.Side);
        _textures["player_right"] = MakePerson(PlayerCoat, PlayerLegs, SkinTone, Face.Side);

        _textures["npc_down"]  = MakePerson(NpcShirt, NpcLegs, SkinTone, Face.Front);
        _textures["npc_up"]    = MakePerson(NpcShirt, NpcLegs, SkinTone, Face.Back);
        _textures["npc_left"]  = MakePerson(NpcShirt, NpcLegs, SkinTone, Face.Side);
        _textures["npc_right"] = MakePerson(NpcShirt, NpcLegs, SkinTone, Face.Side);

        _textures["follower_down"]  = MakePerson(FollowerShirt, NpcLegs, SkinTone, Face.Front);
        _textures["follower_up"]    = MakePerson(FollowerShirt, NpcLegs, SkinTone, Face.Back);
        _textures["follower_left"]  = MakePerson(FollowerShirt, NpcLegs, SkinTone, Face.Side);
        _textures["follower_right"] = MakePerson(FollowerShirt, NpcLegs, SkinTone, Face.Side);

        // Cars: 24x42 (matching collision shape in tscn files)
        _textures["car_red"]    = MakeCar(new Color(0.80f, 0.12f, 0.12f));
        _textures["car_taxi"]   = MakeCar(new Color(0.95f, 0.85f, 0.00f));
        _textures["car_police"] = MakePolice();
        _textures["car_player"] = MakeCar(new Color(0.20f, 0.45f, 0.80f));
    }

    // ── Public accessors ─────────────────────────────────────────────────────

    public ImageTexture GetPlayerTexture(string dir = "down")
    {
        string key = "player_" + dir;
        return _textures.TryGetValue(key, out var t) ? t : _textures["player_down"];
    }

    public ImageTexture GetNPCTexture(string dir = "down", bool recruited = false)
    {
        string prefix = recruited ? "follower" : "npc";
        string key = prefix + "_" + dir;
        return _textures.TryGetValue(key, out var t) ? t : _textures[prefix + "_down"];
    }

    public ImageTexture GetCarTexture(string color = "red")
    {
        string key = "car_" + color;
        return _textures.TryGetValue(key, out var t) ? t : _textures["car_red"];
    }

    // ── Sprite generators ────────────────────────────────────────────────────

    private enum Face { Front, Back, Side }

    private ImageTexture MakePerson(Color body, Color legs, Color skin, Face face)
    {
        var img = Image.Create(16, 24, false, Image.Format.Rgba8);

        if (face == Face.Front)
        {
            // Head
            Fill(img, 4, 1, 8, 7, skin);
            // Eyes
            img.SetPixel(6, 4, new Color(0.10f, 0.10f, 0.10f));
            img.SetPixel(9, 4, new Color(0.10f, 0.10f, 0.10f));
            // Body + arms
            Fill(img, 3, 8, 10, 10, body);
            Fill(img, 1, 8,  2, 9,  body);
            Fill(img, 13, 8, 2, 9,  body);
            // Legs
            Fill(img, 3, 18, 5, 6, legs);
            Fill(img, 9, 18, 5, 6, legs);
        }
        else if (face == Face.Back)
        {
            Fill(img, 4, 1, 8, 7, skin.Darkened(0.10f));
            Fill(img, 3, 8, 10, 10, body.Darkened(0.10f));
            Fill(img, 1, 8,  2,  9, body.Darkened(0.10f));
            Fill(img, 13, 8, 2,  9, body.Darkened(0.10f));
            Fill(img, 3, 18, 5,  6, legs.Darkened(0.10f));
            Fill(img, 9, 18, 5,  6, legs.Darkened(0.10f));
        }
        else // Side
        {
            Fill(img, 4, 1, 7, 7, skin);
            Fill(img, 3, 8, 8, 10, body);
            Fill(img, 2, 8, 1,  9, body.Darkened(0.15f));
            Fill(img, 3, 18, 5, 6, legs);
            Fill(img, 9, 18, 4, 6, legs.Darkened(0.10f));
        }

        return ImageTexture.CreateFromImage(img);
    }

    private ImageTexture MakeCar(Color body)
    {
        var img = Image.Create(24, 42, false, Image.Format.Rgba8);

        // Main body
        Fill(img, 2, 3, 20, 36, body);
        // Windshield area (roof)
        Fill(img, 3, 10, 18, 18, body.Darkened(0.18f));
        // Windows
        Fill(img, 4, 12, 16, 7, WindowColor);
        Fill(img, 4, 23, 16, 7, WindowColor);
        // Wheels (4 corners)
        Fill(img, 0,  4, 4, 7, WheelColor);
        Fill(img, 20, 4, 4, 7, WheelColor);
        Fill(img, 0, 31, 4, 7, WheelColor);
        Fill(img, 20,31, 4, 7, WheelColor);
        // Headlights (top)
        Fill(img, 3,  1, 5, 3, HeadlightCol);
        Fill(img, 16, 1, 5, 3, HeadlightCol);
        // Taillights (bottom)
        Fill(img, 3,  38, 5, 3, TaillightCol);
        Fill(img, 16, 38, 5, 3, TaillightCol);

        return ImageTexture.CreateFromImage(img);
    }

    private ImageTexture MakePolice()
    {
        var img = Image.Create(24, 42, false, Image.Format.Rgba8);

        // Black body
        Fill(img, 2, 3, 20, 36, new Color(0.08f, 0.08f, 0.08f));
        // White door stripe
        Fill(img, 2, 16, 20, 11, new Color(0.92f, 0.92f, 0.92f));
        // Windows
        Fill(img, 4, 12, 16, 7, WindowColor);
        Fill(img, 4, 23, 16, 7, WindowColor);
        // Wheels
        Fill(img, 0,  4, 4, 7, WheelColor);
        Fill(img, 20, 4, 4, 7, WheelColor);
        Fill(img, 0, 31, 4, 7, WheelColor);
        Fill(img, 20,31, 4, 7, WheelColor);
        // Light bar: red + blue
        Fill(img, 7,  0, 5, 4, new Color(1.00f, 0.10f, 0.10f));
        Fill(img, 13, 0, 4, 4, new Color(0.10f, 0.30f, 1.00f));
        // Headlights
        Fill(img, 3,  1, 4, 3, HeadlightCol);
        Fill(img, 17, 1, 4, 3, HeadlightCol);

        return ImageTexture.CreateFromImage(img);
    }

    // ── Pixel helpers ────────────────────────────────────────────────────────

    private static void Fill(Image img, int x, int y, int w, int h, Color c)
    {
        int iw = img.GetWidth();
        int ih = img.GetHeight();
        for (int py = y; py < y + h; py++)
            for (int px = x; px < x + w; px++)
                if (px >= 0 && px < iw && py >= 0 && py < ih)
                    img.SetPixel(px, py, c);
    }
}
