namespace Stormancer.Plugins.GameSession
{
    public class SessionPlayer
    {
        public PlayerStatus Status { get; private set; }
        public string UserId { get; private set; }

        public SessionPlayer(string userId, PlayerStatus status)
        {
            this.UserId = userId;
            this.Status = status;
        }
    }
}