﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;

using FunnySlayerCommon;
using FSpred.Prediction;

namespace Pyke_Ryū
{
    public class Program
    {       
        public static AIHeroClient Player = GameObjects.Player;
        public static Spell Q = new Spell(SpellSlot.Q, 400f);
        public static Spell E = new Spell(SpellSlot.E, 550f);
        public static Spell R = new Spell(SpellSlot.R, 750f);
        public static Menu RootPyke = new Menu("Pyke_Ryū", "Pyke_Ryū", true);
        public static Menu Selector = new Menu("Selector", "Target Menu");
        public static MenuBool Rpyke = new MenuBool("R Pyke", "R KS");
        public static MenuBool Qpyke = new MenuBool("Q Combo", "Q Combo|Harass");
        public static MenuBool Qpykefull = new MenuBool("Q Combo Qpykefull", "Only when can pulling", false);
        public static MenuBool Wpyke = new MenuBool("W Combo", "W Combo|Harass");
        public static MenuBool Epyke = new MenuBool("E Combo", "E Combo|Harass");

        public static void GameEvent_OnGameLoad()
        {
            if(Player.CharacterName != "Pyke" || Player == null)
            {
                return;
            }
            R.SetSkillshot(0.5f, 100f, float.MaxValue, false, SpellType.Circle);
            Game.OnUpdate += RKS;

            Q.SetSkillshot(0.25f, 55f, 2000, true, SpellType.Line);
            E.SetSkillshot(0.25f, 70f, 2000, false, SpellType.Line);
            
            Q.SetCharged("PykeQ", "PykeQ", 400, 1100, 1.155f);

            //Selector.AddTargetSelectorMenu();
            //RootPyke.Add(Selector);
            RootPyke.Add(Rpyke);
            RootPyke.Add(Qpyke);
            RootPyke.Add(Qpykefull);
            RootPyke.Add(Wpyke);
            RootPyke.Add(Epyke);           
            RootPyke.Attach();

            Game.OnUpdate += Game_OnUpdate;
            Game.OnUpdate += Game_OnUpdate1;
            Drawing.OnDraw += Drawing_OnDraw;
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
        }

