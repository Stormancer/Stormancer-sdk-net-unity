using System;

namespace RakNet
{
    static class Const
    {
#if UNITY_IOS && !UNITY_EDITOR
        internal const string LibraryName = "__Internal";
#else
        internal const string LibraryName = "RakNet";
#endif
    }
}

