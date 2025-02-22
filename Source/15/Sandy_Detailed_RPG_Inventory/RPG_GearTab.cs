using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using Sandy_Detailed_RPG_Inventory.MODIntegrations;
using System.ComponentModel;

namespace Sandy_Detailed_RPG_Inventory
{
    public static class TabU
    {
        public const float stdPadding = 20f;
        public const float stdThingIconSize = 28f;
        public const float stdThingRowHeight = 28f;
        public const float stdThingLeftX = 36f;
        public const float stdLineHeight = 22f;
        public const float stdScrollbarWidth = 20f;
        public const float statIconSize = 24f;
        public const float thingIconOuter = 74f;
        public const float thingIconInner = 64f;
        public const float statPanelWidth = 128f;
        public const float pawnPanelSize = 128f;
        public const float pawnPanelSizeAssumption = -28f;
        public const float tipContractionSize = 12f;
    }
    public class Sandy_Detailed_RPG_GearTab : ITab_Pawn_Gear
    {
        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight;
        public static readonly Vector3 PawnTextureCameraOffset = default;
        private bool viewList = false;
        private ThingDef cacheddef = null;
        private bool cachedSimplifiedView = false;
        private bool showHat = false;
        private Rot4 rot = new Rot4(2);
        private Pawn cachedPawn = null;

        public override void OnOpen()
        {

            Sandy_RPG_Settings.resetFrame();
        }

        private bool SimplifiedView
        {
            //constantly searching in dict is slower than checking for cached value
            get { return cacheddef == cachedPawn.def ? cachedSimplifiedView : (cachedSimplifiedView = Sandy_RPG_Settings.simplifiedView[(cacheddef = cachedPawn.def)]); }
            set { Sandy_RPG_Settings.simplifiedView[cachedPawn.def] = (cachedSimplifiedView = value); Sandy_RPG_Settings.instance.Mod.WriteSettings(); }
        }

        public Sandy_Detailed_RPG_GearTab()
        {
            labelKey = "TabGear";
            tutorTag = "Gear";
            Slots.MakePreps(Sandy_RPG_Settings.displayAllSlots);
            UpdateSize();
        }

        protected override void UpdateSize()
        {
            if (size.x != Sandy_RPG_Settings.rpgTabWidth || size.y != Sandy_RPG_Settings.rpgTabHeight)
            {
                if (Sandy_RPG_Settings.rpgTabWidth < Slots.minRecommendedWidth)
                {
                    Sandy_RPG_Settings.rpgTabWidth = Slots.minRecommendedWidth;
                    UpdateSize();
                    return;
                }
                size = new Vector2(Sandy_RPG_Settings.rpgTabWidth, Sandy_RPG_Settings.rpgTabHeight);
                Slots.updateRightMost();
            }

        }

        public override bool IsVisible
        {
            get
            {
                Pawn selPawnForGear = SelPawnForGear;
                return ShouldShowInventory(selPawnForGear) || ShouldShowApparel(selPawnForGear) || ShouldShowEquipment(selPawnForGear);
            }
        }

        private bool CanControl
        {
            get
            {
                //Pawn selPawnForGear = SelPawnForGear;
                return cachedPawn != null && !cachedPawn.Downed && !cachedPawn.InMentalState && (cachedPawn.Faction == Faction.OfPlayer || cachedPawn.IsPrisonerOfColony) && (!cachedPawn.IsPrisonerOfColony || !cachedPawn.Spawned || cachedPawn.Map.mapPawns.AnyFreeColonistSpawned) && (!cachedPawn.IsPrisonerOfColony || (!PrisonBreakUtility.IsPrisonBreaking(cachedPawn) && (cachedPawn.CurJob == null || !cachedPawn.CurJob.exitMapOnArrival)));
            }
        }

        private bool CanControlColonist
        {
            get
            {
                return CanControl && cachedPawn.IsColonistPlayerControlled;
            }
        }

        private Pawn SelPawnForGear
        {
            get
            {
                if (SelPawn != null)
                {
                    return SelPawn;
                }
                if (SelThing is Corpse corpse)
                {
                    return corpse.InnerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + base.SelThing);
            }
        }

        public virtual bool CanShowSlots()
        {
            return cachedPawn.RaceProps.Humanlike;
        }

