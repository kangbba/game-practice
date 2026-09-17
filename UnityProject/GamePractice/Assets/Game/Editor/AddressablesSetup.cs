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

        [MenuItem("★Sayne★/2. 어드레서블 셋업", false, 2)]
        public static void Setup()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.DefaultGroup;
            var count = 0;

            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.HeroesLabel);
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.EnemiesLabel);
            count += MarkFolder(settings, group, ParticlesRoot, AssetAddresses.ParticlesLabel);
            count += MarkFolder(settings, group, MapsRoot, AssetAddresses.MapsLabel);
            count += MarkFolder(settings, group, EquipmentRoot, AssetAddresses.EquipmentLabel);

            // 프로필은 캐릭터 폴더에 같이 둔다. 에셋 이름 = 캐릭터 ID.
            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.ProfilesLabel, "t:CharacterProfile");
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.ProfilesLabel, "t:CharacterProfile");

            // 적 설계값도 적 폴더에 같이 둔다. 스탯·한 벌·싸움 방식·드랍이 전부 여기 한 장에 있다.
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.EnemyPlansLabel, "t:EnemyPlan");

            // 히어로 설계값도 마찬가지로 히어로 폴더에 같이 둔다.
            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.HeroPlansLabel, "t:HeroPlan");

            // 장비 설계값도 장비 폴더에 같이 둔다. 자리·이름·스탯·무기 수치가 전부 여기 한 장에 있다.
            count += MarkFolder(settings, group, EquipmentRoot, AssetAddresses.EquipmentPlansLabel, "t:EquipmentPlan");

            count += MarkFolder(settings, group, "Assets/Game/Drops/Art/Portraits",
                AssetAddresses.DropPortraitsLabel, "t:Sprite");

            count += Mark(settings, group, "Assets/Game/Drops/DropItem.prefab", null);

            count += Mark(settings, group, "Assets/Game/UI/OverlayHPBar.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/DamageText.prefab", null);
            count += Mark(settings, group, "Assets/Game/UIDirection/UltimateCutscenePanel.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/BattlePanel.prefab", null);
            count += Mark(settings, group, "Assets/Game/UIDirection/UIPrefab_WaveStart.prefab", null);
            count += Mark(settings, group, "Assets/Game/UIDirection/UIPrefab_LowHealth.prefab", null);

            count += Mark(settings, group, "Assets/SayneAssets/UI/Tutorial/TutorialWidget.prefab", null);
            count += Mark(settings, group, "Assets/SayneAssets/UI/Tutorial/OverlaySpeechBubble.prefab", null);

            AssetDatabase.SaveAssets();
            Debug.Log($"AddressablesSetup: {count} 개 에셋 등록 완료");
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
