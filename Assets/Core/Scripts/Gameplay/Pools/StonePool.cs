using Core.Scripts.Gameplay.StoneFolder;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Gameplay.Pools
{
    public class StonePool : MonoMemoryPool<Stone>
    {
        protected override void OnSpawned(Stone item)
        {
            base.OnSpawned(item);
            
            item.transform.localScale = Vector3.one;
            item.IsExploded = false;
            item.IsTempMovementFinished = false;
        }
    }
}