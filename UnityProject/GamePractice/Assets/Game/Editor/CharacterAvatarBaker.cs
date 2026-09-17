using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// 머리·머리카락·망토·스카프를 장비에서 몸으로 되돌린다. 프리팹의 소켓에 그림을 직접 붙여 굽고,
    /// CharacterSkin 의 자리 목록은 진짜 갈아입는 장비 소켓(무기)만 남긴다.
    /// 망토와 스카프는 그 히어로를 그 히어로로 보이게 하는 실루엣이지 바꿔 끼우는 물건이 아니다.
    /// 한 번 굽고 끝나는 작업이라 끝나면 MenuItem 만 뗀다.
    /// </summary>
    public static class CharacterAvatarBaker
    {
        /// <summary>프리팹에 구워 넣을 몸 그림. 소켓 이름 → 그림 프리팹 경로.</summary>
        private static readonly Dictionary<string, (string Socket, string Visual)[]> Avatars =
            new Dictionary<string, (string, string)[]>
            {
                ["Assets/Game/Characters/Heroes/Kage/Kage.prefab"] = new[]
                {
                    ("Head", "Assets/Game/Equipment/Head/FoxMaskHead.prefab"),
                    ("Cape", "Assets/Game/Equipment/Cape/NavyCape.prefab"),
                    ("Scarf", "Assets/Game/Equipment/Scarf/CrimsonScarf.prefab"),
                },
                ["Assets/Game/Characters/Heroes/Aldric/Aldric.prefab"] = new[]
                {
                    ("Head", "Assets/Game/Equipment/Head/SilverHead.prefab"),
                    ("Hair", "Assets/Game/Equipment/Hair/SilverHair.prefab"),
                    ("Cape", "Assets/Game/Equipment/Cape/VioletCape.prefab"),
                },
                ["Assets/Game/Characters/Heroes/Nyx/Nyx.prefab"] = new[]
                {
                    ("Head", "Assets/Game/Equipment/Head/WhiteTwinHead.prefab"),
                    ("Hair", "Assets/Game/Equipment/Hair/WhiteHair.prefab"),
                    ("Cape", "Assets/Game/Equipment/Cape/BlackCape.prefab"),
                },
                ["Assets/Game/Characters/Enemies/Goblin/Goblin.prefab"] = new (string, string)[0],
                ["Assets/Game/Characters/Enemies/Ogre/Ogre.prefab"] = new (string, string)[0],
            };

        /// <summary>장비가 붙는 소켓만 남긴다. 여기 없는 소켓은 몸이라 자리 목록에서 빠진다.</summary>
        private static readonly Dictionary<string, EquipmentSlot> Sockets = new Dictionary<string, EquipmentSlot>
        {
            ["Weapon"] = EquipmentSlot.MainHand,
        };

        [MenuItem("★Sayne★/1. 캐릭터 아바타 굽기", false, 1)]
        public static void Bake()
        {
            foreach (var (prefabPath, avatar) in Avatars)
            {
                var root = PrefabUtility.LoadPrefabContents(prefabPath);

                foreach (var (socketName, visualPath) in avatar)
                {
                    BakeVisual(root.transform, socketName, visualPath);
                }

                RebuildSlots(root);

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"CharacterAvatarBaker: 캐릭터 {Avatars.Count} 종의 아바타를 프리팹에 구웠다");
        }

        private static void BakeVisual(Transform root, string socketName, string visualPath)
        {
            var socket = Find(root, socketName);
            if (socket == null)
            {
                Debug.LogWarning($"CharacterAvatarBaker: {socketName} 소켓이 없다 — {visualPath} 는 건너뛴다");
                return;
            }

            // 이미 구운 프리팹을 다시 구워도 그림이 겹치지 않게 소켓을 비우고 붙인다.
            for (var i = socket.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(socket.GetChild(i).gameObject);
            }

            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            var baked = Object.Instantiate(visual, socket, false);
            baked.name = visual.name;
        }

        /// <summary>몸 소켓을 빼고, 남은 장비 소켓을 새 자리 값으로 다시 적는다.</summary>
        private static void RebuildSlots(GameObject root)
        {
            var skin = root.GetComponentInChildren<CharacterSkin>(true);
            var so = new SerializedObject(skin);
            var slots = so.FindProperty("_slots");

            var kept = new List<(EquipmentSlot Slot, Transform Socket)>();

            foreach (var (socketName, slot) in Sockets)
            {
                var socket = Find(root.transform, socketName);
                if (socket != null)
                {
                    kept.Add((slot, socket));
                }
            }

            slots.arraySize = kept.Count;

            for (var i = 0; i < kept.Count; i++)
            {
                var element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_slot").enumValueIndex = (int)kept[i].Slot;
                element.FindPropertyRelative("_transform").objectReferenceValue = kept[i].Socket;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
