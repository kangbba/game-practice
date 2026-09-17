using System;
using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>몸이 어떻게 움직이나 — 애니메이션 재생, 좌우 반전, 피격 연출.</summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterMotion : MonoBehaviour
    {
        /// <summary>이만큼 좌우로 움직여야 방향을 바꾼다. 위아래로만 걸을 때 안 뒤집히게 하는 값이다.</summary>
        private const float FacingThreshold = 0.35f;

        private const float HitRecoilDistance = 0.2f;
        private const float HitRecoilDuration = 0.18f;
        private const float HitFlashDuration = 0.12f;

        private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

        private readonly Dictionary<int, float> _clipLengths = new Dictionary<int, float>();

        private Animator _animator;
        private Character _character;
        private IDisposable _oneShotReturn;
        private bool _isPlayingOneShot;
        private Tween _hitRecoil;
        private Tween _hitFlash;
        private MaterialPropertyBlock _flashBlock;

        /// <summary>그림이 오른쪽을 보고 있나. 연출은 전부 이 값을 본다.</summary>
        public bool IsFacingRight { get; private set; } = true;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _flashBlock = new MaterialPropertyBlock();

            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                _clipLengths[Animator.StringToHash(clip.name)] = clip.length;
            }
        }

        /// <summary>Init 이 끝난 캐릭터가 불러준다. Awake 때는 전투·장비가 아직 없어서 여기서 묶는다.</summary>
        public void Bind(Character character)
        {
            _character = character;

            character.State
                .Subscribe(this, (state, self) => self.PlayState(state))
                .AddTo(this);

            character.Looked
                .Subscribe(this, (direction, self) => self.ApplyFacing(direction))
                .AddTo(this);

            character.Damaged
                .Subscribe(this, (_, self) => self.PlayHit())
                .AddTo(this);

            character.Combat.Attacked
                .Subscribe(this, (attack, self) => self.PlayOnce(attack.AnimationHash))
                .AddTo(this);
        }

        /// <summary>한 번짜리 모션을 전신으로 재생하고, 끝나면 현재 상태 모션으로 돌아간다.</summary>
        protected void PlayOnce(int stateHash, float startNormalized = 0f)
        {
            if (!_clipLengths.TryGetValue(stateHash, out var duration) ||
                !_animator.HasState(CharacterAnimations.BaseLayer, stateHash))
            {
                Debug.LogError($"{name}: 공격 상태 또는 클립이 없습니다 ({stateHash}).", this);
                return;
            }

            _animator.speed = CharacterAnimations.ActionPlaybackSpeed;
            _animator.Play(stateHash, CharacterAnimations.BaseLayer, startNormalized);

            // 다음 프레임을 기다리지 않고 그 자리에서 첫 포즈로 바꾼다.
            _animator.Update(0f);

            _oneShotReturn?.Dispose();
            _isPlayingOneShot = true;

            var remain = duration * (1f - startNormalized) / CharacterAnimations.ActionPlaybackSpeed;
            _oneShotReturn = Observable.Timer(TimeSpan.FromSeconds(remain))
                .Subscribe(this, (_, self) => self.ReturnToState())
                .AddTo(this);
        }

        private void OnDestroy()
        {
            _oneShotReturn?.Dispose();
            _hitRecoil?.Kill();
            _hitFlash?.Kill();
        }

        /// <summary>피격 연출. 그림만 뒤로 밀렸다 돌아오고 하얗게 번쩍인다. 실제 위치는 캐릭터가 그대로 들고 있다.</summary>
        private void PlayHit()
        {
            _hitRecoil?.Kill();
            transform.localPosition = Vector3.zero;

            // 그래픽은 좌우 반전으로 방향을 내므로, 로컬 -x 가 바라보는 쪽의 반대다.
            _hitRecoil = transform.DOPunchPosition(Vector3.left * HitRecoilDistance, HitRecoilDuration, 0, 0f);

            // 장비는 붙었다 떨어지므로 번쩍일 때마다 스프라이트를 다시 모은다.
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);

            _hitFlash?.Kill();
            _hitFlash = DOVirtual.Float(1f, 0f, HitFlashDuration, amount => ApplyFlash(renderers, amount));
        }

        /// <summary>머티리얼을 복제하지 않고 프로퍼티 블록으로만 밝기를 올린다.</summary>
        private void ApplyFlash(SpriteRenderer[] renderers, float amount)
        {
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(_flashBlock);
                _flashBlock.SetFloat(FlashAmount, amount);
                renderer.SetPropertyBlock(_flashBlock);
            }
        }

        private void ReturnToState()
        {
            _oneShotReturn = null;
            _isPlayingOneShot = false;

            PlayState(_character.State.CurrentValue);
        }

        private void PlayState(CharacterStateType state)
        {
            // 죽음·피격·걷기는 한 번짜리 모션을 끊고 들어간다.
            if (state == CharacterStateType.Hit || state == CharacterStateType.Death || state == CharacterStateType.Walk)
            {
                // 기술을 끊는 사유는 피격과 죽음뿐이다. 걷기는 캐스팅 중엔 아예 들어오지 않는다 — 이동 명령이 잠겨 있다.
                if (state != CharacterStateType.Walk)
                {
                    _character.Combat.CancelCast();
                }

                _oneShotReturn?.Dispose();
                _oneShotReturn = null;
                _isPlayingOneShot = false;
            }
            else if (_isPlayingOneShot)
            {
                return;
            }

            _animator.speed = 1f;
            _animator.Play(StateHash(state), CharacterAnimations.BaseLayer, 0f);
            _animator.Update(0f);
        }

        private void ApplyFacing(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) < FacingThreshold)
            {
                return;
            }

            IsFacingRight = direction.x > 0f;

            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (IsFacingRight ? 1f : -1f);
            transform.localScale = scale;
        }

        private static int StateHash(CharacterStateType state)
        {
            return state switch
            {
                CharacterStateType.Walk => CharacterAnimations.Walk,
                CharacterStateType.Hit => CharacterAnimations.Hit,
                CharacterStateType.Death => CharacterAnimations.Death,
                _ => CharacterAnimations.Idle
            };
        }
    }
}
