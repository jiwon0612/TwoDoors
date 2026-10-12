using TMPro;
using TwoDoors.Characters;
using TwoDoors.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TwoDoors.Editor
{
    public static class CombatContentUpgrade
    {
        static Material Material(string name, Color color)
        {
            string path = "Assets/TwoDoors/Generated/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); return m;
        }

        static void Part(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        public static GameObject Weapon(Transform parent, ItemKind kind)
        {
            var old = parent.Find(kind.ToString()); if (old) return old.gameObject;
            var root = new GameObject(kind.ToString()); root.transform.SetParent(parent, false);
            var steel = Material("WeaponSteel", new Color(0.16f, 0.2f, 0.23f));
            var accent = Material("WeaponGrip", new Color(0.35f, 0.17f, 0.07f));
            var edge = Material("WeaponEdge", new Color(0.7f, 0.8f, 0.85f));
            if (kind == ItemKind.Machete)
            {
                Part(root.transform, "Grip", Vector3.zero, new Vector3(0.05f, 0.18f, 0.045f), accent);
                Part(root.transform, "Guard", new Vector3(0, 0.1f, 0), new Vector3(0.12f, 0.025f, 0.05f), steel);
                Part(root.transform, "Blade", new Vector3(0, 0.36f, 0), new Vector3(0.075f, 0.5f, 0.015f), edge);
                root.transform.localRotation = Quaternion.Euler(20, 0, -18);
            }
            else
            {
                bool mg = kind == ItemKind.MachineGun;
                Part(root.transform, "Receiver", new Vector3(0, 0, 0.06f), new Vector3(0.1f, 0.1f, 0.3f), steel);
                Part(root.transform, "Stock", new Vector3(0, -0.025f, -0.17f), new Vector3(0.075f, 0.1f, 0.18f), accent);
                Part(root.transform, "Barrel", new Vector3(0, 0.02f, 0.34f), new Vector3(0.035f, 0.035f, mg ? 0.25f : 0.4f), steel);
                Part(root.transform, "Grip", new Vector3(0, -0.09f, 0), new Vector3(0.055f, 0.14f, 0.065f), accent);
                Part(root.transform, mg ? "Magazine" : "Pump", mg ? new Vector3(0, -0.13f, 0.13f) : new Vector3(0, -0.015f, 0.3f), mg ? new Vector3(0.065f, 0.18f, 0.1f) : new Vector3(0.085f, 0.075f, 0.17f), accent);
                Part(root.transform, "Sight", new Vector3(0, 0.075f, 0.16f), new Vector3(0.015f, 0.04f, 0.02f), edge);
            }
            return root;
        }

        public static void UpgradePlayer(PlayerCharacter player)
        {
            var so = new SerializedObject(player);
            var hands = ((GameObject)so.FindProperty("pistol").objectReferenceValue).transform.parent;
            foreach (var kind in new[] { ItemKind.MachineGun, ItemKind.Shotgun, ItemKind.Machete })
            {
                var model = Weapon(hands, kind); model.SetActive(false);
                so.FindProperty(kind == ItemKind.MachineGun ? "machineGun" : kind == ItemKind.Shotgun ? "shotgun" : "machete").objectReferenceValue = model;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var cc = player.GetComponent<CharacterController>(); cc.skinWidth = 0.035f; cc.stepOffset = 0.15f;
        }

        public static void UpgradeInventory(InventoryView view)
        {
            var so = new SerializedObject(view);
            var panel = ((GameObject)so.FindProperty("panel").objectReferenceValue).GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(1100, 690);
            panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.025f, 0.04f, 0.065f, 1f);
            var title = panel.Find("Title").GetComponent<TMP_Text>(); title.text = "FIELD INVENTORY"; title.fontSize = 38; title.characterSpacing = 5; title.rectTransform.anchoredPosition = new Vector2(-170, 275);
            var hint = panel.Find("Hint").GetComponent<TMP_Text>(); hint.text = "SELECT TO EQUIP / USE     •     1 KNIFE   2 PISTOL   5 AUTO   6 SHOTGUN   7 MACHETE     •     TAB CLOSE"; hint.fontSize = 16; hint.rectTransform.anchoredPosition = new Vector2(0, -300); hint.rectTransform.sizeDelta = new Vector2(1040, 35);
            panel.Find("Close").GetComponent<RectTransform>().anchoredPosition = new Vector2(490, 285);
            var bs = so.FindProperty("buttons"); var ls = so.FindProperty("labels"); bs.arraySize = ls.arraySize = 8;
            for (int i = 0; i < 8; i++)
            {
                UnityEngine.UI.Button button = i < 5 ? (UnityEngine.UI.Button)bs.GetArrayElementAtIndex(i).objectReferenceValue : null;
                TMP_Text label;
                if (!button)
                {
                    var existing = panel.Find("Card" + i);
                    var go = existing ? existing.gameObject : new GameObject("Card" + i, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                    go.transform.SetParent(panel, false); button = go.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = go.GetComponent<UnityEngine.UI.Image>();
                    label = go.GetComponentInChildren<TMP_Text>();
                    if (!label) { var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)); text.transform.SetParent(go.transform, false); label = text.GetComponent<TMP_Text>(); label.font = title.font; }
                }
                else label = (TMP_Text)ls.GetArrayElementAtIndex(i).objectReferenceValue;
                var rect = (RectTransform)button.transform; rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
                rect.anchoredPosition = new Vector2((i % 4 - 1.5f) * 260, i < 4 ? 100 : -140); rect.sizeDelta = new Vector2(240, 220);
                button.GetComponent<UnityEngine.UI.Image>().color = new Color(0.07f, 0.11f, 0.16f);
                var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(0.65f, 1, 0.9f); colors.selectedColor = colors.highlightedColor; colors.disabledColor = new Color(0.7f, 0.75f, 0.8f); button.colors = colors;
                label.rectTransform.anchoredPosition = Vector2.zero; label.rectTransform.sizeDelta = new Vector2(216, 206); label.fontSize = 23; label.alignment = TextAlignmentOptions.Left; label.raycastTarget = false;
                bs.GetArrayElementAtIndex(i).objectReferenceValue = button; ls.GetArrayElementAtIndex(i).objectReferenceValue = label;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(view);
            var controls = view.transform.Find("Controls"); if (controls) controls.GetComponent<TMP_Text>().text = "WASD Move   SHIFT Sprint   E Pick up   TAB Inventory   LMB Attack   R Reload   1/2/5/6/7 Weapons   3 Heal   F Light";
            var hud = view.GetComponent<CharacterDemo>(); if (hud) QuickAccessUpgrade.Setup(hud);
        }

        [MenuItem("TwoDoors/Upgrade Combat Content")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first");
            string playerPath = "Assets/TwoDoors/Prefabs/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(playerPath);
            try { UpgradePlayer(root.GetComponent<PlayerCharacter>()); PrefabUtility.SaveAsPrefabAsset(root, playerPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            foreach (var kind in new[] { ItemKind.MachineGun, ItemKind.Shotgun, ItemKind.Machete })
            {
                var pickup = new GameObject("Pickup" + kind, typeof(BoxCollider), typeof(ItemPickup));
                var data = new SerializedObject(pickup.GetComponent<ItemPickup>()); data.FindProperty("kind").enumValueIndex = (int)kind; data.FindProperty("amount").intValue = 1; data.ApplyModifiedPropertiesWithoutUndo();
                var model = Weapon(pickup.transform, kind); model.SetActive(true);
                pickup.GetComponent<BoxCollider>().size = new Vector3(0.6f, 0.8f, 1.2f);
                PrefabUtility.SaveAsPrefabAsset(pickup, "Assets/TwoDoors/Prefabs/Pickup" + kind + ".prefab"); Object.DestroyImmediate(pickup);
            }
            foreach (var path in new[] { "Assets/CJW/Scenes/Game.unity", "Assets/TwoDoors/Scenes/CharacterTest.unity" })
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                if (scene.isDirty) throw new System.InvalidOperationException("Unsaved scene " + path);
                foreach (var r in scene.GetRootGameObjects())
                {
                    foreach (var player in r.GetComponentsInChildren<PlayerCharacter>(true))
                    {
                        UpgradePlayer(player);
                        var guard = player.GetComponent<ArenaContainment>();
                        if (!guard) guard = player.gameObject.AddComponent<ArenaContainment>();
                        guard.minimum = new Vector2(-11.05f, -10.05f); guard.maximum = new Vector2(11.05f, 14.05f);
                    }
                    foreach (var view in r.GetComponentsInChildren<InventoryView>(true)) UpgradeInventory(view);
                    foreach (var z in r.GetComponentsInChildren<ZombieEnemy>(true)) z.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = 5.76f;
                    foreach (var box in r.GetComponentsInChildren<BoxCollider>(true))
                    {
                        if (!box.name.EndsWith("Wall")) continue;
                        var size = box.size;
                        if (box.bounds.size.x < 1f) size.x = 1.2f / Mathf.Abs(box.transform.lossyScale.x);
                        if (box.bounds.size.z < 1f) size.z = 1.2f / Mathf.Abs(box.transform.lossyScale.z);
                        box.size = size;
                    }
                }
                if (path.Contains("CharacterTest"))
                {
                    int i = 0;
                    foreach (var kind in new[] { ItemKind.MachineGun, ItemKind.Shotgun, ItemKind.Machete })
                    {
                        bool exists = System.Array.Exists(scene.GetRootGameObjects(), r => r.name == "Pickup" + kind);
                        if (exists) { i++; continue; }
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TwoDoors/Prefabs/Pickup" + kind + ".prefab"), scene);
                        go.transform.position = new Vector3(-2 + i++ * 2, 1, -2.5f);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            var zombie = PrefabUtility.LoadPrefabContents("Assets/TwoDoors/Prefabs/Zombie.prefab");
            zombie.GetComponent<UnityEngine.AI.NavMeshAgent>().speed = 5.76f;
            PrefabUtility.SaveAsPrefabAsset(zombie, "Assets/TwoDoors/Prefabs/Zombie.prefab"); PrefabUtility.UnloadPrefabContents(zombie);
            TwoDoors.EditorTools.EventCatalogV2.Generate(); AssetDatabase.SaveAssets();
        }
    }
}
