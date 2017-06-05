using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.p2p
{
    interface IP2pMethod
    {
        Task<ICandidate> GetCandidates();
    }

    

    interface ICandidate
    {

    }
    
}
