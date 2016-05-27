namespace ClinkzSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;
    using Ensage.Common.Menu;
    using Ensage.Items;
    using SharpDX;
    using SharpDX.Direct3D9;

    using Attribute = Ensage.Attribute;


    internal class ClinkzSharp
    {

        private static Ability strafe, arrows, dpAbility;
        private static Item bkb, orchid, hex, medallion, solar, bladeMail, bloodthorn;
        private static readonly Menu Menu = new Menu("ClinkzSharp", "clinkzsharp", true, "npc_dota_hero_Clinkz", true);
        private static Hero me, target;
        private static bool autoKillz;
        private static bool autoFarmz;
        public static float TargetDistance { get; set; }
        private static AbilityToggler itemToggler, skillToggler;
        public static bool dragonLance, hurricanePike;
        private static bool itemTogglerSet, menuSkillSet, ultBool;
        private static readonly int[] Quack = { 0, 50, 60, 70, 80 };
        private static float attackRange, attackRangeDraw;
        private static ParticleEffect effect;
        private static readonly Dictionary<int, ParticleEffect> Effect = new Dictionary<int, ParticleEffect>();
        private static Font text;
        private static Font notice;
        private static Line line;
        private static PowerTreads powerTreads;
        private static Attribute lastAttribute;

        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnUpdate += Farming;
            Game.OnWndProc += Game_OnWndProc;
            Player.OnExecuteOrder += OrderExecute;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnPreReset += Drawing_OnPreReset;
            Drawing.OnPostReset += Drawing_OnPostReset;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

            var menuCombo = new Menu("Combo Options", "wombo", false, @"..\other\statpop_dotalogo", true);
            menuCombo.AddItem(new MenuItem("enable", "Enable").SetValue(true));
            menuCombo.AddItem(new MenuItem("comboKey", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            menuCombo.AddItem(new MenuItem("farmKey", "Farm Key").SetValue(new KeyBind('D', KeyBindType.Press)));
           // menuCombo.AddItem(new MenuItem("enableult", "Enable AutoUlt").SetValue(true));
            menuCombo.AddItem(new MenuItem("orbwalk", "Orbwalk").SetValue(true));
            var itemDict = new Dictionary<string, bool>
            {
                { "item_blade_mail", true },
                { "item_solar_crest", true },
                { "item_black_king_bar", true },
                { "item_medallion_of_courage", true },
                { "item_orchid", true },
                { "item_bloodthorn", true },
                { "item_sheepstick", true },
            };
            menuCombo.AddItem(new MenuItem("Items", "Items:").SetValue(new AbilityToggler(itemDict)));
            menuCombo.AddItem(new MenuItem("bkblogic", "Min targets to BKB").SetValue(new Slider(2, 1, 5)));

            var skillDict = new Dictionary<string, bool>
            {
                { "clinkz_strafe", true },
                { "clinkz_searing_arrows", true },
                { "clinkz_death_pact", true },

            };
            menuCombo.AddItem(new MenuItem("Skills", "Skills:").SetValue(new AbilityToggler(skillDict))
                                    .SetTooltip("set this on so it can cast shadow dance automatically."));

            var menuDraws = new Menu("Drawings", "draws", false, @"..\other\statpop_clock", true);
            menuDraws.AddItem(new MenuItem("drawLastHit", "Draw Last hit").SetValue(true));
            menuDraws.AddItem(new MenuItem("drawAttackRange", "Draw Clinkz attack range").SetValue(true));
            /*
            menuDraws.AddItem(new MenuItem("colorThingy", "AttackRange Color"));
            menuDraws.AddItem(new MenuItem("red", "Red:")).SetFontColor(Color.Red).SetValue(new Slider(100, 0, 255));
            menuDraws.AddItem(new MenuItem("green", "Green:")).SetFontColor(Color.Green).SetValue(new Slider(100, 0, 255));
            menuDraws.AddItem(new MenuItem("blue", "Blue:")).SetFontColor(Color.Blue).SetValue(new Slider(100, 0, 255));

                                                    effect.SetControlPoint(1, new Vector3(Menu.Item("red").GetValue<Slider>().Value,
                                                          Menu.Item("green").GetValue<Slider>().Value,
                                                          Menu.Item("blue").GetValue<Slider>().Value));*/
            Menu.AddSubMenu(menuCombo);
            Menu.AddSubMenu(menuDraws);
            Menu.AddToMainMenu();



            text = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                FaceName = "Segoe UI",// Microsoft Sans Serif looks good too
                Height = 17,
                OutputPrecision = FontPrecision.Default,
                 Quality = FontQuality.ClearType
                });

            notice = new Font(
                Drawing.Direct3DDevice9,
                new FontDescription
                {
                    FaceName = "Segoe UI", // Microsoft Sans Serif looks good too
                    Height = 14,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearType
                });

            line = new Line(Drawing.Direct3DDevice9);

            OnLoadMessage();
        }


        public static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;

            me = ObjectManager.LocalHero; 
            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Clinkz)
                return;

            if (strafe == null)
                strafe = me.Spellbook.SpellQ;

            if (arrows == null)
                arrows = me.Spellbook.SpellW;

            if (dpAbility == null)
                dpAbility = me.Spellbook.SpellR;

            if (bkb == null)
                bkb = me.FindItem("item_black_king_bar");

            if (hex == null)
                hex = me.FindItem("item_sheepstick");

            if (orchid == null)
                orchid = me.FindItem("item_orchid");

            if (bloodthorn == null)
                bloodthorn = me.FindItem("item_bloodthorn");

            if (medallion == null)
                medallion = me.FindItem("item_medallion_of_courage");

            if (bladeMail == null)
                bladeMail = me.FindItem("item_blade_mail");

            if (solar == null)
                solar = me.FindItem("item_solar_crest");

            if (powerTreads == null)
                powerTreads = me.FindItem("item_power_treads") as PowerTreads;

            dragonLance = me.HasModifier("modifier_item_dragon_lance");

            attackRange = dragonLance ? 760 : 630;


            if (!itemTogglerSet)
            {
                itemToggler = Menu.Item("Items").GetValue<AbilityToggler>();
                itemTogglerSet = true;
            }

            if (!menuSkillSet)
            {
                skillToggler = Menu.Item("Skills").GetValue<AbilityToggler>();
                menuSkillSet = true;
            }

            ultBool = dpAbility != null && skillToggler.IsEnabled("clinkz_death_pact");

            const int DPrange = 0x190;
                
            if (powerTreads != null)
                lastAttribute = powerTreads.ActiveAttribute;


            var creepR =
                ObjectManager.GetEntities<Unit>()
                    .Where(
                        creep =>
                            (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral) &&
                             creep.IsAlive && creep.IsVisible && creep.IsSpawned &&
                             creep.Team != me.Team && creep.Position.Distance2D(me.Position) <= DPrange &&
                             me.Spellbook.SpellR.CanBeCasted()).ToList();

            var enemies = ObjectManager.GetEntities<Hero>().Where(x => x.IsAlive && x.Team != me.Team && !x.IsIllusion).ToList();


            if (autoKillz && Menu.Item("enable").GetValue<bool>())
            {
                target = me.ClosestToMouseTarget(1001);

                //orbwalk
                if (target != null && (!target.IsValid || !target.IsVisible || !target.IsAlive || target.Health <= 0))
                {
                    target = null;
                }
                var canCancel = Orbwalking.CanCancelAnimation();
                if (canCancel)
                {
                    if (target != null && !target.IsVisible && !Orbwalking.AttackOnCooldown(target))
                    {
                        target = me.ClosestToMouseTarget();
                    }
                    else if (target == null || !Orbwalking.AttackOnCooldown(target) && target.HasModifiers(new[]
                                    {
                                        "modifier_dazzle_shallow_grave", "modifier_item_blade_mail_reflect",                                        
                                    }, false))                       
                    {
                        var bestAa = me.BestAATarget();
                        if (bestAa != null)
                        {
                            target = me.BestAATarget();
                        }
                    }
                }
                
                if (target != null && target.IsAlive && !target.IsInvul() && !target.IsIllusion)
                {
                    if (me.CanAttack() && me.CanCast() && !me.IsChanneling())
                    {
                        TargetDistance = me.Position.Distance2D(target);

                        if (Menu.Item("orbwalk").GetValue<bool>())
                        {
                            if (!Utils.SleepCheck("attacking"))
                                if (lastAttribute != Attribute.Agility && Utils.SleepCheck("powerTreadsSwitch"))
                                {
                                    SwitchTo(Attribute.Agility);
                                    Utils.Sleep(400, "powerTreadsSwitch");
                                }
                            Orbwalking.Orbwalk(target, Game.Ping, attackmodifiers: true);
                            Utils.Sleep(400, "attacking");
                        }
                        else if (!Menu.Item("orbwalk").GetValue<bool>())
                        {
                            if (arrows != null && arrows.IsValid && arrows.CanBeCasted() && !Utils.SleepCheck("attacking"))
                                arrows.UseAbility(target);
                            Utils.Sleep(200, "attacking");
                        }

                        if (creepR.Count > 0 && !me.Modifiers.ToList().Exists(x => x.Name == "modifier_clinkz_death_pact") && skillToggler.IsEnabled(dpAbility.Name))
                        {
                            var creepmax = creepR.MaxOrDefault(x => x.Health);
                            if (lastAttribute != Attribute.Intelligence && Utils.SleepCheck("powerTreadsSwitch"))
                            {
                                SwitchTo(Attribute.Intelligence);
                                Utils.Sleep(400, "powerTreadsSwitch");
                            }
                            dpAbility.UseAbility(creepmax);
                        }

                        if (strafe != null && strafe.IsValid && strafe.CanBeCasted() && me.CanCast() && me.Distance2D(target) <= attackRange + 90 && Utils.SleepCheck("strafe") && skillToggler.IsEnabled(strafe.Name))
                        {
                            if (lastAttribute != Attribute.Intelligence && Utils.SleepCheck("powerTreadsSwitch"))
                            {
                                SwitchTo(Attribute.Intelligence);
                                Utils.Sleep(400, "powerTreadsSwitch");
                            }
                            strafe.UseAbility();
                            Utils.Sleep(100 + Game.Ping, "strafe");
                        }

                        if (bladeMail != null && bladeMail.IsValid && Utils.SleepCheck("blademail") && itemToggler.IsEnabled(bladeMail.Name) && me.Distance2D(target) <= attackRange + 90)
                        {
                            bladeMail.UseAbility();
                            Utils.Sleep(50 + Game.Ping, "blademail");
                        }

                        if (medallion != null && medallion.IsValid && medallion.CanBeCasted() && Utils.SleepCheck("medallion") && itemToggler.IsEnabled(medallion.Name) && me.Distance2D(target) <= attackRange + 90)
                        {
                            medallion.UseAbility(target);
                            Utils.Sleep(50 + Game.Ping, "medallion");
                        }

                        if (solar != null && solar.IsValid && solar.CanBeCasted() && Utils.SleepCheck("solar") && itemToggler.IsEnabled(solar.Name))
                        {
                            solar.UseAbility(target);
                            Utils.Sleep(50 + Game.Ping, "solar");
                        }


                        if (bkb != null && bkb.IsValid && bkb.CanBeCasted() && Utils.SleepCheck("bkb") && itemToggler.IsEnabled(bkb.Name) && (enemies.Count(x => x.Distance2D(me) <= 650) >= (Menu.Item("bkblogic").GetValue<Slider>().Value)))
                        {
                            bkb.UseAbility();
                            Utils.Sleep(150 + Game.Ping, "bkb");
                        }

                        if (hex != null && hex.IsValid && hex.CanBeCasted() && Utils.SleepCheck("hex") && itemToggler.IsEnabled(hex.Name))
                        {
                            hex.CastStun(target);
                            Utils.Sleep(250 + Game.Ping, "hex");
                            return;
                        }

                        if (orchid != null && orchid.IsValid && orchid.CanBeCasted() && Utils.SleepCheck("orchid") && itemToggler.IsEnabled(orchid.Name))
                        {
                            orchid.CastStun(target);
                            Utils.Sleep(250 + Game.Ping, "orchid");
                            return;
                        }

                        if (bloodthorn != null && bloodthorn.IsValid && bloodthorn.CanBeCasted() && Utils.SleepCheck("bloodthorn") && itemToggler.IsEnabled(bloodthorn.Name))
                        {
                            bloodthorn.CastStun(target);
                            Utils.Sleep(250 + Game.Ping, "orchid");
                            return;
                        }

                        if (!me.IsAttacking() && me.Distance2D(target) >= attackRange && Utils.SleepCheck("follow"))
                        {
                            me.Move(Game.MousePosition);
                            Utils.Sleep(150 + Game.Ping, "follow");
                        }
                    }              
                }
                else
                {
                    me.Move(Game.MousePosition);
                }
            }
        }//gameOnUpdate Close.

        public static void Farming(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame) return;

            me = ObjectManager.LocalHero;
            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Clinkz)
                return;

            var quacklvl = me.Spellbook.SpellW.Level;

            if (autoFarmz)
            {
                var creepW =
                ObjectManager.GetEntities<Unit>()
                    .Where(
                        creep =>
                            (creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                             creep.ClassID == ClassID.CDOTA_Unit_VisageFamiliar ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral ||
                             creep.ClassID == ClassID.CDOTA_Unit_SpiritBear ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Invoker_Forged_Spirit ||
                             creep.ClassID == ClassID.CDOTA_BaseNPC_Creep) &&
                             creep.IsAlive && creep.IsVisible && creep.IsSpawned &&
                             creep.Team != me.Team && creep.Health <= Math.Floor((Quack[quacklvl] + me.DamageAverage) * (1 - creep.MagicDamageResist))
                             && creep.Position.Distance2D(me.Position) <= attackRange).ToList();
                {
                    if (creepW.Count > 0)
                    {
                        var creepmax = creepW.MaxOrDefault(x => x.Health);
                        me.Spellbook.SpellW.UseAbility(creepmax);
                    }
                    else
                    {
                        me.Move(Game.MousePosition);
                    }
                }
            }
        }//Farming Close.
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {

                if (Menu.Item("comboKey").GetValue<KeyBind>().Active)
                {
                    autoKillz = true;
                }
                else
                {
                    autoKillz = false;
                }

                if (Menu.Item("farmKey").GetValue<KeyBind>().Active)
                {
                    autoFarmz = true;
                }
                else
                {
                    autoFarmz = false;
                }
            }
        }//game_onWndProc

        private static void OrderExecute(Player P, ExecuteOrderEventArgs e)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;

            me = ObjectManager.LocalHero;
            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Clinkz)
                return;

            if (e.Ability == null)
                return;

            switch (e.Ability.Name)
            {
                case "clinkz_searing_arrows":
                    if (lastAttribute != Attribute.Agility && Utils.SleepCheck("powerTreadsSwitch"))
                    {
                        SwitchTo(Attribute.Agility);
                        Utils.Sleep(400, "powerTreadsSwitch");
                    }
                    break;
                case "clinkz_death_pact":
                    if (lastAttribute != Attribute.Intelligence && Utils.SleepCheck("powerTreadsSwitch"))
                    {
                        SwitchTo(Attribute.Intelligence);
                        Utils.Sleep(400, "powerTreadsSwitch");
                    }
                    break;
                case "clinkz_wind_walk":
                    if (lastAttribute != Attribute.Intelligence && Utils.SleepCheck("powerTreadsSwitch"))
                    {
                        SwitchTo(Attribute.Intelligence);
                        Utils.Sleep(400, "powerTreadsSwitch");
                    }
                    break;
                case "clinkz_strafe":
                    if (lastAttribute != Attribute.Intelligence && Utils.SleepCheck("powerTreadsSwitch"))
                    {
                        SwitchTo(Attribute.Intelligence);
                        Utils.Sleep(400, "powerTreadsSwitch");
                    }
                    break;
            }
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;

            me = ObjectManager.LocalHero;
            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Clinkz)
                return;

            //newer stuff begunnn
            if (attackRange != attackRangeDraw)
            {
                attackRangeDraw = attackRange;
                if (Effect.TryGetValue(3, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(3);
                }
                if (!Effect.TryGetValue(3, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    effect.SetControlPoint(1, new Vector3(255, 255, 255));
                    effect.SetControlPoint(2, new Vector3(attackRange + 70, 255, 0));
                    Effect.Add(3, effect);
                }
            }
            if (Menu.Item("drawAttackRange").GetValue<bool>())
            {
                if (!Effect.TryGetValue(3, out effect))
                {
                    effect = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    effect.SetControlPoint(1, new Vector3(255, 255, 255));
                    effect.SetControlPoint(2, new Vector3(attackRange + 70, 255, 0));
                    Effect.Add(3, effect);
                }
            }
            else
            {
                if (Effect.TryGetValue(3, out effect))
                {
                    effect.Dispose();
                    Effect.Remove(3);
                }
            }

            //new stuff ends 
            if (Menu.Item("comboKey").GetValue<KeyBind>().Active)
            {
                if (target == null || !target.IsAlive || target.IsIllusion)
                {
                    return;
                }
                var pos = Drawing.WorldToScreen(target.Position);
                Drawing.DrawText("Target", pos, new Vector2(0, 50), Color.Red, FontFlags.AntiAlias | FontFlags.DropShadow);
            }

            if (Menu.Item("drawLastHit").GetValue<bool>())
            {
                drawHPLastHit();
            }
           
        } //DRAWS

        private static void Drawing_OnPreReset(EventArgs args)
        {
            text.OnLostDevice();
            notice.OnLostDevice();
            line.OnLostDevice();
        }

        private static void Drawing_OnPostReset(EventArgs args)
        {
            text.OnResetDevice();
            notice.OnResetDevice();
            line.OnResetDevice();
        }

        public static void Drawing_OnEndScene(EventArgs args)
        {
            if (Drawing.Direct3DDevice9 == null || Drawing.Direct3DDevice9.IsDisposed || !Game.IsInGame)
            {
                return;
            }

            var player = ObjectManager.LocalPlayer;
            me = ObjectManager.LocalHero;
            if (player == null || player.Team == Team.Observer || me.ClassID != ClassID.CDOTA_Unit_Hero_Clinkz)
            {
                return;
            }

            if (Menu.Item("comboKey").GetValue<KeyBind>().Active)
            {
                DrawBox(2, 45, 115, 20, 1, new ColorBGRA(0, 128, 0, 128));
                DrawFilledBox(2, 45, 115, 20, new ColorBGRA(0, 0, 0, 100));
                DrawShadowText("Clinkz#: Comboing!", 2, 45, Color.LightBlue, text);
            }

            if (ultBool)
            {
                DrawBox(120, 45, 65, 20, 1, new ColorBGRA(0, 200, 100, 100));
                DrawFilledBox(120, 45, 65, 20, new ColorBGRA(0, 0, 0, 100));
                DrawShadowText("AutoUlt On", 120, 45, Color.LightBlue, text);
            }
            else
            {
                DrawBox(120, 45, 65, 20, 1, new ColorBGRA(0, 200, 100, 100));
                DrawFilledBox(120, 45, 65, 20, new ColorBGRA(0, 0, 0, 100));
                DrawShadowText("AutoUlt Off", 120, 45, Color.LightBlue, text);
            }
        }

        private static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            text.Dispose();
            notice.Dispose();
            line.Dispose();
        }

        public static void SwitchTo(Attribute attribute, bool switchBack = true)
        {
            if (powerTreads == null)//another check
                return;

            if (me.IsChanneling() || !me.CanUseItems() || me.HasModifiers(new[]
                                    {
                                        "modifier_clinkz_shadow_walk", "modifier_item_smoke_of_deceit",
                                    }, false))
                return;//Just for the PlayerOnExecute

            /* and do this fo each attribute, Or now != next need to think of something better
            if (Treads.ActiveAttribute != Ensage.Attribute.Agility)
               {
                 if (Treads.ActiveAttribute == Ensage.Attribute.Strength)
                    {
                     Treads.UseAbility();
                     Treads.UseAbility();
                    }
                     else
                    {
                    Treads.UseAbility();
                    }
                   }
            */
            var now = 0;
            var next = 0;

            if (powerTreads.ActiveAttribute != Attribute.Strength)
            {
                if (powerTreads.ActiveAttribute != Attribute.Agility)
                {
                    if (powerTreads.ActiveAttribute == Attribute.Intelligence)
                    {
                        now = 2;
                    }
                }
                else
                {
                    now = 3;
                }
            }
            else
            {
                now = 1;
            }

            if (attribute != Attribute.Strength)
            {
                if (attribute != Attribute.Intelligence)
                {
                    if (attribute == Attribute.Agility)
                    {
                        next = 3;
                    }
                }
                else
                {
                    next = 2;
                }
            }
            else
            {
                next = 1;
            }

            if (now == next)
                return;

            var change = next - now % 3;

            if (now == 2 && next == 1) 
                change = 2;

            int i;
            for (i = 0; i < change; i++)
                powerTreads.UseAbility();
        }

        private static double getDmgOnUnit(Unit unit, double bonusdamage)
        {
            var quacklvl = me.Spellbook.SpellW.Level;
            var quelling_blade = me.FindItem("item_quelling_blade");
            double physDamage = me.MinimumDamage + me.BonusDamage;
            if (quelling_blade != null)
            {
                if (me.ClassID == ClassID.CDOTA_Unit_Hero_Clinkz)
                {
                    physDamage = me.MinimumDamage * 1.25 + me.BonusDamage;
                }
            }
            var damageMp = 1 - 0.06 * unit.Armor / (1 + 0.06 * Math.Abs(unit.Armor));

            var realDamage = (bonusdamage + physDamage + Quack[quacklvl]) * damageMp;
            if (unit.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege ||
                unit.ClassID == ClassID.CDOTA_BaseNPC_Tower)
            {
                realDamage = realDamage / 2;
            }

            return realDamage;
        }

        private static void drawHPLastHit()
        {
            var enemies = ObjectManager.GetEntities<Unit>().Where(x => (x.ClassID == ClassID.CDOTA_BaseNPC_Tower || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Lane || x.ClassID == ClassID.CDOTA_BaseNPC_Creep || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral || x.ClassID == ClassID.CDOTA_BaseNPC_Creep_Siege || x.ClassID == ClassID.CDOTA_BaseNPC_Additive || x.ClassID == ClassID.CDOTA_BaseNPC_Building || x.ClassID == ClassID.CDOTA_BaseNPC_Creature) && x.IsAlive && x.IsVisible && x.Team != me.Team && x.Distance2D(me) < attackRange + 600);

            foreach (var enemy in enemies.Where(x => true))
            {
                var health = enemy.Health;
                var maxHealth = enemy.MaximumHealth;
                if (health == maxHealth)
                {
                    continue;
                }
                var damge = (float)getDmgOnUnit(enemy, 0);
                var hpleft = health;
                var hpperc = hpleft / maxHealth;

                var hbarpos = HUDInfo.GetHPbarPosition(enemy);

                Vector2 screenPos;
                var enemyPos = enemy.Position + new Vector3(0, 0, enemy.HealthBarOffset);
                if (!Drawing.WorldToScreen(enemyPos, out screenPos))
                {
                    continue;
                }

                var start = screenPos;

                hbarpos.X = start.X - HUDInfo.GetHPBarSizeX(enemy) / 2;
                hbarpos.Y = start.Y;
                var hpvarx = hbarpos.X;
                var a = (float)Math.Round(damge * HUDInfo.GetHPBarSizeX(enemy) / enemy.MaximumHealth);
                var position = hbarpos + new Vector2(hpvarx * hpperc + 10, -12);
                try
                {
                    Drawing.DrawRect(position, new Vector2(a, HUDInfo.GetHpBarSizeY(enemy) - 4), enemy.Health > damge ? enemy.Health > damge * 2 ? new Color(180, 205, 205, 40) : new Color(255, 0, 0, 60) : new Color(127, 255, 0, 80));
                    Drawing.DrawRect(position, new Vector2(a, HUDInfo.GetHpBarSizeY(enemy) - 4), Color.Black, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static void DrawFilledBox(float x, float y, float w, float h, Color color)
        {
            var vLine = new Vector2[2];

            line.GLLines = true;
            line.Antialias = false;
            line.Width = w;

            vLine[0].X = x + w / 2;
            vLine[0].Y = y;
            vLine[1].X = x + w / 2;
            vLine[1].Y = y + h;

            line.Begin();
            line.Draw(vLine, color);
            line.End();
        }

        public static void DrawBox(float x, float y, float w, float h, float px, Color color)
        {
            DrawFilledBox(x, y + h, w, px, color);
            DrawFilledBox(x - px, y, px, h, color);
            DrawFilledBox(x, y - px, w, px, color);
            DrawFilledBox(x + w, y, px, h, color);
        }

        public static void DrawShadowText(string stext, int x, int y, Color color, Font f)
        {
            f.DrawText(null, stext, x + 1, y + 1, Color.Black);
            f.DrawText(null, stext, x, y, color);
        }

        private static void OnLoadMessage()
        {
            Game.PrintMessage("<font face='helvetica' color='#00FF00'>ClinkzSharp loaded!</font>", MessageType.LogMessage);
            Console.WriteLine(@"> ClinkzSharp is on like Donkey Kong!");
        }

    }
} // close end or we
