using UnityEditor;
using UnityEngine;

namespace Sayne
{
    public class KagePreviewWindow : EditorWindow
    {
        private static readonly string[] Heroes = { "Kage", "Aldric", "Nyx" };
        private int _currentHero;
        private string PrefabPath => $"Assets/Game/Characters/Heroes/{Heroes[_currentHero]}/{Heroes[_currentHero]}.prefab";
        private string AnimationPath => $"Assets/DarkFantasy2D/Animations/Heroes/{Heroes[_currentHero]}";

        private static readonly string[] Clips = { "Idle", "Walk", "Attack1", "Attack2", "Attack3", "Attack4", "Skill", "Hit", "Death", "Ultimate" };
        private PreviewRenderUtility _preview;
        private GameObject _hero;
        private GameObject _graphic;
        private AnimationClip _clip;
        private int _currentClip = 2;
        private float _currentTime;
        private double _lastTime;
        private bool _isPlaying = true;
        private bool _isFacingLeft;

        [MenuItem("★Sayne★/영웅/애니메이션 프리뷰", false, 101)]
        public static void Open()
        {
            GetWindow<KagePreviewWindow>("Hero Motion");
        }

        private void OnEnable()
        {
            _lastTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            _preview?.Cleanup();
            _preview = null;
        }

        private void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            if (_isPlaying && _clip != null)
            {
                _currentTime = (_currentTime + (float)(now - _lastTime)) % (_clip.length + .25f);
                Repaint();
            }
            _lastTime = now;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _currentHero = GUILayout.Toolbar(_currentHero, Heroes);
            if (EditorGUI.EndChangeCheck())
            {
                _preview?.Cleanup();
                _preview = null;
                _currentTime = 0f;
            }
            if (_preview == null && !InitPreview())
            {
                EditorGUILayout.HelpBox("영웅 프리팹과 애니메이션 에셋을 확인하세요.", MessageType.Info);
                return;
            }
            EditorGUI.BeginChangeCheck();
            _currentClip = GUILayout.Toolbar(_currentClip, Clips);
            if (EditorGUI.EndChangeCheck())
            {
                _currentTime = 0;
                LoadClip();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _isPlaying = GUILayout.Toggle(_isPlaying, "재생", "Button", GUILayout.Width(70));
                _isFacingLeft = GUILayout.Toggle(_isFacingLeft, "왼쪽 보기", "Button", GUILayout.Width(90));
                EditorGUI.BeginChangeCheck();
                var time = EditorGUILayout.Slider(Mathf.Min(_currentTime, _clip.length), 0, _clip.length);
                if (EditorGUI.EndChangeCheck()) _currentTime = time;
            }
            var rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type != EventType.Repaint) return;
            Sample();
            _preview.BeginPreview(rect, GUIStyle.none);
            _preview.Render(true);
            GUI.DrawTexture(rect, _preview.EndPreview(), ScaleMode.StretchToFill, false);
        }

        private bool InitPreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return false;
            _preview = new PreviewRenderUtility();
            _hero = _preview.InstantiatePrefabInScene(prefab);
            HeroActionAnimationBuilder.DressPreview(_hero.transform.Find("Graphic"), Heroes[_currentHero]);
            _graphic = _hero.transform.Find("Graphic").gameObject;
            _graphic.GetComponent<Animator>().enabled = false;
            SetupCamera(_preview.camera);
            LoadClip();
            return true;
        }

        private void LoadClip()
        {
            _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationPath}/{Clips[_currentClip]}.anim");
            var action = Clips[_currentClip];
            _preview.camera.orthographicSize = action == "Ultimate" ? 11f : action == "Skill" ? 8f : action == "Attack4" ? 4.5f : 2.8f;
            _preview.camera.transform.position = new Vector3(0f, action == "Ultimate" || action == "Skill" ? 0f : 1.35f, -10f);
        }

        private void Sample()
        {
            _clip.SampleAnimation(_graphic, Mathf.Min(_currentTime, _clip.length));
            _graphic.transform.localScale = new Vector3(_isFacingLeft ? -1 : 1, 1, 1);
        }

        private static void SetupCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = 1.85f;
            camera.transform.position = new Vector3(.20f, 1.35f, -10);
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .15f, .19f, 1);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30;
        }
    }
}
