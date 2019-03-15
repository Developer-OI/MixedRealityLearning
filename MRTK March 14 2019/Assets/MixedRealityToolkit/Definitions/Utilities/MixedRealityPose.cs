﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Core.Definitions.Utilities
{
    [Serializable]
    public struct MixedRealityPose : IEqualityComparer
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public MixedRealityPose(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="position"></param>
        public MixedRealityPose(Vector3 position)
        {
            this.position = position;
            rotation = Quaternion.identity;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rotation"></param>
        public MixedRealityPose(Quaternion rotation)
        {
            position = Vector3.zero;
            this.rotation = rotation;
        }

        /// <summary>
        /// The default value for a Six Dof Transform.
        /// </summary>
        /// <returns>
        /// <see href="https://docs.unity3d.com/ScriptReference/Vector3-zero.html">Vector3.zero</see> and <see href="https://docs.unity3d.com/ScriptReference/Quaternion-identity.html">Quaternion.identity</see>.
        /// </returns>
        public static MixedRealityPose ZeroIdentity { get; } = new MixedRealityPose(Vector3.zero, Quaternion.identity);

        [SerializeField]
        private Vector3 position;

        public Vector3 Position { get { return position; } set { position = value; } }

        [SerializeField]
        private Quaternion rotation;

        public Quaternion Rotation { get { return rotation; } set { rotation = value; } }

        public static MixedRealityPose operator +(MixedRealityPose left, MixedRealityPose right)
        {
            return new MixedRealityPose(left.Position + right.Position, left.Rotation * right.Rotation);
        }

        public static bool operator ==(MixedRealityPose left, MixedRealityPose right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MixedRealityPose left, MixedRealityPose right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{position} | {rotation}";
        }

        /// The Z axis of the pose in world space
        public Vector3 Forward => rotation * Vector3.forward + position;

        /// The Y axis of the pose in world space
        public Vector3 Up => rotation * Vector3.up + position;

        /// The X axis of the pose in world space
        public Vector3 Right => rotation * Vector3.right + position;

        #region IEqualityComparer Implementation

        bool IEqualityComparer.Equals(object left, object right)
        {
            if (ReferenceEquals(null, left) || ReferenceEquals(null, right)) { return false; }
            if (!(left is MixedRealityPose) || !(right is MixedRealityPose)) { return false; }
            return ((MixedRealityPose)left).Equals((MixedRealityPose)right);
        }

        public bool Equals(MixedRealityPose other)
        {
            return Position == other.Position &&
                   Rotation == other.Rotation;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) { return false; }
            return obj is MixedRealityPose && Equals((MixedRealityPose)obj);
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            return obj is MixedRealityPose ? ((MixedRealityPose)obj).GetHashCode() : 0;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion IEqualityComparer Implementation
    }
}
