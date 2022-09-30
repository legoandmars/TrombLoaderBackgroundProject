using System.IO;
using UnityEditor;
using UnityEngine;
using TrombLoader.Data;

namespace TrombLoader 
{
    [CustomEditor(typeof(TromboneEventManager))]
    public class TromboneEventManagerEditor : Editor 
    {
        TromboneEventManager tromboneEventManager;

        private void OnEnable() 
        {
            tromboneEventManager = (TromboneEventManager)target;
        }

        public override void OnInspectorGUI() 
        {
            DrawDefaultInspector();

            GUILayout.Space(20);

            if (GUILayout.Button("Add Background Event"))
            {
                Undo.AddComponent(tromboneEventManager.gameObject, typeof(BackgroundEvent));
            }
        }
    }
}
