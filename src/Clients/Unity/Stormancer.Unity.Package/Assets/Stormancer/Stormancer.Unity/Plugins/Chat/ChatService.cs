using System;
using Stormancer.Plugins.Chat.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Stormancer.Plugins.Chat
{
    public class ChatService
    {
        private readonly Scene _scene;
        private readonly ConcurrentDictionary<long, ChatUserInfo> _users = new ConcurrentDictionary<long, ChatUserInfo>();
        private readonly ConcurrentQueue<ChatMessageDto> _messages = new ConcurrentQueue<ChatMessageDto>();
        private readonly ILogger _logger;
        public IEnumerable<ChatMessageDto> Messages
        {
            get
            {
                return _messages;
            }
        }

        public ChatService(Scene scene)
        {
            _scene = scene;

            _logger = scene.DependencyResolver.Resolve<ILogger>();

            scene.AddRoute<ChatMessageDto>("chat.message", OnMessage);
            scene.AddRoute<ChatUserInfo>("chat.updateinfo", OnUserUpdate);
            scene.AddRoute<ChatMessageDto>("chat.disconnected", OnUserDisconnected);

            _logger.Log(Diagnostics.LogLevel.Trace, "chat", "Routes on the chat scene: \n" 
                + string.Join("\n", scene.RemoteRoutes.Select(r => r.Name).ToArray()));
        }

        public event Action<ChatMessageDto> OnChatMessage;

        public void Send(string message)
        {
            _scene.Send("chat.message", message);
        }

        public void SetUser(string user)
        {
            _scene.Send("chat.updateinfo", user);
        }

        public Task UpdateUsers()
        {
            return _scene.RpcTask<IEnumerable<ChatUserInfo>>("chat.userinfos")
                .Then(users =>
                {
                    _users.Clear();
                    foreach(var user in users)
                    {
                        _users[user.ClientId] = user;
                    }
                });
        }

        private void OnUserUpdate(ChatUserInfo info)
        {
            _users.AddOrUpdate(info.ClientId, info, (_, old) =>
            {
                old.User = info.User;
                return old;
            });       
        }

        private void OnMessage(ChatMessageDto message)
        {
            OnUserUpdate(message.UserInfo);

            _messages.Enqueue(message);
            var action = OnChatMessage;
            if(action != null)
            {
                MainThread.Post(() => action(message));
            }
        }

        private void OnUserDisconnected(ChatMessageDto message)
        {
            ChatUserInfo _;
            _users.TryRemove(message.UserInfo.ClientId, out _);
        }

    }
}