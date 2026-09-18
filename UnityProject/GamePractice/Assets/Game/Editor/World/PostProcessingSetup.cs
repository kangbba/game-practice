using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Sayne.Editor
{
    /// <summary>
    /// 화면 후처리 값을 한 번에 맞춘다. 씬의 글로벌 볼륨이 물고 있는 프로파일을 손본다.
    ///
    /// 글로벌 볼륨은 "이 효과들을 화면 전체에 건다" 고 선언하는 빈 오브젝트다.
    /// 카메라의 Post Processing 이 켜져 있으면 그 카메라가 이 설정을 읽어 그림 위에 입힌다.
    /// </summary>
    public static class PostProcessingSetup
    {
        private const string ProfilePath = "Assets/Settings/SampleSceneProfile.asset";

        public static void Setup()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);

            RemoveEmpty(profile);

            Bloom(profile);
            Vignette(profile);
            Color(profile);
            Tone(profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            Debug.Log("화면 후처리 세팅 완료 — 블룸·비네트·색보정·톤매핑");
        }

        /// <summary>밝은 데가 번진다. 궁극기·불꽃 같은 밝은 연출이 살아난다.</summary>
        private static void Bloom(VolumeProfile profile)
        {
            var bloom = Get<Bloom>(profile);

            Set(bloom.threshold, 0.9f);
            Set(bloom.intensity, 0.55f);
            Set(bloom.scatter, 0.62f);
        }

        /// <summary>가장자리를 살짝 어둡게 해서 시선을 가운데로 모은다.</summary>
        private static void Vignette(VolumeProfile profile)
        {
            var vignette = Get<Vignette>(profile);

            Set(vignette.intensity, 0.28f);
            Set(vignette.smoothness, 0.4f);
        }

        /// <summary>채도와 대비를 조금 올려 색을 진하게. 2D 그림이 또렷해 보인다.</summary>
        private static void Color(VolumeProfile profile)
        {
            var color = Get<ColorAdjustments>(profile);

            Set(color.saturation, 12f);
            Set(color.contrast, 8f);
            Set(color.postExposure, 0.1f);
        }

        /// <summary>밝은 색이 하얗게 타는 걸 막는다. 블룸을 올렸으니 같이 켜 둔다.</summary>
        private static void Tone(VolumeProfile profile)
        {
            var tone = Get<Tonemapping>(profile);

            Set(tone.mode, TonemappingMode.Neutral);
        }

        /// <summary>
        /// 프로파일에 그 효과가 없으면 새로 넣는다.
        /// 볼륨 효과는 프로파일 에셋 안에 같이 저장되는 물건이라, 새로 만들면 파일에 넣어주기까지 해야 한다 —
        /// 안 그러면 목록에 빈 자리만 남는다.
        /// </summary>
        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>();
                component.name = typeof(T).Name;

                AssetDatabase.AddObjectToAsset(component, profile);
            }

            component.active = true;
            return component;
        }

        /// <summary>앞선 실행이 남긴 빈 자리를 걷어낸다.</summary>
        private static void RemoveEmpty(VolumeProfile profile)
        {
            profile.components.RemoveAll(component => component == null);
        }

        /// <summary>볼륨 값은 "덮어쓸지" 를 따로 켜야 실제로 먹는다.</summary>
        private static void Set<T>(VolumeParameter<T> parameter, T value)
        {
            parameter.overrideState = true;
            parameter.value = value;
        }
    }
}
