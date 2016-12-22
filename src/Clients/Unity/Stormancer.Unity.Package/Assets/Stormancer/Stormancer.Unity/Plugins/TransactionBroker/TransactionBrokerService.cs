using System;
using System.Threading.Tasks;
using Stormancer.Core;
using Stormancer.Plugins;
using Stormancer.Plugins.TransactionBroker;
using System.Collections.Generic;

namespace Stormancer
{
    public class TransactionBrokerService
    {
        private readonly Scene _scene;
        private readonly ILogger _logger;

        public Action<string> OnDesynch { get; set; }
        public Func<UpdateDto, Task<int>> OnUpdateGame { get; set; }

        public TransactionBrokerService(Scene scene)
        {
            this._scene = scene;
            this._logger = scene.DependencyResolver.Resolve<ILogger>();

            _scene.AddRoute<string>("tbt.desync", OnDesynchCallBack);
            _scene.AddProcedure("transaction.execute", OnExecuteTransaction, true);
        }

        private void OnDesynchCallBack(string input)
        {
            var action = OnDesynch;
            if (action != null)
            {
                action(input);
            }
        }

        private Task OnExecuteTransaction(RequestContext<IScenePeer> ctx)
        {
            var input = ctx.ReadObject<UpdateDto>();

            Task<UpdateResponseDto> parameterTask;
            var action = OnUpdateGame;
            if (action != null)
            {
                parameterTask = TaskExtensions.InvokeWrapping(action, input).ContinueWith(task =>
                {
                    if (!task.IsFaulted)
                    {
                        return new UpdateResponseDto { Success = true, Hash = task.Result };
                    }
                    else
                    {
                        _logger.Error(task.Exception);
                        return new UpdateResponseDto { Success = false, Reason = task.Exception.InnerExceptions[0].Message };
                    }
                });
            }
            else
            {
                parameterTask = TaskHelper.FromResult(new UpdateResponseDto { Success = false, Reason = "No callback has been registered for updating game." });
                _logger.Log(Diagnostics.LogLevel.Error, "TransactionBroker", "No callback has been registered for updating game.");

            }

                return parameterTask.Then(parameter => ctx.SendValue(parameter, PacketPriority.MEDIUM_PRIORITY));
            }

        public Task SubmitTransaction(string playerId, string command, string args)
        {
            var dto = new TransactionCommandDto { PlayerId = playerId, Command = command, Args = args };
            return _scene.RpcVoid("transaction.submit", dto);
        }

        public Task MapPlayer(string playerId)
        {
            return _scene.RpcVoid("transaction.addplayer", playerId);
        }
    }
}