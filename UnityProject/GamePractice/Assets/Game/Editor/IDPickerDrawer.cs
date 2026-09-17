using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sayne.Editor
{
    /// <summary>
    /// IDPickerAttribute 가 붙은 string 필드를 드롭다운으로 그린다.
    /// 값은 string 그대로 저장되므로 enum 처럼 번호가 밀려 데이터가 뒤바뀌는 사고가 없고,
    /// 고를 때는 상수 목록에서만 집을 수 있어 오타가 원천 차단된다.
    /// 목록에 없는 값이 이미 들어 있으면 지우지 않고 "(없는 ID)" 로 남겨 눈에 띄게 한다.
    /// </summary>
    [CustomPropertyDrawer(typeof(IDPickerAttribute), true)]
    public class IDPickerDrawer : PropertyDrawer
    {
        private const string EmptyLabel = "(없음)";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "IDPicker 는 string 필드에만 쓸 수 있다");
                return;
            }

            var picker = (IDPickerAttribute)attribute;
            var ids = CollectIDs(picker);

            var options = new List<string>();
            var values = new List<string>();

            if (picker.AllowEmpty)
            {
                options.Add(EmptyLabel);
                values.Add(string.Empty);
            }

            foreach (var id in ids)
            {
                options.Add(id);
                values.Add(id);
            }

            var current = property.stringValue ?? string.Empty;
            var index = values.IndexOf(current);

            if (index < 0)
            {
                // 목록에 없는 값 — 삭제하지 않고 맨 뒤에 붙여 둔 채로 보여준다.
                options.Add(string.IsNullOrEmpty(current) ? EmptyLabel : $"{current}  (없는 ID)");
                values.Add(current);
                index = values.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);

            var picked = EditorGUI.Popup(position, label.text, index, options.ToArray());
            if (picked != index)
            {
                property.stringValue = values[picked];
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 출처 타입의 public const string 을 선언 순서대로 긁어온다.
        /// EquipmentID 처럼 자리별 중첩 클래스로 나뉜 테이블도 통째로 고를 수 있게 안쪽까지 따라 들어간다.
        /// </summary>
        private static IEnumerable<string> CollectIDs(IDPickerAttribute picker)
        {
            if (picker.SourceType == null)
            {
                return System.Array.Empty<string>();
            }

            return Collect(picker.SourceType).ToArray();
        }

        private static IEnumerable<string> Collect(System.Type type)
        {
            var ids = type
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue())
                .Where(v => !string.IsNullOrEmpty(v));

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
            {
                ids = ids.Concat(Collect(nested));
            }

            return ids;
        }
    }
}
