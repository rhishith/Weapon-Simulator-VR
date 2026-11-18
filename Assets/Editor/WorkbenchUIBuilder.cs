using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRWeaponSimulator;
using VRWeaponSimulator.UI;

namespace VRWeaponSimulator.Editor
{
    public static class WorkbenchUIBuilder
    {
        private const string RoundedSpritePath = "Assets/VRTemplateAssets/Sprites/UI/Round Radius 10.png";
        private const string OutlineSpritePath = "Assets/VRTemplateAssets/Sprites/UI/Round Radius 10 Outline.png";
        private const string FontPath = "Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset";
        private const string WeaponsFolder = "Assets/_My Files/Prefabs/Weapons";

        [MenuItem("Weapon Simulator/Rebuild Armorer Terminal UI")]
        public static void BuildTerminalUI()
        {
            // 1. Ensure EventSystem with XRUIInputModule
            EnsureEventSystem();

            // 2. Load Assets
            Sprite roundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            Sprite outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutlineSpritePath);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            // Load all 9 weapon prefabs
            string[] weaponNames = new string[] {
                "AR_B", "AR_C", "Pistol_G", "Pistol_K",
                "Recon_P_PreSet", "ShotGun_C", "ShotGun_E", "SMG_F", "SMG_N"
            };

            List<GameObject> weaponPrefabs = new();
            foreach (var wName in weaponNames)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{WeaponsFolder}/{wName}.prefab");
                if (prefab != null) weaponPrefabs.Add(prefab);
            }

            // 3. Find or create WorkbenchHUD root
            GameObject existingHud = GameObject.Find("WorkbenchHUD");
            if (existingHud != null)
            {
                Undo.DestroyObjectImmediate(existingHud);
            }

            GameObject hudGo = new GameObject("WorkbenchHUD");
            Undo.RegisterCreatedObjectUndo(hudGo, "Create Terminal UI");

