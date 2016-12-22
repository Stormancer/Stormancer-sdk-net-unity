namespace Stormancer.Plugins.Matchmaking
{
    public enum MatchState : byte
    {
        Unknown = 255,
        SearchStart = 0,
        CandidateFound = 1,
        WaitingPlayersReady = 2,
        Success = 3, 
        Failed = 4,
        Canceled = 5
    }
}