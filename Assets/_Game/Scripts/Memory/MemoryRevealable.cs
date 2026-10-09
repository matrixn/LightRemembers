using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryRevealable : MonoBehaviour, IMemoryLightTarget
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color neutralColor = new Color(0.42f, 0.42f, 0.42f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color revealedColor = new Color(1.8f, 0.95f, 0.28f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private bool _isRevealed;

        public bool IsRevealed => _isRevealed;

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            _propertyBlock = new MaterialPropertyBlock();
            ApplyVisualState();
        }

        public void SetRevealed(bool revealed)
        {
            if (_isRevealed == revealed)
                return;

            _isRevealed = revealed;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            if (targetRenderers.Length == 0)
                return;

            var baseColor = _isRevealed ? Color.white : neutralColor;
            var emissionColor = _isRevealed ? revealedColor : Color.black;
            foreach (var targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                    continue;

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, baseColor);
                _propertyBlock.SetColor(LegacyColorId, baseColor);
                _propertyBlock.SetColor(EmissionColorId, emissionColor);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
