public enum FollowerRole { None, Recruiter, Enforcer, Financier }

/// <summary>
/// Plain C# class representing a recruited cult follower.
/// Tracked in GameManager.Followers.
/// </summary>
public class Follower
{
    private static int _nextId = 1;

    public int Id { get; }
    public string Name { get; }
    public float Loyalty { get; set; } = 70f;
    public FollowerRole Role { get; set; } = FollowerRole.None;

    public Follower(string name = null)
    {
        Id = _nextId++;
        Name = name ?? $"Follower #{Id}";
    }
}
