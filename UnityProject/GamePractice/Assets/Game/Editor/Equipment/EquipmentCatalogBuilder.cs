using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sayne.Editor
{
    public static class EquipmentCatalogBuilder
    {
        public const string Root = "Assets/Game/Equipment";

        // Same sprite is used on the character and in the inventory. PPU is independent of UI size.
        public static void BuildVisual(string id, EquipmentSlot slot)
        {
            var path = $"{Root}/Art/{id}.png";
            if (!File.Exists(path)) return;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var bounds = OpaqueBounds(sprite.texture);
            var folder = $"{Root}/{slot}";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var go = new GameObject(id);
            try
            {
                // A unit-high item with its opaque bottom at the neck/waist/foot anchor.
                // Socket transforms provide the real dimensions for each hero.
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("b8e9b7a04f8ad4ed28aee4490672b159"));
                float scale = 100f / bounds.height;
                go.transform.localScale = Vector3.one * scale;
                go.transform.localPosition = new Vector3(
                    (sprite.texture.width * .5f - bounds.center.x) / bounds.height,
                    (sprite.texture.height * .5f - bounds.yMin) / bounds.height, 0);
                PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/{id}.prefab");
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static Rect OpaqueBounds(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a > 16)
                { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
            return Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
        }

        /// <summary>
        /// 영웅 프리팹에 장비 자리를 구워 저장한다. 미리보기는 복사본에만 자리를 잡으므로,
        /// 게임에서 장비 그림이 보이려면 이걸 돌려 프리팹에 남겨야 한다. 여러 번 돌려도 결과는 같다.
        /// </summary>
        public static void BakeHeroes()
        {
            foreach (var hero in new[] { "Kage", "Aldric", "Nyx" })
            {
                var path = $"Assets/Game/Characters/Heroes/{hero}/{hero}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                FitHero(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("EquipmentCatalogBuilder: 영웅 3종에 장비 자리를 구웠다");
        }

        public static void FitHero(GameObject root)
        {
            var skin = root.GetComponentInChildren<CharacterSkin>(true);
            var so = new SerializedObject(skin);
            var entries = so.FindProperty("_slots");
            // Preserve the existing weapon binding, replace only catalog socket bindings.
            for (int i = entries.arraySize - 1; i >= 0; i--)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("_slot").enumValueIndex != 0)
                    entries.DeleteArrayElementAtIndex(i);
            var head = root.GetComponentsInChildren<Transform>().First(t => t.name == "Head");
            var baseHead = head.GetComponentsInChildren<SpriteRenderer>().First();
            var headBounds = LocalBounds(head, baseHead);
            // Closed helmets replace the baked head, preventing hair/masks protruding through metal.
            var anchor = Anchor(head, "EquipmentHelmet", new Vector3(headBounds.center.x, headBounds.min.y + .02f), headBounds.size.y * .9f);
            Add(entries, EquipmentSlot.Helmet, anchor, baseHead.sortingOrder + 1, head.GetComponentsInChildren<SpriteRenderer>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds LocalBounds(Transform parent, SpriteRenderer sr)
        {
            var b = sr.sprite.bounds;
            var min = parent.InverseTransformPoint(sr.transform.TransformPoint(b.min));
            var max = parent.InverseTransformPoint(sr.transform.TransformPoint(b.max));
            return new Bounds((min + max) * .5f, max - min);
        }

        private static Transform Anchor(Transform parent, string name, Vector3 position, float size)
        {
            var anchor = parent.Find(name);
            if (anchor == null) { anchor = new GameObject(name).transform; anchor.SetParent(parent, false); }
            anchor.localPosition = position;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one * size;
            return anchor;
        }

        private static void Add(SerializedProperty entries, EquipmentSlot slot, Transform anchor, int order, SpriteRenderer[] covered)
        {
            int index = entries.arraySize++;
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("_slot").enumValueIndex = (int)slot;
            entry.FindPropertyRelative("_transform").objectReferenceValue = anchor;
            entry.FindPropertyRelative("_overrideSortingOrder").boolValue = true;
            entry.FindPropertyRelative("_sortingOrder").intValue = order;
            var hidden = entry.FindPropertyRelative("_coveredSprites");
            hidden.arraySize = covered.Length;
            for (int i = 0; i < covered.Length; i++) hidden.GetArrayElementAtIndex(i).objectReferenceValue = covered[i];
        }
    }
}
