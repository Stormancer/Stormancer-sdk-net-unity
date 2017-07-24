using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer
{
    public class LeaderboardService
    {
        private readonly Scene _scene;

        public LeaderboardService(Scene scene)
        {
            _scene = scene;
        }

        public Task<LeaderboardResult> Query(LeaderboardQuery query)
        {
            return _scene.RpcTask<LeaderboardQuery, LeaderboardResult>("leaderboard.query", query);
        }

        public Task<LeaderboardResult> Query(string cursor)
        {
            return _scene.RpcTask<string, LeaderboardResult>("leaderboard.cursor", cursor);
        }
    }
}
