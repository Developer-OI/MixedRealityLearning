﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Core.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Core.Interfaces.InputSystem;
using Microsoft.MixedReality.Toolkit.Core.Services;
using Microsoft.MixedReality.Toolkit.Core.Utilities.Physics;
using Microsoft.MixedReality.Toolkit.Services.InputSystem;
using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SDK.UX.Pointers
{
    public class PokePointer : BaseControllerPointer, IMixedRealityNearPointer
    {
        [SerializeField]
        protected float distBack;

        [SerializeField]
        protected float distFront;

        [SerializeField]
        protected float debounceThreshold;

        [SerializeField]
        protected LineRenderer line;

        [SerializeField]
        protected GameObject visuals;

        private Vector3 closestNormal = Vector3.forward;
        private float closestDistance = 0.0f;

        private NearInteractionTouchable currentTouchableDown = null;
        private NearInteractionTouchable closestProximityTouchable = null;
        
        protected void OnValidate()
        {
            Debug.Assert(distBack > 0, this);
            Debug.Assert(distFront > 0, this);
            Debug.Assert(debounceThreshold > 0, this);
            Debug.Assert(line != null, this);
            Debug.Assert(visuals != null, this);
        }

        public bool IsNearObject
        {
            get { return (closestProximityTouchable != null); }
        }

        public override void OnPreRaycast()
        {
            if (Rays == null)
            {
                Rays = new RayStep[1];
            }

            // Get pointer position
            Vector3 pointerPosition;
            TryGetPointerPosition(out pointerPosition);

            // Check proximity
            closestProximityTouchable = null;
            {
                float closestDist = distFront; // NOTE: Start at distFront for cutoff
                foreach (var prox in NearInteractionTouchable.Instances)
                {
                    float dist = prox.DistanceToSurface(pointerPosition);
                    if (dist < closestDist)
                    {
                        closestNormal = prox.Forward;
                        closestDist = dist;
                        closestProximityTouchable = prox;
                    }
                }
                closestDistance = closestDist;
            }
            SetActive(IsNearObject);
            visuals.SetActive(IsNearObject);

            // Determine ray direction
            Vector3 rayDirection = PointerDirection;
            if (closestProximityTouchable != null)
            {
                rayDirection = -closestProximityTouchable.Forward;
            }

            // Build ray (poke from in front to the back of the pointer position)
            Vector3 start = pointerPosition - distBack * rayDirection;
            Vector3 end = pointerPosition + distFront * rayDirection;
            Rays[0].UpdateRayStep(ref start, ref end);

            // Check if we are about to leave the currently focused object.
            if (currentTouchableDown != null)
            {
                RaycastHit hitInfo = default(RaycastHit);
                PerformRayCastInternal(Rays[0], out hitInfo);

                if (hitInfo.transform?.gameObject != currentTouchableDown.gameObject)
                {
                    Debug.Assert(Result?.CurrentPointerTarget == currentTouchableDown.gameObject);

                    // We need to raise the event now, since the pointer's focused object will change after we leave this function.
                    // This will make sure the correct object receives the Leave event.
                    TryRaisePokeUp(pointerPosition);
                }
            }

            line.SetPosition(0, pointerPosition);
            line.SetPosition(1, end);
        }

        public override void OnPostRaycast()
        {
            if (Result?.CurrentPointerTarget != null)
            {
                Vector3 touchPosition;
                if (!TryGetPointerPosition(out touchPosition))
                {
                    touchPosition = Result.Details.Point;
                }

                float dist = Vector3.Distance(Result.StartPoint, Result.Details.Point) - distBack;
                bool newIsDown = (dist < debounceThreshold);

                // Send events
                if (newIsDown)
                {
                    TryRaisePokeDown(Result.CurrentPointerTarget, touchPosition);
                }
                else
                {
                    TryRaisePokeUp(touchPosition);
                }
            }

            if (!IsNearObject)
            {
                line.endColor = line.startColor = new Color(1, 1, 1, 0.25f);
            }
            else if (currentTouchableDown == null)
            {
                line.endColor = line.startColor = new Color(1, 1, 1, 0.75f);
            }
            else
            {
                line.endColor = line.startColor = new Color(0, 0, 1, 0.75f);
            }
        }

        private void PerformRayCastInternal(RayStep ray, out RaycastHit hitInfo)
        {
            LayerMask[] prioritizedLayerMasks = null;
            var focusProvider = MixedRealityToolkit.InputSystem?.FocusProvider;
            if (focusProvider != null)
            {
                prioritizedLayerMasks = focusProvider.FocusLayerMasks;
            }
            else
            {
                prioritizedLayerMasks = new LayerMask[] { Physics.DefaultRaycastLayers };
            }

            MixedRealityRaycaster.RaycastSimplePhysicsStep(ray, prioritizedLayerMasks, out hitInfo);
        }

        private void TryRaisePokeDown(GameObject targetObject, Vector3 touchPosition)
        {
            if (currentTouchableDown == null)
            {
                // In order to get reliable up/down event behavior, only allow the closest touchable to be touched.
                if (targetObject == closestProximityTouchable?.gameObject)
                {
                    currentTouchableDown = closestProximityTouchable;

                    if (currentTouchableDown.EventsToReceive == TouchableEventType.Pointer)
                    {
                        MixedRealityToolkit.InputSystem?.RaisePointerDown(this, pointerAction, Handedness);
                    }
                    else if (currentTouchableDown.EventsToReceive == TouchableEventType.Touch)
                    {
                        MixedRealityToolkit.InputSystem?.RaiseOnTouchStarted(InputSourceParent, Controller, Handedness, touchPosition);
                    }
                }
            }
            else
            {
                RaiseTouchUpdated(targetObject, touchPosition);
            }
        }

        private void TryRaisePokeUp(Vector3 touchPosition)
        {
            if (currentTouchableDown != null)
            {
                if (currentTouchableDown.EventsToReceive == TouchableEventType.Pointer)
                {
                    MixedRealityToolkit.InputSystem?.RaisePointerUp(this, pointerAction, Handedness);
                }
                else if (currentTouchableDown.EventsToReceive == TouchableEventType.Touch)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnTouchCompleted(InputSourceParent, Controller, Handedness, touchPosition);
                }

                currentTouchableDown = null;
            }
        }

        private void RaiseTouchUpdated(GameObject targetObject, Vector3 touchPosition)
        {
            if (currentTouchableDown != null)
            {
                Debug.Assert(currentTouchableDown.gameObject == targetObject);
                if (currentTouchableDown.EventsToReceive == TouchableEventType.Touch)
                {
                    MixedRealityToolkit.InputSystem?.RaiseOnTouchUpdated(InputSourceParent, Controller, Handedness, touchPosition);
                }
            }
        }

        public bool TryGetNearGraspPoint(out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetDistanceToNearestSurface(out float distance)
        {
            distance = closestDistance;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetNormalToNearestSurface(out Vector3 normal)
        {
            normal = closestNormal;
            return true;
        }
    }
}
