using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryIntegrityDisplay : MonoBehaviour
    {
        [SerializeField] private MemoryIntegrity integrity;
        [SerializeField] private Text segmentText;
        [SerializeField] private Color activeColor = new Color(1f, 0.78f, 0.38f);
        [SerializeField] private Color lostColor = new Color(0.28f, 0.3f, 0.34f);
        private readonly StringBuilder _text = new StringBuilder(128);

        public void Configure(MemoryIntegrity source, Text text)
        {
            if (integrity != null)
                integrity.Changed -= OnIntegrityChanged;
            integrity = source;
            segmentText = text;
            if (isActiveAndEnabled && integrity != null)
            {
                integrity.Changed -= OnIntegrityChanged;
                integrity.Changed += OnIntegrityChanged;
            }
            Refresh(integrity != null ? integrity.CurrentSegments : 0,
                integrity != null ? integrity.MaximumSegments : 0);
        }

        private void OnEnable()
        {
            if (integrity != null)
            {
                integrity.Changed -= OnIntegrityChanged;
                integrity.Changed += OnIntegrityChanged;
            }
            Refresh(integrity != null ? integrity.CurrentSegments : 0,
                integrity != null ? integrity.MaximumSegments : 0);
        }

        private void OnDisable()
        {
            if (integrity != null)
                integrity.Changed -= OnIntegrityChanged;
        }

        private void OnIntegrityChanged(int current, int maximum) => Refresh(current, maximum);

        private void Refresh(int current, int maximum)
        {
            if (segmentText == null)
                return;
            _text.Clear();
            for (var i = 0; i < maximum; i++)
            {
                if (i > 0) _text.Append("  ");
                var color = i < current ? ColorUtility.ToHtmlStringRGB(activeColor) : ColorUtility.ToHtmlStringRGB(lostColor);
                _text.Append("<color=#").Append(color).Append(">●</color>");
            }
            segmentText.text = _text.ToString();
        }
    }
}
