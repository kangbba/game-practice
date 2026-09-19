using UnityEditor;
using UnityEditor.SceneManagement;

namespace Sayne.Editor
{
    /// <summary>
    /// 프로젝트를 처음 열었을 때 게임 씬을 연다. 유니티는 마지막으로 연 씬을 Library 에 기억하는데 Library 는 레포에 없어서,
    /// 막 클론한 프로젝트는 이름 없는 빈 씬으로 뜬다 — 그 상태로 플레이하면 아무 일도 안 일어난다.
    /// 지금 열린 씬이 저장된 적 없는 빈 씬일 때만 연다. 다른 씬을 열어 둔 사람에게는 아무 일도 하지 않는다.
    /// </summary>
    internal static class DefaultSceneOpener
    {
        private const string GameScenePath = "Assets/Scenes/Game.unity";

        [InitializeOnLoadMethod]
        private static void Watch()
        {
            // 에디터가 다 뜬 뒤에 본다. 켜지는 도중엔 씬이 아직 안 정해졌다.
            EditorApplication.delayCall += OpenIfUntitled;
        }

        private static void OpenIfUntitled()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            var active = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path) || active.isDirty)
            {
                return;
            }

            EditorSceneManager.OpenScene(GameScenePath);
        }
    }
}
