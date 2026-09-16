using System;
using System.Collections.Generic;
using R3;

namespace Sayne
{
    /// <summary>스폰된 히어로마다 HeroController 를 붙여 매 프레임 굴리고, 전투 HUD 버튼 입력을 연결한다.</summary>
    public class HeroControlManager : ManagerBase
    {
        private readonly HeroManager _heroManager;
        private readonly EnemyManager _enemyManager;
        private readonly BattlePhaseUIPanel _battlePanel;
        private readonly IMoveInputSource _moveSource;
        private readonly List<HeroController> _controllers = new List<HeroController>();

        public HeroControlManager(HeroManager heroManager, EnemyManager enemyManager, BattlePhaseUIPanel battlePanel)
        {
            _heroManager = heroManager;
            _enemyManager = enemyManager;
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
                        self._controllers.Add(new HeroController(hero, self._enemyManager, self._moveSource));
                    }
                })
                .RegisterTo(LifeToken);

            _battlePanel.AttackClicked
                .Subscribe(this, (_, self) => self.Dispatch(controller => controller.ManualAttack()))
                .RegisterTo(LifeToken);

            _battlePanel.UltimateClicked
                .Subscribe(this, (_, self) => self.Dispatch(controller => controller.UseUltimate()))
                .RegisterTo(LifeToken);

            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(this, (_, self) => self.UpdateControl())
                .RegisterTo(LifeToken);
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
                if (controller.Hero != null && controller.Hero.IsAlive.CurrentValue)
                {
                    action(controller);
                    return;
                }
            }
        }
    }
}
