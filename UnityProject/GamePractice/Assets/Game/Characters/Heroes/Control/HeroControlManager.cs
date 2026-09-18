using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>스폰된 히어로마다 HeroController 를 붙여 매 프레임 굴리고, 전투 HUD 버튼 입력을 연결한다.</summary>
    public class HeroControlManager : ManagerBase
    {
        private readonly PauseManager _pauseManager;
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly UltimateDirector _ultimateDirector;
        private readonly BattlePanel _battlePanel;
        private readonly IMoveInputSource _moveSource;
        private readonly List<HeroController> _controllers = new List<HeroController>();

        public HeroControlManager(PauseManager pauseManager, HeroManager heroManager, EnemyManager enemyManager,
            UltimateDirector ultimateDirector, BattlePanel battlePanel)
        {
            _pauseManager = pauseManager;
            _heroManager = heroManager;
            _enemyManager = enemyManager;
            _ultimateDirector = ultimateDirector;
            _battlePanel = battlePanel;

            // 입력 우선순위: 조이스틱을 잡고 있으면 조이스틱, 아니면 키보드, 둘 다 조용하면 자동사냥.
            _moveSource = new CompositeMoveSource(
                new JoystickMoveSource(battlePanel.Joystick),
                new KeyboardMoveSource());
        }

        protected override void OnInit()
        {
            _heroManager.Spawned
                .Subscribe(this, (character, self) =>
                {
                    if (character is Hero hero)
                    {
                        self._controllers.Add(new HeroController(hero, self._enemyManager, self._ultimateDirector, self._moveSource));
                    }
                })
                .RegisterTo(LifeToken);

            // 중단 중에도 Update 와 버튼 클릭은 들어온다 — 멈춘 게임에 조작이 먹으면 안 된다.
            // 궁극기 연출(컷씬부터 복귀까지) 동안도 마찬가지다. 이동·평타·스킬 어느 커맨드도 받지 않는다.
            _battlePanel.SkillClicked
                .Where(this, (_, self) => self.CanControl())
                .Subscribe(this, (_, self) => self.Dispatch(controller => controller.UseSkill()))
                .RegisterTo(LifeToken);

            _battlePanel.UltimateClicked
                .Where(this, (_, self) => self.CanControl())
                .Subscribe(this, (_, self) => self.Dispatch(controller => controller.UseUltimate()))
                .RegisterTo(LifeToken);

            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Where(this, (_, self) => self.CanControl())
                .Subscribe(this, (_, self) => self.UpdateControl())
                .RegisterTo(LifeToken);
        }

        private bool CanControl()
        {
            return !_pauseManager.IsPaused.CurrentValue && !_ultimateDirector.IsPlaying.CurrentValue;
        }

        protected override void OnRelease()
        {
            _controllers.Clear();
        }

        private void UpdateControl()
        {
            _controllers.RemoveAll(controller => controller.Hero == null);

            foreach (var controller in _controllers)
            {
                controller.UpdateControl();
            }
        }

        private void Dispatch(Action<HeroController> action)
        {
            foreach (var controller in _controllers)
            {
                if (controller.Hero != null && controller.Hero.IsAlive)
                {
                    action(controller);
                    return;
                }
            }
        }
    }
}
