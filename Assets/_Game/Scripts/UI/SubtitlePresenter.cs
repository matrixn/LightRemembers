using UnityEngine;
using UnityEngine.UI;

namespace LightRemembers.UI
{
    [DisallowMultipleComponent]
    public sealed class SubtitlePresenter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text subtitleText;

        public void Configure(CanvasGroup group, Text text)
        {
            canvasGroup = group;
            subtitleText = text;
            Hide();
        }

        public void Show(string speaker, string line)
        {
            if (subtitleText != null) subtitleText.text = string.IsNullOrWhiteSpace(speaker) ? line : $"{speaker}\n{line}";
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (subtitleText != null) subtitleText.text = string.Empty;
        }
    }
}
