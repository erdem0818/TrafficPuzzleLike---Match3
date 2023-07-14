using TMPro;

namespace Core.Scripts.Gameplay.UI
{
    public class CountTextHandler
    {
        private TextMeshProUGUI _countText;

        public CountTextHandler(TextMeshProUGUI countText)
        {
            _countText = countText;
        }

        public void SetText(int count) => _countText.text = $"{count}";
    }
}