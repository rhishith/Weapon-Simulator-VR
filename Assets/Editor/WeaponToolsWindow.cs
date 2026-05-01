using UnityEngine;
using UnityEditor;
using System.IO;
using VRWeaponSimulator;
using System.Collections.Generic;

public class WeaponFullAutoBuilder : EditorWindow
{
    private GameObject weaponPrefab;
    private Material ghostMaterial;

    private string rootPath = "Assets/My Files/Part Data";

    [MenuItem("Tools/Weapon Simulator/FULL Auto Setup")]
    public static void ShowWindow()
    {
        GetWindow<WeaponFullAutoBuilder>("FULL Auto Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("FULL Weapon Auto Builder", EditorStyles.boldLabel);

        weaponPrefab = (GameObject)EditorGUILayout.ObjectField("Weapon Prefab", weaponPrefab, typeof(GameObject), false);
        ghostMaterial = (Material)EditorGUILayout.ObjectField("Ghost Material", ghostMaterial, typeof(Material), false);

        if (GUILayout.Button("GENERATE COMPLETE SETUP"))
        {
            Generate();
        }
    }

    private void Generate()
    {
        if (weaponPrefab == null)
        {
            Debug.LogError("Assign weapon prefab!");
            return;
        }

        GameObject instance = Instantiate(weaponPrefab);
        instance.name = weaponPrefab.name;

        Undo.RegisterCreatedObjectUndo(instance, "Create Weapon");

        // 🔥 Ensure WeaponBase
        var weaponBase = instance.GetComponent<WeaponBase>();
        if (weaponBase == null)
            weaponBase = instance.AddComponent<WeaponBase>();

        weaponBase.weaponName = instance.name;

        // 🔥 Create roots
        Transform partsRoot = CreateChild(instance.transform, "Parts");
        Transform socketsRoot = CreateChild(instance.transform, "Sockets");
        Transform ghostRoot = CreateChild(instance.transform, "Ghost Visuals");

        // 🔥 Create data folders
        string weaponFolder = $"{rootPath}/{instance.name}";
        CreateFolders(instance.name);

        var meshObjects = GetTopLevelMeshes(instance.transform);

        foreach (var mesh in meshObjects)
        {
            // Skip already generated folders
            if (mesh.parent == partsRoot || mesh.parent == socketsRoot || mesh.parent == ghostRoot)
                continue;

            string name = mesh.name;

            // =====================
            // PART
            // =====================
            GameObject part = Instantiate(mesh.gameObject, partsRoot);
            part.name = name;

            var partComp = AddIfMissing<WeaponPart>(part);
            AddIfMissing<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(part);

            if (!part.GetComponent<Collider>())
                part.AddComponent<BoxCollider>();

            // =====================
            // SOCKET
            // =====================
            GameObject socket = new GameObject(name);
            socket.transform.SetParent(socketsRoot);
            socket.transform.position = mesh.position;
            socket.transform.rotation = mesh.rotation;

            var socketComp = socket.AddComponent<WeaponSocket>();

            // Assign WeaponBase (editor-side for clarity)
            var so = new SerializedObject(socketComp);
            var prop = so.FindProperty("weaponBase");
            if (prop != null)
            {
                prop.objectReferenceValue = weaponBase;
                so.ApplyModifiedProperties();
            }

            // =====================
            // GHOST
            // =====================
            GameObject ghost = Instantiate(mesh.gameObject, ghostRoot);
            ghost.name = name;

            CleanGhost(ghost);

            // =====================
            // PART DATA
            // =====================
            string assetPath = $"{weaponFolder}/{name}.asset";

            PartData data = AssetDatabase.LoadAssetAtPath<PartData>(assetPath);

            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PartData>();
                data.partName = name;

                AssetDatabase.CreateAsset(data, assetPath);
            }

            partComp.data = data;
            EditorUtility.SetDirty(partComp);

            // =====================
            // 🔥 MOVE ORIGINAL (FIX DUPLICATE ISSUE)
            // =====================
            DestroyImmediate(mesh.gameObject);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("🔥 FULL WEAPON SETUP COMPLETE (NO DUPLICATES)");
    }

    // -----------------------
    // HELPERS
    // -----------------------

    private void CreateFolders(string weaponName)
    {
        if (!AssetDatabase.IsValidFolder("Assets/My Files"))
            AssetDatabase.CreateFolder("Assets", "My Files");

        if (!AssetDatabase.IsValidFolder("Assets/My Files/Part Data"))
            AssetDatabase.CreateFolder("Assets/My Files", "Part Data");

        if (!AssetDatabase.IsValidFolder($"{rootPath}/{weaponName}"))
            AssetDatabase.CreateFolder(rootPath, weaponName);
    }

    private Transform CreateChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        return go.transform;
    }

    private List<Transform> GetTopLevelMeshes(Transform root)
    {
        List<Transform> result = new();

        foreach (Transform child in root)
        {
            if (child.GetComponent<MeshRenderer>())
                result.Add(child);
        }

        return result;
    }

    private T AddIfMissing<T>(GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();

        return comp;
    }

    private void CleanGhost(GameObject go)
    {
        foreach (var comp in go.GetComponents<Component>())
        {
            if (comp is Transform) continue;
            if (comp is MeshRenderer || comp is MeshFilter) continue;

            DestroyImmediate(comp);
        }

        if (ghostMaterial != null)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = ghostMaterial;
        }
    }
}