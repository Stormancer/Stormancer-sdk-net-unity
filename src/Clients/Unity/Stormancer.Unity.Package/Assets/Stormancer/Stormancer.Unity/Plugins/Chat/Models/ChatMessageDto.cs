namespace Stormancer.Plugins.Chat.Models
{
    public struct ChatMessageDto
    {
        public ChatUserInfo UserInfo { get; set; }
        public string Message { get; set; }
        public long Timestamp { get; set; }
    }
}
