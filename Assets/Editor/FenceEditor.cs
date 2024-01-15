using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FenceCreator))]
public class FenceEditor : Editor
{
    FenceCreator creator;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate Collider"))
        {
            Undo.RecordObject(creator, "Create New");

            creator.GenerateCollider();
        }
    }

    private void OnSceneGUI()
    {
        if (creator.autoUpdate && Event.current.type == EventType.Repaint)
        {
            creator.UpdateRoad();
        }
    }

    private void OnEnable()
    {
        creator = (FenceCreator)target;
    }
}
