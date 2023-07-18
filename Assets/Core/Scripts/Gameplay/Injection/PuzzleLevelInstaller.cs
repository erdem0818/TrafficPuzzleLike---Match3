using Core.Scripts.Gameplay.GridFolder;
using Core.Scripts.Gameplay.Pools;
using Core.Scripts.Gameplay.Signals;
using Core.Scripts.Gameplay.StoneFolder;
using Core.Scripts.Gameplay.UI;
using TMPro;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Gameplay.Injection
{
    public interface IPoolProvider
    {
        public StonePool GetPool(StoneColor stoneColor);
    }
    
    public class PuzzleLevelInstaller : MonoInstaller, IPoolProvider
    {
        [Header("Puzzle Grid Settings")] 
        [SerializeField] private Board board;
        
        [Header("Stone Pool Settings")] 
        [SerializeField] private Stone blueBirdStone;
        [SerializeField] private Stone GreenBirdStone;
        [SerializeField] private Stone RedBirdStone;
        [SerializeField] private Stone YellowBirdStone;
        //[SerializeField] private Stone colorfulStone;
        //[SerializeField] private Stone quadraStone;
        //[SerializeField] private Stone colRowStone;
        //[SerializeField] private Stone allOneColorStone;
        
        [Space(15)]
        [SerializeField] private StoneList stoneList;
        [SerializeField] private TextMeshProUGUI countText;
        
        public override void InstallBindings()
        {
            SignalBusInstaller.Install(Container);
            
            Container.BindInstance(board);
            Container.BindInstance(stoneList);
            Container.BindInstance(new CountTextHandler(countText));
            Container.Bind<IPoolProvider>().FromInstance(this).AsSingle();
            
            //green is default
            BindStonePool(GreenBirdStone, StoneColor.Green, "Stones");

            BindSignals();
        }
        
        private void BindStonePool(Stone prefabP, StoneColor typeP, string nameP)
        {
            Container.BindMemoryPool<Stone, StonePool>()
                .WithId((ushort)typeP)
                .WithInitialSize(15)
                .FromComponentInNewPrefab(prefabP)
                .UnderTransformGroup(nameP)
                .NonLazy();
        }
        
        private void BindSignals()
        {
            Container.DeclareSignal<OnStoneSpawnedSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnStoneArrivedSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnExplosionHappenedSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnStoneMovedAfterExplosionSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnEveryStoneMovementFinishedSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnOneClickLoopFinishedSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnStoneMovedOnEmptySignal>().OptionalSubscriber();
            Container.DeclareSignal<OnLevelWinSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnLevelFailSignal>().OptionalSubscriber();
            
            //Container.DeclareSignal<OnColorfulStoneClickSignal>().OptionalSubscriber();
            //Container.DeclareSignal<OnQuadraStoneClickSignal>().OptionalSubscriber();
            //Container.DeclareSignal<OnColRowBoosterClickSignal>().OptionalSubscriber();
            //Container.DeclareSignal<OnAllOneColorBoosterClickSignal>().OptionalSubscriber();
            Container.DeclareSignal<OnQuestDoneSignal>().OptionalSubscriber();
        }
        
        public StonePool GetPool(StoneColor stoneColor)
        {
            return Container.ResolveId<StonePool>((ushort)stoneColor);
        }
    }
}