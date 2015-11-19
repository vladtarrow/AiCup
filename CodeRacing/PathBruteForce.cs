﻿using System;
using System.Drawing;
using System.Linq;

namespace Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk
{
    public class PathPattern
    {
        public int From;
        public int To;
        public int Step;

        public AMove Move;
    }

    public enum TurnPatterns
    {
        ToNext,
        ToCenter,
        FromCenter
    }

    public class TurnPattern
    {
        public TurnPatterns Pattern;
        //public double Coeff = 1;
    }

    public class PathBruteForce
    {
        public const double BonusImportanceCoeff = 20;

        public readonly PathPattern[] Patterns;
        public ACar Self;
        public int Id;

        private PathPattern[] _patterns;
        private Moves _movesStack, _bestMovesStack;
        private int _bestTime;
        private double _bestImportance;
        private Point[] _bruteWayPoints;

        private delegate void CarCallback(ACar car, int time, double importance);

        private Moves _cache;
        public int LastSuccess; // Когда последний раз брут что-то находил
        private int _lastCall; // Когда последний раз вызывали. Если не вызывали - значит был success

        private Point _turnCenter, _turnTo;
        private double _needDist;
        private int _interval;

        private ABonus[] _bonusCandidates;

        private static bool _isBetterTime(int time1, double importance1, int time2, double importance2)
        {
            return time1 - importance1 * BonusImportanceCoeff < time2 - importance2 * BonusImportanceCoeff;
        }

        public PathBruteForce(PathPattern[] patterns, int interval, int id)
        {
            Patterns = patterns;
            _interval = interval;
            Id = id;
        }

        private void _doRecursive(ACar model, int idx, int totalTime, double totalImportance)
        {
            model = model.Clone();

            if (idx == _patterns.Length)
            {
                var m = new AMove
                {
                    EnginePower = 1,
                    IsBrake = false,
                    WheelTurn = _turnTo.Clone(),
                    Times = 0
                };

                var end = true;
                for (; /*totalTime < _bestTime &&*/ _turnTo.GetDistanceTo2(model) > _needDist * _needDist; totalTime++)
                {
                    if (!_modelMove(model, m, ref totalImportance))
                    {
                        end = false;
                        break;
                    }
                    m.Times++;
                }
                if (!MyStrategy.CheckVisibility(Self.Original, model, _turnTo))
                    return;

                if (end && _isBetterTime(totalTime, totalImportance, _bestTime, _bestImportance))
                {
                    _bestTime = totalTime;
                    _bestImportance = totalImportance;
                    _bestMovesStack = _movesStack.Clone();
                    _bestMovesStack.Add(m);
                }
                return;
            }

            var pattern = _patterns[idx];
            _carMoveFunc(model, pattern.From, pattern.To, pattern.Step,
                new AMove
                {
                    EnginePower = pattern.Move.EnginePower,
                    IsBrake = pattern.Move.IsBrake,
                    WheelTurn = pattern.Move.WheelTurn,
                    Times = 0
                }, totalTime, totalImportance, (aCar, totalTimeAfter, totalImportanceAfter) =>
                {
                    // TODO: как-то отсечь мб?
                    _doRecursive(aCar.Clone(), idx + 1, totalTimeAfter, totalImportanceAfter);
                });
        }

        private Moves _lastSuccessStack;

        private int _selectThisTick;

        public void SelectThis()
        {
            _selectThisTick = MyStrategy.world.Tick;
        }

