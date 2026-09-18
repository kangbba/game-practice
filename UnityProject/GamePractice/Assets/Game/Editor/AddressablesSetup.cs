using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// Game 폴더의 에셋을 스캔해 어드레서블로 등록한다. 주소 = 파일명, 라벨 = AssetAddresses 상수.
    /// 폴더에 에셋을 추가한 뒤 [Game > Setup Addressables] 를 실행하면 된다.
    /// </summary>
    public static class AddressablesSetup
    {
        private const string HeroesRoot = "Assets/Game/Characters/Heroes";
        private const string EnemiesRoot = "Assets/Game/Characters/Enemies";
        private const string ParticlesRoot = "Assets/Game/Particles";
        private const string MapsRoot = "Assets/Game/Maps";
        private const string EquipmentRoot = "Assets/Game/Equipment";

        public static void Setup()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.DefaultGroup;
            var count = 0;

            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.HeroesLabel);
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.EnemiesLabel);
            count += MarkFolder(settings, group, ParticlesRoot, AssetAddresses.ParticlesLabel);
            count += MarkFolder(settings, group, MapsRoot, AssetAddresses.MapsLabel);
            count += MarkEquipment(settings, group);

            // 프로필은 캐릭터 폴더에 같이 둔다. 에셋 이름 = 캐릭터 ID.
            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.ProfilesLabel, "t:CharacterProfile");
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.ProfilesLabel, "t:CharacterProfile");

            // 적 설계값도 적 폴더에 같이 둔다. 스탯·시작 장비 세트·싸움 방식·드랍이 전부 여기 한 장에 있다.
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.EnemyDataLabel, "t:EnemyData");

            // 히어로 설계값도 마찬가지로 히어로 폴더에 같이 둔다.
            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.HeroDataLabel, "t:HeroData");

            // 장비 설계값(아이템 카드)은 그 아이템 폴더에 프리팹과 같이 둔다.
            count += MarkFolder(settings, group, EquipmentRoot, AssetAddresses.EquipmentPlansLabel, "t:EquipmentPlan");

            count += Mark(settings, group, "Assets/Game/Drops/Art/Portraits/Gold.png", null);
            count += Mark(settings, group, "Assets/Game/Drops/Art/Portraits/Heal.png", null);
            count += Mark(settings, group, "Assets/Game/Drops/DropItem.prefab", null);

            count += Mark(settings, group, "Assets/Game/Loading/LoadingPanel.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/OverlayHPBar.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/WorldHPBar.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/DamageText.prefab", null);
            count += Mark(settings, group, "Assets/Game/ScreenPerformance/UltimateCutscenePanel.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/BattlePanel.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/Popup/GrowthWindow.prefab", null);
            count += Mark(settings, group, "Assets/Game/Equipment/UI/EquipmentWindow.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/Popup/FormationWindow.prefab", null);
            count += Mark(settings, group, "Assets/Game/ScreenPerformance/WaveStartPanel.prefab", null);
            count += Mark(settings, group, "Assets/Game/ScreenPerformance/LowHealthPanel.prefab", null);

            count += Mark(settings, group, "Assets/SayneAssets/UI/Speech/SpeechBubbleWidget.prefab", null);
            count += Mark(settings, group, "Assets/SayneAssets/UI/Speech/OverlaySpeechBubble.prefab", null);
            count += Mark(settings, group, "Assets/SayneAssets/UI/Speech/WorldSpeechBubble.prefab", null);

            AssetDatabase.SaveAssets();
            Debug.Log($"AddressablesSetup: {count} 개 에셋 등록 완료");
        }

        /// <summary>
        /// 장비 프리팹만 장비로 올린다 — 부위 스크립트(IEquipment)가 붙은 것만. 장비 폴더엔 화살처럼 장비가 아닌 프리팹도 있다.
        /// 주소는 프리팹 이름 = 장비 ID.
        /// </summary>
        private static int MarkEquipment(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            if (!settings.GetLabels().Contains(AssetAddresses.EquipmentLabel))
            {
                settings.AddLabel(AssetAddresses.EquipmentLabel);
            }

            var count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { EquipmentRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path).TryGetComponent<IEquipment>(out _))
                {
                    count += Mark(settings, group, path, AssetAddresses.EquipmentLabel);
                    continue;
                }

                // 예전에 장비로 잘못 올라간 것(화살 등)은 장비 라벨을 뗀다.
                var stale = settings.FindAssetEntry(guid);
                if (stale != null)
                {
                    stale.SetLabel(AssetAddresses.EquipmentLabel, false);
                }
            }

            return count;
        }

        private static int MarkFolder(AddressableAssetSettings settings, AddressableAssetGroup group,
            string root, string label, params string[] filters)
        {
            if (!settings.GetLabels().Contains(label))
            {
                settings.AddLabel(label);
            }

            var count = 0;
            if (filters.Length == 0)
            {
                filters = new[] { "t:Prefab" };
            }

            foreach (var filter in filters)
            {
                foreach (var guid in AssetDatabase.FindAssets(filter, new[] { root }))
                {
                    count += Mark(settings, group, AssetDatabase.GUIDToAssetPath(guid), label);
                }
            }

            return count;
        }

        private static int Mark(AddressableAssetSettings settings, AddressableAssetGroup group,
            string path, string label)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"AddressablesSetup: {path} 를 찾을 수 없다");
                return 0;
            }

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(path);

            if (label != null)
            {
                entry.SetLabel(label, true, true);
            }

            return 1;
        }
    }
}
