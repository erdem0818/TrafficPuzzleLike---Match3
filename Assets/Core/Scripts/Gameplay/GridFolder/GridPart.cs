using System;
using System.Collections.Generic;
using System.Linq;
using Core.Scripts.Gameplay.QuestObjects;
using Core.Scripts.Gameplay.StoneFolder;
using UnityEngine;

namespace Core.Scripts.Gameplay.GridFolder
{
    public class GridPart : MonoBehaviour
    {
        [SerializeField] private Coord2D _coord;
        public Coord2D GetGridPos => new Coord2D {x = _coord.x, y = _coord.y};
        
        [SerializeField] public bool IsEmpty = true;
        
        [SerializeField] public Stone stone;
        [SerializeField] public QuestObject questObject;

        [SerializeField] private List<Arrow> _arrows = new ();
        
        public void SetPosition(int x, int z)
        {
            _coord.x = x;
            _coord.y = z;
            transform.localPosition = new Vector3(x, 0f, z);
        }
        
        public void AddArrow(Arrow arrow)
        {
            if (!_arrows.Contains(arrow))
                _arrows.Add(arrow);
        }

        public void SetArrows(bool b)
        {
            foreach (var arrow in _arrows)
            {
                arrow.CanPlaceable = b;
                arrow.SetArrowColor();
            }
        }

        public bool Any(Direction direction)
        {
            return _arrows.Any(a => a.Direction == direction);
        }

        public bool Has(Direction direction)
        {
            return direction switch
            {
                Direction.Up => Any(Direction.Down),
                Direction.Down => Any(Direction.Up),
                Direction.Right => Any(Direction.Left),
                Direction.Left => Any(Direction.Right),
                Direction.None => false,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }
    }
}