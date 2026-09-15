using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    [RequireComponent(typeof(Animator))]
    public class CharacterGraphic : MonoBehaviour
    {
        /// <summary>공격은 사전모션을 건너뛰고 타격 직전부터 재생한다.</summary>
        private const float AttackStartNormalized = 0.4f;

        private readonly Dictionary<int, float> _clipLengths = new Dictionary<int, float>();

        private Animator _animator;
        private Character _character;
        private IDisposable _oneShotReturn;
        private int _oneShotLayer = -1;

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            foreach (var clip in _animator.runtimeAnimatorController.animationClips)
            {
                _clipLengths[Animator.StringToHash(clip.name)] = clip.length;
            }

            _character = GetComponentInParent<Character>();
            Bind(_character);
        }

        protected virtual void Bind(Character character)
        {
            character.State
                .Subscribe(this, (state, self) => self.PlayState(state))
                .AddTo(this);

            character.IsFacingRight
                .Subscribe(this, (isFacingRight, self) => self.ApplyFacing(isFacingRight))
                .AddTo(this);

            character.Attacked
                .Subscribe(this, (_, self) => self.PlayOnce(CharacterAnimations.Attack, AttackStartNormalized))
                .AddTo(this);

        }

        /// <summary>서 있으면 전신으로, 걷는 중이면 상체만 재생한다.</summary>
        protected void PlayOnce(int stateHash, float startNormalized = 0f)
        {
            var isMoving = _character.State.CurrentValue == CharacterState.Walk;
            var layer = isMoving ? CharacterAnimations.UpperBodyLayer : CharacterAnimations.BaseLayer;

            _animator.Play(stateHash, layer, startNormalized);

            _oneShotReturn?.Dispose();
            _oneShotLayer = layer;

            var remain = _clipLengths[stateHash] * (1f - startNormalized);
            _oneShotReturn = Observable.Timer(TimeSpan.FromSeconds(remain))
                .Subscribe((self: this, layer), (_, state) => state.self.ReturnFrom(state.layer))
                .AddTo(this);
        }

        private void ReturnFrom(int layer)
        {
            _oneShotReturn = null;
            _oneShotLayer = -1;

            if (layer == CharacterAnimations.UpperBodyLayer)
            {
                _animator.Play(CharacterAnimations.Empty, CharacterAnimations.UpperBodyLayer, 0f);
                return;
            }

            PlayState(_character.State.CurrentValue);
        }

        private void PlayState(CharacterState state)
        {
            if (_oneShotLayer == CharacterAnimations.BaseLayer
                && state != CharacterState.Death
                && state != CharacterState.Hit)
            {
                return;
            }

            _animator.Play(StateHash(state), CharacterAnimations.BaseLayer, 0f);
        }

        private void ApplyFacing(bool isFacingRight)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
            transform.localScale = scale;
        }

        private static int StateHash(CharacterState state)
        {
            return state switch
            {
                CharacterState.Walk => CharacterAnimations.Walk,
                CharacterState.Hit => CharacterAnimations.Hit,
                CharacterState.Death => CharacterAnimations.Death,
                _ => CharacterAnimations.Idle
            };
        }
    }
}
