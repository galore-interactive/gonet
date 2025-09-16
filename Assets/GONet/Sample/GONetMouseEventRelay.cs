using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using GONet;

/// <summary>
/// Relays Unity's mouse events (OnMouseEnter, OnMouseExit, OnMouseDown) from a child GameObject 
/// with a Collider to parent GameObjects that have GONetParticipantCompanionBehaviour components.
/// 
/// WHY THIS EXISTS:
/// Unity's built-in OnMouseEnter/Exit/Down methods only fire on GameObjects that have both 
/// a Collider AND the MonoBehaviour with these methods on the SAME GameObject. In networked
/// scenarios, it's common to have the Collider on a child object (like a visual mesh) while 
/// the networking logic lives on a parent GameObject. This relay bridges that gap.
/// 
/// HOW TO USE:
/// 1. Add this component to any child GameObject that has a Collider
/// 2. Ensure your parent GameObjects have components derived from GONetParticipantCompanionBehaviour
/// 3. Implement OnMouseEnter/OnMouseExit/OnMouseDown in those parent components as needed
/// 
/// PERFORMANCE NOTES:
/// - All reflection is done once at Start() and cached
/// - Only components that actually implement the mouse methods are stored
/// - No string-based SendMessage calls or repeated reflection during runtime
/// 
/// EXAMPLE SCENARIO:
/// GameObject Structure:
///   - NetworkedItem (has ClaimableObjectViaRpcs : GONetParticipantCompanionBehaviour)
///     - VisualMesh (has MeshCollider and this GONetMouseEventRelay component)
/// 
/// The mouse events on VisualMesh will be relayed up to NetworkedItem's ClaimableObjectViaRpcs.
/// </summary>
public class GONetMouseEventRelay : MonoBehaviour
{
    private struct ComponentMethodCache
    {
        public GONetParticipantCompanionBehaviour component;
        public MethodInfo onMouseEnter;
        public MethodInfo onMouseExit;
        public MethodInfo onMouseDown;
    }

    private List<ComponentMethodCache> cachedTargets = new List<ComponentMethodCache>();

    void Start()
    {
        // Search up the parent hierarchy
        Transform current = transform.parent; // Start with immediate parent
        while (current != null)
        {
            var components = current.GetComponents<GONetParticipantCompanionBehaviour>();
            foreach (var component in components)
            {
                // Get the actual runtime type of each component
                var componentType = component.GetType();
                var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var cache = new ComponentMethodCache
                {
                    component = component,
                    onMouseEnter = componentType.GetMethod("OnMouseEnter", bindingFlags, null, System.Type.EmptyTypes, null),
                    onMouseExit = componentType.GetMethod("OnMouseExit", bindingFlags, null, System.Type.EmptyTypes, null),
                    onMouseDown = componentType.GetMethod("OnMouseDown", bindingFlags, null, System.Type.EmptyTypes, null)
                };

                // Only add if at least one method exists
                if (cache.onMouseEnter != null || cache.onMouseExit != null || cache.onMouseDown != null)
                {
                    cachedTargets.Add(cache);
                }
            }
            current = current.parent;
        }
    }

    void OnMouseEnter()
    {
        foreach (var target in cachedTargets)
        {
            target.onMouseEnter?.Invoke(target.component, null);
        }
    }

    void OnMouseExit()
    {
        foreach (var target in cachedTargets)
        {
            target.onMouseExit?.Invoke(target.component, null);
        }
    }

    void OnMouseDown()
    {
        foreach (var target in cachedTargets)
        {
            target.onMouseDown?.Invoke(target.component, null);
        }
    }
}