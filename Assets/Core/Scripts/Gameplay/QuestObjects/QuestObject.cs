using Core.Scripts.Gameplay.GridFolder;
using Core.Scripts.Gameplay.Signals;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Gameplay.QuestObjects
{
    public class QuestObject : MonoBehaviour
    {
        [Inject] protected Board PuzzleGrid;
        [Inject] private SignalBus _signalBus;
        
        [field: SerializeField] public bool CanEffectedByNeighbor { get; set; }
        public bool IsDone { get; set; } = false;
        
        public Coord2D gridCoordinate;

        public virtual void PlayQuest()
        {
            if (IsDone)
                return;
            
            IsDone = true;
            _signalBus.TryFire(new OnQuestDoneSignal{QuestObject = this});
        }
    }
}
