﻿using System.Collections.Generic;
using System.Linq;
using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    public class ACircularUnit : AUnit
    {
        public double Radius;

        public ACircularUnit(CircularUnit unit) : base(unit)
        {
            Radius = unit.Radius;
        }

        public ACircularUnit(ACircularUnit unit) : base(unit)
        {
            Radius = unit.Radius;
        }

        public ACircularUnit()
        {
        }

        public bool IntersectsWith(ACircularUnit unit)
        {
            // если касаются, то false
            return GetDistanceTo2(unit) < Geom.Sqr(Radius + unit.Radius);
        }

        public ACircularUnit CheckIntersections(IEnumerable<ACircularUnit> units)
        {
            return units.FirstOrDefault(IntersectsWith);
        }
    }
}