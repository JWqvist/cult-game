using Godot;
using System.Collections.Generic;

/// <summary>
/// Autoload singleton. Loads Kenney CC0 sprites at startup.
/// Falls back to a magenta placeholder if a file is missing.
/// Access textures via AssetManager.Instance.Get*Texture() methods.
/// Must be first in autoload order so textures are ready before scene scripts.
/// </summary>
public partial class AssetManager : Node
{
    public static AssetManager Instance { get; private set; }

    private readonly Dictionary<string, Texture2D> _textures = new();

    public override void _Ready()
    {
        Instance = this;
        LoadAll();
        GD.Print("[AssetManager] Kenney sprites loaded.");
    }

    private void LoadAll()
    {
        // Characters — single texture per type; rotation handled by Sprite2D
        var playerTex   = LoadTexture("res://assets/kenney/racing-pack/PNG/Characters/character_brown_blue.png");
        var npcTex      = LoadTexture("res://assets/kenney/racing-pack/PNG/Characters/character_black_green.png");
        var followerTex = LoadTexture("res://assets/kenney/racing-pack/PNG/Characters/character_blonde_red.png");

        foreach (string dir in new[] { "down", "up", "left", "right" })
        {
            _textures["player_"   + dir] = playerTex;
            _textures["npc_"      + dir] = npcTex;
            _textures["follower_" + dir] = followerTex;
        }

        // Cars
        _textures["car_red"]    = LoadTexture("res://assets/kenney/racing-pack/PNG/Cars/car_red_1.png");
        _textures["car_taxi"]   = LoadTexture("res://assets/kenney/racing-pack/PNG/Cars/car_yellow_1.png");
        _textures["car_police"] = LoadTexture("res://assets/kenney/racing-pack/PNG/Cars/car_black_1.png");
        _textures["car_player"] = LoadTexture("res://assets/kenney/racing-pack/PNG/Cars/car_red_1.png");
    }

    // ── Public accessors ─────────────────────────────────────────────────────

    public Texture2D GetPlayerTexture(string dir = "down")
    {
        string key = "player_" + dir;
        return _textures.TryGetValue(key, out var t) ? t : _textures["player_down"];
    }

    public Texture2D GetNPCTexture(string dir = "down", bool recruited = false)
    {
        string prefix = recruited ? "follower" : "npc";
        string key = prefix + "_" + dir;
        return _textures.TryGetValue(key, out var t) ? t : _textures[prefix + "_down"];
    }

    public Texture2D GetCarTexture(string color = "red")
    {
        string key = "car_" + color;
        return _textures.TryGetValue(key, out var t) ? t : _textures["car_red"];
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private Texture2D LoadTexture(string resPath)
    {
        var tex = GD.Load<Texture2D>(resPath);
        if (tex != null) return tex;

        GD.PrintErr($"[AssetManager] Failed to load {resPath}, using fallback.");
        return GenerateFallback();
    }

    private static ImageTexture GenerateFallback()
    {
        var img = Image.Create(32, 32, false, Image.Format.Rgba8);
        img.Fill(new Color(1f, 0f, 1f)); // magenta = obvious missing texture
        return ImageTexture.CreateFromImage(img);
    }
}
