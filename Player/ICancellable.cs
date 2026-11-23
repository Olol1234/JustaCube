// Anything on the player that plays a cast and can be interrupted (a slam, an aimed shot).
// Whoever interrupts (a knockback, death) cancels every ICancellable without knowing which cast
// is running. Cancel must hand the cube back to the roller and do nothing if nothing is playing.
public interface ICancellable
{
    bool IsPlaying { get; }
    void Cancel();
}
