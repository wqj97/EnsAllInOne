﻿using EnsoulSharp;
using EnsoulSharp.SDK;
using FunnySlayerCommon;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominationAIO.NewPlugins
{
    public static class EventsIrelia
    {
        public static void KS(EventArgs args)
        {
            if (!Irelia.Q.IsReady())
                return;

            var targets = GameObjects.EnemyHeroes.Where(i => !i.IsAlly && !i.IsDead && Irelia.Q.CanCast(i)).Where(i => i.Health <= Helper.GetQDmg(i) + Helper.GetQDmg(i) * 0.08f).OrderBy(i => i.DistanceToPlayer());

            if (targets == null)
                return;

            foreach (var target in targets)
            {
                if (!Helper.UnderTower(target.Position) || MenuSettings.KeysSettings.TurretKey.Active)
                {
                    Irelia.Q.Cast(target);
                    return;
                }
            }
        }
        public static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name == "IreliaEMissile")
            {
                Irelia.E1Pos = args.End;
                return;
            }


            if (sender.IsMe && args.Slot.ToString() == "Q")
            {
                Irelia.lastQ = Variables.GameTimeTickCount;
                if (Variables.GameTimeTickCount > Irelia.SheenTimer + 1650)
                {
                    Irelia.SheenTimer = Variables.GameTimeTickCount;
                    return;
                }
            }
        }

        public static void Combo(EventArgs args)
        {
            if(Orbwalker.ActiveMode != OrbwalkerMode.Combo || ObjectManager.Player.IsDead)
            {
                return;
            }

            var target = TargetSelector.GetTarget(1000, DamageType.Physical);
            if (target == null)
                return;

            if (LogicR.IRELIA_RCOMBO() && MenuSettings.RSettings.Rcombo.Enabled)
            {
                return;
            }
            else
            {
                if (MenuSettings.ESettings.Ecombo.Enabled)
                    LogicE.EPrediction(true);

                if (MenuSettings.QSettings.Qcombo.Enabled)
                {
                    LogicQ.GapCloserTargetCanKillable();

                    if (MenuSettings.WSettings.Wcombo.Enabled)
                        LogicQ.QW(target);

                    switch (MenuSettings.QSettings.QListComboMode.Index)
                    {
                        case 1:
                            LogicQ.FocusCanQTarget(target);
                            return;
                        case 2:
                            LogicQ.NewExtreamLogic(target);
                            return;
                        case 0:
                            LogicQ.QGapCloserPos(target.Position);
                            return;
                    }
                }               
            }
        }
        public static void AIBaseClient_OnBuffLose(AIBaseClient sender, AIBaseClientBuffRemoveEventArgs args)
        {
            if (sender.NetworkId == ObjectManager.Player.NetworkId || sender.IsMe)
            {
                if (args.Buff.Name == "sheen"
                    || args.Buff.Name == "3078trinityforce"
                    || args.Buff.Name == "6632buff"
                    || args.Buff.Name.Contains("sheen")
                    || args.Buff.Name.Contains("trinity")
                    || args.Buff.Name.Contains("iceborn")
                    || args.Buff.Name.Contains("Lich"))
                {
                    Irelia.SheenTimer = Variables.GameTimeTickCount;
                    return;
                }
            }
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            if (MenuSettings.KeysSettings.FleeKey.Active)
            {
                #region New E pred
                LogicE.EPrediction(false);
                #endregion
                LogicQ.QGapCloserPos(Game.CursorPos);
                return;
            }
            if (MenuSettings.KeysSettings.SemiE.Active && Irelia.E.IsReady())
            {
                #region New E pred
                LogicE.EPrediction(false);
                return;
                #endregion
            }
            if (MenuSettings.KeysSettings.SemiR.Active && Irelia.R.IsReady())
            {
                #region R
                try
                {
                    var targets = TargetSelector.GetTargets(900, DamageType.Physical);
                    Vector3 Rpos = Vector3.Zero;

                    if (targets != null)
                    {
                        foreach (var Rprediction in targets.Select(i => Irelia.R.GetPrediction(i)).Where(i => i.Hitchance >= EnsoulSharp.SDK.HitChance.High || (i.Hitchance >= EnsoulSharp.SDK.HitChance.Medium && i.AoeTargetsHitCount > 1)).OrderByDescending(i => i.AoeTargetsHitCount))
                        {
                            Rpos = Rprediction.CastPosition;
                        }
                        if (Rpos != Vector3.Zero)
                        {
                            Irelia.R.Cast(Rpos);
                            return;
                        }
                    }                   
                }
                catch (Exception ex)
                {
                    Console.WriteLine("R.cast Error" + ex);
                }
                #endregion
            }
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead) return;

            if(Irelia.Q.IsReady() && MenuSettings.QSettings.DrawQ.Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Irelia.Q.Range, System.Drawing.Color.Red);
                Render.Circle.DrawCircle(Game.CursorPos, 150f, (Variables.GameTimeTickCount - Irelia.SheenTimer > 1600) ? System.Drawing.Color.Green : System.Drawing.Color.Red);
            }

            if (MenuSettings.ClearSettings.DrawMinions.Enabled)
            {
                var minions = ObjectManager.Get<AIMinionClient>().Where(i => !i.IsDead && !i.IsAlly && i.IsValidTarget(2000) && Helper.CanQ(i));
                if(minions != null)
                {
                    foreach(var min in minions)
                    {
                        Render.Circle.DrawCircle(min.Position, 70f, System.Drawing.Color.Red, 10, false);
                        //Drawing.DrawCircle(min.Position, 80f, System.Drawing.Color.White);
                    }
                }
            }
        }
    }
}
