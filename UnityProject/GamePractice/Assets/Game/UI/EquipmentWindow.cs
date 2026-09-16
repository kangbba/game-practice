using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// 디아블로식 장비창. 좌측 = 인체 배치 슬롯(투구/몸통/양팔), 우측 = 후보 목록.
    /// 장착 표시의 진실의 원천은 캐릭터의 CharacterEquipment 이고, 이 창은 SetEquippedID 로 따라 그릴 뿐이다.
    /// 드랍은 대기 상태만 만들고, 적용 버튼을 눌러야 장착 요청(Applied)이 나간다.
    /// </summary>
    public class EquipmentWindow : MonoBehaviour
    {
        [SerializeField] private EquipmentSlotWidget _weaponSlot;
        [SerializeField] private RectTransform _candidatesRoot;
        [SerializeField] private EquipmentCandidateWidget _candidatePrefab;
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _closeButton;

        private readonly Dictionary<string, (string name, Sprite icon)> _catalog
            = new Dictionary<string, (string name, Sprite icon)>();

        private readonly Subject<string> _applied = new Subject<string>();

        private string _equippedWeaponID = string.Empty;
        private string _pendingWeaponID;
        private bool _hasPending;

        /// <summary>적용이 확정된 무기 ID. 빈 문자열 = 맨손.</summary>
        public Observable<string> Applied => _applied;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            _weaponSlot.Dropped
                .Subscribe(this, (weaponID, self) => self.SetPending(weaponID))
                .AddTo(this);

            _applyButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Apply())
                .AddTo(this);

            _closeButton.onClick.AsObservable()
                .Subscribe(this, (_, self) => self.Hide())
                .AddTo(this);
        }

        private void OnDestroy()
        {
            _applied.Dispose();
        }

        public void Show()
        {
            _hasPending = false;
            RefreshSlot();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>장비 상태(진실의 원천)가 바뀌면 여기로 들어온다. 빈 문자열 = 맨손.</summary>
        public void SetEquippedID(string weaponID)
        {
            _equippedWeaponID = weaponID ?? string.Empty;
            _hasPending = false;
            RefreshSlot();
        }

        public void SetCandidates(IReadOnlyList<(string weaponID, string displayName, Sprite icon)> items)
        {
            _catalog.Clear();

            for (var i = _candidatesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_candidatesRoot.GetChild(i).gameObject);
            }

            foreach (var (weaponID, displayName, icon) in items)
            {
                _catalog[weaponID] = (displayName, icon);
                Instantiate(_candidatePrefab, _candidatesRoot).Setup(weaponID, displayName, icon);
            }
        }

        private void SetPending(string weaponID)
        {
            _pendingWeaponID = weaponID;
            _hasPending = true;

            var (name, icon) = Lookup(weaponID);
            _weaponSlot.SetItem($"{name} (대기)", icon);
        }

        private void Apply()
        {
            if (!_hasPending)
            {
                return;
            }

            _hasPending = false;
            _applied.OnNext(_pendingWeaponID);
        }

        private void RefreshSlot()
        {
            var (name, icon) = Lookup(_equippedWeaponID);
            _weaponSlot.SetItem(name, icon);
        }

        private (string name, Sprite icon) Lookup(string weaponID)
        {
            var key = weaponID ?? string.Empty;
            return _catalog.TryGetValue(key, out var entry)
                ? entry
                : (string.IsNullOrEmpty(key) ? "맨손" : key, null);
        }
    }
}
