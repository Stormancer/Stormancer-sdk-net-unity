using Stormancer.Plugins.Friends.Dto;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Stormancer.Plugins.Friends
{
    public class FriendsService
    {
        private readonly Scene _scene;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, Friend> _friends = new ConcurrentDictionary<string, Friend>();

        public FriendsService(Scene scene)
        {
            _scene = scene;
            _logger = _scene.DependencyResolver.Resolve<ILogger>();
            scene.RegisterRoute<FriendListUpdateDto>("friends.notification", OnFriendNotification);
        }

        public event Action<Friend> FriendAdded;
        public event Action<Friend> FriendUpdated;
        public event Action<Friend> FriendRemoved;

        public IEnumerable<Friend> Friends
        {
            get
            {
                return _friends.Values;
            }
        }

        private void OnFriendNotification(FriendListUpdateDto update)
        {
            switch (update.Operation)
            {
                case "remove":
                    OnFriendRemove(update);
                    break;
                case "update":
                    OnFriendUpdate(update);
                    break;
                case "add":
                    OnFriendAdd(update);
                    break;
                case "update.status":
                    OnFriendUpdateStatus(update);
                    break;
                default:
                    _logger.Log(Diagnostics.LogLevel.Error, "friends", "Unknown friends operation: " + update.Operation);
                    break;
            }
        }


        private void OnFriendAdd(FriendListUpdateDto update)
        {
            _friends[update.ItemId] = update.Data;

            var action = FriendAdded;
            if (action != null)
            {
                MainThread.Post(() =>
                {
                    action(update.Data);
                });
            }
        }

        private void OnFriendUpdate(FriendListUpdateDto update)
        {
            Friend friend;
            if (_friends.TryGetValue(update.ItemId, out friend))
            {
                friend.Status = update.Data.Status;
                friend.Details = update.Data.Details;
                friend.LastConnected = update.Data.LastConnected;
                friend.UserId = update.Data.UserId;

                var action = FriendUpdated;
                if (action != null)
                {
                    MainThread.Post(() =>
                    {
                        action(friend);
                    });
                }
            }
            else
            {
                _logger.Log(Diagnostics.LogLevel.Warn, "friends.update", "Unknown friend with id " + update.ItemId);
            }
        }

        private void OnFriendUpdateStatus(FriendListUpdateDto update)
        {
            Friend friend;
            if (_friends.TryGetValue(update.ItemId, out friend))
            {
                friend.Status = update.Data.Status;
                var action = FriendUpdated;
                if (action != null)
                {
                    MainThread.Post(() =>
                    {
                        action(friend);
                    });
                }
            }
            else
            {
                _logger.Log(Diagnostics.LogLevel.Warn, "friends.updatestatus", "Unknown friend with id " + update.ItemId);
            }
        }

        private void OnFriendRemove(FriendListUpdateDto update)
        {
            Friend friend;
            if (_friends.TryRemove(update.ItemId, out friend))
            {
                var action = FriendRemoved;
                if(action != null)
                {
                    MainThread.Post(() =>
                    {
                        action(update.Data);
                    });
                }
            }
            else
            {
                _logger.Log(Diagnostics.LogLevel.Warn, "friends.remove", "Unknown friend with id " + update.ItemId);

            }
        }

        public Task InviteFriend(string friendId)
        {
            return _scene.RpcVoid("friends.invitefriend", friendId);
        }

        public Task AnswerFriendInvitation(string friendId, bool accept = true)
        {
            var serializer = _scene.Host.Serializer();
            return _scene.RpcVoid("friends.acceptfriendinvitation", s =>
            {
                serializer.Serialize(friendId, s);
                serializer.Serialize(accept, s);
            });
        }

        public Task RemoveFriend(string friendId)
        {
            return _scene.RpcVoid("friends.removefriend", friendId);
        }

        public Task SetStatus(FriendListStatusConfig status, string details)
        {
            var serializer = _scene.Host.Serializer();
            return _scene.RpcVoid("friends.setstatus", s =>
            {
                serializer.Serialize(status, s);
                serializer.Serialize(details, s);
            });
        }
    }
}