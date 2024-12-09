# if UNITY_EDITOR
using iffnsStuff.MarchingCubeEditor.Core;
using iffnsStuff.MarchingCubeEditor.EditTools;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace iffnsStuff.MarchingCubeEditor.SceneEditor
{
    public class MarchingCubeEditor : EditorWindow
    {
        int gridResolutionX = 20;
        int gridResolutionY = 20;
        int gridResolutionZ = 20;
        bool addingShape = false;
        bool limitMaxHeight;
        bool invertNormals;
        bool displayPreviewShape;
        Vector3 originalShapePosition;

        Color additionColor = new Color(1f, 0.5f, 0f, 0.5f);
        Color subtractionColor = new Color(1f, 0f, 0f, 0.5f);

        [MenuItem("Tools/iffnsStuff/MarchingCubeEditor")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MarchingCubeEditor));
        }

        readonly static List<MarchingCubeEditor> editors = new();
        static MarchingCubesController linkedMarchingCubesController;
        static EditShape selectedShape;
        static void FindSceneObjectsIfNeeded()
        {
            if(linkedMarchingCubesController == null)
            {
                linkedMarchingCubesController = Object.FindObjectOfType<MarchingCubesController>(false);

                foreach(MarchingCubeEditor editor in editors)
                {
                    editor.Repaint();
                }
            }

            if (selectedShape == null)
            {
                selectedShape = Object.FindObjectOfType<EditShape>(false);

                foreach (MarchingCubeEditor editor in editors)
                {
                    editor.Repaint();
                }
            }
        }

        private void OnEnable()
        {
            FindSceneObjectsIfNeeded();

            editors.Add(this);

            if(editors.Count == 1)
            {
                EditorApplication.hierarchyChanged += FindSceneObjectsIfNeeded;
            }
        }

        private void OnDisable()
        {
            editors.Remove(this);

            if(editors.Count == 0)
            {
                EditorApplication.hierarchyChanged -= FindSceneObjectsIfNeeded;
            }
        }

        void OnGUI()
        {
            DrawSetupUI();
            DrawEditUI();
        }

        //Components
        void DrawSetupUI()
        {
            GUILayout.Label("Scene component:");

            linkedMarchingCubesController = EditorGUILayout.ObjectField(
               linkedMarchingCubesController,
               typeof(MarchingCubesController),
               true) as MarchingCubesController;

            if (linkedMarchingCubesController == null)
            {
                EditorGUILayout.HelpBox("Add a Marching cube prefab to your scene and link it to this scrip", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X");
            GUILayout.Label("Y");
            GUILayout.Label("Z");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            gridResolutionX = EditorGUILayout.IntField(gridResolutionX);
            gridResolutionY = EditorGUILayout.IntField(gridResolutionY);
            gridResolutionZ = EditorGUILayout.IntField(gridResolutionZ);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply and set empty"))
            {
                linkedMarchingCubesController.Initialize(gridResolutionX, gridResolutionY, gridResolutionZ, true);
                nextUpdateTime = EditorApplication.timeSinceStartup;
            }

            GUILayout.Label("Save data:");
            linkedMarchingCubesController.linkedSaveData = EditorGUILayout.ObjectField(
               linkedMarchingCubesController.linkedSaveData,
               typeof(ScriptableObjectSaveData),
               true) as ScriptableObjectSaveData;

            if (linkedMarchingCubesController.linkedSaveData != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"Save data")) linkedMarchingCubesController.SaveAndLoadManager.SaveGridData(linkedMarchingCubesController.linkedSaveData);
                if (GUILayout.Button($"Load data")) LoadData();
                EditorGUILayout.EndHorizontal();
            }

            // Auto initialize and load
            // ToDo: Check if it makes sense to move this into the MarchingCubeController using [ExecuteInEditMode] and OnEnable
            if (!linkedMarchingCubesController.IsInitialized)
            {
                bool loadData = linkedMarchingCubesController.linkedSaveData != null;

                linkedMarchingCubesController.Initialize(gridResolutionX, gridResolutionY, gridResolutionZ, !loadData);

                if (loadData) linkedMarchingCubesController.SaveAndLoadManager.LoadGridData(linkedMarchingCubesController.linkedSaveData);
            }
        }

        void DrawEditUI()
        {

            GUILayout.Label("Editing:");
            selectedShape = EditorGUILayout.ObjectField(
               selectedShape,
               typeof(EditShape),
               true) as EditShape;

            if (selectedShape)
            {
                // Add and subtract
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"Add {selectedShape.transform.name}")) linkedMarchingCubesController.ModificationManager.ModifyData(selectedShape, new BaseModificationTools.AddShapeModifier());
                if (GUILayout.Button($"Subtract {selectedShape.transform.name}")) linkedMarchingCubesController.ModificationManager.ModifyData(selectedShape, new BaseModificationTools.SubtractShapeModifier());
                EditorGUILayout.EndHorizontal();

                // Max height
                limitMaxHeight = EditorGUILayout.Toggle("Limit max height", limitMaxHeight);

                // Adding shape
                bool newAddingShape = EditorGUILayout.Toggle("Add Shape Mode", addingShape);
                if (newAddingShape && !addingShape) //Toggle on
                {
                    linkedMarchingCubesController.EnableAllColliders = true;
                    originalShapePosition = selectedShape.transform.position;
                }
                else if (!newAddingShape && addingShape) //Toggle off
                {
                    linkedMarchingCubesController.EnableAllColliders = false;
                    selectedShape.transform.position = originalShapePosition;
                    selectedShape.gameObject.SetActive(true);
                }
                addingShape = newAddingShape;

                // Display preview shape
                displayPreviewShape = EditorGUILayout.Toggle("Display preview shape", displayPreviewShape);

                // Invert normals
                bool newInvertedNormals = EditorGUILayout.Toggle("Inverted normals", invertNormals);
                if (newInvertedNormals != invertNormals)
                {
                    linkedMarchingCubesController.InvertAllNormals = newInvertedNormals;
                    invertNormals = newInvertedNormals;
                }
            }

            if (addingShape)
            {
                SceneView.duringSceneGui += OnSceneGUI; // Subscribe to SceneView GUI event

                EditorGUILayout.HelpBox("Controls:\n" +
                    "Click to add\n" +
                    "Ctrl Click to subtract\n" +
                    "Shift Scroll to scale", MessageType.None);
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGUI; // Unsubscribe when not in use
            }
        }

        //Helper functions
        void LoadData()
        {
            linkedMarchingCubesController.SaveAndLoadManager.LoadGridData(linkedMarchingCubesController.linkedSaveData);
            gridResolutionX = linkedMarchingCubesController.GridResolutionX;
            gridResolutionY = linkedMarchingCubesController.GridResolutionY;
            gridResolutionZ = linkedMarchingCubesController.GridResolutionZ;
        }

        double nextUpdateTime;
        readonly double timeBetweenUpdates = 0.1f;

        void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            /*
            if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Tab)
            {
                InvertNormals(!invertNormals);

                Debug.Log("Invert");

                e.Use();
            }
            */

            if (addingShape && selectedShape)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    selectedShape.transform.position = hit.point;

                    if (displayPreviewShape)
                    {
                        if (EditorApplication.timeSinceStartup >= nextUpdateTime) //Only update once in a while
                        {
                            if (e.control) linkedMarchingCubesController.ModificationManager.ShowPreviewData(selectedShape, new BaseModificationTools.SubtractShapeModifier());
                            else
                            {
                                linkedMarchingCubesController.ModificationManager.ShowPreviewData(selectedShape, new BaseModificationTools.AddShapeModifier());
                                /*
                                if (limitMaxHeight) linkedMarchingCubesController.PreviewAddShapeWithMaxHeight(selectedShape, hit.point.y);
                                else linkedMarchingCubesController.PreviewAddShape(selectedShape);
                                */
                            }

                            selectedShape.gameObject.SetActive(false);
                            linkedMarchingCubesController.DisplayPreviewShape = true;

                            nextUpdateTime = EditorApplication.timeSinceStartup + timeBetweenUpdates;
                        }

                        if (e.type == EventType.MouseDown && e.button == 0) // Left-click event
                        {
                            linkedMarchingCubesController.ModificationManager.ApplyPreviewChanges();
                            e.Use();
                        }
                    }
                    else
                    {
                        selectedShape.gameObject.SetActive(true);
                        selectedShape.Color = e.control ? subtractionColor : additionColor;

                        if (e.type == EventType.MouseDown && e.button == 0) // Left-click event
                        {
                            if (e.control) linkedMarchingCubesController.ModificationManager.ModifyData(selectedShape, new BaseModificationTools.SubtractShapeModifier());
                            else
                            {
                                linkedMarchingCubesController.ModificationManager.ModifyData(selectedShape, new BaseModificationTools.AddShapeModifier());
                                /*
                                if (limitMaxHeight) linkedMarchingCubesController.AddShapeWithMaxHeight(selectedShape, hit.point.y, true);
                                else linkedMarchingCubesController.AddShape(selectedShape, true);
                                */
                            }

                            e.Use();
                            return;
                        }
                    }

                    if (e.shift && e.type == EventType.ScrollWheel)
                    {
                        float scaleDelta = e.delta.x * -0.03f; // Scale factor; reverse direction if needed

                        selectedShape.transform.localScale *= (scaleDelta + 1);

                        e.Use(); // Mark event as handled
                    }
                }
                else
                {
                    linkedMarchingCubesController.DisplayPreviewShape = false;
                    selectedShape.gameObject.SetActive(false);
                }
            }
        }
    }
}
#endif
