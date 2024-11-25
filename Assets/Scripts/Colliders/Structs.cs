using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Collections;

namespace Project
{
    public interface IColliderDesc {}

    public struct RigidbodyDesc
    {
        public float3 velocity;
        public float mass;
        public float bounciness;
    }

}