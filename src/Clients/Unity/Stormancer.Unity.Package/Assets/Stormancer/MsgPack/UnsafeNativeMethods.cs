#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2012 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace MsgPack
{


	internal static class UnsafeNativeMethods
	{
        //private static int _libCAvailability = 0;
		private const int _libCAvailability_Unknown = 0;
		private const int _libCAvailability_MSVCRT = 1;
		private const int _libCAvailability_LibC = 2;
		private const int _libCAvailability_None = -1;
	

		public static bool TryMemCmp( byte[] s1, byte[] s2, /*SIZE_T*/UIntPtr size, out int result )
		{


			result = 0;
			return false;
		}
	}

}