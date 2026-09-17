using R3;
using TMPro;
using UnityEngine;

namespace Sayne
{
    /// <summary>Result 페이즈 동안만 보이는 정산 화면. 방금 끝난 웨이브 결과를 그린다.</summary>
    public class ResultPhaseUIPanel : PhaseUIBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _killText;
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private TextMeshProUGUI _goldText;

        public override string PhaseKey => PhaseID.Result;

        public void Bind(WaveManager waveManager, CurrencyManager currencyManager)
        {
            waveManager.LastResult
                .Subscribe(this, (result, self) => self.Draw(result))
                .RegisterTo(destroyCancellationToken);

            currencyManager.Gold
                .Subscribe(this, (gold, self) => self._goldText.text = $"보유 골드 {gold:N0}")
                .RegisterTo(destroyCancellationToken);
        }

        private void Draw(WaveResult result)
        {
            _titleText.text = $"{result.Wave.Label} 클리어";
            _killText.text = $"처치 {result.Kills}";
            _timeText.text = $"소요 {result.ClearSeconds:0.0}초";
        }
    }
}
