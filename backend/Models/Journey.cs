namespace backend.Models;

// A journey is a named track dedicated to practicing one pattern/strategy.
// It is only a UI grouping — ranking is always done over the user's pooled
// Trade ledger (see ScoringService), never a single journey.
public class Journey
{
    public int Id { get; set; }

    // No auth in V1; the personal single user defaults to 1.
    public int UserId { get; set; } = 1;

    public string Name { get; set; } = "Untitled Journey";

    // Free-form pattern label, e.g. "bull-flag", "failed-breakout".
    public string PatternTag { get; set; } = string.Empty;

    // Sandbox journeys are excluded from all ranking/Edge (safe experimentation).
    public bool IsSandbox { get; set; } = false;

    // Archiving hides the folder; its trades stay counted forever (tombstoned).
    public bool Archived { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
