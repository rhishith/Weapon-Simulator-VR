using UnityEngine;
using UnityEditor;
using System.IO;
using VRWeaponSimulator;
using System.Collections.Generic;

public class WeaponFullAutoBuilder : EditorWindow
{
    private GameObject weaponPrefab;
    private Material ghostMaterial;

    private const string RootDataPath = "Assets/_My Files/Part Data";
    private const string WeaponsPrefabFolder = "Assets/_My Files/Prefabs/Weapons";
    private const string DefaultGhostMatPath = "Assets/_My Files/Materials/M_Ghost.mat";

    [MenuItem("Tools/Weapon Simulator/FULL Auto Setup")]
    public static void ShowWindow()
    {
        GetWindow<WeaponFullAutoBuilder>("FULL Auto Setup");
    }

    private void OnEnable()
    {
        if (ghostMaterial == null)
            ghostMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultGhostMatPath);
    }

    private void OnGUI()
    {
        GUILayout.Label("Weapon Assembly & Disassembly Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        weaponPrefab = (GameObject)EditorGUILayout.ObjectField("Single Weapon Prefab", weaponPrefab, typeof(GameObject), false);
        ghostMaterial = (Material)EditorGUILayout.ObjectField("Ghost Material", ghostMaterial, typeof(Material), false);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("GENERATE SINGLE WEAPON SETUP", GUILayout.Height(30)))
        {
            if (weaponPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a weapon prefab!", "OK");
                return;
            }
            EnsureGhostMaterial();
            SetupWeapon(weaponPrefab, true, true, ghostMaterial);
        }

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("🔥 SETUP ALL WEAPONS IN PREFAB FOLDER", GUILayout.Height(36)))
        {
            EnsureGhostMaterial();
            SetupAllWeaponsInFolder(ghostMaterial);
        }
        GUI.backgroundColor = Color.white;
    }

    private void EnsureGhostMaterial()
    {
        if (ghostMaterial == null)
            ghostMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultGhostMatPath);
    }

    public static void SetupAllWeaponsInFolder(Material customGhostMat = null)
    {
        Material mat = customGhostMat != null ? customGhostMat : AssetDatabase.LoadAssetAtPath<Material>(DefaultGhostMatPath);
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { WeaponsPrefabFolder });
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Debug.Log($"[WeaponBuilder] Setting up: {prefab.name} from {path}");
            SetupWeapon(prefab, true, false, mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WeaponBuilder] Successfully configured assembly & disassembly for {count} weapons in {WeaponsPrefabFolder}!");
    }

    public static GameObject SetupWeapon(GameObject prefab, bool saveAsPrefab, bool keepInScene, Material customGhostMat = null)
    {
        Material ghostMat = customGhostMat != null ? customGhostMat : AssetDatabase.LoadAssetAtPath<Material>(DefaultGhostMatPath);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
            instance = Instantiate(prefab);

        // Unpack prefab so we can restructure hierarchy freely
        if (PrefabUtility.IsPartOfPrefabInstance(instance))
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        instance.name = prefab.name;
        Undo.RegisterCreatedObjectUndo(instance, "Create Weapon Setup");

        // Ensure WeaponBase on root
        var weaponBase = instance.GetComponent<WeaponBase>();
        if (weaponBase == null)
            weaponBase = instance.AddComponent<WeaponBase>();

        weaponBase.weaponName = instance.name;

        // Create roots
        Transform partsRoot = CreateChild(instance.transform, "Parts");
        Transform socketsRoot = CreateChild(instance.transform, "Sockets");
        Transform ghostRoot = CreateChild(instance.transform, "Ghost Visuals");

        // Flatten any deeply nested detachable parts (e.g. Pistol_G_Mag inside Receiver_Part)
        ExtractNestedDetachableParts(instance.transform, partsRoot, socketsRoot, ghostRoot);

        // Create data folder for this weapon
        string weaponFolder = $"{RootDataPath}/{instance.name}";
        EnsureFolderExists(RootDataPath);
        EnsureFolderExists(weaponFolder);

        var meshObjects = GetTopLevelMeshes(instance.transform, partsRoot, socketsRoot, ghostRoot);

        foreach (var mesh in meshObjects)
        {
            string partName = mesh.name;

            // =====================
            // 1. PART
            // =====================
            GameObject part = Instantiate(mesh.gameObject, partsRoot);
            part.name = partName;
            part.transform.position = mesh.position;
            part.transform.rotation = mesh.rotation;

            var partComp = AddIfMissing<WeaponPart>(part);
            var grab = AddIfMissing<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(part);

            // Fit BoxCollider on Part
            var partBox = part.GetComponent<BoxCollider>();
            if (partBox == null)
                partBox = part.AddComponent<BoxCollider>();

            var renderers = part.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);

                partBox.center = part.transform.InverseTransformPoint(b.center);
                Vector3 lossy = part.transform.lossyScale;
                partBox.size = new Vector3(
                    lossy.x != 0 ? b.size.x / lossy.x : b.size.x,
                    lossy.y != 0 ? b.size.y / lossy.y : b.size.y,
                    lossy.z != 0 ? b.size.z / lossy.z : b.size.z
                );
            }

            // Setup Rigidbody
            var rb = AddIfMissing<Rigidbody>(part);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // =====================
            // 2. PART DATA SCRIPTABLE OBJECT
            // =====================
            string assetPath = $"{weaponFolder}/{partName}.asset";
            PartData data = AssetDatabase.LoadAssetAtPath<PartData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<PartData>();
                data.partName = partName;
                data.type = DeterminePartType(partName);
                data.weight = 1.0f;
                AssetDatabase.CreateAsset(data, assetPath);
            }
            partComp.data = data;
            rb.mass = data.weight;
            EditorUtility.SetDirty(partComp);

            // =====================
            // 3. SOCKET
            // =====================
            GameObject socket = new GameObject(partName);
            socket.transform.SetParent(socketsRoot);
            socket.transform.position = mesh.position;
            socket.transform.rotation = mesh.rotation;

            // Add trigger BoxCollider for socket hover/snap detection
            var socketBox = socket.AddComponent<BoxCollider>();
            socketBox.isTrigger = true;
            if (partBox != null)
            {
                socketBox.center = partBox.center;
                socketBox.size = new Vector3(
                    Mathf.Max(partBox.size.x * 1.35f, 0.08f),
                    Mathf.Max(partBox.size.y * 1.35f, 0.08f),
                    Mathf.Max(partBox.size.z * 1.35f, 0.08f)
                );
            }

            var socketComp = socket.AddComponent<WeaponSocket>();
            socketComp.weaponBase = weaponBase;
            socketComp.expectedPart = partComp;
            socketComp.startingSelectedInteractable = grab;

            // =====================
            // 4. GHOST VISUAL
            // =====================
            GameObject ghost = Instantiate(mesh.gameObject, ghostRoot);
            ghost.name = partName;
            ghost.transform.position = mesh.position;
            ghost.transform.rotation = mesh.rotation;

            CleanGhost(ghost, ghostMat);
            socketComp.ghostMesh = ghost;

            // Remove original mesh GameObject
            DestroyImmediate(mesh.gameObject);
        }

        // Setup WeaponGhostVisualController on ghostRoot
        var ghostCtrl = AddIfMissing<WeaponGhostVisualController>(ghostRoot.gameObject);
        ghostCtrl.weaponBase = weaponBase;
        ghostCtrl.ghostRoot = ghostRoot.gameObject;

        // Register parts on WeaponBase
        weaponBase.AutoCollectParts();

        if (saveAsPrefab)
        {
            string prefabSavePath = $"{WeaponsPrefabFolder}/{instance.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabSavePath);
            Debug.Log($"[WeaponBuilder] Saved prefab: {prefabSavePath}");
        }

        if (!keepInScene)
        {
            DestroyImmediate(instance);
            return null;
        }

        return instance;
    }

    private static void ExtractNestedDetachableParts(Transform root, Transform partsRoot, Transform socketsRoot, Transform ghostRoot)
    {
        List<Transform> toUnparent = new();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root || t == partsRoot || t == socketsRoot || t == ghostRoot)
                continue;

            // If it's a Magazine, Sight, Muzzle, etc. nested inside another mesh (e.g. Pistol_G_Mag inside Pistol_G_Receiver_Part)
            if (t.parent != root && t.parent != null && (t.name.Contains("Mag") || t.name.Contains("Sight_Back") || t.name.Contains("Muzzle")))
            {
                toUnparent.Add(t);
            }
        }

        foreach (var t in toUnparent)
        {
            t.SetParent(root, true);
        }
    }

    private static PartType DeterminePartType(string name)
    {
        if (name.Contains("Mag")) return PartType.Magazine;
        if (name.Contains("Barrel")) return PartType.Barrel;
        if (name.Contains("Bolt") || name.Contains("Slide")) return PartType.Slide;
        if (name.Contains("Stock")) return PartType.Stock;
        if (name.Contains("Sight") || name.Contains("Optic")) return PartType.Sight;
        if (name.Contains("Grip") || name.Contains("Bipod")) return PartType.Grip;
        if (name.Contains("Receiver")) return PartType.Receiver;
        if (name.Contains("Trigger") || name.Contains("Tigger")) return PartType.Trigger;
        if (name.Contains("Rail")) return PartType.Rail;
        if (name.Contains("Muzzle") || name.Contains("Brake")) return PartType.Muzzle;
        return PartType.Other;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) return t;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static List<Transform> GetTopLevelMeshes(Transform root, Transform partsRoot, Transform socketsRoot, Transform ghostRoot)
    {
        List<Transform> result = new();
        foreach (Transform child in root)
        {
            if (child == partsRoot || child == socketsRoot || child == ghostRoot)
                continue;

            if (child.GetComponent<MeshRenderer>() != null || child.GetComponentInChildren<MeshRenderer>() != null)
                result.Add(child);
        }
        return result;
    }

    private static T AddIfMissing<T>(GameObject go) where T : Component
    {
        var comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    private static void CleanGhost(GameObject go, Material ghostMat)
    {
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(col);

        foreach (var comp in go.GetComponentsInChildren<Component>(true))
        {
            if (comp is Transform || comp is MeshRenderer || comp is MeshFilter) continue;
            DestroyImmediate(comp);
        }

        if (ghostMat != null)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = ghostMat;
        }
    }
}
