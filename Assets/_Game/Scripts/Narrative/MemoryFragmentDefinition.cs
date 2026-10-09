using UnityEngine;

namespace LightRemembers.Narrative
{
    [CreateAssetMenu(menuName = "Light Remembers/Memory Fragment")]
    public sealed class MemoryFragmentDefinition : ScriptableObject
    {
        [SerializeField] private string fragmentId;
        [SerializeField] private string title;
        [TextArea(2, 5)] [SerializeField] private string description;
        public string FragmentId => fragmentId;
        public string Title => title;
        public string Description => description;

        public void Configure(string id, string fragmentTitle, string fragmentDescription)
        {
            fragmentId = id;
            title = fragmentTitle;
            description = fragmentDescription;
        }
    }
}
