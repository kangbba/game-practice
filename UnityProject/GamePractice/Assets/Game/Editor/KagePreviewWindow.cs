using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sayne
{
    public class KagePreviewWindow : EditorWindow
    {
        private static readonly string[] Clips = { "Idle", "Walk", "Attack", "Hit", "Death", "Ultimate" };
        private PreviewRenderUtility _preview;
        private GameObject _hero;
        private GameObject _graphic;
        private AnimationClip _clip;
        private int _currentClip = 2;
        private float _currentTime;
        private double _lastTime;
        private bool _isPlaying = true;
        private bool _isFacingLeft;

        [MenuItem("★Sayne★/Kage/애니메이션 프리뷰")]
        public static void Open()
        {
            GetWindow<KagePreviewWindow>("Kage Preview");
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
            if (_preview == null && !InitPreview())
            {
                EditorGUILayout.HelpBox("Kage 프리팹을 먼저 생성하세요: Tools > Dark Fantasy > Kage > Rebuild Assets", MessageType.Info);
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KageBuilder.PrefabPath);
            if (prefab == null) return false;
            _preview = new PreviewRenderUtility();
            _hero = _preview.InstantiatePrefabInScene(prefab);
            _graphic = _hero.transform.Find("Graphic").gameObject;
            _graphic.GetComponent<Animator>().enabled = false;
            SetupCamera(_preview.camera);
            LoadClip();
            return true;
        }

        private void LoadClip()
        {
            _clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{KageBuilder.AnimationPath}/{Clips[_currentClip]}.anim");
        }

        private void Sample()
        {
            _clip.SampleAnimation(_graphic, Mathf.Min(_currentTime, _clip.length));
            _graphic.transform.localScale = new Vector3(_isFacingLeft ? -1 : 1, 1, 1);
        }

        private static void SetupCamera(Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = 1.65f;
            camera.transform.position = new Vector3(.15f, 1.20f, -10);
            camera.transform.rotation = Quaternion.identity;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.12f, .15f, .19f, 1);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 30;
        }

        public static void ExportAttackPreview()
        {
            var preview = new PreviewRenderUtility();
            var sheet = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KageBuilder.PrefabPath);
                var hero = preview.InstantiatePrefabInScene(prefab);
                var graphic = hero.transform.Find("Graphic").gameObject;
                graphic.GetComponent<Animator>().enabled = false;
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(KageBuilder.AnimationPath + "/Attack.anim");
                SetupCamera(preview.camera);
                var times = new[] { 0f, .075f, .115f, .155f, .21f, .50f };
                for (var i = 0; i < times.Length; i++)
                {
                    clip.SampleAnimation(graphic, times[i]);
                    preview.BeginStaticPreview(new Rect(0, 0, 512, 512));
                    preview.Render(true);
                    var frame = preview.EndStaticPreview();
                    sheet.SetPixels((i % 3) * 512, (1 - i / 3) * 512, 512, 512, frame.GetPixels());
                    DestroyImmediate(frame);
                }
                sheet.Apply();
                Directory.CreateDirectory("Temp/KageReview");
                File.WriteAllBytes("Temp/KageReview/Attack.png", sheet.EncodeToPNG());
            }
            finally
            {
                preview.Cleanup();
                DestroyImmediate(sheet);
            }
        }
    }
}
