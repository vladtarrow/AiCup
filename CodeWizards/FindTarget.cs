﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeWizards2016.DevKit.CSharpCgdk
{
    public partial class MyStrategy
    {
        private long _LastMmTarget = 0;

        Target _findTarget(AWizard self, Point moveTo)
        {
            var t0 = FindBonusTarget(self);
            var tfrost = FindCastTarget(self, ProjectileType.FrostBolt);
            var tfball = FindCastTarget(self, ProjectileType.Fireball);
            var tmm = FindCastTarget(self, ProjectileType.MagicMissile);
            var t2 = FindStaffTarget(self);
            var t3 = FindCastTarget2(self, t0.Target ?? moveTo, ProjectileType.MagicMissile);
            var t3fball = FindCastTarget2(self, t0.Target ?? moveTo, ProjectileType.Fireball);
            var ult = FindUltimateTarget(self);

            Point ret = null;
            if (t0.Target != null)
            {
                FinalMove.Apply(t0.Move);
                ret = t0.Target;
            }

            if (tfball.Target != null && tfball.Damage + 1 >= t3fball.Damage && tfball.Damage > tmm.Damage)
            {
                FinalMove.Action = tfball.Move.Action;
                FinalMove.MinCastDistance = tfball.Move.MinCastDistance;
                FinalMove.MaxCastDistance = tfball.Move.MaxCastDistance;
                FinalMove.CastAngle = tfball.Move.CastAngle;
                return new Target { Type = ret == null ? TargetType.Opponent : TargetType.Bonus };
            }
            if (tfrost.Target != null && tfrost.Time <= Math.Min(t2.Time, t3.Time))
            {
                FinalMove.Action = tfrost.Move.Action;
                FinalMove.MinCastDistance = tfrost.Move.MinCastDistance;
                FinalMove.MaxCastDistance = tfrost.Move.MaxCastDistance;
                FinalMove.CastAngle = tfrost.Move.CastAngle;
                return new Target { Type = ret == null ? TargetType.Opponent : TargetType.Bonus };
            }
            if (tmm.Target != null && tmm.Time <= Math.Min(t2.Time, t3.Time))
            {
                var wiz = t3.Target as AWizard;
                if (wiz != null && CanRush(ASelf, wiz) && t3.TargetId != tmm.TargetId)
                {
                }
                else
                {
                    FinalMove.Action = tmm.Move.Action;
                    FinalMove.MinCastDistance = tmm.Move.MinCastDistance;
                    FinalMove.MaxCastDistance = tmm.Move.MaxCastDistance;
                    FinalMove.CastAngle = tmm.Move.CastAngle;
                    _LastMmTarget = tmm.TargetId;
                    return new Target {Type = ret == null ? TargetType.Opponent : TargetType.Bonus};
                }
            }
            if (t0.Target == null && t2.Target != null && t2.Time <= Math.Min(tmm.Time, t3.Time))
            {
                // иначе вообще не кидает хасту/щит
                if (ult.Target == null || t2.Target is AWizard)
                {
                    FinalMove.Apply(t2.Move);
                    return new Target {Type = TargetType.Opponent};
                }
            }
            if (t3fball.Target != null)
            {
                FinalMove.Apply(t3fball.Move);
                return new Target { Type = TargetType.Opponent };
            }
            if (t3.Target != null && t3.Time <= Math.Min(tmm.Time, t2.Time))
            {
                FinalMove.Apply(t3.Move);
                return new Target { Type = TargetType.Opponent };
            }

            if (ult.Target != null)
            {
                FinalMove.Apply(ult.Move);
                return new Target { Type = TargetType.Teammate };
            }

            if (ret == null)
                return null;
            

            return new Target { Type = TargetType.Bonus };
        }

        MovingInfo FindUltimateTarget(AWizard self)
        {
            var ret = new MovingInfo(null, int.MaxValue, new FinalMove(new Move()));

            const int threshold = 29;
            var minTicks = int.MaxValue;
            AWizard selTarget = null;
            var selAction = ActionType.None;

            foreach (var action in new[] {ActionType.Haste, ActionType.Shield})
            {
                if (action == ActionType.Haste && self.HasteSkillLevel < 5 ||
                    action == ActionType.Shield && self.ShieldSkillLevel < 5
                    )
                    continue;

                var teammates = MyWizards
                    .Where(x => x.Id != self.Id
                                && self.GetDistanceTo(x) <= self.CastRange
                                && x.RemainingStatusByAction(action) <= threshold
                    ).
                    ToArray();

                foreach (var teammate in teammates)
                {
                    var ticks = 0;
                    var my = new AWizard(self);
                    while (Math.Abs(my.GetAngleTo(teammate)) > Game.StaffSector/2)
                    {
                        ticks++;
                        my.MoveTo(null, teammate);
                    }
                    if (ticks < minTicks && my.CanCastUltimate(action, teammate))
                    {
                        minTicks = ticks;
                        selTarget = teammate;
                        selAction = action;
                    }
                }

                if (selTarget == null
                    && teammates.Length == 0
                    && self.RemainingStatusByAction(action) <= threshold
                    && self.CanUseUltimate(action)
                    )
                {
                    minTicks = 100;
                    selTarget = self;
                    selAction = action;
                }
            }

            if (selTarget == null)
                return ret;

            ret.Target = selTarget;
            ret.TargetId = selTarget.Id;
            ret.Time = minTicks;

            if (self.CanCastUltimate(selAction, selTarget))
            {
                ret.Move.Action = selAction;
                ret.Move.StatusTargetId = selTarget.Id;
            }
            else
            {
                ret.Move.MoveTo(null, selTarget);
            }

            return ret;
        }

        MovingInfo FindTreeTarget(AWizard self)
        {
            var res = new MovingInfo(null, int.MaxValue, new FinalMove(new Move()));
            var trees = TreesObserver.Trees.Where(t => self.GetDistanceTo(t) <= Game.StaffRange + t.Radius).ToArray();
            var minTicks = int.MaxValue;
            ATree selTarget = null;

            foreach (var tree in trees)
            {
                var my = new AWizard(self);
                var ticks = 0;
                while (Math.Abs(my.GetAngleTo(tree)) > Game.StaffSector/2)
                {
                    my.MoveTo(null, tree);
                    ticks++;
                }
                if (ticks < minTicks && my.CanStaffAttack(tree))
                {
                    minTicks = ticks;
                    selTarget = tree;
                }
            }

            if (selTarget == null)
                return res;

            res.Time = minTicks;
            res.Target = selTarget;
            res.TargetId = selTarget.Id;
            if (minTicks == 0)
                res.Move.Action = ActionType.Staff;
            else
                res.Move.MoveTo(null, selTarget);

            return res;
        }

        MovingInfo FindBonusTarget(AWizard self)
        {
            var minTime = int.MaxValue;
            var selGo = 0;
            Point selMoveTo = null;
            foreach (var _bonus in BonusesObserver.Bonuses)
            {
                if (_bonus.GetDistanceTo(self) - self.Radius - _bonus.Radius > Game.StaffRange * 3)
                    continue;
                if (_bonus.RemainingAppearanceTicks > 60)
                    continue;

                var nearest = Combats
                    .Where(x => x.Id != self.Id && self.GetDistanceTo2(x) < Geom.Sqr(self.VisionRange))
                    .ToArray();

                foreach (var angle in Utility.Range(self.Angle, Math.PI * 2 + self.Angle, 24, false))
                {
                    var bonus = new ABonus(_bonus);
                    var my = new AWizard(self);
                    var moveTo = my + Point.ByAngle(angle) * self.VisionRange;
                    int time = 0;
                    int go = 0;
                    while (my.GetDistanceTo(bonus) > my.Radius + bonus.Radius && time < 60)
                    {
                        if (!my.MoveTo(moveTo, null, w => !CheckIntersectionsAndTress(w, nearest)))
                        {
                            break;
                        }
                        var wait = !bonus.Exists;
                        bonus.SkipTick();
                        time++;
                        if (my.GetDistanceTo(bonus) <= my.Radius + bonus.Radius)
                        {
                            while (!bonus.Exists)
                            {
                                bonus.SkipTick();
                                time++;
                            }
                            if (wait)
                                time++;

                            if (time < minTime)
                            {
                                minTime = time;
                                selMoveTo = moveTo;
                                selGo = go;
                            }
                            break;
                        }
                        go++;
                    }

                }
            }
            var moving = new MovingInfo(selMoveTo, minTime, new FinalMove(new Move()));
            if (selMoveTo != null)
            {
                if (minTime == 1 || selGo > 0)
                    moving.Move.MoveTo(selMoveTo, null);
                else
                    moving.Target = self;
            }
            return moving;
        }

        MovingInfo _findStaffTarget(AWizard self)
        {
            var potentialColliders = Combats
                .Where(x => x.Id != self.Id && self.GetDistanceTo2(x) < Geom.Sqr(Game.StaffRange * 6))
                .ToArray();
            int minTicks = int.MaxValue;
            var move = new FinalMove(new Move());

            var attacked = self.GetStaffAttacked(potentialColliders).Cast<ACombatUnit>().ToArray();

            ACircularUnit selTarget = attacked.FirstOrDefault(x => x.IsOpponent);
            if (selTarget != null) // если уже можно бить
            {
                move.Action = ActionType.Staff;
                return new MovingInfo(selTarget, 0, move);
            }

            if (self.MmSkillLevel == 5)
            {
                // т.к. стрелять можно без задержки
                // возможно, нужно сделать исключение, если прокачан посох
                return new MovingInfo(null, int.MaxValue, move);
            }

            Point selMoveTo = null;

            foreach (var opp in OpponentCombats)
            {
                var dist = self.GetDistanceTo(opp);
                if (dist > Game.StaffRange * 5 || !opp.IsAssailable)
                    continue;

                var range = opp.Radius + Game.StaffRange;
                foreach (var delta in new[] { -range, -range / 2, 0, range / 2, range })
                {
                    var angle = Math.Atan2(delta, dist);
                    var moveTo = self + (opp - self).Normalized().RotateClockwise(angle) * self.VisionRange;

                    var nearstCombats = Combats
                        .Where(x => x.GetDistanceTo(self) <= Math.Max(x.VisionRange, self.VisionRange) * 1.2)
                        .Select(Utility.CloneCombat)
                        .ToArray();

                    var targetsSelector = new TargetsSelector(nearstCombats) {EnableMinionsCache = true};
                    var nearstOpponents = nearstCombats.Where(x => x.IsOpponent).ToArray();

                    var my = nearstCombats.FirstOrDefault(x => x.Id == self.Id) as AWizard;
                    var his = nearstCombats.FirstOrDefault(x => x.Id == opp.Id);

                    var allowRush = opp is AFetish || opp is AWizard;
                    var canHitNow = opp.EthalonCanHit(self, checkCooldown: !allowRush);

                    var ticks = 0;
                    var ok = true;
                    var buildingsHit = false;

                    while (ticks < (allowRush ? 65 : 35) && my.GetDistanceTo2(his) > Geom.Sqr(Game.StaffRange + his.Radius))
                    {
                        foreach (var x in nearstOpponents) // свои как-бы стоят на месте
                        {
                            var tar = targetsSelector.Select(x);
                            buildingsHit = buildingsHit ||
                                           (x is ABuilding && tar != null && tar.Id == my.Id && x.EthalonCanHit(my));
                            x.EthalonMove(tar ?? my);
                        }

                        if (!my.MoveTo(moveTo, his, w => !CheckIntersectionsAndTress(w, potentialColliders)))
                        {
                            ok = false;
                            break;
                        }
                        ticks++;
                    }

                    if (ok && !(opp is AOrc))
                    {
                        while (Math.Abs(my.GetAngleTo(his)) > Game.StaffSector / 2)
                        {
                            my.MoveTo(null, his);
                            foreach (var x in nearstOpponents)
                            {
                                var tar = targetsSelector.Select(x);
                                buildingsHit = buildingsHit ||
                                               (x is ABuilding && tar != null && tar.Id == my.Id && x.EthalonCanHit(my));
                                x.EthalonMove(tar ?? my);
                            }
                            ticks++;
                        }
                    }

                    Func<ACombatUnit, bool> check = x =>
                    {
                        if ((opp is AWizard) && (opp as AWizard).IsBesieded && !(x is ABuilding))
                            return true;

                        if (canHitNow && x.Id == opp.Id) // он и так доставал
                            return true;

                        if (!x.EthalonCanHit(my) && (!(x is ABuilding) || !buildingsHit))
                            return true;

                        if (his.Id == x.Id && my.StaffDamage >= his.Life)
                            return true;

                        var target = targetsSelector.Select(x);
                        if (target != null && target.Id != my.Id)
                            return true;

                        return false;
                    };

                    if (opp is AWizard)
                    {
                        ticks -= 5;
                        if ((opp as AWizard).IsBesieded)
                        {
                            ticks -= 10;
                        }
                    }

                    if (ok && ticks < minTicks)
                    {
                        if (my.CanStaffAttack(his))
                        {
                            if (nearstOpponents.All(check))
                            {
                                // успею-ли я вернуться обратно
                                while (my.GetDistanceTo(self) > my.MaxForwardSpeed)//TODO:HACK
                                {
                                    my.MoveTo(self, null);
                                    foreach (var x in nearstOpponents)
                                    {
                                        var tar = targetsSelector.Select(x);

                                        buildingsHit = buildingsHit ||
                                            (x is ABuilding && tar != null && tar.Id == my.Id && x.EthalonCanHit(my));

                                        if (tar != null)
                                            x.EthalonMove(tar);
                                        else
                                            x.SkipTick();
                                    }
                                }
                                if (nearstOpponents.All(check))
                                {
                                    selTarget = opp;
                                    selMoveTo = moveTo;
                                    minTicks = ticks;
                                }
                            }
                        }
                    }
                }
            }
            if (selTarget != null)
            {
                bool angleOk = Math.Abs(self.GetAngleTo(selTarget)) <= Game.StaffSector / 2,
                    distOk = self.GetDistanceTo2(selTarget) <= Geom.Sqr(Game.StaffRange + selTarget.Radius);

                if (!distOk)
                    move.MoveTo(selMoveTo, selTarget);
                else if (!angleOk)
                    move.MoveTo(null, selTarget);
            }
            return new MovingInfo(selTarget, Math.Max(0, minTicks), move);
        }

        private bool _isFireballGoodSeg(AWizard self, AProjectile.ProjectilePathSegment seg)
        {
            if (seg.State == AProjectile.ProjectilePathState.Free)
                return false;

            if (seg.SelfDamage > 0) // TODO: можно и пожертвовать
                return false;

            return seg.OpponentBurned > 2
                   || seg.OpponentBurned > 0 && seg.Target is AWizard
                   || seg.OpponentBurned > 0
                       && seg.Target is ABuilding
                       && self.Mana >= 2*Game.FireballManacost
                       && seg.OpponentDamage > Game.FireballExplosionMaxDamage - 1 //TODO: костыль
                ;
        }

        MovingInfo _findCastTarget(AWizard self, ProjectileType projectileType)
        {
            var actionType = Utility.GetActionByProjectileType(projectileType);

            var move = new FinalMove(new Move());
            if (self.RemainingActionCooldownTicks > 0 || 
                self.RemainingCooldownTicksByAction[(int) actionType] > 0 ||
                self.Mana < Const.ProjectileInfo[(int) projectileType].ManaCost ||
                !self.IsActionAvailable(actionType)
                )
                return new MovingInfo(null, int.MaxValue, move);

            var angles = new List<double>();
            foreach (var x in OpponentCombats)
            {
                var distTo = self.GetDistanceTo(x);
                if (distTo > self.CastRange + x.Radius + Const.ProjectileInfo[(int)projectileType].DamageRadius + 3)
                    continue;

                var angleTo = self.GetAngleTo(x);
                if (Math.Abs(angleTo) > Math.PI/3)
                    continue;

                var deltaAngle = Math.Atan2(x.Radius, distTo);
                angles.AddRange(new[] {angleTo, angleTo + deltaAngle, angleTo - deltaAngle}.Where(a => Math.Abs(a) <= Game.StaffSector/2));
            }
            if (angles.Count > 0)
                angles.AddRange(Utility.Range(-Game.StaffSector / 2, Game.StaffSector / 2, 16));

            ACombatUnit selTarget = null;
            double
                selMinDist = 0,
                selMaxDist = self.CastRange + 20,
                selCastAngle = 0,
                selMaxDamage = 0;

            if (projectileType == ProjectileType.Fireball)
            {
                var maxDamage = 0.0;
                var maxBurned = 0;

                foreach (var angle in angles)
                {
                    var proj = new AProjectile(new AWizard(self), angle, projectileType);
                    var path = EmulateProjectileWithNearest(proj);
                    for (var i = 0; i < path.Count; i++)
                    {
                        var seg = path[i];

                        if (_isFireballGoodSeg(self, seg))
                        {
                            if (seg.OpponentBurned > maxBurned
                                || seg.OpponentBurned == maxBurned && seg.OpponentDamage > maxDamage
                                //|| seg.OpponentBurned == maxBurned && Utility.Equals(seg.OpponentDamage, maxDamage)
                                //TODO: combare by angle and priority
                                )
                            {
                                maxBurned = seg.OpponentBurned;
                                maxDamage = seg.OpponentDamage;
                                selCastAngle = angle;
                                selMinDist = selMaxDist = seg.StartDistance;
                                selTarget = seg.Target;
                                selMaxDamage = seg.OpponentDamage;
                            }
                        }
                    }
                }
            }
            else
            {

                double
                    selPriority = int.MaxValue,
                    selAngleTo = 0;

                foreach (var angle in angles)
                {
                    var proj = new AProjectile(new AWizard(self), angle, projectileType);
                    var path = EmulateProjectileWithNearest(proj);
                    for (var i = 0; i < path.Count; i++)
                    {
                        if (path[i].State == AProjectile.ProjectilePathState.Free)
                            continue;

                        // TODO: если можно убить нескольких, убивать того, у кого больше жизней
                        var combat = path[i].Target;
                        if (!combat.IsAssailable)
                            continue;

                        var myAngle = self.Angle + angle;
                        var hisAngle = self.Angle + self.GetAngleTo(combat);
                        var angleTo = Geom.GetAngleBetween(myAngle, hisAngle);

                        var priority = GetCombatPriority(self, combat);
                        if (combat.IsOpponent &&
                            (priority < selPriority || Utility.Equals(priority, selPriority) && angleTo < selAngleTo) &&
                            self.CheckProjectileCantDodge(proj, Combats.FirstOrDefault(x => x.Id == combat.Id))
                            )
                        {
                            selTarget = combat;
                            selCastAngle = angle;
                            selAngleTo = angleTo;
                            selMinDist = i == 0 ||
                                         path[i - 1].State == AProjectile.ProjectilePathState.Free &&
                                         path[i - 1].Length < 40
                                ? path[i].StartDistance - 1
                                : path[i].StartDistance - 20;
                            selMaxDist = i >= path.Count - 2
                                ? (self.CastRange + 500)
                                : (path[i + 1].EndDistance + path[i].EndDistance)/2;
                            selPriority = priority;
                            selMaxDamage = path[i].OpponentDamage;
                        }
                    }
                }
            }
            if (selTarget == null)
                return new MovingInfo(null, int.MaxValue, move);

            move.Action = actionType;
            move.MinCastDistance = selMinDist;
            move.MaxCastDistance = selMaxDist;
            move.CastAngle = selCastAngle;
#if DEBUG
            _lastProjectileTick = World.TickIndex;
            _lastProjectilePoints = new[]
            {
                self + Point.ByAngle(self.Angle + selCastAngle) * selMinDist,
                self + Point.ByAngle(self.Angle + selCastAngle) * Math.Min(Self.CastRange, selMaxDist),
            };
#endif
            return new MovingInfo(selTarget, 0, move) {Damage = selMaxDamage, TargetId = selTarget.Id};
        }

        MovingInfo _findCastTarget2(AWizard self, Point moveTo, ProjectileType projectileType)
        {
            var move = new FinalMove(new Move());

            if (projectileType == ProjectileType.MagicMissile)
            {
                AUnit mmSelTarget = null;
                Point mmSelFirstMoveTo = null;
                var mmMinTicks = int.MaxValue;
                double mmMinPriority = int.MaxValue;

                foreach (var opp in OpponentCombats)
                {
                    if (self.GetDistanceTo2(opp) > Geom.Sqr(self.CastRange + opp.Radius + 40) || !opp.IsAssailable)
                        continue;

                    var nearest = Combats
                        .Where(x => self.GetDistanceTo2(x) < Geom.Sqr(Math.Max(x.VisionRange, self.VisionRange) * 1.3))
                        .Select(Utility.CloneCombat)
                        .ToArray();

                    var targetsSelector = new TargetsSelector(nearest) {EnableMinionsCache = true};

                    var nearstOpponents = nearest
                        .Where(x => x.IsOpponent)
                        .ToArray();

                    var canHitNow = opp.EthalonCanHit(self, checkCooldown: !(opp is AWizard));

                    var ticks = 0;
                    var my = nearest.FirstOrDefault(x => x.Id == self.Id) as AWizard;
                    var his = nearest.FirstOrDefault(x => x.Id == opp.Id);
                    if (my == null || his == null)
                        continue;

                    Point firstMoveTo = null;
                    var buildingsHit = false;
                    while (!my.EthalonCanCastMagicMissile(his, checkCooldown: false))
                    {
                        if (ticks > 40)
                            break;

                        var m = moveTo;
                        var stopIfCannotMove = true;
                        if (m == null && my.EthalonCanCastMagicMissile(his, checkCooldown: false, checkAngle: false))
                        {
                            stopIfCannotMove = false;
                            m = my + (my - his);
                            var tmp = new AWizard(my);
                            tmp.MoveTo(m, his, w => !CheckIntersectionsAndTress(w, nearest));
                            
                            if (EstimateDanger(my, false) <= EstimateDanger(tmp, false))
                                m = null;
                        }
                        if (m == null)
                            m = his;

                        if (ticks == 0)
                            firstMoveTo = m;

                        if (!my.MoveTo(m, his, w => !CheckIntersectionsAndTress(w, nearest)) && Utility.PointsEqual(m, his) && stopIfCannotMove)
                            break;
                        
                        foreach (var x in nearest)
                        {
                            if (x.Id == my.Id)
                                continue;
                            var tar = targetsSelector.Select(x);
                            buildingsHit = buildingsHit ||
                                           (x.IsOpponent && x is ABuilding && tar != null && tar.Id == my.Id && x.EthalonCanHit(my));

                            if (x.IsOpponent)
                                x.EthalonMove(tar ?? my);
                            else if (tar != null)
                                x.EthalonMove(tar);
                            else
                                x.SkipTick();
                        }
                        ticks++;
                    }

                    if (his is AWizard && (his as AWizard).IsBesieded)
                        ticks -= 15; // чтобы дать больше приоритета визарду

                    var priority = GetCombatPriority(self, his);
                    if (ticks < mmMinTicks || ticks == mmMinTicks && priority < mmMinPriority)
                    {
                        if (my.EthalonCanCastMagicMissile(his))
                        {
                            if (nearstOpponents.All(x =>
                            {
                                if (canHitNow && x.Id == opp.Id) // он и так доставал
                                    return true;

                                if (!x.EthalonCanHit(my) && (!(x is ABuilding) || !buildingsHit))
                                    return true;

                                if (his.Id == x.Id && CanRush(my, x))
                                    return true;

                                var target = targetsSelector.Select(x);
                                if (target != null && target.Id != my.Id)
                                    return true;

                                return false;
                            })
                                )
                            {
                                mmMinTicks = ticks;
                                mmMinPriority = priority;
                                mmSelTarget = opp;
                                mmSelFirstMoveTo = firstMoveTo;
                            }
                        }
                    }
                }


                if (mmSelTarget != null)
                {
                    mmMinTicks = Math.Max(0, mmMinTicks);
                    move.MoveTo(moveTo ?? mmSelFirstMoveTo, mmSelTarget);
                    return new MovingInfo(mmSelTarget, mmMinTicks, move) {TargetId = mmSelTarget.Id};
                }
            }


            const int walkLimit = 9;

            if (projectileType == ProjectileType.Fireball && self.FireballSkillLevel == 5 && Math.Max(self.RemainingActionCooldownTicks, self.RemainingFireballCooldownTicks) <= walkLimit)
            {
                var fbMaxDamage = 0.0;
                Point fbSelTarget = null;
                var fbMinTicks = int.MaxValue;

                foreach (var ang in Utility.Range(-Game.StaffSector, Game.StaffSector, 10))
                {
                    var nearest = Combats
                        .Where(x => self.GetDistanceTo2(x) < Geom.Sqr(Math.Max(x.VisionRange, self.VisionRange) * 1.3))
                        .Select(Utility.CloneCombat)
                        .ToArray();

                    var targetsSelector = new TargetsSelector(nearest) {EnableMinionsCache = true};

                    var ticks = 0;
                    var my = nearest.FirstOrDefault(x => x.Id == self.Id) as AWizard;
                    var dir = my + Point.ByAngle(my.Angle + ang)*1000;

                    while (ticks <= walkLimit)
                    {
                        if (my.CanUseFireball())
                        {
                            var proj = new AProjectile(my, 0, ProjectileType.Fireball);
                            var path = proj.Emulate(nearest, 0.0);

                            var damage =
                                path.Where(x => _isFireballGoodSeg(my, x))
                                    .Select(x => x.OpponentDamage)
                                    .DefaultIfEmpty(0)
                                    .Max();
                            if (damage > fbMaxDamage)
                            {
                                fbMaxDamage = damage;
                                fbSelTarget = dir;
                                fbMinTicks = ticks;
                            }
                        }

                        foreach (var x in nearest)
                        {
                            if (x.Id == my.Id)
                                continue;

                            if (x is AMinion)
                                x.EthalonMove(targetsSelector.Select(x));
                            else
                                x.SkipTick();
                        }

                        if (!my.MoveTo(dir, dir, w => !CheckIntersectionsAndTress(w, nearest)))
                            break;
                        
                        if (nearest.Any(x => x.IsOpponent && x is ABuilding && x.EthalonCanHit(my) && targetsSelector.Select(x) == my))
                            break;
                        ticks++;
                    }
                }


                if (fbSelTarget != null)
                {
                    move.MoveTo(fbSelTarget, fbSelTarget);
                    return new MovingInfo(fbSelTarget, fbMinTicks, move) {Damage = fbMaxDamage};
                }
            }

            return new MovingInfo(null, int.MaxValue, move);
        }

        Target FindTarget(AWizard self, Point moveTo = null)
        {
            TimerStart();
            var ret = _findTarget(self, moveTo);
            TimerEndLog("FindTarget", 1);
            return ret;
        }

        MovingInfo FindStaffTarget(AWizard self)
        {
            TimerStart();
            var ret = _findStaffTarget(self);
            TimerEndLog("FindStaffTarget", 1);
            return ret;
        }

        MovingInfo FindCastTarget(AWizard self, ProjectileType projectileType)
        {
            TimerStart();
            var ret = _findCastTarget(self, projectileType);
            TimerEndLog("FindCastTarget", 1);
            return ret;
        }

        MovingInfo FindCastTarget2(AWizard self, Point moveTo, ProjectileType projectileType)
        {
            TimerStart();
            var ret = _findCastTarget2(self, moveTo, projectileType);
            TimerEndLog("FindCastTarget2", 1);
            return ret;
        }

        List<AProjectile.ProjectilePathSegment> EmulateProjectileWithNearest(AProjectile projectile)
        {
            return projectile.Emulate(Combats.Where(x => x.GetDistanceTo(ASelf) - x.Radius < 900).ToArray(), 0.0);
        }

        static double GetCombatPriority(AWizard self, ACombatUnit unit)
        {
            // чем меньше - тем важнее стрелять в него первого
            var res = unit.Life;
            if (unit is AWizard)
            {
                res /= 4;
                if ((unit as AWizard).IsBesieded)
                {
                    res /= 4;
                }
                res -= 50;
            }
            var dist = self.GetDistanceTo(unit);
            if (dist <= Game.StaffRange + unit.Radius + 10)
            {
                res -= 60;
                res += Math.Log(dist);
            }
            return res;
        }

        public static int _lastProjectileTick;
        public static Point[] _lastProjectilePoints;

        bool CanRush(AWizard self, ACombatUnit opp)
        {
            var wizard = opp as AWizard;
            var minion = opp as AMinion;

            if (wizard != null)
            {
                if (wizard.IsBesieded)
                    return true;

                if (wizard.Life <= self.MagicMissileDamage)
                    return true;
                if (self.Life <= wizard.MagicMissileDamage)
                    return false;

                if (self.Life >= wizard.Life + 3 * self.MagicMissileDamage)
                    return true;
            }
            else if (minion != null)
            {
                if (minion.Life <= self.MagicMissileDamage)
                    return true;
            }
            return false;
        }

        bool CheckIntersectionsAndTress(AWizard self, IEnumerable<ACircularUnit> units)
        {
            if (self.GetFirstIntersection(units) != null)
                return true;
            var nearestTree = TreesObserver.GetNearestTree(self);
            return nearestTree != null && self.IntersectsWith(nearestTree);
        }




        enum TargetType
        {
            Opponent,
            Teammate,
            Bonus,
        }

        class Target
        {
            public TargetType Type;
        }

        class MovingInfo
        {
            public Point Target;
            public int Time;
            public FinalMove Move;
            public double Damage;
            public long TargetId;

            public MovingInfo(Point target, int time, FinalMove move)
            {
                Target = target;
                Time = time;
                Move = move;
            }
        }
    }
}
