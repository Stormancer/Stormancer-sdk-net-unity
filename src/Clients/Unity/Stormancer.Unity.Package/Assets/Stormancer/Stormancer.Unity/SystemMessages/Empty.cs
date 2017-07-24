namespace Stormancer.Dto
{
    public class Empty
    {
        private static Empty _instance = new Empty();
        public static Empty Instance
        {
            get
            {
                return _instance;
            }
        }

        /// <summary>
        /// Dummy property to prevent MsgPack from crashing when serializing an empty object.
        /// </summary>
        public bool value { get { return true; } set { } }
    }
}
