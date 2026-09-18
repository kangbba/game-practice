using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
    /// ③ 본편 — 손에 든 무기(Weapon.PlayUltimateAsync)가 굽는다. 적의 죽음은 보류돼, HP 가 0 이 돼도 쓰러지지 않고 선 채로 맞는다.
    /// ④ 여운 — 궁극기가 끝나도 배경은 어두운 채로 둔다.
    /// ⑤ 쓰러짐 — 보류를 풀어 HP 0 인 적을 죽음처리하고, 쓰러지는 모션이 끝날 때까지 기다린다. HP 가 남은 적은 그대로 선다.
    /// ⑥ 복귀 — 배경을 1초에 걸쳐 걷고 카메라를 평소 시점으로 돌린 뒤, 모두를 평소 레이어로 내린다.
    ///
    /// 연출 전체(복귀까지)가 도는 동안의 전역 상태는 IsPlaying 하나다. 시작과 끝에 여기서 명시적으로 켜고 끈다.
    /// 적 AI·웨이브·UI 는 전부 이걸 구독한다 — 캐릭터가 궁극기를 쓰는 중인지(조작 잠금)와는 다른 이야기다.
    /// </summary>
    public class UltimateDirector : ManagerBase
    {
        // ---- 연출 시간표(초). 숫자를 여기서만 고치면 전체 호흡이 바뀐다. ----

        /// <summary>② ⑥ 궁극기 백그라운드가 깔리고 걷히는 시간.</summary>
        private const float BackgroundFadeSeconds = 1f;

        /// <summary>② 배경이 다 깔린 뒤 기술을 쓰기 전 숨 고르기.</summary>
        private const float StageSettleSeconds = 0.8f;

        /// <summary>④ 기술이 끝나고 쓰러뜨리기 전 여운.</summary>
        private const float AfterSkillSeconds = 1.2f;

        /// <summary>⑤ 다 쓰러진 뒤 누워 있는 걸 보여주는 시간. 아무도 안 쓰러졌으면 ⑤ 자체를 건너뛴다.</summary>
        private const float FallenLingerSeconds = 1.2f;

        /// <summary>궁극기 백그라운드가 다 깔렸을 때의 색. 알파 244 라 맵이 아주 희미하게만 비친다.</summary>
        private static readonly Color BackgroundColor = new Color32(0x0E, 0x00, 0x24, 244);

        /// <summary>무대에 올릴 적의 반경. 궁극기 무기가 닿는 끝과 같다.</summary>
        private const float StageRadius = 10f;

        private const string BackgroundName = "UltimateBackground";

        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly CameraManager _cameraManager;
        private readonly ScreenPerformanceManager _screenPerformanceManager;

        private readonly ReactiveProperty<bool> _isPlaying = new ReactiveProperty<bool>();

        private SpriteRenderer _background;

        /// <summary>궁극기 연출 중. 누른 순간부터 일상으로 완전히 돌아올 때까지 참. 적 AI·웨이브·UI 가 이걸 본다.</summary>
        public ReadOnlyReactiveProperty<bool> IsPlaying => _isPlaying;

        public UltimateDirector(HeroManager heroManager, EnemyManager enemyManager, CameraManager cameraManager,
            ScreenPerformanceManager screenPerformanceManager)
        {
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _cameraManager = cameraManager;
            _screenPerformanceManager = screenPerformanceManager;
        }

        protected override void OnInit()
        {
            CreateBackground();

            _heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        hero.Combat.UltimateRequested
                            .Subscribe((self, hero), (_, state) => state.self.PlayAsync(state.hero).Forget())
                            .RegisterTo(hero.destroyCancellationToken);
                    }
                })
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            Object.Destroy(_background.gameObject);
            _isPlaying.Dispose();
        }

        /// <summary>
        /// 궁극기 백그라운드. 카메라 자식으로 붙어 화면을 꽉 채우는 판 한 장이다.
        /// 그려지는 순서는 거리가 아니라 정렬 레이어가 정한다 — 맵·평소 캐릭터 위, 무대 위 캐릭터 아래.
        /// </summary>
        private void CreateBackground()
        {
            var texture = Texture2D.whiteTexture;
            var card = new GameObject(BackgroundName, typeof(SpriteRenderer));
            card.transform.SetParent(_cameraManager.Camera.transform, false);

            _background = card.GetComponent<SpriteRenderer>();
            _background.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), texture.width);
            _background.sortingLayerName = SortingLayers.UltimateBackground;
            _background.color = WithAlpha(0f);
            _background.enabled = false;

            card.AddComponent<SkyCard>();
        }

        private async UniTaskVoid PlayAsync(Hero hero)
        {
            var token = LifeToken;
            _isPlaying.Value = true;

            // ① 컷씬
            await _screenPerformanceManager.PlayUltimateCutsceneAsync(hero, token);

            // ② 무대 — 올린 적은 ⑥ 에서 다시 내려야 하므로, 그때까지 시체가 치워지지 않게 쥔다.
            var targets = CollectTargets(hero);
            var corpseHold = _enemyManager.HoldCorpses();
            Stage(hero, targets, true);
            var stage = new UltimateStage(hero, targets);
            stage.FaceNearest();
            _cameraManager.SetView(CameraView.Ultimate);
            await FadeBackgroundAsync(BackgroundColor.a, token);
            await DelayAsync(StageSettleSeconds, token);

            // ③ 본편 — 어떻게 때릴지는 든 무기가 정한다
            var deathHold = _enemyManager.HoldDeaths();
            await hero.WornWeapon.PlayUltimateAsync(stage, token);

            // ④ 여운
            await DelayAsync(AfterSkillSeconds, token);

            // ⑤ 쓰러짐
            deathHold.Dispose();
            var fallSeconds = LongestFall(targets);
            if (fallSeconds > 0f)
            {
                await DelayAsync(fallSeconds + FallenLingerSeconds, token);
            }

            // ⑥ 복귀
            _cameraManager.SetView(CameraView.Battle);
            await FadeBackgroundAsync(0f, token);
            Stage(hero, targets, false);
            corpseHold.Dispose();

            hero.Combat.EndUltimate();
            _isPlaying.Value = false;
        }

        /// <summary>무대에 올릴 적. 지금 서 있는 적 중 반경 안에 든 것만, 이 순간 한 번 고정한다.</summary>
        private List<Enemy> CollectTargets(Hero hero)
        {
            var targets = new List<Enemy>();

            foreach (var enemy in _enemyManager.CurrentEnemies)
            {
                var offset = enemy.transform.position - hero.transform.position;
                offset.y = 0f;

                if (enemy.IsAlive && offset.magnitude <= StageRadius)
                {
                    targets.Add(enemy);
                }
            }

            return targets;
        }

        /// <summary>주인공과 타겟을 궁극기용 레이어로 올리거나 평소 레이어로 내린다. 배경은 올릴 때 켜고 내릴 때 끈다.</summary>
        private void Stage(Hero hero, List<Enemy> targets, bool isOn)
        {
            hero.SetOnUltimateStage(isOn);

            foreach (var target in targets)
            {
                target.SetOnUltimateStage(isOn);
            }

            _background.enabled = isOn;
        }



        /// <summary>이번에 죽음처리된 적 중 가장 오래 쓰러지는 모션 길이. 아무도 안 죽었으면 0.</summary>
        private static float LongestFall(List<Enemy> targets)
        {
            var longest = 0f;

            foreach (var target in targets)
            {
                if (target.IsDead)
                {
                    longest = Mathf.Max(longest, target.DeathSeconds);
                }
            }

            return longest;
        }

        private UniTask FadeBackgroundAsync(float alpha, CancellationToken token)
        {
            var done = new UniTaskCompletionSource();

            DOVirtual.Float(_background.color.a, alpha, BackgroundFadeSeconds,
                    value => _background.color = WithAlpha(value))
                .SetEase(Ease.InOutSine)
                .SetLink(_background.gameObject)
                .OnComplete(() => done.TrySetResult());

            return done.Task.AttachExternalCancellation(token);
        }

        private static Color WithAlpha(float alpha)
        {
            var color = BackgroundColor;
            color.a = alpha;
            return color;
        }

        private static UniTask DelayAsync(float seconds, CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
        }
    }
}
