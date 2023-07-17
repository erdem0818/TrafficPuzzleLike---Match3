using System;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Gameplay.Signals;
using Core.Scripts.Gameplay.StoneFolder;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;
using Sirenix.Utilities;
using Direction = Core.Scripts.Gameplay.StoneFolder.Direction;

namespace Core.Scripts.Gameplay.GridFolder
{
    public class Board : MonoBehaviour
    {
        [Inject] private SignalBus _signalBus;

        [SerializeField] private bool debug;
        
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        
        private GridPart[,] _gridArray;
        private Stone[,] _stoneArray;
        private readonly GridPart[] _gridBuffer = new GridPart[4];

        public Vector2Int Size => new (_width, _height);

        private bool _isThereAnyStoneCanMove;
        
        private void OnEnable()
        {
            _signalBus.Subscribe<OnStoneArrivedSignal>(CheckForExplosionInvoker);
        }

        private void OnDisable()
        {
            _signalBus.Unsubscribe<OnStoneArrivedSignal>(CheckForExplosionInvoker);
        }

        private void Awake()
        {
            InitGrid(_width, _height);
            int ct = 0;
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    SetCell(j, i, transform.GetChild(ct).GetComponent<GridPart>());
                    ct++;
                }
            }
            
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (GetCell(j, i).stone != null)
                        SetStoneInArray(j, i, GetCell(j, i).stone);
                }
            }
        }
        
        public void InitGrid(int width, int height)
        {
            _width  = width;
            _height = height;
            _gridArray  = new GridPart[width, height];
            _stoneArray = new Stone[width, height];
        }
        
        private void CheckForExplosionInvoker()
        {
            //StartCoroutine(CheckForExplosion());
            CheckForExplosion().Forget();
        }
        
        private async UniTask CheckForExplosion()
        {
            IList<Stone> filled = GetFilledList();
            while (IsThereAnyExplosion(filled, 4) || IsThereAnyBooster(filled))
            {
                //todo first check for boosters ?

                var boosters = CheckExplosionByBoosters(filled);
                var stonesOne = CheckExplosionInRightDirection(filled, 4);
                var stonesTwo = CheckExplosionInUpDirection(filled, 4);
                
                IList<Stone> expStones = new List<Stone>();
                
                foreach (Stone stone in boosters)
                    expStones.Add(stone);
                foreach (Stone stone in stonesOne)
                    expStones.Add(stone);
                foreach (Stone stone in stonesTwo)
                    expStones.Add(stone);

                foreach (var expStone in expStones)
                {
                    if (expStone != null)
                    {
                        expStone.Explode();
                        GetCell(expStone.GridCoordinate.x, expStone.GridCoordinate.y).IsEmpty = true;
                        GetCell(expStone.GridCoordinate.x, expStone.GridCoordinate.y).stone = null;
                        SetStoneInArray(expStone.GridCoordinate.x, expStone.GridCoordinate.y, null);
                    }
                }
                
                bool IsStonesExploded() => expStones.All(s => s.IsExploded);
                //Debug.Log($"Is ALL EXPLODED {IsStonesExploded()}");
                
                //yield return new WaitUntil(IsStonesExploded);
                await UniTask.WaitUntil(IsStonesExploded);
                
                _signalBus.TryFire<OnExplosionHappenedSignal>();

                //yield return StartCoroutine(CheckForMovement(filled));
                await CheckForMovement(filled);
                
                filled.Clear();
                filled = GetFilledList();
            }
            
            Debug.Log("Check For Explosion Ended");
            _signalBus.TryFire<OnOneClickLoopFinishedSignal>();
        }
        
        private bool IsThereAnyExplosion(IEnumerable<Stone> filled, int numberOfStonesToCheck)
        {
            //for right direction
            int expCount = 0;
            foreach (var t in filled)
            {
                int ct = 0;
                Stone tempStone = t;
                Coord2D coord2D = tempStone.GridCoordinate;
                // 1 2 3
                for (int i = 1; i < numberOfStonesToCheck; i++)
                {
                    if (!IsValidAccess(coord2D.x + i, coord2D.y))
                        break;

                    Stone neighbor = GetCell(coord2D.x + i, coord2D.y).stone;
                    if (neighbor == null)
                    {
                        if (debug)
                            Debug.Log("Neighbor is null");
                        break;
                    }
                    
                    if(tempStone.StoneColor != neighbor.StoneColor)
                        break;
                    //just increase because if it is not breaking it is a match
                    ct += neighbor.StoneColor == tempStone.StoneColor ? 1 : 0;
                    //4 or 3
                    if (ct == numberOfStonesToCheck - 1 || ct == numberOfStonesToCheck - 2)
                    {
                        expCount++;
                    }
                }
            }
            
            //for up direction
            foreach (var t in filled)
            {
                int ct = 0;
                Stone tempStone = t;
                Coord2D coord2D = tempStone.GridCoordinate;

                for (int i = 1; i < numberOfStonesToCheck; i++)
                {
                    if (!IsValidAccess(coord2D.x, coord2D.y + i))
                        break;

                    Stone neighbor = GetCell(coord2D.x, coord2D.y + i).stone;
                    if (neighbor == null)
                    {
                        if (debug)
                            Debug.Log("Neighbor is null");
                        break;
                    }

                    if (tempStone.StoneColor != neighbor.StoneColor)
                        break;
                    
                    ct += neighbor.StoneColor == tempStone.StoneColor ? 1 : 0;

                    if (ct == numberOfStonesToCheck - 1 || ct == numberOfStonesToCheck - 2)
                    {
                        expCount++;
                    }
                }
            }

            return expCount != 0;
        }

        private bool IsThereAnyBooster(IEnumerable<Stone> filled)
        {
            return filled.Any(s => s.StoneColor is StoneColor.Quadra or StoneColor.ColRow or StoneColor.AllOneColor);
        }

        private IList<Stone> CheckExplosionByBoosters(IList<Stone> filled)
        {
            IList<Stone> explodeStones = new List<Stone>();
            var boosterStones =
                filled.Where(s =>
                    s.StoneColor is StoneColor.Quadra or StoneColor.ColRow or StoneColor.AllOneColor); // column - row booster and every same color booster will be added
            
            foreach (var booster in boosterStones)
            {
                if (booster.StoneColor == StoneColor.Quadra)
                {
                    Coord2D tempCoord = booster.GridCoordinate;
                    var nArr= GetGridPartListInNeighbor(tempCoord);
                    foreach (var t in nArr)
                    {
                        if (t != null && t.stone != null && !explodeStones.Contains(t.stone))
                            explodeStones.Add(t.stone);
                    }
                    
                    explodeStones.Add(booster); //also add self
                }

                if (booster.StoneColor == StoneColor.ColRow)
                {
                    Coord2D tempCoord = booster.GridCoordinate;
                    bool isHorizontal = booster.Direction is Direction.Left or Direction.Right;
                    bool isVertical = booster.Direction is Direction.Up or Direction.Down;
                    if (isHorizontal)
                    {
                        var onRow = GetFilledListOnRow(tempCoord);
                        onRow.ForEach(s => explodeStones.Add(s));
                    }
                    else if(isVertical) //but this includes none direction
                    {
                        var onColumn = GetFilledListOnColumn(tempCoord);
                        onColumn.ForEach(s => explodeStones.Add(s));
                    }
                }

                if (booster.StoneColor == StoneColor.AllOneColor)
                {
                    GridPart targetCell = null;
                    int ct = 0;
                    while (targetCell == null && ct < 4)
                    {
                        switch (ct)
                        {
                            case 0:
                                Coord2D frontCoord = booster.GetFrontCoord(booster.GridCoordinate);
                                targetCell = GetCell(frontCoord.x, frontCoord.y);
                                break;
                            case 1:
                                Coord2D right = booster.GetRightCoord(booster.GridCoordinate);
                                targetCell = GetCell(right.x, right.y);
                                break;
                            case 2:
                                Coord2D behind = booster.GetBehindCoord(booster.GridCoordinate);
                                targetCell = GetCell(behind.x, behind.y);
                                break;
                            case 3:
                                Coord2D left = booster.GetLeftCoord(booster.GridCoordinate);
                                targetCell = GetCell(left.x, left.y);
                                break;
                        }

                        ct++;
                    }
                 
                    if (targetCell != null && targetCell.stone != null)
                    {
                        var allStonesByColor = GetFilledByColor(targetCell.stone.StoneColor);
                        allStonesByColor.ForEach(s => explodeStones.Add(s));
                    }
                    
                    explodeStones.Add(booster); // also add self
                }
            }
            return explodeStones;
        }
        private IList<Stone> CheckExplosionInRightDirection(IList<Stone> filled, int numberOfStonesToCheck)
        {
            IList<Stone> explodeStones = new List<Stone>();

            foreach (var t in filled)
            {
                int ct = 0;
                Stone tempStone = t;
                Coord2D coord2D = tempStone.GridCoordinate;
                // 1 2 3
                for (int i = 1; i < numberOfStonesToCheck; i++)
                {
                    if (!IsValidAccess(coord2D.x + i, coord2D.y))
                        break;

                    Stone neighbor = GetCell(coord2D.x + i, coord2D.y).stone;
                    if (neighbor == null)
                    {
                        if (debug)
                            Debug.Log("Neighbor is null");
                        break;
                    }
                    
                    if(tempStone.StoneColor != neighbor.StoneColor)
                        break;
                    
                    ct += neighbor.StoneColor == tempStone.StoneColor ? 1 : 0;

                    if (ct == numberOfStonesToCheck - 1) // 4 matched
                    {

                        for (int j = 0; j < numberOfStonesToCheck; j++)
                        {
                            GridPart looked = GetCell(coord2D.x + j, coord2D.y);
                            explodeStones.Add(looked.stone);
                            _stoneArray[coord2D.x + j, coord2D.y] = null;
                        }
                        break;
                    }
                    
                    if (ct == numberOfStonesToCheck - 2) // 3 matched
                    {

                        for (int j = 0; j < numberOfStonesToCheck - 1; j++)
                        {
                            GridPart looked = GetCell(coord2D.x + j, coord2D.y);
                            explodeStones.Add(looked.stone);
                            _stoneArray[coord2D.x + j, coord2D.y] = null;
                        }
                        break;
                    }
                }
            }

            return explodeStones;
        }
        
        private IList<Stone> CheckExplosionInUpDirection(IList<Stone> filled, int numberOfStonesToCheck)
        {
            IList<Stone> explodeStones = new List<Stone>();

            foreach (var t in filled)
            {
                int ct = 0;
                Stone tempStone = t;
                Coord2D coord2D = tempStone.GridCoordinate;
                // 1 2 3
                for (int i = 1; i < numberOfStonesToCheck; i++)
                {
                    if (!IsValidAccess(coord2D.x, coord2D.y + i))
                        break;

                    Stone neighbor = GetCell(coord2D.x, coord2D.y + i).stone;
                    if (neighbor == null)
                    {
                        if (debug)
                            Debug.Log("Neighbor is null");
                        break;
                    }
                    
                    if(tempStone.StoneColor != neighbor.StoneColor)
                        break;

                    ct += neighbor.StoneColor == tempStone.StoneColor ? 1 : 0;

                    if (ct == numberOfStonesToCheck - 1)
                    {
                        for (int j = 0; j < numberOfStonesToCheck; j++)
                        {
                            GridPart looked = GetCell(coord2D.x, coord2D.y + j);
                            explodeStones.Add(looked.stone);
                            _stoneArray[coord2D.x, coord2D.y + j] = null;
                        }
                        break;
                    }
                    
                    if (ct == numberOfStonesToCheck - 2)
                    {
                        for (int j = 0; j < numberOfStonesToCheck - 1; j++)
                        {
                            GridPart looked = GetCell(coord2D.x, coord2D.y + j);
                            explodeStones.Add(looked.stone);
                            _stoneArray[coord2D.x, coord2D.y + j] = null;
                        }
                        break;
                    }
                }
            }

            return explodeStones;
        }
        
        private async UniTask CheckForMovement(IList<Stone> filled)
        {
            while (IsThereAnyMovableStone())
            {
                await CheckMovementForRight(filled);
                await CheckMovementForDown(filled);
                await CheckMovementForLeft(filled);
                await CheckMovementForUp(filled);
            }

            //yield return null;
            await UniTask.Yield();
            
            _signalBus.TryFire<OnEveryStoneMovementFinishedSignal>();
        }

        private UniTask CheckMovementForRight(IList<Stone> filled)
        {
            IList<Stone> localMovables = new List<Stone>();
            int howMany = 0;
            do
            {
                IList<Stone> rightDirections = filled.Where(stone =>
                    stone != null && !stone.IsExploded && stone.Direction == Direction.Right).ToList();
                howMany = 0;
                foreach (var stone in rightDirections)
                {
                    if (StoneCanMove(stone, Direction.Right))
                    {
                        //Debug.Log($"can move {stone.name}", stone);
                        stone.MoveToTarget(new Coord2D(stone.GridCoordinate.x + 1, stone.GridCoordinate.y));
                        howMany++;
                        localMovables.Add(stone);
                    }
                }
            } while (howMany > 0);

            bool AllRightFinished() => localMovables.All(s => s.IsTempMovementFinished);
            //return new WaitUntil(AllRightFinished);
            return UniTask.WaitUntil(AllRightFinished);
        }
        
        private UniTask CheckMovementForDown(IList<Stone> filled)
        {
            IList<Stone> localMovables = new List<Stone>();
            int howMany = 0;
            do
            {
                IList<Stone> downDirections = filled.Where(stone =>
                    stone != null && !stone.IsExploded && stone.Direction == Direction.Down).ToList();
                howMany = 0;
                foreach (var stone in downDirections)
                {
                    if (StoneCanMove(stone, Direction.Down))
                    {
                        stone.MoveToTarget(new Coord2D(stone.GridCoordinate.x, stone.GridCoordinate.y - 1));
                        howMany++;
                        localMovables.Add(stone);
                    }
                }
            } while (howMany > 0);
            
            bool AllDownFinished() => localMovables.All(s => s.IsTempMovementFinished); // was rightDirections
            //return new WaitUntil(AllDownFinished);
            return UniTask.WaitUntil(AllDownFinished);
        }
        
        private UniTask CheckMovementForLeft(IList<Stone> filled)
        {
            IList<Stone> localMovables = new List<Stone>();
            int howMany = 0;
            do
            {
                IList<Stone> leftDirections = filled.Where(stone =>
                    stone != null && !stone.IsExploded && stone.Direction == Direction.Left).ToList();
                howMany = 0;
                foreach (var stone in leftDirections)
                {
                    if (StoneCanMove(stone, Direction.Left))
                    {
                        //Debug.Log($"can move {stone.name}", stone);
                        stone.MoveToTarget(new Coord2D(stone.GridCoordinate.x - 1, stone.GridCoordinate.y));
                        howMany++;
                        localMovables.Add(stone);
                    }
                }
            } while (howMany > 0);
            
            bool AllLeftFinished() => localMovables.All(s => s.IsTempMovementFinished); // was rightDirections
            //return new WaitUntil(AllLeftFinished);
            return UniTask.WaitUntil(AllLeftFinished);
        }
        
        private UniTask CheckMovementForUp(IList<Stone> filled)
        {
            IList<Stone> localMovables = new List<Stone>();
            int howMany = 0;
            do
            {
                IList<Stone> upDirections = filled.Where(stone =>
                    stone != null && !stone.IsExploded && stone.Direction == Direction.Up).ToList();
                howMany = 0;
                foreach (var stone in upDirections.Where(s => StoneCanMove(s, Direction.Up)))
                {
                    //Debug.Log($"can move {stone.name}", stone);
                    stone.MoveToTarget(new Coord2D(stone.GridCoordinate.x, stone.GridCoordinate.y + 1));
                    howMany++;
                    localMovables.Add(stone);
                }
            } while (howMany > 0);
            
            bool AllUpFinished() => localMovables.All(s => s.IsTempMovementFinished); // was rightDirections
            //return new WaitUntil(AllUpFinished);
            return UniTask.WaitUntil(AllUpFinished);
        }
        
        #region Access Methods
        public GridPart GetCell(int x, int y)
        {
            return !IsValidAccess(x, y) ? null : _gridArray[x, y];
        }

        public void SetCell(int x, int y, GridPart part)
        {
            if (IsValidAccess(x, y))
                _gridArray[x, y] = part;
        }

        public void SetStoneInArray(int x, int y, Stone stone)
        {
            if (IsValidAccess(x, y))
                _stoneArray[x, y] = stone;
        }
        
        public int GetEndPoint(Direction direction, Coord2D coord)
        {
            int endPoint = 0;
            int x = coord.x;
            int y = coord.y;
            int start = 0;

            switch (direction)
            {
                case Direction.Up:
                    
                    start = 0;
                    while (GetCell(x, start).IsEmpty && start < _height)
                        start++;
                    
                    endPoint = start;
                    endPoint -= 1;
                    
                    break;
                case Direction.Down:
                    
                    start = _height - 1;
                    while (GetCell(x, start).IsEmpty && start >= 0)
                        start--;
                    
                    endPoint = start;
                    endPoint += 1;
                    
                    break;
                case Direction.Right:
                    start = 0;
                    while (GetCell(start, y).IsEmpty && start < _width)
                        start++;

                    endPoint = start;
                    endPoint -= 1;
                    break;
                case Direction.Left:
                    start = _width - 1;
                    while (GetCell(start, y).IsEmpty && start >= 0)
                        start--;

                    endPoint = start;
                    endPoint += 1;
                    break;
                case Direction.None:
                    endPoint = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            
            return endPoint;
        }
        #endregion
        #region Booleans

        private bool IsThereAnyMovableStone()
        {
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    Stone temp = _stoneArray[j, i];
                    if (temp == null)
                        continue;

                    if (StoneCanMove(temp, temp.Direction))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        private bool IsValidAccess(int x, int y)
        {
            if (x >= _width || y >= _height)
            {
                //Debug.Log("No Index");
                return false;
            }

            if (x < 0 || y < 0)
            {
                //Debug.Log("Negative");
                return false;
            }

            return true;
        }

        public bool IsOnEdge(int x, int y)
        {
            if (x == 0 || x == _width - 1)
                return true;

            if (y == 0 || y == _height - 1)
                return true;

            return false;
        }
        
        public bool IsRowFull(int y)
        {
            for (int i = 0; i < _width; i++)
            {
                if (!GetCell(i, y).IsEmpty) continue;
                return false;
            }

            return true;
        }
        
        public bool IsColumnFull(int x)
        {
            for (int i = 0; i < _height; i++)
            {
                if (!GetCell(x, i).IsEmpty) continue;
                return false;
            }

            return true;
        }

        public bool IsRowEmpty(int y)
        {
            for (int i = 0; i < _width; i++)
            {
                if (GetCell(i, y).IsEmpty) continue;
                return false;
            }

            return true;
        }
        
        public bool IsColumnEmpty(int x)
        {
            for (int i = 0; i < _height; i++)
            {
                if (GetCell(x, i).IsEmpty) continue;
                return false;
            }

            return true;
        }

        public bool IsFrontOfArrowEmpty(int x, int y)
        {
            return GetCell(x, y).IsEmpty;
        }

        private bool StoneCanMove(Stone stone, Direction direction)
        {
            Coord2D frontPos = stone.GridCoordinate;
            //Coord2D frontPos = stone.GetFrontCoord(stone.GridCoordinate);
            switch (direction)
            {
                case Direction.Up:
                    frontPos.y += 1;
                    break;
                case Direction.Down:
                    frontPos.y -= 1;
                    break;
                case Direction.Right:
                    frontPos.x += 1;
                    break;
                case Direction.Left:
                    frontPos.x -= 1;
                    break;
                case Direction.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
            
            return IsValidAccess(frontPos.x, frontPos.y) && GetCell(frontPos.x, frontPos.y).IsEmpty;
        }
        //but should not include direction.none stones
        public bool IsThereAnyStoneOnGrid()
        {
            for (int i = 0; i < _height; i++)
            {
                for (var j = 0; j < _width; j++)
                {
                    Stone temp = _stoneArray[j, i];
                    if (temp == null) continue;
                    if(temp.Direction == Direction.None)
                        continue;

                    return true;
                }
            }

            return false;
        }
        #endregion
        #region Utility
        private void ClearCells(IList<GridPart> cellList)
        {
            foreach (var part in cellList)
            {
                part.IsEmpty = true;
                //part.SetColor();
                part.stone = null;
            }
        }
        
        public IList<Stone> GetFilledList()
        {
            IList<Stone> result = new List<Stone>();
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if(GetCell(j, i).stone != null && GetCell(j, i).stone.Direction != Direction.None) 
                        result.Add(GetCell(j, i).stone);
                }
            }
            return result;
        }

        private IList<Stone> GetFilledListOnColumn(Coord2D coord)
        {
            int x = coord.x;
            
            IList<Stone> result = new List<Stone>();
            for (int i = 0; i < _height; i++)
            {
                GridPart cell = GetCell(x, i);
                if (cell != null && cell.stone != null && cell.stone.Direction != Direction.None)
                    result.Add(cell.stone);
            }
            return result;
        }

        private IList<Stone> GetFilledListOnRow(Coord2D coord)
        {
            int y = coord.y;
            
            IList<Stone> result = new List<Stone>();
            for (int i = 0; i < _width; i++)
            {
                GridPart cell = GetCell(i, y);
                if (cell != null && cell.stone != null && cell.stone.Direction != Direction.None)
                    result.Add(cell.stone);
            }
            return result;
        }

        private IList<Stone> GetFilledByColor(StoneColor color)
        {
            IList<Stone> result = new List<Stone>();
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    GridPart cell = GetCell(i, j);
                    if (cell == null || cell.stone == null || cell.stone.Direction == Direction.None)
                        continue;

                    if (cell.stone.StoneColor == color)
                        result.Add(cell.stone);
                }
            }
            return result;
        }

        //potential filling null stones
        public GridPart[] GetGridPartListInNeighbor(Coord2D coord2D) //Span<GridPart>
        {
            //Span<GridPart> result = stackalloc
            for (var i = 0; i < _gridBuffer.Length; i++)
                _gridBuffer[i] = null;
            
            int index = 0;
            _gridBuffer[index++] = GetCell(coord2D.x, coord2D.y + 1);
            _gridBuffer[index++] = GetCell(coord2D.x + 1, coord2D.y);
            _gridBuffer[index++] = GetCell(coord2D.x, coord2D.y - 1);
            _gridBuffer[index++] = GetCell(coord2D.x - 1, coord2D.y);
            
            return _gridBuffer;
        }
        
        public void LogFilledArray()
        {
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (GetCell(j, i).stone != null)
                    {
                        GridPart part = GetCell(j, i);
                        Debug.Log(part.name, part);
                        Debug.Log(part.stone.name, part.stone);
                    }
                }
            }
        }
        #endregion
    }
}