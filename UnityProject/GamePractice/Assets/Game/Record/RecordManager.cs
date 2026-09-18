using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sayne
{
    /// <summary>
    /// 게임의 기록을 모으는 곳. 남의 일에 끼어들지 않고 보기만 한다 — 아무도 이 매니저를 향해 무언가를 하지 않는다.
    /// 그래서 무엇을 더 세고 싶어도 여기만 늘어나고 다른 매니저는 그대로다.
    ///
    /// 기록은 종류와 열쇠(적 ID 같은 것) 한 쌍으로 찾는다. 아직 한 번도 안 쌓인 기록도 0 으로 흐른다 —
    /// 퀘스트가 시작하자마자 구독할 수 있어야 하기 때문이다.
    /// </summary>
    public class RecordManager : ManagerBase
    {
        private readonly EnemyManager _enemyManager;
        private readonly GrowthManager _growthManager;
        private readonly CurrencyManager _currencyManager;
        private readonly WaveManager _waveManager;

        private readonly Dictionary<(RecordType type, string key), ReactiveProperty<long>> _records =
            new Dictionary<(RecordType, string), ReactiveProperty<long>>();

        /// <summary>초 아래를 버리면 시간이 영영 안 흐른다. 실제 누적은 여기에 두고 초가 넘어갈 때만 기록에 적는다.</summary>
        private float _playTime;

        public RecordManager(EnemyManager enemyManager, GrowthManager growthManager,
            CurrencyManager currencyManager, WaveManager waveManager)
        {
            _enemyManager = enemyManager;
            _growthManager = growthManager;
            _currencyManager = currencyManager;
            _waveManager = waveManager;
        }

        protected override void OnInit()
        {
            // 적 하나에 두 줄이 올라간다 — 그 적의 기록과, 열쇠 없는 통산 기록.
            _enemyManager.Died
                .Subscribe(this, (enemy, self) =>
                {
                    self.Add(RecordType.EnemyKill, 1, enemy.ID);
                    self.Add(RecordType.EnemyKill, 1);
                })
                .RegisterTo(LifeToken);

            _growthManager.Level
                .Subscribe(this, (level, self) => self.Raise(RecordType.LevelReach, level))
                .RegisterTo(LifeToken);

            _growthManager.ExpGained
                .Subscribe(this, (exp, self) => self.Add(RecordType.ExpEarned, exp))
                .RegisterTo(LifeToken);

            _currencyManager.GoldGained
                .Subscribe(this, (gold, self) => self.Add(RecordType.GoldEarned, gold))
                .RegisterTo(LifeToken);

            _waveManager.WaveStarted
                .Subscribe(this, (_, self) => self.Add(RecordType.WaveReach, 1))
                .RegisterTo(LifeToken);

            // 스케일 시간이라 중단 중에는 저절로 멈춘다.
            Observable.EveryUpdate(UnityFrameProvider.Update)
                .Subscribe(this, (_, self) => self.TickPlayTime())
                .RegisterTo(LifeToken);
        }

        protected override void OnRelease()
        {
            foreach (var record in _records.Values)
            {
                record.Dispose();
            }

            _records.Clear();
        }

        /// <summary>기록을 읽는 유일한 문. 열쇠를 비우면 그 종류의 통산값이다.</summary>
        public ReadOnlyReactiveProperty<long> Get(RecordType type, string key = null)
        {
            return GetRecord(type, key);
        }

        /// <summary>쌓이는 기록. 처치 수·번 골드처럼 더해지기만 하는 것들이다.</summary>
        private void Add(RecordType type, long amount, string key = null)
        {
            GetRecord(type, key).Value += amount;
        }

        /// <summary>최고값 기록. 레벨처럼 도달한 지점을 남기는 것들이라 내려가지 않는다.</summary>
        private void Raise(RecordType type, long value, string key = null)
        {
            var record = GetRecord(type, key);

            if (value > record.Value)
            {
                record.Value = value;
            }
        }

        private void TickPlayTime()
        {
            var before = (long)_playTime;
            _playTime += Time.deltaTime;

            var now = (long)_playTime;

            if (now != before)
            {
                GetRecord(RecordType.PlayTime, null).Value = now;
            }
        }

        private ReactiveProperty<long> GetRecord(RecordType type, string key)
        {
            var id = (type, key ?? string.Empty);

            if (!_records.TryGetValue(id, out var record))
            {
                record = new ReactiveProperty<long>();
                _records[id] = record;
            }

            return record;
        }
    }
}
