using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileDatabase))]
public class TileDatabaseEditor : Editor
{
    public TileDatabase current
    {
        get
        {
            return (TileDatabase)target;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10f);
        if (GUILayout.Button("Activate all tiles"))
            current.ActivateAllTiles();
        GUILayout.Space(2f);

        if (GUILayout.Button("Deactivate all tiles"))
            current.DeactivateAllTiles();
        GUILayout.Space(2f);

        if (GUILayout.Button("Deactivate specific size"))
            current.DeactivateSpecificSizedTiles();
        GUILayout.Space(10f);

        if (GUILayout.Button("Set random rarities"))
            current.RandomRarities();
        GUILayout.Space(2f);

        if (GUILayout.Button("Set rarity to max"))
            current.SetAllTilesRaritiesToMax();
    }
}
