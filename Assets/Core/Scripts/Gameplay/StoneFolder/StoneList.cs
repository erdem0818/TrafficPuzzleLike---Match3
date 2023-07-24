using System;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Gameplay.GridFolder;
using Core.Scripts.Gameplay.Pools;
using Core.Scripts.Gameplay.Signals;
using Core.Scripts.Gameplay.UI;
using TMPro;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Core.Scripts.Gameplay.StoneFolder
{
    public class StoneList : MonoBehaviour
    {
        [Inject] private Board _puzzleGrid;
        [Inject] private SignalBus _signalBus;
        [Inject] private CountTextHandler _textHandler;
        //[Inject] private QuestListener _questListener;

        [Inject(Id = "StonePool")] private StonePool _stonePool;

        [SerializeField] private GameObject gonnaSpawned;

        private Stone _currentStone;
        private StoneColor _nextColor;

        [Header("DESIRED STONE ORDER")]
        [SerializeField] private List<StoneColor> desiredStoneList;
        [SerializeField] private int maxClickCount;
        private int _clickCount = 0;

        private List<Stone> _allStones;
        private bool _isWon = false;

        private void OnEnable()
        {
            _signalBus.Subscribe<OnOneClickLoopFinishedSignal>(CheckIfWinOrFail);
            _signalBus.Subscribe<OnOneClickLoopFinishedSignal>(OnExplosion);
            
            _signalBus.Subscribe<OnStoneMovedOnEmptySignal>(CheckIfWinOrFail);
            _signalBus.Subscribe<OnStoneMovedOnEmptySignal>(OnExplosion);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<OnOneClickLoopFinishedSignal>(CheckIfWinOrFail);
            _signalBus.Unsubscribe<OnOneClickLoopFinishedSignal>(OnExplosion);

            _signalBus.Unsubscribe<OnStoneMovedOnEmptySignal>(CheckIfWinOrFail);
            _signalBus.Unsubscribe<OnStoneMovedOnEmptySignal>(OnExplosion);
        }

        private void Awake()
        {
            DebugVisual(GetFirstColor());
            
            _clickCount = maxClickCount;
            _textHandler.SetText(_clickCount);

            _allStones = FindObjectsOfType<Stone>().ToList();

            _nextColor = NextColor();
        }
        public Stone GetNextStone()
        {
            if (_clickCount <= 0)
                return null;
            
            _clickCount--;
            
            _currentStone = GetStoneByColor(_nextColor);
            _currentStone.StoneColor = _nextColor;
            return _currentStone;
        }
        
        public void PrepareNextStone()
        {
            _textHandler.SetText(_clickCount);

            if (_clickCount <= 0) return;
            
            _nextColor = NextColor();
            DebugVisual(_nextColor);
        }

        private StoneColor NextColor()
        {
            if (desiredStoneList.Count > 0)
            {
                var res = GetFirstColor();
                desiredStoneList.Remove(res);
                return res;
            }

            var colorsOnboard = ColorsOnBoard();
            int rnd = Random.Range(0, colorsOnboard.Count);
            StoneColor rndCl = colorsOnboard[rnd];
            return rndCl;
        }
        

        private void CheckIfWinOrFail()
        {
            if(!_puzzleGrid.IsThereAnyStoneOnGrid() /* || _questListener.IsAllQuestsDone() */)
            {
                _signalBus.TryFire<OnLevelWinSignal>();
                _isWon = true;
            }
            
            if (_clickCount <= 0 || desiredStoneList.Count <= 0)
            {
                if(_isWon) return;
                
                _signalBus.TryFire<OnLevelFailSignal>();
            }
        }
        
        private void OnExplosion()
        {
            _allStones.Clear();
            var temp = _puzzleGrid.GetFilledList();
            foreach (var s in temp)
                _allStones.Add(s);
            
            ClearByColor(StoneColor.Red);
            ClearByColor(StoneColor.Blue);
            ClearByColor(StoneColor.Yellow);
            ClearByColor(StoneColor.Green);
        }

        private void ClearByColor(StoneColor color)
        {
            bool anyColor = _allStones.Any(s => !s.IsExploded && s.StoneColor == color);
            if (anyColor) return;
            for (int i = desiredStoneList.Count - 1; i >= 0; i--)
                desiredStoneList.Remove(color);
        }

        private IList<StoneColor> ColorsOnBoard()
        {
            var result = new List<StoneColor>();
            var temp = _puzzleGrid.GetFilledList();
            
            if(temp.Any(s => s.StoneColor == StoneColor.Green))
                result.Add(StoneColor.Green);
            if(temp.Any(s => s.StoneColor == StoneColor.Blue))
                result.Add(StoneColor.Blue);
            if(temp.Any(s => s.StoneColor == StoneColor.Red))
                result.Add(StoneColor.Red);
            if(temp.Any(s => s.StoneColor == StoneColor.Yellow))
                result.Add(StoneColor.Yellow);

            return result;
        }

        private Stone GetStoneByColor(StoneColor color)
        {
            return color == StoneColor.None ? null : _stonePool.Spawn();
        }

        public StoneColor GetFirstColor()
        {
            return desiredStoneList[0];
        }

        #region Event Methods

        private void SetColorfulBooster()
        {
            desiredStoneList[0] = StoneColor.Colorful;
            DebugVisual(StoneColor.Colorful);
        }

        private void SetQuadraBooster()
        {
            desiredStoneList[0] = StoneColor.Quadra;
            DebugVisual(StoneColor.Quadra);
        }

        private void SetColRowBooster()
        {
            desiredStoneList[0] = StoneColor.ColRow;
            DebugVisual(StoneColor.ColRow);
        }

        private void SetAllOneColorBooster()
        {
            desiredStoneList[0] = StoneColor.AllOneColor;
            DebugVisual(StoneColor.AllOneColor);
        }

        #endregion
        
        private void DebugVisual(StoneColor color)
        {
            for (int i = 0; i < gonnaSpawned.transform.childCount; i++) //gonna be 8 ı guess
                gonnaSpawned.transform.GetChild(i).gameObject.SetActive(false);
            
            if(((int)color) <= 3)
                gonnaSpawned.transform.GetChild((int)color).gameObject.SetActive(true);
        }
    }
}