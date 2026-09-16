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
        private const string WeaponsRoot = "Assets/Game/Weapons";

        [MenuItem("★Sayne★/어드레서블 셋업")]
        public static void Setup()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.DefaultGroup;
            var count = 0;

            count += MarkFolder(settings, group, HeroesRoot, AssetAddresses.HeroesLabel);
            count += MarkFolder(settings, group, EnemiesRoot, AssetAddresses.EnemiesLabel);
            count += MarkFolder(settings, group, ParticlesRoot, AssetAddresses.ParticlesLabel);
            count += MarkFolder(settings, group, MapsRoot, AssetAddresses.MapsLabel);
            count += MarkFolder(settings, group, WeaponsRoot, AssetAddresses.WeaponsLabel);

            count += Mark(settings, group, "Assets/Game/UI/WorldHPBar.prefab", null);
            count += Mark(settings, group, "Assets/Game/UI/BattlePhaseUIPanel.prefab", null);

            AssetDatabase.SaveAssets();
            Debug.Log($"AddressablesSetup: {count} 개 에셋 등록 완료");
        }

        private static int MarkFolder(AddressableAssetSettings settings, AddressableAssetGroup group,
            string root, string label)
        {
            if (!settings.GetLabels().Contains(label))
            {
                settings.AddLabel(label);
            }

            var count = 0;
            foreach (var filter in new[] { "t:Prefab", "t:CharacterDefinition", "t:WeaponDefinition" })
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
