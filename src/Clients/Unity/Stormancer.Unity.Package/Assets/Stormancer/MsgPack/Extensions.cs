using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MsgPack
{
    public static class Extensions
    {
        public static string GetAssemblyName(this Assembly assembly)
        {
            return assembly.ToString().Split(',')[0];
        }
    }
}
