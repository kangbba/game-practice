using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 캐릭터 모션을 에디터에서 그대로 돌려 본다.
    ///
    /// 볼 목록을 손으로 적어 두지 않고 <b>그 캐릭터의 컨트롤러 베이스 레이어</b>에서 읽는다 —
    /// 런타임이 Play 하는 것과 정확히 같은 목록이라, 여기서 도는 건 게임에서도 돌고
    /// 여기서 빈 칸이면 게임에서도 안 나온다. (전에는 목록이 박혀 있어서 Aldric·Nyx 의 Idle·Walk·Hit·Death 처럼
    /// 공용 폴더에 있는 클립을 못 찾고 빈 화면만 나왔다.)
    /// </summary>
    public class CharacterPreviewWindow : EditorWindow
    {
        private readonly struct Target
        {
            public readonly string Name;
            public readonly string Folder;
            public readonly bool IsHero;

            public Target(string name, string folder, bool isHero)
            {
                Name = name;
                Folder = folder;
                IsHero = isHero;
            }

            public string PrefabPath => $"Assets/Game/Characters/{Folder}/{Name}/{Name}.prefab";
        }

        private static readonly Target[] Targets =
        {
            new Target("Kage", "Heroes", true),
            new Target("Aldric", "Heroes", true),
            new Target("Nyx", "Heroes", true),
            new Target("Goblin", "Enemies", false),
            new Target("Ogre", "Enemies", false),
        };

        /// <summary>루프일 때 끝과 시작 사이에 두는 숨. 없으면 마지막 포즈를 볼 새가 없다.</summary>
        private const float TailPause = .25f;

        private PreviewRenderUtility _preview;
        private GameObject _graphic;

        private readonly List<(string State, AnimationClip Clip)> _motions = new List<(string, AnimationClip)>();
        private string[] _stateNames = System.Array.Empty<string>();

        private int _currentTarget;
        private int _currentMotion;
        private float _currentTime;
        private double _lastTime;

        private bool _isPlaying = true;
        private bool _isLooping = true;
        private bool _isFacingLeft;
        private float _zoom = 1f;

        private AnimationClip Clip => _currentMotion >= 0 && _currentMotion < _motions.Count ? _motions[_currentMotion].Clip : null;

        [MenuItem("★Sayne★/공통/애니메이션 프리뷰", false, 101)]
        public static void Open()
        {
            GetWindow<CharacterPreviewWindow>("Character Motion");
        }

        private void OnEnable()
        {
            _lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            Release();
        }

        private void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            var delta = (float)(now - _lastTime);
            _lastTime = now;

            var clip = Clip;
            if (!_isPlaying || clip == null) return;

            _currentTime += delta;

            if (_isLooping)
            {
                _currentTime %= clip.length + TailPause;
            }
            else if (_currentTime >= clip.length)
            {
                // 한 번만 볼 때는 마지막 포즈에서 선다 — 끝 자세가 궁금해서 루프를 끄는 거니까.
                _currentTime = clip.length;
                _isPlaying = false;
            }

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _currentTarget = GUILayout.Toolbar(_currentTarget, Targets.Select(target => target.Name).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                Release();
                _currentMotion = 0;
                _currentTime = 0f;
            }

            if (_preview == null && !InitPreview())
            {
                EditorGUILayout.HelpBox($"{Targets[_currentTarget].PrefabPath} 를 못 읽었다. 프리팹과 애니메이터를 확인해라.", MessageType.Warning);
                return;
            }

            if (_motions.Count == 0)
            {
                EditorGUILayout.HelpBox("컨트롤러 베이스 레이어에 상태가 없다.", MessageType.Warning);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _currentMotion = GUILayout.SelectionGrid(Mathf.Clamp(_currentMotion, 0, _motions.Count - 1), _stateNames, 6);
            if (EditorGUI.EndChangeCheck())
            {
                _currentTime = 0f;
                _isPlaying = true;
                FitCamera();
            }

            var clip = Clip;
            if (clip == null)
            {
                EditorGUILayout.HelpBox($"'{_motions[_currentMotion].State}' 상태에 모션이 안 걸려 있다 — 게임에서도 이 상태는 아무것도 안 보인다.", MessageType.Error);
                return;
            }

            DrawControls(clip);
            DrawInfo(clip);

            var rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type != EventType.Repaint) return;

            clip.SampleAnimation(_graphic, Mathf.Min(_currentTime, clip.length));
            _graphic.transform.localScale = new Vector3(_isFacingLeft ? -1f : 1f, 1f, 1f);

            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.Render(true);
            GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.StretchToFill, false);
        }

        private void DrawControls(AnimationClip clip)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _isPlaying = GUILayout.Toggle(_isPlaying, "재생", "Button", GUILayout.Width(60));
                _isLooping = GUILayout.Toggle(_isLooping, "루프", "Button", GUILayout.Width(60));
                _isFacingLeft = GUILayout.Toggle(_isFacingLeft, "왼쪽 보기", "Button", GUILayout.Width(80));

                if (GUILayout.Button("처음으로", GUILayout.Width(70)))
                {
                    _currentTime = 0f;
                }

                EditorGUI.BeginChangeCheck();
                var time = EditorGUILayout.Slider(Mathf.Min(_currentTime, clip.length), 0f, clip.length);
                if (EditorGUI.EndChangeCheck())
                {
                    _currentTime = time;
                    _isPlaying = false;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("줌", GUILayout.Width(24));
                EditorGUI.BeginChangeCheck();
                _zoom = EditorGUILayout.Slider(_zoom, .4f, 3f);
                if (EditorGUI.EndChangeCheck()) FitCamera();
            }
        }

        private void DrawInfo(AnimationClip clip)
        {
            var frames = Mathf.RoundToInt(clip.length * clip.frameRate);
            var line = $"{_currentTime:0.00}초 / 총 {clip.length:0.00}초 · {frames}프레임 @ {clip.frameRate:0}fps · 루프 {(clip.isLooping ? "켬" : "끔")}";

            var events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Length > 0)
            {
                line += $"\n이벤트 {events.Length}개: {string.Join(", ", events.Select(item => $"{item.functionName}@{item.time:0.00}초"))}";
            }

            EditorGUILayout.HelpBox(line, MessageType.None);
        }

        private bool InitPreview()
        {
            var target = Targets[_currentTarget];
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(target.PrefabPath);
            if (prefab == null) return false;

            _preview = new PreviewRenderUtility();
            var instance = _preview.InstantiatePrefabInScene(prefab);

            var graphic = instance.transform.Find("Graphic");
            if (graphic == null)
            {
                Release();
                return false;
            }

            // 영웅은 벗은 몸으로 구워져 있어서, 입히지 않으면 게임에서 보던 모습이 아니다. 적은 그대로가 완성이다.
            if (target.IsHero)
            {
                HeroActionAnimationBuilder.DressPreview(graphic, target.Name);
            }

            _graphic = graphic.gameObject;

            var animator = _graphic.GetComponent<Animator>();
            if (animator == null)
            {
                Release();
                return false;
            }

            animator.enabled = false;
            CollectMotions(animator);

            var camera = _preview.camera;
            camera.orthographic = true;
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .15f, .19f, 1f);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30f;

            FitCamera();
            return true;
        }

        /// <summary>컨트롤러 베이스 레이어의 상태를 그대로 목록으로 삼는다 — 런타임이 Play 하는 이름이 곧 이 이름이다.</summary>
        private void CollectMotions(Animator animator)
        {
            _motions.Clear();

            if (animator.runtimeAnimatorController is AnimatorController controller && controller.layers.Length > 0)
            {
                foreach (var child in controller.layers[CharacterAnimations.BaseLayer].stateMachine.states)
                {
                    _motions.Add((child.state.name, child.state.motion as AnimationClip));
                }
            }

            _stateNames = _motions.Select(motion => motion.Clip == null ? $"{motion.State} ✕" : motion.State).ToArray();
            _currentMotion = Mathf.Clamp(_currentMotion, 0, Mathf.Max(0, _motions.Count - 1));
        }

        private void FitCamera()
        {
            var clip = Clip;
            if (_preview == null || _graphic == null || clip == null) return;

            HeroActionAnimationBuilder.FitReviewCamera(_graphic, _preview.camera, clip);
            _preview.camera.orthographicSize /= _zoom;
        }

        private void Release()
        {
            _preview?.Cleanup();
            _preview = null;
            _graphic = null;
            _motions.Clear();
            _stateNames = System.Array.Empty<string>();
        }
    }
}
