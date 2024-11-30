using GONet.Editor;
using GONet.Generation;
using GONet;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Monitors all serialized inspector fields with <see cref="GONetAutoMagicalSyncAttribute"/> for changes and marks gonet as dirty when they change.
/// </summary>
[CustomEditor(typeof(GONetParticipantCompanionBehaviour), editorForChildClasses: true)]
public class GONetParticipantCompanionBehaviourCustomInspector : UnityEditor.Editor
{
    private GONetParticipantCompanionBehaviour targetCompanion;
    private SerializedProperty[] fieldsToMonitor_AutoMagicalSync;
    private object[] previousValues;
    private object[] newValues;

    private void OnEnable()
    {
        targetCompanion = (GONetParticipantCompanionBehaviour)target;

        // Populate fieldsToMonitor_AutoMagicalSync with all fields that have GONetAutoMagicalSyncAttribute
        List<SerializedProperty> monitoredFields = new List<SerializedProperty>();
        foreach (FieldInfo field in targetCompanion.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<GONetAutoMagicalSyncAttribute>() != null)
            {
                SerializedProperty property = serializedObject.FindProperty(field.Name);
                if (property != null)
                {
                    monitoredFields.Add(property);
                }
            }
        }

        fieldsToMonitor_AutoMagicalSync = monitoredFields.ToArray();
        previousValues = new object[fieldsToMonitor_AutoMagicalSync.Length];
        newValues = new object[fieldsToMonitor_AutoMagicalSync.Length];
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (!Application.isPlaying)
        {
            // Populate previousValues with current values from serializedObject
            for (int i = 0; i < fieldsToMonitor_AutoMagicalSync.Length; i++)
            {
                previousValues[i] = GetPropertyValue(fieldsToMonitor_AutoMagicalSync[i]);
            }
        }

        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            if (!Application.isPlaying)
            {
                // Populate newValues with updated values from serializedObject
                for (int i = 0; i < fieldsToMonitor_AutoMagicalSync.Length; ++i)
                {
                    newValues[i] = GetPropertyValue(fieldsToMonitor_AutoMagicalSync[i]);

                    // Compare previous and new values
                    if (!Equals(previousValues[i], newValues[i]))
                    {
                        string fieldName = fieldsToMonitor_AutoMagicalSync[i].displayName;
                        string gameObjectPath = DesignTimeMetadata.GetFullPath(targetCompanion.GetComponent<GONetParticipant>());

                        GONetSpawnSupport_DesignTime.AddGONetDesignTimeDirtyReason(
                            $"{targetCompanion.GetType().Name} on GO at '{gameObjectPath}' has changed the monitored field '{fieldName}' in editor. NOTE: The path to the GameObject that changed might incorrectly list it is in the project, when in fact it is in a scene.");

                        EditorUtility.SetDirty(targetCompanion);
                    }
                }
            }
        }
    }

    private object GetPropertyValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                return property.intValue;
            case SerializedPropertyType.Boolean:
                return property.boolValue;
            case SerializedPropertyType.Float:
                return property.floatValue;
            case SerializedPropertyType.String:
                return property.stringValue;
            case SerializedPropertyType.Color:
                return property.colorValue;
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue;
            case SerializedPropertyType.LayerMask:
                return property.intValue;
            case SerializedPropertyType.Enum:
                return property.enumValueIndex;
            case SerializedPropertyType.Vector2:
                return property.vector2Value;
            case SerializedPropertyType.Vector3:
                return property.vector3Value;
            case SerializedPropertyType.Vector4:
                return property.vector4Value;
            case SerializedPropertyType.Rect:
                return property.rectValue;
            case SerializedPropertyType.ArraySize:
                return property.arraySize;
            case SerializedPropertyType.AnimationCurve:
                return property.animationCurveValue;
            case SerializedPropertyType.Bounds:
                return property.boundsValue;
            case SerializedPropertyType.Gradient:
                // Gradients require reflection to access, but it's rare to use them directly in custom inspectors
                return null;
            default:
                return null;
        }
    }
}
