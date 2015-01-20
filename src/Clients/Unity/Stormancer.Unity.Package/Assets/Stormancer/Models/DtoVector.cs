using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Stormancer.Unity.Models
{
    public class DtoVector
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static implicit operator Vector3(DtoVector dto)
        {
            return new Vector3(dto.X, dto.Y, dto.Z);
        }

        public static explicit operator DtoVector(Vector3 vector)
        {
            return new DtoVector { X = vector.x, Y = vector.y, Z = vector.z };
        }
    }
}
