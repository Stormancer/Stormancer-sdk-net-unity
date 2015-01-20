using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Stormancer.Unity.Models
{
    public class DtoQuaternion
    {
        public float W { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static implicit operator Quaternion(DtoQuaternion dto)
        {
            return new Quaternion(dto.X, dto.Y, dto.Z, dto.W);
        }

        public static explicit operator DtoQuaternion(Quaternion quaternion)
        {
            return new DtoQuaternion { X = quaternion.x, Y = quaternion.y, Z = quaternion.z, W = quaternion.w };
        }
    }
}