            hudGo.transform.position = new Vector3(0.00f, 1.48f, 1.22f);
            hudGo.transform.rotation = Quaternion.Euler(12f, 180f, 0f);

            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = hudGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1020, 640);
            rect.localScale = Vector3.one * 0.0009f;

            var scaler = hudGo.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            hudGo.AddComponent<TrackedDeviceGraphicRaycaster>();
            hudGo.AddComponent<GraphicRaycaster>();

            var hudComp = hudGo.AddComponent<WorkbenchHUD>();
            hudComp.availableWeaponPrefabs = weaponPrefabs;
            hudComp.weaponSpawnPosition = new Vector3(0.0f, 1.16f, 0.80f);
            hudComp.weaponSpawnEulerAngles = new Vector3(0f, 90f, 0f);

            // Set current weapon from scene if exists
            var currentWeapon = Object.FindFirstObjectByType<WeaponBase>();
            hudComp.targetWeapon = currentWeapon;

            // 4. Background Glass Panel
            GameObject mainBg = CreatePanel(hudGo.transform, "MainBackground", roundSprite, new Color(0.05f, 0.07f, 0.11f, 0.95f));
            SetAnchors(mainBg.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);

            // Subtle border outline
            GameObject border = CreatePanel(mainBg.transform, "BorderOutline", outlineSprite, new Color(0.00f, 0.90f, 1.00f, 0.35f));
            SetAnchors(border.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);

            // Top Header Glow Accent Line
            GameObject topGlow = CreatePanel(hudGo.transform, "TopAccentLine", null, new Color(0.00f, 0.90f, 1.00f, 0.85f));
            SetAnchors(topGlow.GetComponent<RectTransform>(), 0.02f, 0.99f, 0.98f, 1.0f, Vector2.zero, Vector2.zero);

            // ==============================================================
            // 5. TOP WEAPON SELECTOR TABS (ROW)
            // ==============================================================
            GameObject tabsRoot = new GameObject("WeaponTabs");
            tabsRoot.transform.SetParent(hudGo.transform, false);
            var tabsRect = tabsRoot.AddComponent<RectTransform>();
            SetAnchors(tabsRect, 0.02f, 0.89f, 0.98f, 0.97f, Vector2.zero, Vector2.zero);

            var hLayout = tabsRoot.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 6f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;

            string[] tabLabels = new string[] {
                "AR-B", "AR-C", "Pistol-G", "Pistol-K",
                "Recon-P", "ShotGun-C", "ShotGun-E", "SMG-F", "SMG-N"
            };

            for (int i = 0; i < weaponPrefabs.Count; i++)
            {
                int capturedIndex = i;
                string label = i < tabLabels.Length ? tabLabels[i] : weaponPrefabs[i].name;

                Button tabBtn = CreateButton(tabsRoot.transform, $"Tab_{label}", label, roundSprite, font, 13, new Color(0.12f, 0.16f, 0.22f, 0.9f), Color.white);
                tabBtn.onClick.AddListener(() => hudComp.SelectWeapon(capturedIndex));
                hudComp.weaponTabButtons.Add(tabBtn);
            }

            // ==============================================================
            // 6. HEADER BAR: Title, Category, Status Badge
            // ==============================================================
            GameObject headerRoot = new GameObject("HeaderBar");
            headerRoot.transform.SetParent(hudGo.transform, false);
            var headerRect = headerRoot.AddComponent<RectTransform>();
            SetAnchors(headerRect, 0.03f, 0.73f, 0.97f, 0.87f, Vector2.zero, Vector2.zero);

            // Weapon Title Text
            hudComp.weaponNameText = CreateText(headerRoot.transform, "WeaponTitle", "AR-15 MODULAR CARBINE (AR_C)", font, 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetAnchors(hudComp.weaponNameText.rectTransform, 0f, 0.45f, 0.65f, 1f, Vector2.zero, Vector2.zero);

            // Category Subtitle Text
            hudComp.weaponCategoryText = CreateText(headerRoot.transform, "CategorySubtitle", "ASSAULT RIFLE  |  5.56x45mm NATO  |  SEMI / AUTO", font, 12, FontStyles.Normal, new Color(0.55f, 0.68f, 0.80f), TextAlignmentOptions.Left);
            SetAnchors(hudComp.weaponCategoryText.rectTransform, 0f, 0f, 0.65f, 0.42f, Vector2.zero, Vector2.zero);

            // Status Badge Pill (Top-Right)
            GameObject badgeGo = CreatePanel(headerRoot.transform, "StatusBadge", roundSprite, new Color(0.12f, 0.85f, 0.55f, 0.25f));
            SetAnchors(badgeGo.GetComponent<RectTransform>(), 0.67f, 0.15f, 1f, 0.85f, Vector2.zero, Vector2.zero);
            hudComp.statusBadgeBackground = badgeGo.GetComponent<Image>();

            hudComp.statusBadgeText = CreateText(badgeGo.transform, "BadgeText", "[ FULLY ASSEMBLED ]", font, 14, FontStyles.Bold, new Color(0.12f, 0.85f, 0.55f), TextAlignmentOptions.Center);
            SetAnchors(hudComp.statusBadgeText.rectTransform, 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);

            // ==============================================================
            // 7. PROGRESS BAR SECTION
            // ==============================================================
            GameObject progressRoot = new GameObject("ProgressSection");
            progressRoot.transform.SetParent(hudGo.transform, false);
            var progRect = progressRoot.AddComponent<RectTransform>();
            SetAnchors(progRect, 0.03f, 0.64f, 0.97f, 0.71f, Vector2.zero, Vector2.zero);

            // Progress Count Label
            hudComp.progressCountText = CreateText(progressRoot.transform, "ProgressLabel", "11 / 11 Parts (100%)", font, 13, FontStyles.Bold, new Color(0.7f, 0.82f, 0.95f), TextAlignmentOptions.Right);
            SetAnchors(hudComp.progressCountText.rectTransform, 0.65f, 0.5f, 1f, 1.2f, Vector2.zero, Vector2.zero);

            var progTitle = CreateText(progressRoot.transform, "ProgTitle", "ARMORER ASSEMBLY COMPLETION", font, 12, FontStyles.Bold, new Color(0.5f, 0.6f, 0.72f), TextAlignmentOptions.Left);
            SetAnchors(progTitle.rectTransform, 0f, 0.5f, 0.65f, 1.2f, Vector2.zero, Vector2.zero);

            // Progress Bar Track
            GameObject track = CreatePanel(progressRoot.transform, "ProgressBarTrack", roundSprite, new Color(0.10f, 0.13f, 0.18f, 0.95f));
            SetAnchors(track.GetComponent<RectTransform>(), 0f, 0f, 1f, 0.45f, Vector2.zero, Vector2.zero);

            // Progress Bar Fill
            GameObject fill = CreatePanel(track.transform, "ProgressBarFill", roundSprite, new Color(0.12f, 0.85f, 0.55f));
            var fillImg = fill.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1.0f;
            SetAnchors(fill.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);
            hudComp.progressBar = fillImg;

            // ==============================================================
            // 8. CONTENT SECTION: Left = Actions (6 Buttons), Right = Telemetry Card
            // ==============================================================

            // Left Column: Armorer Actions
            GameObject actionsRoot = new GameObject("ActionsColumn");
            actionsRoot.transform.SetParent(hudGo.transform, false);
            var actRect = actionsRoot.AddComponent<RectTransform>();
            SetAnchors(actRect, 0.03f, 0.04f, 0.50f, 0.60f, Vector2.zero, Vector2.zero);

            var actTitle = CreateText(actionsRoot.transform, "ActionsTitle", "ARMORER WORKBENCH CONTROLS", font, 13, FontStyles.Bold, new Color(0.00f, 0.90f, 1.00f), TextAlignmentOptions.Left);
            SetAnchors(actTitle.rectTransform, 0f, 0.90f, 1f, 1f, Vector2.zero, Vector2.zero);

            // Button Grid (3 rows x 2 columns)
            GameObject grid = new GameObject("ButtonGrid");
            grid.transform.SetParent(actionsRoot.transform, false);
            var gridRect = grid.AddComponent<RectTransform>();
            SetAnchors(gridRect, 0f, 0f, 1f, 0.88f, Vector2.zero, Vector2.zero);

            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(235, 62);
            gl.spacing = new Vector2(10, 10);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 2;

            // 1. Field Strip
            var btnStrip = CreateButton(grid.transform, "Btn_FieldStrip", "FIELD STRIP", roundSprite, font, 14, new Color(0.85f, 0.35f, 0.15f, 0.85f), Color.white);
            btnStrip.onClick.AddListener(hudComp.FieldStripWeapon);

            // 2. Assemble All
            var btnAssemble = CreateButton(grid.transform, "Btn_AssembleAll", "ASSEMBLE ALL", roundSprite, font, 14, new Color(0.12f, 0.75f, 0.45f, 0.85f), Color.white);
            btnAssemble.onClick.AddListener(hudComp.AssembleAllParts);

            // 3. Eject Magazine
            var btnMag = CreateButton(grid.transform, "Btn_EjectMag", "EJECT MAGAZINE", roundSprite, font, 14, new Color(0.10f, 0.55f, 0.85f, 0.85f), Color.white);
            btnMag.onClick.AddListener(hudComp.EjectMagazine);

            // 4. Cycle Bolt / Rack
            var btnBolt = CreateButton(grid.transform, "Btn_CycleBolt", "CYCLE BOLT", roundSprite, font, 14, new Color(0.25f, 0.45f, 0.85f, 0.85f), Color.white);
            btnBolt.onClick.AddListener(hudComp.CycleBoltAction);

            // 5. Assembly Mode Toggle
            var btnMode = CreateButton(grid.transform, "Btn_ToggleMode", "MODE: REALISTIC", roundSprite, font, 13, new Color(0.45f, 0.25f, 0.75f, 0.85f), Color.white);
            btnMode.onClick.AddListener(hudComp.ToggleAssemblyMode);
            hudComp.modeButtonText = btnMode.GetComponentInChildren<TMP_Text>();

            // 6. Ghost Visuals Toggle
            var btnGhost = CreateButton(grid.transform, "Btn_ToggleGhosts", "GHOSTS: PROXIMITY", roundSprite, font, 13, new Color(0.20f, 0.28f, 0.38f, 0.85f), Color.white);
            btnGhost.onClick.AddListener(hudComp.CycleGhostMode);
            hudComp.ghostButtonText = btnGhost.GetComponentInChildren<TMP_Text>();

            // ==============================================================
            // Right Column: Live Component Telemetry Card
            // ==============================================================
            GameObject cardRoot = CreatePanel(hudGo.transform, "TelemetryCard", roundSprite, new Color(0.08f, 0.11f, 0.17f, 0.92f));
            var cardRect = cardRoot.GetComponent<RectTransform>();
            SetAnchors(cardRect, 0.53f, 0.04f, 0.97f, 0.60f, Vector2.zero, Vector2.zero);

            // Card outline
            GameObject cardOutline = CreatePanel(cardRoot.transform, "CardOutline", outlineSprite, new Color(0.00f, 0.90f, 1.00f, 0.25f));
            SetAnchors(cardOutline.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);

            // Card Header
            var cardHeader = CreateText(cardRoot.transform, "CardHeader", "LIVE COMPONENT TELEMETRY", font, 13, FontStyles.Bold, new Color(0.00f, 0.90f, 1.00f), TextAlignmentOptions.Left);
            SetAnchors(cardHeader.rectTransform, 0.05f, 0.86f, 0.95f, 0.96f, Vector2.zero, Vector2.zero);

            // Divider Line
            GameObject divider = CreatePanel(cardRoot.transform, "CardDivider", null, new Color(0.00f, 0.90f, 1.00f, 0.25f));
            SetAnchors(divider.GetComponent<RectTransform>(), 0.05f, 0.83f, 0.95f, 0.84f, Vector2.zero, Vector2.zero);

            // Part Title
            hudComp.telemetryTitleText = CreateText(cardRoot.transform, "PartTitle", "COMPONENT TELEMETRY", font, 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Left);
            SetAnchors(hudComp.telemetryTitleText.rectTransform, 0.05f, 0.68f, 0.95f, 0.81f, Vector2.zero, Vector2.zero);

            // Type Badge & Weight
            hudComp.telemetryTypeBadgeText = CreateText(cardRoot.transform, "TypeBadge", "[ RECEIVER ]", font, 13, FontStyles.Bold, new Color(0.12f, 0.85f, 0.55f), TextAlignmentOptions.Left);
            SetAnchors(hudComp.telemetryTypeBadgeText.rectTransform, 0.05f, 0.54f, 0.50f, 0.66f, Vector2.zero, Vector2.zero);

            hudComp.telemetryWeightText = CreateText(cardRoot.transform, "WeightText", "Mass: 0.85 kg", font, 13, FontStyles.Normal, new Color(1.0f, 0.80f, 0.20f), TextAlignmentOptions.Right);
            SetAnchors(hudComp.telemetryWeightText.rectTransform, 0.50f, 0.54f, 0.95f, 0.66f, Vector2.zero, Vector2.zero);

            // Status Line
            hudComp.telemetryStatusText = CreateText(cardRoot.transform, "StatusText", "Status: <color=#00E5FF>Attached to Socket</color>", font, 13, FontStyles.Normal, new Color(0.85f, 0.90f, 0.95f), TextAlignmentOptions.Left);
            SetAnchors(hudComp.telemetryStatusText.rectTransform, 0.05f, 0.40f, 0.95f, 0.52f, Vector2.zero, Vector2.zero);

            // Description Box
            hudComp.telemetryDescriptionText = CreateText(cardRoot.transform, "DescriptionText", "<i>Grab or point at any weapon component to inspect armorer specifications and technical details...</i>", font, 12, FontStyles.Italic, new Color(0.65f, 0.75f, 0.85f), TextAlignmentOptions.TopLeft);
            hudComp.telemetryDescriptionText.textWrappingMode = TextWrappingModes.Normal;
            SetAnchors(hudComp.telemetryDescriptionText.rectTransform, 0.05f, 0.05f, 0.95f, 0.38f, Vector2.zero, Vector2.zero);

            // 9. Initial display update
            hudComp.UpdateDisplay();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[WorkbenchUIBuilder] Successfully built and saved Armorer Terminal UI in Main.unity!");
        }

        private static void EnsureEventSystem()
        {
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<XRUIInputModule>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
                Debug.Log("[WorkbenchUIBuilder] Created EventSystem with XRUIInputModule");
            }
            else
            {
                if (es.GetComponent<XRUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<XRUIInputModule>();
                    Debug.Log("[WorkbenchUIBuilder] Added XRUIInputModule to existing EventSystem");
                }
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string text, Sprite sprite, TMP_FontAsset font, float fontSize, Color bgColor, Color textColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.25f + new Color(0.1f, 0.1f, 0.1f, 0f);
            colors.pressedColor = bgColor * 0.75f;
            colors.selectedColor = bgColor * 1.15f;
            btn.colors = colors;

            // Add VR tactile feedback
            go.AddComponent<VRButtonFeedback>();

            // Label
            var txt = CreateText(go.transform, "Label", text, font, fontSize, FontStyles.Bold, textColor, TextAlignmentOptions.Center);
            SetAnchors(txt.rectTransform, 0f, 0f, 1f, 1f, Vector2.zero, Vector2.zero);

            return btn;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }
    }
}
