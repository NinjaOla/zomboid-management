namespace ZombieGateway.Features.Shared;

public sealed record HandlerResult(bool Success, string Message, Exception? Exception = null)
{
    public static HandlerResult Ok(string message) => new(true, message);
    public static HandlerResult Fail(string message) => new(false, message);
    public static HandlerResult Unreachable(Exception ex) =>
        new(false, "⚠️ Could not reach the server. It may be offline or unreachable.", ex);
}
