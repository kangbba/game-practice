using UnityEngine;

namespace Sayne
{
    /// <summary>캐릭터의 신원. 이름·초상화·고유색처럼 싸움과 무관하고 변하지 않는 것들. 에셋 이름 = 캐릭터 ID.</summary>
    [CreateAssetMenu(menuName = "Game/Character Profile", fileName = "CharacterProfile")]
    public class CharacterProfile : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _portrait;

        /// <summary>캐릭터를 상징하는 색. UI 강조와 연출에 쓴다.</summary>
        [SerializeField] private Color _themeColor = Color.white;

        public string DisplayName => _displayName;
        public Sprite Portrait => _portrait;
        public Color ThemeColor => _themeColor;
    }
}
