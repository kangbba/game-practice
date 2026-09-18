using UnityEngine;
using UnityEngine.UI;

namespace Sayne
{
    /// <summary>
    /// UI 에 캐릭터를 비추는 작은 무대. 비추는 일 자체는 GameObjectImage 가 하고,
    /// 여기서는 "인형은 이렇게 세운다" 만 안다 — 빌보드를 눕히지 않고, 숨 쉬는 모션을 직접 틀고, 장비를 갈아입힌다.
    ///
    /// 인형은 Init 을 안 거친 빈 몸이라 싸우지도 움직이지도 않는다. 무엇을 걸칠지는 바깥에서 Wear 로 알려준다.
    /// 프리팹이 오른쪽을 보고 있으므로 인형도 오른쪽을 본다.
    /// </summary>
    public class CharacterPreviewStage
    {
        /// <summary>발끝이 원점인 몸을 화면 가운데로 올리는 카메라 자리와 담는 크기.</summary>
        private static readonly Vector3 ViewOffset = new Vector3(0f, 1.1f, -10f);
        private const float ViewSize = 1.6f;

        private readonly GameObjectImage _image;

        private CharacterSkin _dollSkin;

        public CharacterPreviewStage(RawImage image)
        {
            // 창을 다시 열어도 무대는 하나다.
            _image = image.GetComponent<GameObjectImage>() ?? image.gameObject.AddComponent<GameObjectImage>();
            _image.SetView(ViewOffset, Vector3.zero);
            _image.SetOrthographic(ViewSize);

            image.enabled = true;
        }

        /// <summary>창이 열려 있는 동안만 살아 움직인다. 닫혀 있으면 마지막 모습이 그대로 멈춘다.</summary>
        public void SetActive(bool isActive)
        {
            _image.IsLive = isActive;
        }

        /// <summary>무대에 세울 몸을 바꾼다. 장비는 벗은 채로 서므로 이어서 Wear 로 입혀야 한다.</summary>
        public void SetDoll(Hero prefab)
        {
            var doll = _image.Show(prefab.gameObject);
            doll.name = "Doll";

            // 전장 카메라를 따라 눕는 빌보드는 무대에선 필요 없다 — 무대 카메라는 정면에서 본다.
            var billboard = doll.GetComponentInChildren<Billboard>();
            billboard.enabled = false;
            billboard.transform.localRotation = Quaternion.identity;

            // 인형은 Init 을 안 거친 빈 몸이라 아무도 상태를 정해주지 않는다. 숨 쉬는 모션은 여기서 직접 틀어 둔다.
            // 창이 열린 동안 게임은 멈춰 있으므로 시간도 스케일 밖의 것을 쓴다.
            var animator = doll.GetComponentInChildren<Animator>();
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.Play(CharacterAnimations.Idle, CharacterAnimations.BaseLayer, 0f);

            _dollSkin = doll.GetComponentInChildren<CharacterSkin>();
        }

        /// <summary>그 자리에 이 그림을 걸친다. 그림이 없는 장비(맨손 등)는 벗은 것과 같다.</summary>
        public void Wear(EquipmentSlot slot, GameObject visual)
        {
            if (visual == null)
            {
                _dollSkin.TakeOff(slot);
                return;
            }

            _dollSkin.Wear(slot, visual);
        }
    }
}
