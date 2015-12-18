using UnityEngine;
using System.Collections;
using Stormancer;

namespace Stormancer
{
    public class IClock
    {
        private Client client;

        public long Clock
        {
            get
            {
                return client.Clock;
            }
        }

        public IClock(Client c)
        {
            client = c;
        }
    }
}