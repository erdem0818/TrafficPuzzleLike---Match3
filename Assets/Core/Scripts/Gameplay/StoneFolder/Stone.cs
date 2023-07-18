using System;
using Core.Scripts.Gameplay.GridFolder;
using Core.Scripts.Gameplay.Injection;
using Core.Scripts.Gameplay.Pools;
using Core.Scripts.Gameplay.QuestObjects;
using Core.Scripts.Gameplay.Signals;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Core.Scripts.Gameplay.StoneFolder
{
    public enum StoneColor : ushort
    {
        Yellow = 0, Blue = 1, Green = 2, Red = 3, Colorful = 4, Quadra = 5, ColRow = 6, AllOneColor = 7, None = 8
    }

    public enum Direction : ushort
    {
        Up = 0, Down = 1, Right = 2, Left = 3, None = 4
    }
    
    //TODO RESET STONE FUNC WHEN RETURNING TO POOL
    public class Stone : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;
        [Inject] private Board _puzzleGrid;
        [Inject] private IPoolProvider _poolProvider;
        //[Inject] private ParticleController _particlePool;
        [Inject] private StoneList _stoneList;

        private StonePool _stonePool = null;
        
        [field: SerializeField] public StoneColor StoneColor { get; set; }
        [field: SerializeField] public Direction Direction { get; set; }
        [field: SerializeField] public Coord2D GridCoordinate { get; set; }
        [field: SerializeField] public Coord2D SpawnCoordinate { get; set; }
        
        public bool IsExploded { get; set; } = false;
        public bool IsTempMovementFinished { get; set; } = false;
        
        private Tween _moveToTargetTween = null;

        public bool debug;
        
        private void OnValidate()
        {
            if(!debug) return;
            
            gameObject.name = $"Stone ({GridCoordinate.x},{GridCoordinate.y})";
        }
        
        private void OnDisable()
        {
            _moveToTargetTween?.Kill();
        }

        private void Awake()
        {
            if (StoneColor != StoneColor.None)
                _stonePool = _poolProvider.GetPool(StoneColor.Green); //_poolProvider.GetPool(StoneColor);
        }
        
        public void Move(bool afterSpawn = false)
        {
            int endpoint = _puzzleGrid.GetEndPoint(Direction, afterSpawn ? GridCoordinate : SpawnCoordinate);
            GridPart tempPart = null;

            if (Direction is Direction.Up or Direction.Down)
            {
                tempPart = _puzzleGrid.GetCell(afterSpawn ? GridCoordinate.x : SpawnCoordinate.x, endpoint);

                if (tempPart == null)
                {
                    Debug.Log("Temp Part is null");
                    return;
                }
            }
            else
            {
                tempPart = _puzzleGrid.GetCell(endpoint, afterSpawn ? GridCoordinate.y : SpawnCoordinate.y);
                if (tempPart == null)
                {
                    Debug.Log("Temp Part is null");
                    return;
                }
            }
            
            var partGridPos = tempPart.transform.position;
            var targetPos = new Vector3(partGridPos.x, 0.3f, partGridPos.z);
            Coord2D targetIndex = new()
            {
                x = tempPart.GetGridPos.x,
                y = tempPart.GetGridPos.y
            };
            _puzzleGrid.SetStoneInArray(targetIndex.x, targetIndex.y, this);
            GridCoordinate = targetIndex;
            
            tempPart.stone = this;
            tempPart.IsEmpty = false;
            //tempPart.SetColor();
            
            int gap = Direction switch
            {
                Direction.Up => (SpawnCoordinate.y - endpoint) * -1,
                Direction.Down => (SpawnCoordinate.y - endpoint),
                Direction.Right => (SpawnCoordinate.x - endpoint) * -1,
                Direction.Left => (SpawnCoordinate.x - endpoint),
                Direction.None => 0,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            transform.DOMove(targetPos, Values.OneGridPassTime * gap)
                .SetEase(Ease.OutSine)
                .SetAutoKill()
                .OnComplete(() =>
                {
                    if (StoneColor == StoneColor.Colorful)
                    {
                        var frontCoord = GetFrontCoord(targetIndex);
                        var frontCell = _puzzleGrid.GetCell(frontCoord.x, frontCoord.y);
                        if (frontCell != null && frontCell.stone != null)
                        {
                            StoneColor = frontCell.stone.StoneColor != StoneColor.None ? frontCell.stone.StoneColor : _stoneList.GetFirstColor();
                            
                            transform.GetChild(0).gameObject.SetActive(false);
                            transform.GetChild((int)StoneColor + 1).gameObject.SetActive(true);
                        }
                    }

                    _signalBus.TryFire(new OnStoneArrivedSignal{Stone = this});

                    if (_puzzleGrid.IsOnEdge(targetIndex.x, targetIndex.y))
                    {
                        GridPart cell = _puzzleGrid.GetCell(targetIndex.x, targetIndex.y);
                        cell.SetArrows(cell.IsEmpty);
                    }
                });
        }

        #region Coord Methods
        
        public Coord2D GetFrontCoord(Coord2D targetIndex)
        {
            var frontCoord = Direction switch
            {
                Direction.Up => new Coord2D(targetIndex.x, targetIndex.y + 1),
                Direction.Down => new Coord2D(targetIndex.x, targetIndex.y - 1),
                Direction.Right => new Coord2D(targetIndex.x + 1, targetIndex.y),
                Direction.Left => new Coord2D(targetIndex.x - 1, targetIndex.y),
                Direction.None => new Coord2D(targetIndex.x, targetIndex.y),
                _ => throw new ArgumentOutOfRangeException()
            };
            return frontCoord;
        }
        
        public Coord2D GetBehindCoord(Coord2D targetIndex)
        {
            var frontCoord = Direction switch
            {
                Direction.Up => new Coord2D(targetIndex.x, targetIndex.y - 1),
                Direction.Down => new Coord2D(targetIndex.x, targetIndex.y + 1),
                Direction.Right => new Coord2D(targetIndex.x - 1, targetIndex.y),
                Direction.Left => new Coord2D(targetIndex.x + 1, targetIndex.y),
                Direction.None => new Coord2D(targetIndex.x, targetIndex.y),
                _ => throw new ArgumentOutOfRangeException()
            };
            return frontCoord;
        }
        
        public Coord2D GetRightCoord(Coord2D targetIndex)
        {
            var frontCoord = Direction switch
            {
                Direction.Up => new Coord2D(targetIndex.x + 1, targetIndex.y),
                Direction.Down => new Coord2D(targetIndex.x - 1, targetIndex.y),
                Direction.Right => new Coord2D(targetIndex.x, targetIndex.y - 1),
                Direction.Left => new Coord2D(targetIndex.x, targetIndex.y + 1),
                Direction.None => new Coord2D(targetIndex.x, targetIndex.y),
                _ => throw new ArgumentOutOfRangeException()
            };
            return frontCoord;
        }
        
        public Coord2D GetLeftCoord(Coord2D targetIndex)
        {
            var frontCoord = Direction switch
            {
                Direction.Up => new Coord2D(targetIndex.x - 1, targetIndex.y),
                Direction.Down => new Coord2D(targetIndex.x + 1, targetIndex.y),
                Direction.Right => new Coord2D(targetIndex.x, targetIndex.y + 1),
                Direction.Left => new Coord2D(targetIndex.x, targetIndex.y - 1),
                Direction.None => new Coord2D(targetIndex.x, targetIndex.y),
                _ => throw new ArgumentOutOfRangeException()
            };
            return frontCoord;
        }
        #endregion

        private int _howManyPass = 0;
        public void MoveToTarget(Coord2D coord2D, bool isEmpty = false)
        {
            _howManyPass++;
            //Debug.Log(_howManyPass);
            IsTempMovementFinished = false;
            
            var cell = _puzzleGrid.GetCell(coord2D.x, coord2D.y);
            cell.IsEmpty = false;
            cell.stone = this;

            Coord2D behind = GetBehindCoord(cell.GetGridPos);
            var previousCell = _puzzleGrid.GetCell(behind.x, behind.y);
            previousCell.IsEmpty = true;
            previousCell.stone = null;

            bool isOnEdge = _puzzleGrid.IsOnEdge(coord2D.x, coord2D.y);
            
            if (isOnEdge)
            {
                cell.IsEmpty = true;
                cell.stone = null;
            }
            
            _puzzleGrid.SetStoneInArray(behind.x, behind.y, null);
            _puzzleGrid.SetStoneInArray(coord2D.x, coord2D.y, isOnEdge ? null : this);
            
            GridCoordinate = new Vector2Int(coord2D.x, coord2D.y);
            
            _moveToTargetTween?.Kill(false);
            float dur = isEmpty ? (_puzzleGrid.Size.x * Values.OneGridPassTime) * 0.5f : Values.OneGridPassTime * _howManyPass;

            _moveToTargetTween = transform.DOMove(cell.transform.position + Vector3.up * .3f, dur)
                .SetEase(Ease.OutSine)
                .OnComplete(() =>
                {
                        switch (isEmpty)
                        {
                            case false:
                                _signalBus.TryFire<OnStoneMovedAfterExplosionSignal>();
                                break;
                            case true:
                                _signalBus.TryFire<OnStoneMovedOnEmptySignal>();
                                break;
                        }

                        if (isOnEdge && cell.Has(Direction))
                        {
                            GetSmaller();
                        }
                        
                        IsTempMovementFinished = true;
                        _howManyPass = 0;
                }).SetAutoKill();
        }
        
        public void Explode()
        {
            GetSmaller();        
            
            var neighbors = _puzzleGrid.GetGridPartListInNeighbor(GridCoordinate);
            foreach (var gridPart in neighbors)
            {
                if (gridPart != null && gridPart.questObject != null && gridPart.questObject.CanEffectedByNeighbor)
                {
                    gridPart.questObject.PlayQuest();
                }
            }

            QuestObject questObject = _puzzleGrid.GetCell(GridCoordinate.x, GridCoordinate.y).questObject;
            if(questObject != null)
                questObject.PlayQuest();
        }

        private void GetSmaller()
        {
            //var p = _particlePool.GetParticle();
            //p.transform.position = transform.position + Vector3.up * .5f;
            //p.Play();
            
            transform.DOScale(Vector3.zero, 0.5f)
                .SetEase(Ease.InBounce)
                .OnComplete(() =>
                {
                    if (!IsExploded)
                    {
                        _stonePool.Despawn(this);
                        IsExploded = true;
                    }
                    
                    _puzzleGrid.GetCell(GridCoordinate.x, GridCoordinate.y).SetArrows(true);
                });

            //DOVirtual.DelayedCall(p.main.duration, () => _particlePool.DespawnParticle(p));
        }
        
        public void SetupStone(StoneColor stoneColor, Direction direction)
        {
            StoneColor = stoneColor;
            Direction = direction;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", GetColorByEnum(stoneColor));
            GetComponent<MeshRenderer>().SetPropertyBlock(block);
            
            transform.rotation = Direction switch
            {
                Direction.Up => Quaternion.identity,
                Direction.Down => Quaternion.Euler(0f, -180f, 0f),
                Direction.Right => Quaternion.Euler(0f, -270f, 0f),
                Direction.Left => Quaternion.Euler(0f, -90f, 0f),
                Direction.None => Quaternion.identity,
                _ => throw new ArgumentOutOfRangeException(nameof(Direction), Direction, null)
            };
        }

        private static Color GetColorByEnum(StoneColor color)
        {
            return color switch
            {
                StoneColor.Blue => Color.blue,
                StoneColor.Yellow => Color.yellow,
                StoneColor.Green => Color.green,
                StoneColor.Red => Color.red,
                StoneColor.Colorful => Color.black,
                StoneColor.Quadra => Color.magenta,
                StoneColor.ColRow => Color.cyan,
                StoneColor.AllOneColor => new Color(200, 200, 50),
                StoneColor.None => Color.gray,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
    }
}