        private static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsMe && args.Slot == SpellSlot.Q)
            {
                lastchangingQ = Variables.GameTimeTickCount;
            }
        }

        public static float lastchangingQ = 0;
        private static void Game_OnUpdate1(EventArgs args)
        {
            if (Q.IsCharging)
            {
                Orbwalker.AttackEnabled = false;
            }
            else
            {
                Orbwalker.AttackEnabled = true;
            }
        }
        private static int lastrcast = 0;
        private static void RKS(EventArgs args)
        {
            if (Player.IsDead || !Rpyke.Enabled || Variables.GameTimeTickCount - lastrcast <= R.Delay * 1000 + 500) return;

            var targets = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(R.Range) && R.GetPrediction(i).Hitchance >= EnsoulSharp.SDK.HitChance.High);

            if (targets.Any())
            {
                var gettarget = TargetSelector.GetTarget(targets, DamageType.True);
                if(gettarget != null)
                {
                    if(gettarget.Health <= R.GetDamage(gettarget))
                    {
                        lastrcast = Variables.GameTimeTickCount;
                        var pred = FSpred.Prediction.Prediction.GetPrediction(R, gettarget);
                        if(pred.Hitchance >= FSpred.Prediction.HitChance.High)
                        {
                            R.Cast(pred.CastPosition);
                        }
                        return;
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.White);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (FunnySlayerCommon.OnAction.BeforeAA || FunnySlayerCommon.OnAction.OnAA)
                return;

            if(Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                var target = FunnySlayerCommon.FSTargetSelector.GetFSTarget(2000);
                if(target != null && !target.IsDead && target.IsValid())
                {
                    if(target.IsValidTarget(Q.Range + 300))
                    {
                        if (target.IsValidTarget(400))
                        {
                            if (E.IsReady() && !Q.IsCharging && !ObjectManager.Player.Spellbook.IsCharging)
                            {
                                Orbwalker.AttackEnabled = true;
                                CASTE();
                            }
                            else
                            {
                                CASTQ();
                            }
                        }
                        else
                        {
                            var pred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, target);
                            if (pred.Hitchance >= SebbyLibPorted.Prediction.HitChance.High && !Q.IsCharging && !ObjectManager.Player.Spellbook.IsCharging)
                            {
                                Orbwalker.AttackEnabled = true;
                                if (E.Cast(pred.CastPosition))
                                    return;
                            }
                            else
                            {
                                var Qpred = FSpred.Prediction.Prediction.GetPrediction(Q, target);
                                if (Qpred.Hitchance >= FSpred.Prediction.HitChance.High)
                                {
                                    if (Q.IsCharging)
                                    {
                                        Orbwalker.AttackEnabled = false;
                                        if (Q.Cast(Qpred.CastPosition))
                                            return;
                                    }
                                    else
                                    {
                                        CASTQ();
                                    }
                                }
                                else
                                {
                                    CASTQ();
                                }
                            }
                        }
                    }
                    else
                    {
                        CASTW();
                    }
                }
                else
                {
                    CASTQ();
                    CASTE();
                }                                             
            }
        }

        private static void CASTQ()
        {
            var targets = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(Q.ChargedMaxRange)).ToList();
            if (targets == null) return;

            if (!Qpyke.Enabled) return;

            if (Q.IsReady())
            {
                if (Q.IsCharging)
                {
                    Orbwalker.AttackEnabled = false;

                    if ((!Qpykefull.Enabled || Variables.GameTimeTickCount - lastchangingQ > 1000))
                    {
                        var pred = FSpred.Prediction.Prediction.GetPrediction(Q, targets.OrderBy(i => i.Health).FirstOrDefault(i => FSpred.Prediction.Prediction.GetPrediction(Q, i).Hitchance >= FSpred.Prediction.HitChance.High));
                        if (pred.Hitchance >= FSpred.Prediction.HitChance.High)
                        {
                            if (Q.Cast(pred.CastPosition))
                                return;
                        }
                    }                   
                }
                else
                {
                    var target = targets.FirstOrDefault(i => FSpred.Prediction.Prediction.GetPrediction(Q, i).Hitchance >= FSpred.Prediction.HitChance.High);
                    if (target.IsValidTarget(400))
                    {
                        Q.Cast(target);
                    }
                    var pred = FSpred.Prediction.Prediction.GetPrediction(Q, targets.OrderBy(i => i.Health).FirstOrDefault(i => FSpred.Prediction.Prediction.GetPrediction(Q, i).Hitchance >= FSpred.Prediction.HitChance.High));
                    if (pred.Hitchance >= FSpred.Prediction.HitChance.High)
                    {
                        if(Q.StartCharging())
                        {
                            Orbwalker.AttackEnabled = false;
                            return;
                        }
                    }
                }
            }
        }
        private static void CASTW()
        {
            if (!Wpyke.Enabled)
                return;

            if (Q.IsCharging) return;

            var target = FSTargetSelector.GetFSTarget(2000);
            if (target == null) return;
            
            if((new Spell(SpellSlot.W)).IsReady())
            {
                if (!target.IsValidTarget(E.Range + Q.Range))
                {
                    if ((new Spell(SpellSlot.W)).Cast())
                        return;
                }
            }
        }
        private static void CASTE()
        {
            var targets = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E.Range)).ToList();
            if (targets == null) return;

            if (Q.IsCharging) return;

            if (!Epyke.Enabled) return;

            if (E.IsReady())
            {
                var pred = SebbyLibPorted.Prediction.Prediction.GetPrediction(E, targets.OrderBy(i => i.Health).FirstOrDefault(i => FSpred.Prediction.Prediction.GetPrediction(E, i).Hitchance >= FSpred.Prediction.HitChance.High));
                if (pred.Hitchance >= SebbyLibPorted.Prediction.HitChance.High)
                {
                    if (E.Cast(pred.CastPosition))
                        return;
                }
            }
        }
    }
}
