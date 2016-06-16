using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARTrackedObjectLive))] 
public class ARTrackedObjectLiveEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        ARTrackedObjectLive arto = (ARTrackedObjectLive)target;
		if (arto == null) return;

		arto.MarkerTag = EditorGUILayout.TextField("Marker Tag", arto.MarkerTag);

		ARMarker marker = arto.GetMarker();
		EditorGUILayout.LabelField("Got Marker", marker == null ? "no" : "yes");
		if (marker != null)
        {
			string type = ARMarker.MarkerTypeNames[marker.MarkerType];
			EditorGUILayout.LabelField("Marker UID", (marker.UID != ARMarker.NO_ID ? marker.UID.ToString() : "Not loaded") + " (" + type + ")");	
		}

        arto.remainVisible = EditorGUILayout.Toggle("Remain Visible", arto.remainVisible);

        EditorGUILayout.Separator();
		
		arto.eventReceiver = (GameObject)EditorGUILayout.ObjectField("Event Receiver:", arto.eventReceiver, typeof(GameObject), true);
	}
}
