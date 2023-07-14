using System;
using Core.Scripts.Gameplay.Signals;
using Core.Scripts.Gameplay.StoneFolder;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Gameplay.GridFolder
{
    public class Arrow : MonoBehaviour
    {
        [Inject] private Board _grid;
        [Inject] private SignalBus _signalBus;
        //[Inject] private LevelManager _levelManager;

        [field: SerializeField] public Direction Direction { get; set; }
        
        [field: SerializeField] public Coord2D GridPosition { get; set; }

        [field: SerializeField] public bool CanPlaceable { get; set; } = true;

        private Color32 _startColor; // = new Color32((byte)200, (byte)109, (byte)224, (byte)255);
        private void OnEnable()
        {
            _signalBus.Subscribe<OnOneClickLoopFinishedSignal>(CheckFront);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<OnOneClickLoopFinishedSignal>(CheckFront);
        }

        private void Start()
        {
            //_startColor = transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material.GetColor("_BaseColor");
            
            if(_grid == null) return;
            
            if (_grid.GetCell(GridPosition.x, GridPosition.y) != null)
            {
                _grid.GetCell(GridPosition.x, GridPosition.y).AddArrow(this);
            }
            
            //SetArrowColor();
        }

        private void CheckFront()
        {
            //if (_levelManager.IsTutorialActive)
            //    return;
            
            var cell = _grid.GetCell(GridPosition.x, GridPosition.y);
            if (cell == null) return;
            CanPlaceable = cell.stone == null;
            SetArrowColor();
        }
        
        public void RotateArrow()
        {
            transform.GetChild(0).localRotation = Direction switch
            {
                Direction.Up    => Quaternion.Euler(0f, 0f, 0f),
                Direction.Down  => Quaternion.Euler(0f, 180f, 0f),
                Direction.Right => Quaternion.Euler(0f, 90f, 0f),
                Direction.Left  => Quaternion.Euler(0f, 270f, 0f),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        public void SetArrowColor()
        {
            //MaterialPropertyBlock block = new MaterialPropertyBlock();
            //block.SetColor("_BaseColor", CanPlaceable ? _startColor : Color.gray);
            //transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
    }
}