        protected override void FillTab()
        {
            var selPawnForGear = SelPawnForGear;
            if (cachedPawn != selPawnForGear)
            {
                cachedPawn = selPawnForGear;
            }

            if (!CanShowSlots())
            {
                base.FillTab();
                return;
            }

            Rect rect = new Rect(0f, TabU.stdPadding, size.x, size.y - TabU.stdPadding);
            float num = 0f;
            //
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            //
            string tmptext = "Sandy_ViewList".Translate(); //autofitting text in case of translations
            Vector2 tmpvector = Text.CalcSize(tmptext);
            Rect rect0 = new Rect(TabU.stdPadding / 2f, 2f, tmpvector.x + TabU.stdThingIconSize, TabU.stdThingRowHeight);
            Widgets.CheckboxLabeled(rect0, tmptext, ref viewList, false, null, null, false);
            // dev tool
            if (Prefs.DevMode && Widgets.ButtonText(new Rect(rect.xMax - 18f - 125f, 5f, 115f, Text.LineHeight), "Dev tool..."))
            {
                Find.WindowStack.Add(new FloatMenu(DebugToolsPawns.PawnGearDevOptions(SelPawnForGear)));
            }
            //
            bool simplified;
            if (viewList)
            {
                simplified = false;
            }
            else
            {
                tmptext = "Sandy_SimplifiedView".Translate();
                tmpvector = Text.CalcSize(tmptext);
                rect0 = new Rect(rect0.x + rect0.width, rect0.y, tmpvector.x + TabU.stdThingIconSize, TabU.stdThingRowHeight);
                simplified = SimplifiedView;
                bool onpressed(bool pressed) { if (pressed) SimplifiedView = (simplified = !simplified); return simplified; }
                Sandy_Utility.CustomCheckboxLabeled(rect0, tmptext, onpressed);
            }
            //
            rect = rect.ContractedBy(TabU.stdPadding / 2);
            Rect position = new Rect(rect.x, rect.y, rect.width, rect.height);
            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            GUI.BeginGroup(position);

            Rect viewRect = new Rect(0f, 0f, position.width - TabU.stdScrollbarWidth, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);

            int horSlots = Slots.horSlotCount;
            if (viewList)
            {
                DrawViewList(ref num, viewRect);
            }
            else
            {
                //basics
                bool isVisible = IsVisible;
                float statX;
                if (simplified)
                {
                    horSlots = (int)((size.x - TabU.stdPadding - TabU.stdScrollbarWidth - TabU.statPanelWidth) / TabU.thingIconOuter);
                    statX = horSlots * TabU.thingIconOuter;
                }
                else
                {
                    statX = Slots.statPanelX;
                }
                //stats
                if (isVisible)
                {
                    DrawStats1(ref num, statX);
                    GUI.color = new Color(1f, 1f, 1f, 1f);
                }
                //
                float pawnTop = Slots.rightMost & !simplified ? 0f : num;
                float pawnLeft = simplified ? statX : Slots.slotPanelWidth;
                float EquipmentTop = ((int)((pawnTop + TabU.pawnPanelSize + TabU.pawnPanelSizeAssumption) / TabU.thingIconOuter) + 1) * TabU.thingIconOuter;
                pawnTop += (EquipmentTop - pawnTop - TabU.pawnPanelSize) / 2f; //correcting pawn panel position to be exactly between equipment and stats
                                                                               //Pawn
                if (isVisible)
                {
                    Rect PawnRect = new Rect(pawnLeft, pawnTop, TabU.pawnPanelSize, TabU.pawnPanelSize);
                    DrawColonist(PawnRect, cachedPawn);
                }
                //equipment
                List<Thing> unslotedEquipment = new List<Thing>();
                if (ShouldShowEquipment(cachedPawn))
                {
                    ThingWithComps secondary = null;
                    foreach (ThingWithComps current in cachedPawn.equipment.AllEquipmentListForReading)
                    {
                        if (current != cachedPawn.equipment.Primary)
                        {
                            if (secondary == null) secondary = current;
                            else unslotedEquipment.Add(current);
                        }
                    }

                    if (secondary == null && !Sandy_RPG_Settings.displayAllSlots)
                    {
                        Rect newRect1 = new Rect(pawnLeft + TabU.thingIconInner / 2f, EquipmentTop, TabU.thingIconInner, TabU.thingIconInner);
                        if (cachedPawn.equipment.Primary == null)
                        {
                            GUI.DrawTexture(newRect1, Sandy_Utility.texBG);
                            Rect tipRect = newRect1.ContractedBy(TabU.tipContractionSize);
                            TooltipHandler.TipRegion(newRect1, "Primary_Weapon".Translate());
                        }
                        else
                        {
                            DrawThingRow1(newRect1, cachedPawn.equipment.Primary, false, true);
                        }
                    }
                    else
                    {
                        Rect newRect1 = new Rect(pawnLeft, EquipmentTop, TabU.thingIconInner, TabU.thingIconInner);
                        if (cachedPawn.equipment.Primary == null)
                        {
                            GUI.DrawTexture(newRect1, Sandy_Utility.texBG);
                            Rect tipRect = newRect1.ContractedBy(TabU.tipContractionSize);
                            TooltipHandler.TipRegion(newRect1, "Primary_Weapon".Translate());
                        }
                        else
                        {
                            DrawThingRow1(newRect1, cachedPawn.equipment.Primary, false, true);
                        }
                        //
                        Rect newRect2 = new Rect(newRect1.x + TabU.thingIconOuter, newRect1.y, newRect1.width, newRect1.height);
                        if (secondary == null)
                        {
                            GUI.DrawTexture(newRect2, Sandy_Utility.texBG);
                            Rect tipRect = newRect2.ContractedBy(TabU.tipContractionSize);
                            TooltipHandler.TipRegion(newRect2, "Secondary_Weapon".Translate());
                        }
                        else
                        {
                            DrawThingRow1(newRect2, secondary, false, true);
                        }
                    }

                    num = Math.Max(EquipmentTop + TabU.thingIconOuter, num);
                }
                //apparel
                List<Thing> unslotedApparel = new List<Thing>();
                float curSlotPanelHeight = 0f;
                if (ShouldShowApparel(cachedPawn))
                {
                    if (simplified)
                    {
                        DrawInventory1(cachedPawn.apparel.WornApparel, ref curSlotPanelHeight, 0f, horSlots, x => (x as Apparel).def.apparel.bodyPartGroups[0].listOrder);
                    }
                    else
                    {
                        //equiped apparel
                        curSlotPanelHeight = Slots.slotPanelHeight;
                        HashSet<int> usedSlots = new HashSet<int>();
                        List<Pair<ItemSlotDef, Apparel>> lockedSlots = new List<Pair<ItemSlotDef, Apparel>>();
                        foreach (Apparel apparel in cachedPawn.apparel.WornApparel)
                        {
                            List<ItemSlotDef> slotlist = Slots.dict[apparel.def];
                            if (slotlist == null)
                            {
                                unslotedApparel.Add(apparel);
                            }
                            else
                                for (var i = 0; i < slotlist.Count; i++)
                                {
                                    var slot = slotlist[i];
                                    if (slot.listid == int.MinValue)
                                        continue;
                                    else if (usedSlots.Contains(slot.listid))
                                    {
                                        unslotedApparel.Add(apparel);
                                        continue;
                                    }
                                    //
                                    if (i == 0)
                                    {
                                        Rect apRect = new Rect((slot.xPos + Slots.offsets[slot.xPos, slot.yPos].x) * TabU.thingIconOuter, (slot.yPos + Slots.offsets[slot.xPos, slot.yPos].y) * TabU.thingIconOuter, TabU.thingIconInner, TabU.thingIconInner);
                                        DrawThingRow1(apRect, apparel, false, false, false);
                                        usedSlots.Add(slot.listid);
                                    }
                                    else
                                        lockedSlots.Add(new Pair<ItemSlotDef, Apparel>(slot, apparel));
                                }
                        }
                        //locked slots
                        foreach (var pair in lockedSlots)
                        {
                            if (!pair.First.hidden
                                && !usedSlots.Contains(pair.First.listid)
                                && (Sandy_RPG_Settings.displayBG || Sandy_RPG_Settings.displayAllSlots))
                            {
                                Rect apRect = new Rect((pair.First.xPos + Slots.offsets[pair.First.xPos, pair.First.yPos].x) * TabU.thingIconOuter, (pair.First.yPos + Slots.offsets[pair.First.xPos, pair.First.yPos].y) * TabU.thingIconOuter, TabU.thingIconInner, TabU.thingIconInner);
                                DrawThingRow1(apRect, pair.Second, false, false, true);
                                usedSlots.Add(pair.First.listid);
                                TooltipHandler.TipRegion(apRect, pair.First.label);
                            }

                        }
                        //empty slots
                        foreach (ItemSlotDef slot in Slots.currentSlots)
                        {
                            if (!slot.hidden
                                && !usedSlots.Contains(slot.listid)
                                && (Sandy_RPG_Settings.displayBG || Sandy_RPG_Settings.displayAllSlots || Sandy_RPG_Settings.displayStaticSlotBG && slot.Default))
                            {
                                Rect apRect = new Rect((slot.xPos + Slots.offsets[slot.xPos, slot.yPos].x) * TabU.thingIconOuter, (slot.yPos + Slots.offsets[slot.xPos, slot.yPos].y) * TabU.thingIconOuter, TabU.thingIconInner, TabU.thingIconInner);
                                GUI.DrawTexture(apRect, Sandy_Utility.texBG);
                                TooltipHandler.TipRegion(apRect, slot.label);
                            }
                        }
                    }
                }
                num = simplified ? curSlotPanelHeight : Math.Max(num, curSlotPanelHeight);

                if (unslotedEquipment.Count > 0)
                    DrawInventory(unslotedEquipment, "Equipment", viewRect, ref num);

                if (unslotedApparel.Count > 0)
                {
                    var tmp = GUI.color;
                    GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);//Widgets.SeparatorLineColor;
                    Widgets.DrawLineHorizontal(0f, num, Slots.horSlotCount * TabU.thingIconOuter);
                    num += TabU.thingIconOuter - TabU.thingIconInner;
                    GUI.color = tmp;
                    DrawInventory1(unslotedApparel, ref num, 0, Slots.horSlotCount, x => (x as Apparel).def.apparel.bodyPartGroups[0].listOrder);
                }
            }
            //inventory
            if (ShouldShowInventory(cachedPawn))
            {
                if (simplified) viewRect.width = (horSlots - 1) * TabU.thingIconOuter + TabU.thingIconInner;
                DrawInventory(cachedPawn.inventory.innerContainer, "Inventory", viewRect, ref num, true);
            }
            //
            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num + TabU.stdPadding;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawColonist(Rect rect, Pawn pawn)
        {
            Vector2 pos = new Vector2(rect.width, rect.height);
            GUI.DrawTexture(rect, PortraitsCache.Get(pawn, pos, rot, PawnTextureCameraOffset, 1.18f, true, true, showHat, true, null, null, true));
            //
            var tmp = new Rect(rect.x + rect.width - 24, rect.y + rect.height - 24, 24, 24);
            tmp.xMin += (tmp.width - 24) / 2;
            tmp.width = 24;
            //
            if (showHat)
            {
                if (Widgets.ButtonImage(tmp, Sandy_Utility.texHideHeadgear))
                {
                    showHat = false;
                }
            }
            else
            {
                if (Widgets.ButtonImage(tmp, Sandy_Utility.texShowHeadgear))
                {
                    showHat = true;
                }
            }
            //

            tmp = new Rect(rect.x + rect.width - 24 * 2 - 2, rect.y + rect.height - 24, 24, 24);
            if (Widgets.ButtonImage(tmp, TexUI.RotRightTex))
            {
                rot.AsInt = rot.AsInt + 1 % 4;
            }
        }

