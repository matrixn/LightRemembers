using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryCorruptionController : MonoBehaviour
    {
        [SerializeField] private MemoryIntegrity integrity;
        [SerializeField] private MemoryRemnant recallRemnant;
        [SerializeField] private MemoryRemnant echoRemnant;
        [SerializeField] private MemoryCollapseHandler collapseHandler;
        [SerializeField] private MemoryEncounterResetter encounterResetter;
        [SerializeField] private SanctuaryLight sanctuary;
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField, Min(0f)] private float hitImmunitySeconds = 2.5f;

        private int _corruptionStage;
        private float _nextHitTime;
        private bool _collapsing;

        public MemoryIntegrity Integrity => integrity;
        public int CorruptionStage => _corruptionStage;
        public bool IsCollapsing => _collapsing;
        public bool IsProtected => sanctuary != null && sanctuary.Contains(transform.position);

        public void Configure(MemoryIntegrity memoryIntegrity, MemoryRemnant recallMemory,
            MemoryRemnant echoMemory, MemoryCollapseHandler collapse, MemoryEncounterResetter resetter,
            SanctuaryLight safeLight, SubtitlePresenter presenter, float hitImmunity = 2.5f)
        {
            integrity = memoryIntegrity;
            recallRemnant = recallMemory;
            echoRemnant = echoMemory;
            collapseHandler = collapse;
            encounterResetter = resetter;
            sanctuary = safeLight;
            subtitles = presenter;
            hitImmunitySeconds = Mathf.Max(0f, hitImmunity);
        }

        public bool TryReceiveHollowHit()
        {
            if (_collapsing || Time.time < _nextHitTime || IsProtected || integrity == null || !integrity.LoseOne())
                return false;

            _nextHitTime = Time.time + hitImmunitySeconds;
            switch (_corruptionStage++)
            {
                case 0:
                    MemoryAbilityState.CorruptRecall();
                    recallRemnant?.Appear(MemoryAbility.Recall, this);
                    subtitles?.Show(string.Empty, "I can't remember how to hold it together. Something remains nearby.");
                    break;
                case 1:
                    MemoryAbilityState.CorruptEcho();
                    echoRemnant?.Appear(MemoryAbility.Echo, this);
                    subtitles?.Show(string.Empty, "I can't remember how it moved. Something remains nearby.");
                    break;
                default:
                    _collapsing = true;
                    subtitles?.Hide();
                    if (collapseHandler != null)
                        collapseHandler.BeginCollapse();
                    else
                        CompleteCollapse();
                    break;
            }
            return true;
        }

        public bool TryRestore(MemoryAbility ability)
        {
            if (_collapsing || !MemoryAbilityState.RestoreMemory(ability))
                return false;
            integrity?.RestoreOne();
            subtitles?.Show(string.Empty, ability == MemoryAbility.Recall
                ? "The shape returns to me."
                : "I remember its movement.");
            return true;
        }

        public void StabilizeAtSanctuary()
        {
            if (_collapsing)
                return;
            MemoryAbilityState.RestoreAllMemories();
            integrity?.RestoreFull();
            _corruptionStage = 0;
            encounterResetter?.ClearRemnants();
        }

        public void CompleteCollapse()
        {
            MemoryAbilityState.RestoreAllMemories();
            integrity?.RestoreFull();
            _corruptionStage = 0;
            _nextHitTime = Time.time + hitImmunitySeconds;
            _collapsing = false;
        }

        public void ResetEncounterMemory()
        {
            encounterResetter?.ResetTransientMemory();
            recallRemnant?.Hide();
            echoRemnant?.Hide();
        }

#if UNITY_EDITOR
        public void DebugForceCorruption(MemoryAbility ability)
        {
            if (_collapsing || integrity == null || !integrity.LoseOne())
                return;
            if (ability == MemoryAbility.Recall && MemoryAbilityState.CorruptRecall())
            {
                _corruptionStage = Mathf.Max(_corruptionStage, 1);
                recallRemnant?.Appear(ability, this);
            }
            else if (ability == MemoryAbility.Echo && MemoryAbilityState.CorruptEcho())
            {
                _corruptionStage = Mathf.Max(_corruptionStage, 2);
                echoRemnant?.Appear(ability, this);
            }
        }

        public void DebugForceCollapse()
        {
            if (_collapsing)
                return;
            _collapsing = true;
            if (collapseHandler != null)
                collapseHandler.BeginCollapse();
            else
                CompleteCollapse();
        }
#endif
    }
}
