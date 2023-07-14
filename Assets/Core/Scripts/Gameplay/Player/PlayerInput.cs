using System;
using Core.Scripts.Gameplay.GridFolder;
using Core.Scripts.Gameplay.Signals;
using Core.Scripts.Gameplay.StoneFolder;
using JetBrains.Annotations;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Gameplay.Player
{
    public class PlayerInput : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private StoneList _stoneList;
        [Inject] private Board _puzzleGrid;
        
        private Camera _mainCam;

        private bool _canInput = true;
        
        private void OnEnable()
        {
            _signalBus.Subscribe<OnOneClickLoopFinishedSignal>(SetInput);
            _signalBus.Subscribe<OnStoneMovedOnEmptySignal>(SetInput);

            //LeanTouch.OnFingerDown += OnButtonDown;
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<OnOneClickLoopFinishedSignal>(SetInput);
            _signalBus.Unsubscribe<OnStoneMovedOnEmptySignal>(SetInput);

            //LeanTouch.OnFingerDown -= OnButtonDown;
        }
        
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnButtonDown();
            }
        }
        
        private void OnButtonDown()
        {
            if(!_canInput) return;
            
            var arrow = GetSelectedGridPart<Arrow>(Values.ArrowMask);
            if(arrow == null) return;
            if(!arrow.CanPlaceable) return;

            if (!_puzzleGrid.IsFrontOfArrowEmpty(arrow.GridPosition.x, arrow.GridPosition.y))
                return;

            if (IsVertical(arrow))
            {
                if (_puzzleGrid.IsColumnFull(arrow.GridPosition.x))
                {
                    Debug.Log("Column Full");
                    return;
                }
            }
            else
            {
                if (_puzzleGrid.IsRowFull(arrow.GridPosition.y))
                {
                    Debug.Log("Row Full");
                    return;
                }
            }
            
            Stone tempStone = _stoneList.GetNextStone();
            _stoneList.PrepareNextStone();
            Vector3 pos = arrow.transform.position;

            if (tempStone == null)
            {
                Debug.Log("TEMP STONE IS NULL");
                return;
            }

            tempStone.transform.position = pos + Vector3.up * .3f;
            tempStone.SpawnCoordinate = arrow.GridPosition;
            tempStone.Direction = arrow.Direction;
            tempStone.SetupStone(tempStone.StoneColor, arrow.Direction);
            tempStone.name = $"Stone ({tempStone.GridCoordinate.x},{tempStone.GridCoordinate.y})";
            
            if (IsVertical(arrow) && _puzzleGrid.IsColumnEmpty(arrow.GridPosition.x) ||
                IsHorizontal(arrow) && _puzzleGrid.IsRowEmpty(arrow.GridPosition.y))
            {
                Coord2D target = GetTargetCoordForEmpty(arrow);
                tempStone.MoveToTarget(target, true);
            }
            else
            {
                tempStone.Move();
            }
            
            _signalBus.TryFire<OnStoneSpawnedSignal>();
            _canInput = false;
        }

        private void SetInput()
        {
            _canInput = true;
        }

        public void LockUnlockInput(bool b) => _canInput = b;

        [CanBeNull]
        private T GetSelectedGridPart<T>(int bitmask) where T : MonoBehaviour
        {
            if (_mainCam == null)
                _mainCam = Camera.main;
            
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = _mainCam!.nearClipPlane;
            Ray ray = _mainCam.ScreenPointToRay(mousePos);

            T res = null;

            if (Physics.Raycast(ray, out var hitInfo, 100, bitmask))
                res = hitInfo.collider.GetComponent<T>();

            Debug.DrawLine(ray.origin, hitInfo.point, Color.blue);
            return hitInfo.collider != null ? res : null;
        }
        
        //todo refactor is this should be here
        private Coord2D GetTargetCoordForEmpty(Arrow arrowP)
        {
            Coord2D targetCoord2D = arrowP.GridPosition;
            switch (arrowP.Direction)
            {
                case Direction.Down:
                    targetCoord2D.y = 0;
                    break;
                case Direction.Up:
                    targetCoord2D.y = _puzzleGrid.Size.y - 1;
                    break;
                case Direction.Right:
                    targetCoord2D.x = _puzzleGrid.Size.x - 1;
                    break;
                case Direction.Left:
                    targetCoord2D.x = 0;
                    break;
                case Direction.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return targetCoord2D;
        }

        private bool IsHorizontal(Arrow arrow)
        {
            return arrow.Direction is Direction.Right or Direction.Left;
        }

        private bool IsVertical(Arrow arrow)
        {
            return arrow.Direction is Direction.Up or Direction.Down;
        }
    }
}