        public Moves Do(ACar car, Points pts)
        {
            // Проверка что данный путь был выбран
            if (_selectThisTick + 1 != MyStrategy.world.Tick)
                _lastSuccessStack = null;

            Self = car.Clone();

            if (_lastCall == LastSuccess)
                LastSuccess = _lastCall;

            for (var t = 0; t < MyStrategy.world.Tick - _lastCall && _lastSuccessStack != null && _lastSuccessStack.Count > 0; t++)
            {
                _lastSuccessStack[0].Times--;
                _lastSuccessStack.Normalize();
            }
            if (_lastSuccessStack != null && _lastSuccessStack.Count == 0)
                _lastSuccessStack = null;

            _lastCall = MyStrategy.world.Tick;

            // Если был success на прошлом тике, то продолжаем. Или каждые _interval тиков.
            if (LastSuccess != MyStrategy.world.Tick - 1 && (MyStrategy.world.Tick - (LastSuccess + 1))%_interval != 0)
                return _lastSuccessStack;

            _turnCenter = pts[1];

            var extended = ExtendWaySegments(pts);
            _bruteWayPoints = extended.GetRange(0, Math.Min(70, extended.Count)).ToArray();
#if DEBUG
            var bruteWayPoints = new Points();
            bruteWayPoints.AddRange(_bruteWayPoints);
            MyStrategy.SegmentsDrawQueue.Add(new Tuple<Brush, Points>(Brushes.Brown, bruteWayPoints));
#endif
            _needDist = MyStrategy.game.TrackTileSize/2;
            _turnTo = _bruteWayPoints[_bruteWayPoints.Length - 1];
#if DEBUG
            MyStrategy.CircleFillQueue.Add(new Tuple<Brush, ACircle>(Brushes.OrangeRed, new ACircle { X = _turnTo.X, Y = _turnTo.Y, Radius = 20}));
#endif

            _patterns = Patterns.Select(pt => new PathPattern
            {
                From = pt.From,
                To = pt.To,
                Step = pt.Step,
                Move = pt.Move.Clone()
            }).ToArray();
            foreach (var p in _patterns)
            {
                if (p.Move.WheelTurn is TurnPattern)
                {
                    var turnPattern = p.Move.WheelTurn as TurnPattern;
                    if (turnPattern.Pattern == TurnPatterns.ToCenter)
                        p.Move.WheelTurn = _turnCenter;
                    else if (turnPattern.Pattern == TurnPatterns.ToNext)
                        p.Move.WheelTurn = Self.GetAngleTo(_turnTo) < 0 ? -1 : 1;
                    else if (turnPattern.Pattern == TurnPatterns.FromCenter)
                        p.Move.WheelTurn = Self.GetAngleTo(_turnCenter) < 0 ? 1 : -1;
                }
            }

            _movesStack = new Moves();
            _bestMovesStack = new Moves();
            _bestTime = MyStrategy.Infinity;
            _bestImportance = 0;

            _bonusCandidates = MyStrategy.world.Bonuses
                .Where(b => Self.GetDistanceTo(b) < MyStrategy.game.TrackTileSize*5)
                .Select(b => new ABonus(b))
                .Where(b =>
                {
                    var selfCell = MyStrategy.GetCell(Self);
                    var bCell = MyStrategy.GetCell(b);
                    if (selfCell.Equals(bCell))
                        return true;
                    var dist = MyStrategy.BfsDist(selfCell.I, selfCell.J, bCell.I, bCell.J, new Cell[]{});
                    return dist <= 5;
                })
                .ToArray();

            if (_cache != null)
            {
                for (var k = 0; k < _patterns.Length; k++)
                {
                    _patterns[k].From = Math.Max(0, _cache[k].Times - 8);
                    _patterns[k].To = _cache[k].Times + 8;
                    _patterns[k].Step = 2;
                }
            }

            _doRecursive(Self, 0, 0, 0);
            _cache = null;
            if (_bestTime == MyStrategy.Infinity)
                return _lastSuccessStack;

            if (_bestMovesStack.ComputeTime() != _bestTime)
                throw new Exception("ComputeTime != BestTime");

            LastSuccess = MyStrategy.world.Tick;
            _cache = _bestMovesStack.Clone();
            _bestMovesStack.Normalize();
            _lastSuccessStack = _bestMovesStack.Clone();
            return _bestMovesStack;
        }


        private void _carMoveFunc(ACar model, int from, int to, int step, AMove m, int time, double importance, CarCallback callback)
        {
            model = model.Clone();
            m.Times = 0;

            for (var i = 0; i < from; i++)
            {
                if (!_modelMove(model, m, ref importance))
                    return;
                m.Times++;
            }

            for (var t = from; t <= to; t += step)
            {
                _movesStack.Add(m);
                callback(model, time + t, importance);
                _movesStack.Pop();
                for (var r = 0; r < step; r++)
                {
                    if (!_modelMove(model, m, ref importance))
                        return;
                    m.Times++;
                }
            }
        }


        private bool _modelMove(ACar car, AMove m, ref double totalImportance)
        {
            var turn = m.WheelTurn is Point ? MyStrategy.TurnRound(car.GetAngleTo(m.WheelTurn as Point)) : Convert.ToDouble(m.WheelTurn);
            var prevCar = car.Clone();
            car.Move(m.EnginePower, turn, m.IsBrake, false);
            foreach (var bonus in _bonusCandidates)
            {
                if (car.TakeBonus(bonus) && !prevCar.TakeBonus(bonus))
                    totalImportance += bonus.GetImportance(car.Original);
            }
            return car.GetRect().All(p => !MyStrategy.IntersectTail(p));
        }

        public Points ExtendWaySegments(Points pts)
        {
            var res = new Points();
            for (var idx = 1; idx < pts.Count; idx++)
            {
                var a = pts[idx - 1];
                var b = pts[idx];

                var delta = 50.0;
                var c = (int)(a.GetDistanceTo(b) / delta + 2);
                delta = a.GetDistanceTo(b) / c;
                var dir = (b - a).Normalized();
                for (var i = 0; i <= c; i++)
                    res.Add(a + dir * (delta * i));
            }
            return res;
        }
    }
}
