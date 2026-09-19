using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sayne
{
    /// <summary>
    /// 궁극기 연출의 주인. 궁극기를 누른 순간부터 화면이 완전히 돌아올 때까지를 한 흐름(UniTask 하나)으로 굴린다.
    ///
    /// ① 컷씬 — 게임이 멈춘 채 돈다.
    /// ② 무대 — 타임스케일이 돌아오면 주인공과 타겟을 궁극기용 레이어로 올리고, 궁극기 백그라운드를 1초에 걸쳐 어둡게 깐다.
    ///          카메라도 궁극기 시점으로 다가간다.
    /// ③ 본편 — 손에 든 무기(Weapon.PlayUltimateAsync)가 굽는다. 적의 죽음은 보류돼, HP 가 0 이 돼도 죽지 않고(Dying) 선 채로 맞는다.
    /// ④ 여운 — 궁극기가 끝나도 배경은 어두운 채로 둔다.
    /// ⑤ 죽음처리 — 보류를 풀어 Dying 인 적을 죽음처리하고, 죽는 모션이 끝날 때까지 기다린다. HP 가 남은 적은 그대로 선다.
    /// ⑥ 복귀 — 배경을 1초에 걸쳐 걷고 카메라를 평소 시점으로 돌린 뒤, 모두를 평소 레이어로 내린다.
    ///
    /// 연출 전체(복귀까지)가 도는 동안의 전역 상태는 IsPlaying 하나다. 시작과 끝에 여기서 명시적으로 켜고 끈다.
    /// 영웅 조작·적 AI·웨이브·UI 는 전부 이걸 본다 — 영웅이 본편 기술을 쓰는 중인지(Hero.IsUsingUltimate)와는 다른 이야기다.
    /// </summary>
    public class UltimateDirector : ManagerBase
    {
        // ---- 연출 시간표(초). 숫자를 여기서만 고치면 전체 호흡이 바뀐다. ----

        /// <summary>② ⑥ 궁극기 백그라운드가 깔리고 걷히는 시간.</summary>
        private const float BackgroundFadeSeconds = 1f;

        /// <summary>② 배경이 다 깔린 뒤 기술을 쓰기 전 숨 고르기.</summary>
        private const float StageSettleSeconds = 0.8f;

        /// <summary>④ 기술이 끝나고 죽음처리 전 여운.</summary>
        private const float AfterSkillSeconds = 1.2f;

        /// <summary>⑤ 죽는 모션이 끝나고 누워 있는 걸 보여주는 시간. 아무도 안 죽었으면 ⑤ 의 기다림 자체를 건너뛴다.</summary>
        private const float AfterDeathSeconds = 1.2f;

        /// <summary>③ 본편에서 무대 위 적이 한 대 맞을 때마다 화면이 흔들리는 세기(월드 단위)와 길이.</summary>
        private const float HitShakeStrength = 0.15f;
        private const float HitShakeSeconds = 0.12f;

        private readonly EnemyManager _enemyManager;
        private readonly ScreenPerformanceManager _screenPerformanceManager;

        private readonly ReactiveProperty<bool> _isPlaying = new ReactiveProperty<bool>();

        private UltimateBackground _background;

        /// <summary>궁극기 연출 중. 누른 순간부터 일상으로 완전히 돌아올 때까지 참. 영웅 조작·적 AI·웨이브·UI 가 이걸 본다.</summary>
        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        public UltimateDirector(EnemyManager enemyManager, ScreenPerformanceManager screenPerformanceManager)
        {
            _enemyManager = enemyManager;
            _screenPerformanceManager = screenPerformanceManager;
        }

        protected override void OnInit()
        {
            _background = UltimateBackground.Create(GameCamera.Camera);
        }

        protected override void OnRelease()
        {
            Object.Destroy(_background.gameObject);
            _isPlaying.Dispose();
        }

        /// <summary>
        /// 지금 궁극기를 쓸 수 있나. 쿨·잠금이 풀렸고, 든 무기의 궁극기 반경 안에 살아 있는 적이 하나라도 있어야 한다 —
        /// 무대에 오를 적이 없으면 쿨만 날린다. 버튼 켜짐·버튼 누름·자동 시전이 전부 이 하나를 본다.
        /// </summary>
        public bool CanPlay(Character caster)
        {
            return caster.Combat.CanUseUltimate
                && _enemyManager.HasAliveEnemyInRadius(caster.transform.position, caster.Combat.Weapon.UltimateRadius);
        }

        /// <summary>궁극기 연출 전체(①~⑥). 누른 순간부터 화면이 완전히 돌아올 때까지. 궁극기를 쓸 수 있는지는 부르는 쪽이 이미 확인했다.</summary>
        public async UniTaskVoid PlayUltimateSequenceAsync(Hero hero)
        {
            var token = LifeToken;
            _isPlaying.Value = true;

            // ① 컷씬
            await _screenPerformanceManager.PlayUltimateCutsceneAsync(hero, token);

            // ② 무대 — 지금 반경 안에 살아 있는 적을 이 순간 한 번 고정해 올린다.
            //          올린 적은 ⑥ 에서 다시 내려야 하므로, 그때까지 시체가 치워지지 않게 쥔다.
            var targets = _enemyManager.FindAliveEnemiesInRadius(hero.transform.position, hero.Combat.Weapon.UltimateRadius);
            var corpseHold = _enemyManager.HoldCorpses();
            hero.SetOnUltimateStage(true);
            _enemyManager.SetOnUltimateStage(targets, true);
            var stage = new UltimateStage(_enemyManager, hero, targets);
            stage.FaceNearest();
            GameCamera.Frame(CameraView.Ultimate, new List<Character>(targets) { hero });
            await _background.ShowAsync(BackgroundFadeSeconds, token);
            await DelayAsync(StageSettleSeconds, token);

            // ③ 본편 — 어떻게 때릴지는 든 무기가 정한다. 무대 위 적이 맞을 때마다 화면이 흔들린다.
            var deathHold = _enemyManager.HoldDeaths();
            using (targets.Select(target => target.Damaged).Merge()
                       .Subscribe(_ => GameCamera.Shake(HitShakeStrength, HitShakeSeconds)))
            {
                await hero.PlayUltimateAsync(stage, token);
            }

            // ④ 여운
            await DelayAsync(AfterSkillSeconds, token);

            // ⑤ 죽음처리
            deathHold.Dispose();
            var deathSeconds = _enemyManager.GetLongestDeathSeconds(targets);
            if (deathSeconds > 0f)
            {
                await DelayAsync(deathSeconds + AfterDeathSeconds, token);
            }

            // ⑥ 복귀
            GameCamera.SetView(CameraView.Battle);
            await _background.HideAsync(BackgroundFadeSeconds, token);
            hero.SetOnUltimateStage(false);
            _enemyManager.SetOnUltimateStage(targets, false);
            corpseHold.Dispose();

            _isPlaying.Value = false;
        }

        private static UniTask DelayAsync(float seconds, CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
        }
    }
}
