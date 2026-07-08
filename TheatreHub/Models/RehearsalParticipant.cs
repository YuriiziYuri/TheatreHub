namespace TheatreHub.Models;

public class RehearsalParticipant
{
    public int RehearsalId { get; set; }

    public Rehearsal Rehearsal { get; set; } = null!;

    public int ActorId { get; set; }

    public Actor Actor { get; set; } = null!;
}