using System;
using System.Collections.Generic;
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
            var path = $"{ItemFolder(slot, id)}/{id}.png";
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
                Save(go, slot);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private const string HeroArtRoot = "Assets/DarkFantasy2D/Art/Heroes";
        private const string SpriteMaterialGuid = "b8e9b7a04f8ad4ed28aee4490672b159";
        private static readonly string[] Heroes = { "Kage", "Aldric", "Nyx" };
        private static readonly string[] Sides = { Boots.FrontSide, Boots.RearSide };

        /// <summary>영웅 아트에서 떼어 낸 몸통 장비의 ID. 그림은 그 영웅의 몸통 그림을 그대로 쓴다. 누구 것이든 아무나 입는다.</summary>
        private static readonly Dictionary<string, string> ChestIDs = new Dictionary<string, string>
        {
            { "Aldric", "AldricCoat" }, { "Kage", "KageArmor" }, { "Nyx", "NyxDress" }
        };

        /// <summary>
        /// 원본 다리 그림 안에서 신발이 있던 자리(왼쪽·아래 기준 픽셀). 다리 그림을 바지와 신발로 자를 때 나온 값이다.
        /// 영웅마다 원래 신발이 있던 이 자리에 신발 자리를 잡으므로, 그 아트의 신발을 신으면 원래 그림과 똑같이 놓인다.
        /// </summary>
        private static readonly Dictionary<string, RectInt> BootRects = new Dictionary<string, RectInt>
        {
            { "AldricFront", new RectInt(4, 0, 173, 145) },
            { "AldricRear", new RectInt(8, 0, 187, 157) },
            { "KageFront", new RectInt(17, 18, 243, 282) },
            { "KageRear", new RectInt(9, 24, 255, 267) },
            { "NyxFront", new RectInt(10, 2, 209, 193) },
            { "NyxRear", new RectInt(11, 2, 225, 203) },
        };

        /// <summary>영웅 아트에 그려져 있던 신발·몸통을 장비 프리팹으로 만든다. 여러 번 돌려도 결과는 같다.</summary>
        public static void BuildBodyVisuals()
        {
            foreach (var hero in Heroes)
            {
                BuildBoots(hero);
                BuildChest(hero);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 신발 한 켤레 = 앞발·뒷발 그림 두 장. 밑창 가운데를 원점에 두고 너비를 1 로 맞춘다 —
        /// 높이로 맞추면 목 짧은 신발이 목 긴 신발 자리에서 거인 신발이 된다.
        /// </summary>
        private static void BuildBoots(string hero)
        {
            var id = $"{hero}Boots";
            var go = new GameObject(id);
            try
            {
                foreach (var side in Sides)
                {
                    var sprite = ImportSprite($"{ItemFolder(EquipmentSlot.Boots, id)}/{id}_{side}.png");
                    var child = new GameObject(side);
                    child.transform.SetParent(go.transform, false);
                    AddRenderer(child, sprite);
                    child.transform.localScale = Vector3.one * (sprite.pixelsPerUnit / sprite.rect.width);
                    child.transform.localPosition = new Vector3(0f, .5f * sprite.rect.height / sprite.rect.width, 0f);
                }

                Save(go, EquipmentSlot.Boots);
            }
            finally { Object.DestroyImmediate(go); }
        }

        /// <summary>몸통 장비는 영웅 몸통 그림을 그대로 쓴다. 목(위쪽 가운데)을 원점에 두고 높이를 1 로 맞춘다.</summary>
        private static void BuildChest(string hero)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{HeroArtRoot}/{hero}/{hero}_Torso.png");
            var go = new GameObject(ChestIDs[hero]);
            try
            {
                AddRenderer(go, sprite);
                var scale = sprite.pixelsPerUnit / sprite.rect.height;
                go.transform.localScale = Vector3.one * scale;
                go.transform.localPosition = new Vector3(
                    -(sprite.rect.width * .5f - sprite.pivot.x) / sprite.rect.height,
                    -(sprite.rect.height - sprite.pivot.y) / sprite.rect.height, 0f);
                Save(go, EquipmentSlot.Chest);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static Sprite ImportSprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static SpriteRenderer AddRenderer(GameObject go, Sprite sprite)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteMaterialGuid));
            return renderer;
        }

        /// <summary>아이템 하나의 폴더. 그 아이템의 프리팹·설계값·그림이 여기 같이 있다 — 부위 폴더 아래 아이템마다 하나.</summary>
        public static string ItemFolder(EquipmentSlot slot, string id)
        {
            return $"{Root}/{SlotFolder(slot)}/{id}";
        }

        private static string SlotFolder(EquipmentSlot slot)
        {
            return slot == EquipmentSlot.MainHand ? "Weapon" : slot.ToString();
        }

        /// <summary>부위 스크립트를 붙여 아이템 폴더에 저장한다. 부위는 이 스크립트가 스스로 밝힌다.</summary>
        private static void Save(GameObject go, EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Helmet: go.AddComponent<Helmet>(); break;
                case EquipmentSlot.Chest: go.AddComponent<Armor>(); break;
                case EquipmentSlot.Boots: go.AddComponent<Boots>(); break;
                default: throw new System.ArgumentOutOfRangeException(nameof(slot), slot, "이 부위는 빌더가 굽지 않는다.");
            }

            var folder = ItemFolder(slot, go.name);
            Directory.CreateDirectory(folder);
            PrefabUtility.SaveAsPrefabAsset(go, $"{folder}/{go.name}.prefab");
        }

        private static Transform Bone(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
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
            foreach (var hero in Heroes)
            {
                var path = $"Assets/Game/Characters/Heroes/{hero}/{hero}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                FitHero(root, hero);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("EquipmentCatalogBuilder: 영웅 3종에 장비 자리를 구웠다");
        }

        public static void FitHero(GameObject root, string hero)
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
            FitChest(entries, root.transform);
            foreach (var side in Sides) FitBoots(entries, root.transform, hero, side);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 몸통 자리 = 원래 몸통 그림의 목에서 아래로 그 높이만큼. 몸통 그림은 통째로 옷이라 프리팹에서는 꺼 둔다 —
        /// 프리팹은 벗은 몸이고, 옷은 장비로만 입는다. 꺼 둔 그림은 자리의 치수를 재는 기준으로 남는다.
        /// </summary>
        private static void FitChest(SerializedProperty entries, Transform root)
        {
            var torso = Bone(root, "Torso");
            var skin = torso.Find("Skin").GetComponent<SpriteRenderer>();
            skin.enabled = false;
            var bounds = LocalBounds(torso, skin);
            var anchor = Anchor(torso, "EquipmentChest", new Vector3(bounds.center.x, bounds.max.y), bounds.size.y);
            Add(entries, EquipmentSlot.Chest, anchor, skin.sortingOrder, Array.Empty<SpriteRenderer>());
        }

        /// <summary>
        /// 신발 자리 = 원래 다리 그림 안에서 신발이 있던 곳의 밑창 가운데, 크기는 그 신발의 너비.
        /// 프리팹의 다리는 신발을 뗀 바지 그림이다 — 프리팹은 벗은 몸이고, 신발은 장비로만 신는다.
        /// </summary>
        private static void FitBoots(SerializedProperty entries, Transform root, string hero, string side)
        {
            var leg = Bone(root, $"{side}Leg");
            var skin = leg.Find("Skin").GetComponent<SpriteRenderer>();
            var stale = leg.Find("Pants");
            if (stale != null) Object.DestroyImmediate(stale.gameObject);

            // 바지 그림은 원래 다리 그림과 캔버스·피벗이 같아서, 갈아 끼워도 아래 치수 계산은 그대로다.
            skin.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{HeroArtRoot}/{hero}/{hero}_{side}LegPants.png");
            var sprite = skin.sprite;

            var rect = BootRects[hero + side];
            var min = leg.InverseTransformPoint(skin.transform.TransformPoint(
                (rect.xMin - sprite.pivot.x) / sprite.pixelsPerUnit, (rect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit, 0f));
            var max = leg.InverseTransformPoint(skin.transform.TransformPoint(
                (rect.xMax - sprite.pivot.x) / sprite.pixelsPerUnit, (rect.yMax - sprite.pivot.y) / sprite.pixelsPerUnit, 0f));

            var anchor = Anchor(leg, "EquipmentBoots", new Vector3((min.x + max.x) * .5f, min.y), max.x - min.x);
            Add(entries, EquipmentSlot.Boots, anchor, skin.sortingOrder + 1, Array.Empty<SpriteRenderer>(), side);
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

        private static void Add(SerializedProperty entries, EquipmentSlot slot, Transform anchor, int order,
            SpriteRenderer[] covered, string variant = "")
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
            entry.FindPropertyRelative("_variant").stringValue = variant;
        }
    }
}
