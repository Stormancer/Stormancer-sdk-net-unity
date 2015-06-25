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
using System.Diagnostics.Contracts;

namespace MsgPack.Serialization.DefaultSerializers
{
    internal sealed class System_VersionMessagePackSerializer
#if UNITY_IOS
 : MessagePackSerializer
#else        
        : MessagePackSerializer<Version>
#endif
    {
        public System_VersionMessagePackSerializer(PackerCompatibilityOptions packerCompatibilityOptions)
#if UNITY_IOS
            : base(typeof(Version), packerCompatibilityOptions) { }
#else
			: base( packerCompatibilityOptions ) { }

#endif

        
#if UNITY_IOS
        protected internal sealed override void PackToCore(Packer packer, object obj)
        {
            var objectTree = (Version)obj; 
#else
        protected internal sealed override void PackToCore(Packer packer, Version objectTree)            
        {
#endif
            packer.PackArrayHeader(4);
            packer.Pack(objectTree.Major);
            packer.Pack(objectTree.Minor);
            packer.Pack(objectTree.Build);
            packer.Pack(objectTree.Revision);
        }

#if UNITY_IOS
        protected internal sealed override object UnpackFromCore(Unpacker unpacker)
#else
		protected internal sealed override Version UnpackFromCore( Unpacker unpacker )
#endif
        {
            long length = unpacker.LastReadData.AsInt64();
            int[] array = new int[4];
            for (int i = 0; i < length && i < 4; i++)
            {
                if (!unpacker.Read())
                {
                    throw SerializationExceptions.NewMissingItem(i);
                }

                array[i] = unpacker.LastReadData.AsInt32();
            }

            return new Version(array[0], array[1], array[2], array[3]);
        }
    }
}