        private void DrawThingRow1(Rect rect, Thing thing, bool inventory = false, bool equipment = false, bool InBackground = false)
        {
            GUI.DrawTexture(rect, Sandy_Utility.texBG);
            if (InBackground)
            {
                if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
                {
                    Rect rect1 = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
                    GUI.color = thing.DrawColor.SaturationChanged(0f);
                    Widgets.ThingIcon(rect1, thing.def, null, thing.StyleDef, thing.def.uiIconScale * 0.7f, Color.gray.SaturationChanged(0f).ToTransparent(0.5f));
                }
                return;
            }

            if (Sandy_RPG_Settings.useColorCoding)
            {
                if (thing.TryGetQuality(out QualityCategory c))
                {
                    switch (c)
                    {
                        case QualityCategory.Legendary:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colLegendary);
                                break;
                            }
                        case QualityCategory.Masterwork:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colMasterwork);
                                break;
                            }
                        case QualityCategory.Excellent:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colExcellent);
                                break;
                            }
                        case QualityCategory.Good:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colGood);
                                break;
                            }
                        case QualityCategory.Normal:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colNormal);
                                break;
                            }
                        case QualityCategory.Poor:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colPoor);
                                break;
                            }
                        case QualityCategory.Awful:
                            {
                                Sandy_RPG_Settings.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colAwful);
                                break;
                            }
                    }
                }
            }

            if (!Sandy_RPG_Settings.apparelHealthbar)
            {
                Rect rect5 = rect.ContractedBy(2f);
                var hp = (float)thing.HitPoints;
                var maxhp = Mathf.Max(thing.MaxHitPoints, hp);
                float num2 = rect5.height * (hp / maxhp);
                rect5.yMin = rect5.yMax - num2;
                rect5.height = num2;

                if (thing.HitPoints <= (maxhp / 2)) GUI.DrawTexture(rect5, Sandy_Utility.texTattered);
                else GUI.DrawTexture(rect5, Sandy_Utility.texNotTattered);
            }
            //
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Rect rect1 = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
                Widgets.ThingIcon(rect1, thing, thing.def.uiIconScale);
            }

            //bool flag = false;

            if (Mouse.IsOver(rect))
            {
                Color oldcolor = GUI.color;
                GUI.color = HighlightColor;
                //GUI.DrawTexture(rect, TexUI.HighlightTex);
                if (Widgets.ButtonImage(rect, TexUI.HighlightTex) && Event.current.button == 1)
                {
                    var list = PopupMenu(cachedPawn, thing, inventory);
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                GUI.color = oldcolor;
            }

            string text = thing.LabelCap;
            DrawSlotIcons(thing, equipment, inventory, rect, rect.x, rect.yMax - 20f);

            Text.WordWrap = true;
            string text3 = text + "\n" + ThingDetailedTip(thing, inventory);
            TooltipHandler.TipRegion(rect, text3);

            if (!Mouse.IsOver(rect))
            {
                GUI.color = Color.white;
                if (Sandy_RPG_Settings.apparelHealthbar && thing.def.useHitPoints)
                {
                    var hp = (float)thing.HitPoints;
                    var maxhp = Mathf.Max((float)thing.MaxHitPoints, hp);
                    var pct = hp / maxhp;
                    Rect rect5 = rect.ContractedBy(4f);
                    rect5.xMin += rect.width - 11f;
                    rect5.yMin = rect5.yMax - rect5.height * pct;
                    if (hp < maxhp * 0.2f) GUI.DrawTexture(rect5, Sandy_Utility.texRed);
                    else if (hp < maxhp * 0.5f) GUI.DrawTexture(rect5, Sandy_Utility.texYellow);
                    else if (hp == maxhp) GUI.DrawTexture(rect5, Sandy_Utility.texGreen);
                    else GUI.DrawTexture(rect5, Sandy_Utility.texBar);
                }
            }
        }

        public void DrawSlotIcon(Rect slotRect, ref float x, ref float y, Texture2D tex, string tip)
        {
            if (x + 20f > slotRect.xMax)
            {
                x = slotRect.x;
                y -= 20f;
            }

            var rect = new Rect(x, y, 20f, 20f);
            GUI.DrawTexture(rect, tex);
            TooltipHandler.TipRegion(rect, tip);
            x += 20f;
        }

        public void DrawSlotIcons(Thing thing, bool equipment, bool inventory, Rect slotRect, float x, float y)
        {
            if (!CanControlColonist)
                return;

            if (equipment && cachedPawn.story.traits.HasTrait(TraitDefOf.Brawler) && thing.def.IsRangedWeapon)
                DrawSlotIcon(slotRect, ref x, ref y, Sandy_Utility.texForced, "BrawlerHasRangedWeapon".Translate());

            Apparel apparel = thing as Apparel;

            bool eqLocked = cachedPawn.IsQuestLodger() && (inventory || !EquipmentUtility.QuestLodgerCanUnequip(thing, cachedPawn));
            bool apLocked = apparel != null && cachedPawn.apparel != null && cachedPawn.apparel.IsLocked(apparel);

            if (apLocked || eqLocked)
                DrawSlotIcon(slotRect, ref x, ref y, Sandy_Utility.texLock, apLocked ? "DropThingLocked".Translate() : "DropThingLodger".Translate());

            if (apparel != null)
            {
                if (cachedPawn.outfits.forcedHandler.IsForced(apparel))
                    DrawSlotIcon(slotRect, ref x, ref y, Sandy_Utility.texForced, "ForcedApparel".Translate());

                if (apparel.WornByCorpse)
                    DrawSlotIcon(slotRect, ref x, ref y, Sandy_Utility.texTainted, "WasWornByCorpse".Translate());
            }
            MODIntegration.DrawSlotIcons(this, thing, equipment, inventory, slotRect, ref x, ref y);
        }

        List<FloatMenuOption> PopupMenu(Pawn pawn, Thing thing, bool inventory)
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>
            {
                new FloatMenuOption("DefInfoTip".Translate(), delegate ()
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing, null));
                }, TexButton.Info, Color.white)
            };

            if (!CanControlColonist) return list;

            Apparel apparel = thing as Apparel;
            bool eqLocked = pawn.IsQuestLodger() && (inventory || !EquipmentUtility.QuestLodgerCanUnequip(thing, pawn));
            bool apLocked = apparel != null && pawn.apparel != null && pawn.apparel.IsLocked(apparel);

            if (!apLocked && !eqLocked && pawn.apparel?.Contains(apparel) == true && pawn.outfits?.forcedHandler != null)
                if (pawn.outfits.forcedHandler.IsForced(apparel) == true)
                {
                    list.Add(new FloatMenuOption($"{"ForcedApparel".Translate()}: {"ClearForcedApparel".Translate()}", delegate ()
                    {
                        pawn.outfits.forcedHandler.ForcedApparel.Remove(apparel);
                    }, Sandy_Utility.texForced, Color.white));
                }
                else
                {
                    list.Add(new FloatMenuOption("ForcedApparel".Translate(), delegate ()
                    {
                        pawn.outfits.forcedHandler.ForcedApparel.Add(apparel);
                    }, Sandy_Utility.texForced, Color.white));
                }

            Action action = null;
            if (!apLocked && !eqLocked)
                action = () =>
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    InterfaceDrop(thing);
                };
            list.Add(new FloatMenuOption(apLocked ? "DropThingLocked".Translate() : eqLocked ? "DropThingLodger".Translate() : "DropThing".Translate(),
                action, Sandy_Utility.texButtonDrop, Color.white));


            return list;
        }

        public void TryDrawOverallArmor1(ref float top, float left, float width, StatDef stat, string label, Texture image)
        {
            float overall = 0f;
            float natural = cachedPawn.GetStatValue(stat, true);
            List<BodyPartRecord> allParts = cachedPawn.RaceProps.body.AllParts;
            List<Apparel> list = cachedPawn.apparel?.WornApparel;
            string tip = "";
            for (int i = 0; i < allParts.Count; i++)
            {
                float partOverall = 1f - Mathf.Clamp01(natural / 2f);
                float partTotal = natural;
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                        {
                            float part = list[j].GetStatValue(stat, true);
                            partTotal += part;
                            partOverall *= 1f - Mathf.Clamp01(part / 2f);
                        }
                    }
                }
                overall += allParts[i].coverageAbs * (1f - partOverall);
                if (allParts[i].depth == BodyPartDepth.Outside
                    && (allParts[i].coverage >= 0.1 || allParts[i].def == BodyPartDefOf.Eye/* || allParts[i].def == BodyPartDefOf.Neck*/))
                    tip = string.Concat(new string[] { tip, allParts[i].LabelCap, " ", partTotal.ToStringPercent(), "\n" });
            }
            overall = Mathf.Clamp(overall * 2f, 0f, 2f);
            Rect rect = new Rect(left, top, width, TabU.statIconSize);
            Sandy_Utility.LabelWithIcon(rect, image, TabU.statIconSize, TabU.statIconSize, label, overall.ToStringPercent(), tip);
            top += TabU.stdThingRowHeight;
        }

        private void TryDrawMassInfo1(ref float top, float left, float width)
        {
            if (cachedPawn.Dead || !ShouldShowInventory(cachedPawn))
                return;
            //
            float num = MassUtility.GearAndInventoryMass(cachedPawn);
            float num2 = MassUtility.Capacity(cachedPawn, null);
            Rect rect = new Rect(left, top, width, TabU.statIconSize);
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, TabU.statIconSize, TabU.statIconSize), Sandy_Utility.texMass);
            TooltipHandler.TipRegion(rect, "MassCarriedSimple".Translate());
            rect.xMin += TabU.statIconSize;
            Sandy_Utility.BarWithInvertedText(rect, Math.Min(num / num2, 1f), "SandyMassValue".Translate(num.ToString("0.##"), num2.ToString("0.##")));
            top += TabU.stdThingRowHeight;
        }

        private void TryDrawComfyTemperatureRange1(ref float top, float left, float width)
        {
            if (cachedPawn.Dead)
                return;
            //
            float curwidth = Sandy_RPG_Settings.displayTempOnTheSameLine ? width / 2f : width;
            float statValue = cachedPawn.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            Rect rect = new Rect(left, top, curwidth, TabU.statIconSize);
            Sandy_Utility.LabelWithIcon(rect, Sandy_Utility.texMinTemp, TabU.statIconSize, TabU.statIconSize, StatDefOf.ComfyTemperatureMin.label, statValue.ToStringTemperature("F0"));
            //
            if (Sandy_RPG_Settings.displayTempOnTheSameLine) left += curwidth;
            else top += TabU.stdThingRowHeight;
            //
            statValue = cachedPawn.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            rect = new Rect(left, top, curwidth, TabU.statIconSize);
            Sandy_Utility.LabelWithIcon(rect, Sandy_Utility.texMaxTemp, TabU.statIconSize, TabU.statIconSize, StatDefOf.ComfyTemperatureMax.label, statValue.ToStringTemperature("F0"));
            top += TabU.stdThingRowHeight;
        }

        private void DrawThingRow(ref float y, float width, Thing thing, bool inventory = false)
        {
            Rect rect = new Rect(0f, y, width, TabU.stdThingRowHeight);
            Widgets.InfoCardButton(rect.width - TabU.statIconSize, y, thing);
            rect.width -= TabU.statIconSize;
            bool flag = false;
            if (CanControl && (inventory || CanControlColonist || (cachedPawn.Spawned && !cachedPawn.Map.IsPlayerHome)))
            {
                Rect rect2 = new Rect(rect.width - TabU.statIconSize, y, TabU.statIconSize, TabU.statIconSize);
                bool flag2 = false;
                if (cachedPawn.IsQuestLodger())
                {
                    flag2 = (inventory || !EquipmentUtility.QuestLodgerCanUnequip(thing, cachedPawn));
                }
                Apparel apparel;
                bool flag3 = (apparel = (thing as Apparel)) != null && cachedPawn.apparel != null && cachedPawn.apparel.IsLocked(apparel);
                flag = (flag2 || flag3);
                if (Mouse.IsOver(rect2))
                {
                    if (flag3) TooltipHandler.TipRegion(rect2, "DropThingLocked".Translate());
                    else if (flag2) TooltipHandler.TipRegion(rect2, "DropThingLodger".Translate());
                    else TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                }

                Color color = flag ? Color.grey : Color.white;
                Color mouseoverColor = flag ? color : GenUI.MouseoverColor;
                if (Widgets.ButtonImage(rect2, Sandy_Utility.texButtonDrop, color, mouseoverColor, !flag) && !flag)
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    InterfaceDrop(thing);
                }
                rect.width -= TabU.statIconSize;
            }
            if (CanControlColonist)
            {
                if (thing.IngestibleNow && cachedPawn.WillEat(thing) || thing.def.IsDrug && cachedPawn.CanTakeDrug(thing.def))
                {
                    Rect rect3 = new Rect(rect.width - TabU.statIconSize, y, TabU.statIconSize, TabU.statIconSize);
                    TooltipHandler.TipRegionByKey(rect3, "ConsumeThing", thing.LabelNoCount, thing);
                    if (Widgets.ButtonImage(rect3, Sandy_Utility.texButtonIngest, true))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        FoodUtility.IngestFromInventoryNow(cachedPawn, thing);
                    }
                }
                rect.width -= TabU.statIconSize;
            }
            Rect rect4 = rect;
            rect4.xMin = rect4.xMax - 60f;
            CaravanThingsTabUtility.DrawMass(thing, rect4);
            rect.width -= 60f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, TabU.stdThingIconSize, TabU.stdThingIconSize), thing, 1f);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect rect5 = new Rect(TabU.stdThingLeftX, y, rect.width - TabU.stdThingLeftX, rect.height);
            string text = thing.LabelCap;
            if (thing is Apparel apparel2 && cachedPawn.outfits != null && cachedPawn.outfits.forcedHandler.IsForced(apparel2))
            {
                text += ", " + "ApparelForcedLower".Translate();
            }
            if (flag)
            {
                text += " (" + "ApparelLockedLower".Translate() + ")";
            }
            Text.WordWrap = false;
            Widgets.Label(rect5, text.Truncate(rect5.width, null));
            Text.WordWrap = true;
            if (Mouse.IsOver(rect))
            {
                string text2 = ThingDetailedTip(thing, inventory);
                TooltipHandler.TipRegion(rect, text2);
            }
            y += TabU.stdThingRowHeight;
        }

        public void TryDrawOverallArmor(ref float curY, float width, StatDef stat, string label)
        {
            float num = 0f;
            float num2 = Mathf.Clamp01(cachedPawn.GetStatValue(stat, true) / 2f);
            List<BodyPartRecord> allParts = cachedPawn.RaceProps.body.AllParts;
            List<Apparel> list = cachedPawn.apparel?.WornApparel;
            for (int i = 0; i < allParts.Count; i++)
            {
                float num3 = 1f - num2;
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                        {
                            float num4 = Mathf.Clamp01(list[j].GetStatValue(stat, true) / 2f);
                            num3 *= 1f - num4;
                        }
                    }
                }
                num += allParts[i].coverageAbs * (1f - num3);
            }
            num = Mathf.Clamp(num * 2f, 0f, 2f);
            Rect rect = new Rect(0f, curY, width, 100f);
            Widgets.Label(rect, label.Truncate(120f, null));
            rect.xMin += 120f;
            Widgets.Label(rect, num.ToStringPercent());
            curY += TabU.stdLineHeight;
        }

        private void TryDrawMassInfo(ref float curY, float width)
        {
            if (cachedPawn.Dead || !ShouldShowInventory(cachedPawn))
            {
                return;
            }
            Rect rect = new Rect(0f, curY, width, TabU.stdLineHeight);
            float num = MassUtility.GearAndInventoryMass(cachedPawn);
            float num2 = MassUtility.Capacity(cachedPawn, null);
            Widgets.Label(rect, "MassCarried".Translate(num.ToString("0.##"), num2.ToString("0.##")));
            curY += TabU.stdLineHeight;
        }

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (cachedPawn.Dead)
                return;
            //
            Rect rect = new Rect(0f, curY, width, TabU.stdLineHeight);
            float statValue = cachedPawn.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            float statValue2 = cachedPawn.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            Widgets.Label(rect, string.Concat(new string[] { "ComfyTemperatureRange".Translate(), ": ", statValue.ToStringTemperature("F0"), " ~ ", statValue2.ToStringTemperature("F0") }));
            curY += 22f;
        }

        private void InterfaceDrop(Thing t)
        {
            if (t is Apparel apparel && cachedPawn.apparel != null && cachedPawn.apparel.WornApparel.Contains(apparel))
            {
                cachedPawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, apparel), JobTag.Misc);
            }
            else if (t is ThingWithComps thingWithComps && cachedPawn.equipment != null && cachedPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                cachedPawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thingWithComps), JobTag.Misc);
            }
            else if (!t.def.destroyOnDrop)
            {
                cachedPawn.inventory.innerContainer.TryDrop(t, cachedPawn.Position, cachedPawn.Map, ThingPlaceMode.Near, out Thing thing, null, null);
            }
        }

        private bool ShouldShowInventory(Pawn p)
        {
            return p.RaceProps.Humanlike || p.inventory.innerContainer.Any;
        }

        private bool ShouldShowApparel(Pawn p)
        {
            return p.apparel != null && (p.RaceProps.Humanlike || p.apparel.WornApparel.Any<Apparel>());
        }

        private bool ShouldShowEquipment(Pawn p)
        {
            return p.equipment != null;
        }

        private bool ShouldShowOverallArmor(Pawn p)
        {
            return p.RaceProps.Humanlike || ShouldShowApparel(p) || p.GetStatValue(StatDefOf.ArmorRating_Sharp, true) > 0f || p.GetStatValue(StatDefOf.ArmorRating_Blunt, true) > 0f || p.GetStatValue(StatDefOf.ArmorRating_Heat, true) > 0f;
        }

        protected void DrawViewList(ref float num, Rect viewRect)
        {
            //stats
            DrawStats(ref num, viewRect);
            //equipment
            if (this.ShouldShowEquipment(cachedPawn))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps thing in this.cachedPawn.equipment.AllEquipmentListForReading)
                {
                    DrawThingRow(ref num, viewRect.width, thing, false);
                }
            }
            //apparel
            if (this.ShouldShowApparel(cachedPawn))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel thing2 in from ap in cachedPawn.apparel.WornApparel
                                           orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                           select ap)
                {
                    DrawThingRow(ref num, viewRect.width, thing2, false);
                }
            }
        }

        protected void DrawInventory(IEnumerable<Thing> list, string title, Rect viewRect, ref float num, bool inventory = false)
        {
            Widgets.ListSeparator(ref num, viewRect.width, title.Translate());
            foreach (var item in list) DrawThingRow(ref num, viewRect.width, item, inventory);
        }

        protected virtual void DrawStats1(ref float top, float left)
        {
            this.TryDrawMassInfo1(ref top, left, TabU.statPanelWidth);
            this.TryDrawComfyTemperatureRange1(ref top, left, TabU.statPanelWidth);

            bool showArmor = ShouldShowOverallArmor(cachedPawn);
            if (showArmor)
            {
                TryDrawOverallArmor1(ref top, left, TabU.statPanelWidth, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate(), Sandy_Utility.texArmorSharp);
                TryDrawOverallArmor1(ref top, left, TabU.statPanelWidth, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate(), Sandy_Utility.texArmorBlunt);
                TryDrawOverallArmor1(ref top, left, TabU.statPanelWidth, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate(), Sandy_Utility.texArmorHeat);
            }
            MODIntegration.DrawStats1(this, ref top, left, showArmor);
        }

        protected virtual void DrawStats(ref float top, Rect rect)
        {
            TryDrawMassInfo(ref top, rect.width);
            TryDrawComfyTemperatureRange(ref top, rect.width);
            //armor
            bool showArmor = ShouldShowOverallArmor(cachedPawn);
            if (showArmor)
            {
                Widgets.ListSeparator(ref top, rect.width, "OverallArmor".Translate());
                TryDrawOverallArmor(ref top, rect.width, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate());
                TryDrawOverallArmor(ref top, rect.width, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate());
                TryDrawOverallArmor(ref top, rect.width, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate());
            }

            MODIntegration.DrawStats(this, ref top, rect, showArmor);
        }

        protected void DrawInventory1(IEnumerable<Thing> list, ref float top, float left, int horSlots, Func<Thing, int> sortby = null, bool inventory = false)
        {
            int i = 0;
            if (sortby == null) sortby = x => i;
            foreach (var thing in list.OrderByDescending(sortby))
            {
                Rect rect = new Rect(left + (i % horSlots) * TabU.thingIconOuter, top + (i / horSlots) * TabU.thingIconOuter, TabU.thingIconInner, TabU.thingIconInner);
                DrawThingRow1(rect, thing, inventory);
                i++;
            }
            //
            top += ((list.Count() / horSlots) + 1) * TabU.thingIconOuter;
        }

        static string ThingDetailedTip(Thing thing, bool inventory)
        {
            string text = thing.DescriptionDetailed;

            if (!inventory)
            {
                float mass = thing.GetStatValue(StatDefOf.Mass, true) * thing.stackCount;
                string smass = mass.ToString("G") + " kg";
                text = string.Concat(new object[] { text, "\n", smass });
            }

            if (thing.def.useHitPoints)
            {
                text = string.Concat(new object[]
                {
                        text,
                        "\n",
                        thing.HitPoints,
                        " / ",
                        thing.MaxHitPoints
                });
            }
            return text;
        }
    }
}
