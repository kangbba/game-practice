using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Sayne.Editor
{
    /// <summary>
    /// 에디터 밖에서 에디터 함수를 돌리는 문. tmp/editor/run.request 에 "네임스페이스.타입.메서드" 한 줄을 적어 두면
    /// 열린 에디터가 컴파일·임포트가 끝난 뒤 그 정적 메서드(인자 없음)를 한 번 부르고, 결과를 run.result 에 남긴 뒤 요청을 지운다.
    /// 메뉴를 손으로 누르지 않고도 빌더·점검을 돌릴 수 있게 하려고 둔다. 에디터는 포커스를 받아야 스크립트를 다시 컴파일한다.
    /// </summary>
    public static class EditorRequestRunner
    {
        private const string RequestPath = "tmp/editor/run.request";
        private const string ResultPath = "tmp/editor/run.result";

        [InitializeOnLoadMethod]
        private static void Watch()
        {
            EditorApplication.update -= Check;
            EditorApplication.update += Check;
        }

        private static void Check()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath)) return;

            var target = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);

            try
            {
                Find(target).Invoke(null, null);
                File.WriteAllText(ResultPath, $"OK {target}\n");
            }
            catch (Exception exception)
            {
                File.WriteAllText(ResultPath, $"FAIL {target}\n{(exception as TargetInvocationException)?.InnerException ?? exception}\n");
            }
        }

        /// <summary>"네임스페이스.타입.메서드" 에서 마지막 점 앞이 타입, 뒤가 메서드다. 공개·비공개 정적 메서드를 다 찾는다.</summary>
        private static MethodInfo Find(string target)
        {
            var split = target.LastIndexOf('.');
            var typeName = target.Substring(0, split);
            var methodName = target.Substring(split + 1);

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .First(found => found != null);

            return type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
