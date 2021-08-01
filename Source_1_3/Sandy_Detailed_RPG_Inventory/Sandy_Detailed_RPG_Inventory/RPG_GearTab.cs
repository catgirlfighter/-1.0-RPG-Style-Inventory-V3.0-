﻿using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using Sandy_Detailed_RPG_Inventory.MODIntegrations;

namespace Sandy_Detailed_RPG_Inventory
{
    public class Sandy_Detailed_RPG_GearTab : ITab_Pawn_Gear
    {
        private Vector2 scrollPosition = Vector2.zero;
        private float scrollViewHeight;
        private const float stdPadding = 20f;
        private const float stdThingIconSize = 28f;
        private const float stdThingRowHeight = 28f;
        private const float stdThingLeftX = 36f;
        private const float stdLineHeight = 22f;
        private const float stdScrollbarWidth = 20f;
        private const float statIconSize = 24f;
        private const float thingIconOuter = 74f;
        private const float thingIconInner = 64f;
        public const float statPanelWidth = 128f;
        private const float pawnPanelSize = 128f;
        private const float pawnPanelSizeAssumption = -28f;
        private const float tipContractionSize = 12f;
        public static readonly Vector3 PawnTextureCameraOffset = default(Vector3);
        private bool viewList = false;
        private ThingDef cacheddef = null;
        private bool cachedSimplifiedView = false;

        private bool simplifiedView
        {
            //constantly searching in dict is slower than checking for cached value
            get { return cacheddef == SelPawn.def ? cachedSimplifiedView : (cachedSimplifiedView = Sandy_RPG_Settings.simplifiedView[(cacheddef = SelPawn.def)]); }
            set { Sandy_RPG_Settings.simplifiedView[SelPawn.def] = (cachedSimplifiedView = value); Sandy_RPG_Settings.instance.Mod.WriteSettings(); }
        }

        public Sandy_Detailed_RPG_GearTab()
        {
            labelKey = "TabGear";
            tutorTag = "Gear";
            MakePreps(Sandy_RPG_Settings.displayAllSlots);
            UpdateSize();
        }

        protected override void UpdateSize()
        {
            if (size.x != Sandy_RPG_Settings.rpgTabWidth || size.y != Sandy_RPG_Settings.rpgTabHeight)
            {
                if (Sandy_RPG_Settings.rpgTabWidth < minRecommendedWidth)
                {
                    Sandy_RPG_Settings.rpgTabWidth = minRecommendedWidth;
                    UpdateSize();
                    return;
                }
                size = new Vector2(Sandy_RPG_Settings.rpgTabWidth, Sandy_RPG_Settings.rpgTabHeight);
                updateRightMost();
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
                Pawn selPawnForGear = SelPawnForGear;
                return !selPawnForGear.Downed && !selPawnForGear.InMentalState && (selPawnForGear.Faction == Faction.OfPlayer || selPawnForGear.IsPrisonerOfColony) && (!selPawnForGear.IsPrisonerOfColony || !selPawnForGear.Spawned || selPawnForGear.Map.mapPawns.AnyFreeColonistSpawned) && (!selPawnForGear.IsPrisonerOfColony || (!PrisonBreakUtility.IsPrisonBreaking(selPawnForGear) && (selPawnForGear.CurJob == null || !selPawnForGear.CurJob.exitMapOnArrival)));
            }
        }

        private bool CanControlColonist
        {
            get
            {
                return CanControl && SelPawnForGear.IsColonistPlayerControlled;
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
                Corpse corpse = SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + base.SelThing);
            }
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, stdPadding, size.x, size.y - stdPadding).ContractedBy(stdPadding / 2);
            Rect position = new Rect(rect.x, rect.y, rect.width, rect.height);
            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            Rect viewRect = new Rect(0f, 0f, position.width - stdScrollbarWidth, this.scrollViewHeight);
            float num = 0f;
            //
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            //
            string tmptext = "Sandy_ViewList".Translate(); //autofitting text in case of translations
            Vector2 tmpvector = Text.CalcSize(tmptext);
            Rect rect0 = new Rect(stdPadding / 2f, 2f, tmpvector.x + stdThingIconSize, stdThingRowHeight);
            Widgets.CheckboxLabeled(rect0, tmptext, ref viewList, false, null, null, false);
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
                rect0 = new Rect(rect0.x + rect0.width, rect0.y, tmpvector.x + stdThingIconSize, stdThingRowHeight);
                simplified = simplifiedView;
                Func<bool, bool> onpressed = delegate (bool pressed) { if (pressed) simplifiedView = (simplified = !simplified); return simplified; };
                Sandy_Utility.CustomCheckboxLabeled(rect0, tmptext, onpressed);
            }
            //

            GUI.BeginGroup(position);

            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect, true);

            int horSlots = horSlotCount;
            if (viewList)
            {
                DrawViewList(ref num, viewRect);
            }
            else
            {
                //basics
                if (SelPawnForGear.RaceProps.Humanlike)
                {
                    bool isVisible = IsVisible;
                    float statX;
                    if (simplified)
                    {
                        horSlots = (int)((size.x - stdPadding - stdScrollbarWidth - statPanelWidth) / thingIconOuter);
                        statX = horSlots * thingIconOuter;
                    }
                    else
                    {
                        statX = statPanelX;
                    }
                    //stats
                    if (isVisible)
                    {
                        DrawStats1(ref num, statX);
                        GUI.color = new Color(1f, 1f, 1f, 1f);
                    }
                    //
                    float pawnTop = rightMost & !simplified ? 0f : num;
                    float pawnLeft = simplified ? statX : slotPanelWidth;
                    float EquipmentTop = ((int)((pawnTop + pawnPanelSize + pawnPanelSizeAssumption) / thingIconOuter) + 1) * thingIconOuter;
                    pawnTop += (EquipmentTop - pawnTop - pawnPanelSize) / 2f; //correcting pawn panel position to be exactly between equipment and stats
                    //Pawn
                    if (isVisible)
                    {
                        Rect PawnRect = new Rect(pawnLeft, pawnTop, pawnPanelSize, pawnPanelSize);
                        DrawColonist(PawnRect, SelPawnForGear);
                    }
                    //equipment
                    List<Thing> unslotedEquipment = new List<Thing>();
                    if (ShouldShowEquipment(SelPawnForGear))
                    {
                        ThingWithComps secondary = null;
                        foreach (ThingWithComps current in SelPawnForGear.equipment.AllEquipmentListForReading)
                        {
                            if (current != SelPawnForGear.equipment.Primary)
                            {
                                if (secondary == null) secondary = current;
                                else unslotedEquipment.Add(current);
                            }
                        }

                        if (secondary == null && !Sandy_RPG_Settings.displayAllSlots)
                        {
                            Rect newRect1 = new Rect(pawnLeft + thingIconInner / 2f, EquipmentTop, thingIconInner, thingIconInner);
                            if (SelPawnForGear.equipment.Primary == null)
                            {
                                GUI.DrawTexture(newRect1, Sandy_Utility.texBG);
                                Rect tipRect = newRect1.ContractedBy(tipContractionSize);
                                TooltipHandler.TipRegion(newRect1, "Primary_Weapon".Translate());
                            }
                            else
                            {
                                DrawThingRow1(newRect1, SelPawnForGear.equipment.Primary, false, true);
                            }
                        }
                        else
                        {
                            Rect newRect1 = new Rect(pawnLeft, EquipmentTop, thingIconInner, thingIconInner);
                            if (SelPawnForGear.equipment.Primary == null)
                            {
                                GUI.DrawTexture(newRect1, Sandy_Utility.texBG);
                                Rect tipRect = newRect1.ContractedBy(tipContractionSize);
                                TooltipHandler.TipRegion(newRect1, "Primary_Weapon".Translate());
                            }
                            else
                            {
                                DrawThingRow1(newRect1, SelPawnForGear.equipment.Primary, false, true);
                            }
                            //
                            Rect newRect2 = new Rect(newRect1.x + thingIconOuter, newRect1.y, newRect1.width, newRect1.height);
                            if (secondary == null)
                            {
                                GUI.DrawTexture(newRect2, Sandy_Utility.texBG);
                                Rect tipRect = newRect2.ContractedBy(tipContractionSize);
                                TooltipHandler.TipRegion(newRect2, "Secondary_Weapon".Translate());
                            }
                            else
                            {
                                DrawThingRow1(newRect2, secondary, false, true);
                            }
                        }

                        num = Math.Max(EquipmentTop + thingIconOuter, num);
                    }
                    //apparel
                    List<Thing> unslotedApparel = new List<Thing>();
                    float curSlotPanelHeight = 0f;
                    if (this.ShouldShowApparel(SelPawnForGear))
                    {
                        if (simplified)
                        {
                            DrawInventory1(SelPawnForGear.apparel.WornApparel, ref curSlotPanelHeight, 0f, horSlots, x => (x as Apparel).def.apparel.bodyPartGroups[0].listOrder);
                        }
                        else
                        {
                            //equiped apparel
                            curSlotPanelHeight = slotPanelHeight;
                            HashSet<int> usedSlots = new HashSet<int>();
                            foreach (Apparel current2 in SelPawnForGear.apparel.WornApparel)
                            {
                                ItemSlotDef slot = dict[current2.def];
                                if (slot == null)
                                {
                                    unslotedApparel.Add(current2);
                                }
                                else
                                {
                                    usedSlots.Add(slot.listid);
                                    Rect apRect = new Rect((slot.xPos + offsets[slot.xPos, slot.yPos].x) * thingIconOuter, (slot.yPos + offsets[slot.xPos, slot.yPos].y) * thingIconOuter, thingIconInner, thingIconInner);
                                    DrawThingRow1(apRect, current2, false);
                                }
                            }
                            //empty slots
                            foreach (ItemSlotDef slot in currentSlots)
                            {
                                if (!slot.hidden 
                                    && !usedSlots.Contains(slot.listid) 
                                    && (Sandy_RPG_Settings.displayBG || Sandy_RPG_Settings.displayAllSlots || Sandy_RPG_Settings.displayStaticSlotBG && slot.Default))
                                {
                                    Rect apRect = new Rect((slot.xPos + offsets[slot.xPos, slot.yPos].x) * thingIconOuter, (slot.yPos + offsets[slot.xPos, slot.yPos].y) * thingIconOuter, thingIconInner, thingIconInner);
                                    GUI.DrawTexture(apRect, Sandy_Utility.texBG);
                                    Rect tipRect = apRect.ContractedBy(tipContractionSize);
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
                        Widgets.DrawLineHorizontal(0f, num, horSlotCount * thingIconOuter);
                        num += thingIconOuter - thingIconInner;
                        GUI.color = tmp;
                        DrawInventory1(unslotedApparel, ref num, 0, horSlotCount, x => (x as Apparel).def.apparel.bodyPartGroups[0].listOrder);
                    }
                }
                else
                {
                    TryDrawMassInfo(ref num, viewRect.width);
                    TryDrawComfyTemperatureRange(ref num, viewRect.width);
                }
            }
            //inventory
            if (this.ShouldShowInventory(SelPawnForGear))
            {
                if (simplified) viewRect.width = (horSlots - 1) * thingIconOuter + thingIconInner;
                DrawInventory(SelPawnForGear.inventory.innerContainer, "Inventory", viewRect, ref num, true);
            }
            //
            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num + stdPadding;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawColonist(Rect rect, Pawn pawn)
        {
            Vector2 pos = new Vector2(rect.width, rect.height);

            GUI.DrawTexture(rect, PortraitsCache.Get(pawn, pos, Rot4.South, PawnTextureCameraOffset, 1.18f));
        }

        private void DrawThingRow1(Rect rect, Thing thing, bool inventory = false, bool equipment = false)
        {
            GUI.DrawTexture(rect, Sandy_Utility.texBG);
            if (Sandy_RPG_Settings.useColorCoding)
            {
                QualityCategory c;
                if (thing.TryGetQuality(out c))
                {
                    switch (c)
                    {
                        case QualityCategory.Legendary:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colLegendary);
                                break;
                            }
                        case QualityCategory.Masterwork:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colMasterwork);
                                break;
                            }
                        case QualityCategory.Excellent:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colExcellent);
                                break;
                            }
                        case QualityCategory.Good:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colGood);
                                break;
                            }
                        case QualityCategory.Normal:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colNormal);
                                break;
                            }
                        case QualityCategory.Poor:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colPoor);
                                break;
                            }
                        case QualityCategory.Awful:
                            {
                                Sandy_Utility.texFrame.DrawTexture(rect, Sandy_RPG_Settings.colAwful);
                                break;
                            }
                    }
                }
            }

            string text = thing.LabelCap;
            if (!Sandy_RPG_Settings.apparelHealthbar)
            {
                Rect rect5 = rect.ContractedBy(2f);
                float num2 = rect5.height * ((float)thing.HitPoints / thing.MaxHitPoints);
                rect5.yMin = rect5.yMax - num2;
                rect5.height = num2;

                if (thing.HitPoints <= ((float)thing.MaxHitPoints / 2)) GUI.DrawTexture(rect5, Sandy_Utility.texTattered);
                else GUI.DrawTexture(rect5, Sandy_Utility.texNotTattered);
            }
            //
            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Rect rect1 = new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f);
                Widgets.ThingIcon(rect1, thing, 1f);
            }
            bool flag = false;
            if (Mouse.IsOver(rect))
            {
                Color oldcolor = GUI.color;
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
                Widgets.InfoCardButton(rect.x, rect.y, thing);
                if (CanControl && (inventory || CanControlColonist || (SelPawnForGear.Spawned && !SelPawnForGear.Map.IsPlayerHome)))
                {
                    Rect rect2 = new Rect(rect.xMax - statIconSize, rect.y, statIconSize, statIconSize);
                    bool flag2 = false;// this.SelPawnForGear.IsQuestLodger() && !(thing is Apparel);
                    if (SelPawnForGear.IsQuestLodger())flag2 = (inventory || !EquipmentUtility.QuestLodgerCanUnequip(thing, SelPawnForGear));
                    Apparel apparel;
                    bool flag3 = (apparel = (thing as Apparel)) != null && SelPawnForGear.apparel != null && SelPawnForGear.apparel.IsLocked(apparel);
                    flag = (flag2 || flag3);
                    if (Mouse.IsOver(rect2))
                    {
                        if (flag3)TooltipHandler.TipRegion(rect2, "DropThingLocked".Translate());
                        else if (flag2)TooltipHandler.TipRegion(rect2, "DropThingLodger".Translate());
                        else TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                    }
                    Color color = flag ? Color.grey : Color.white;
                    Color mouseoverColor = flag ? color : GenUI.MouseoverColor;
                    if (Widgets.ButtonImage(rect2, Sandy_Utility.texButtonDrop, color, mouseoverColor, !flag) && !flag)
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        InterfaceDrop(thing);
                    }
                }

                GUI.color = oldcolor;
            }
            else
            {
                GUI.color = Color.white;
                if (Sandy_RPG_Settings.apparelHealthbar && thing.def.useHitPoints)
                {
                    var pct = (float)thing.HitPoints / thing.MaxHitPoints;
                    Rect rect5 = rect.ContractedBy(4f);
                    rect5.xMin += rect.width - 11f;
                    rect5.yMin = rect5.yMax - rect5.height * pct;
                    if (thing.HitPoints < thing.MaxHitPoints * 0.2f) GUI.DrawTexture(rect5, Sandy_Utility.texRed);
                    else if(thing.HitPoints < thing.MaxHitPoints * 0.5f) GUI.DrawTexture(rect5, Sandy_Utility.texYellow);
                    else if (thing.HitPoints == thing.MaxHitPoints) GUI.DrawTexture(rect5, Sandy_Utility.texGreen);
                    else GUI.DrawTexture(rect5, Sandy_Utility.texBar);
                }
            }
            Apparel apparel2 = thing as Apparel;
            if (apparel2 != null && SelPawnForGear.outfits != null)
            {
                if (apparel2.WornByCorpse)
                {
                    Rect rect3 = new Rect(rect.xMax - 20f, rect.yMax - 20f, 20f, 20f);
                    GUI.DrawTexture(rect3, Sandy_Utility.texTainted);
                    TooltipHandler.TipRegion(rect3, "WasWornByCorpse".Translate());
                }
                if (SelPawnForGear.outfits.forcedHandler.IsForced(apparel2))
                {
                    text += ", " + "ApparelForcedLower".Translate();
                    Rect rect4 = new Rect(rect.x, rect.yMax - 20f, 20f, 20f);
                    GUI.DrawTexture(rect4, Sandy_Utility.texForced);
                    TooltipHandler.TipRegion(rect4, "ForcedApparel".Translate());
                }
            }
            if (equipment)
            {
                if (SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler) && thing.def.IsRangedWeapon)
                {
                    Rect rect6 = new Rect(rect.x, rect.yMax - 20f, 20f, 20f);
                    GUI.DrawTexture(rect6, Sandy_Utility.texForced);
                    TooltipHandler.TipRegion(rect6, "BrawlerHasRangedWeapon".Translate());
                }
            }
            if (flag)
            {
                text += " (" + "ApparelLockedLower".Translate() + ")";
            }
            Text.WordWrap = true;
            string text3 = text + "\n" + ThingDetailedTip(thing, inventory);
            TooltipHandler.TipRegion(rect, text3);

            MODIntegration.DrawThingRow1(this, rect, thing, equipment);
        }

        public void TryDrawOverallArmor1(ref float top, float left, float width, StatDef stat, string label, Texture image)
        {
            float num = 0f;
            float num2 = Mathf.Clamp01(SelPawnForGear.GetStatValue(stat, true));
            List<BodyPartRecord> allParts = SelPawnForGear.RaceProps.body.AllParts;
            List<Apparel> list = (SelPawnForGear.apparel == null) ? null : SelPawnForGear.apparel.WornApparel;
            string tip = "";
            for (int i = 0; i < allParts.Count; i++)
            {
                float num3 = 1f - num2 / 2f;
                float rawnum = num2;
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                        {
                            float num4 = Mathf.Clamp01(list[j].GetStatValue(stat, true));
                            rawnum += num4;
                            num3 *= 1f - num4 / 2f;
                        }
                    }
                }
                num += allParts[i].coverageAbs * (1f - num3);
                if (allParts[i].depth == BodyPartDepth.Outside && (allParts[i].coverage >= 0.1 || allParts[i].def == BodyPartDefOf.Eye || allParts[i].def == BodyPartDefOf.Neck))
                    tip = string.Concat(new string[] { tip, allParts[i].LabelCap, " ", (rawnum).ToStringPercent(), "\n" });
            }
            num = Mathf.Clamp(num * 2f, 0f, 2f);
            Rect rect = new Rect(left, top, width, statIconSize);
            Sandy_Utility.LabelWithIcon(rect, image, statIconSize, statIconSize, label, num.ToStringPercent(), tip);
            top += stdThingRowHeight;
        }

        private void TryDrawMassInfo1(ref float top, float left, float width)
        {
            if (SelPawnForGear.Dead || !ShouldShowInventory(SelPawnForGear))
                return;
            //
            float num = MassUtility.GearAndInventoryMass(SelPawnForGear);
            float num2 = MassUtility.Capacity(SelPawnForGear, null);
            Rect rect = new Rect(left, top, width, statIconSize);
            Sandy_Utility.LabelWithIcon(rect, Sandy_Utility.texMass, statIconSize, statIconSize, "MassCarriedSimple".Translate(), "SandyMassValue".Translate(num.ToString("0.##"), num2.ToString("0.##")));
            top += stdThingRowHeight;
        }

        private void TryDrawComfyTemperatureRange1(ref float top, float left, float width)
        {
            if (SelPawnForGear.Dead)
                return;
            //
            float curwidth = Sandy_RPG_Settings.displayTempOnTheSameLine ? width / 2f : width;
            float statValue = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            Rect rect = new Rect(left, top, curwidth, statIconSize);
            Sandy_Utility.LabelWithIcon(rect, Sandy_Utility.texMinTemp, statIconSize, statIconSize, StatDefOf.ComfyTemperatureMin.label, statValue.ToStringTemperature("F0"));
            //
            if (Sandy_RPG_Settings.displayTempOnTheSameLine) left += curwidth;
            else top += stdThingRowHeight;
            //
            statValue = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            rect = new Rect(left, top, curwidth, statIconSize);
            Sandy_Utility.LabelWithIcon(rect, Sandy_Utility.texMaxTemp, statIconSize, statIconSize, StatDefOf.ComfyTemperatureMax.label, statValue.ToStringTemperature("F0"));
            top += stdThingRowHeight;
        }

        private void DrawThingRow(ref float y, float width, Thing thing, bool inventory = false)
        {
            Rect rect = new Rect(0f, y, width, stdThingRowHeight);
            Widgets.InfoCardButton(rect.width - statIconSize, y, thing);
            rect.width -= statIconSize;
            bool flag = false;
            if (CanControl && (inventory || CanControlColonist || (SelPawnForGear.Spawned && !SelPawnForGear.Map.IsPlayerHome)))
            {
                Rect rect2 = new Rect(rect.width - statIconSize, y, statIconSize, statIconSize);
                bool flag2 = false;
                if (SelPawnForGear.IsQuestLodger())
                {
                    flag2 = (inventory || !EquipmentUtility.QuestLodgerCanUnequip(thing, SelPawnForGear));
                }
                Apparel apparel;
                bool flag3 = (apparel = (thing as Apparel)) != null && SelPawnForGear.apparel != null && SelPawnForGear.apparel.IsLocked(apparel);
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
                rect.width -= statIconSize;
            }
            if (CanControlColonist)
            {
                if (/*FoodUtility.WillIngestFromInventoryNow(SelPawnForGear, thing)*/thing.IngestibleNow && SelPawnForGear.WillEat(thing))
                {
                    Rect rect3 = new Rect(rect.width - statIconSize, y, statIconSize, statIconSize);
                    TooltipHandler.TipRegionByKey(rect3, "ConsumeThing", thing.LabelNoCount, thing);
                    if (Widgets.ButtonImage(rect3, Sandy_Utility.texButtonIngest, true))
                    {
                        SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                        FoodUtility.IngestFromInventoryNow(SelPawnForGear, thing);
                    }
                }
                rect.width -= statIconSize;
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
                Widgets.ThingIcon(new Rect(4f, y, stdThingIconSize, stdThingIconSize), thing, 1f);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect rect5 = new Rect(stdThingLeftX, y, rect.width - stdThingLeftX, rect.height);
            string text = thing.LabelCap;
            Apparel apparel2 = thing as Apparel;
            if (apparel2 != null && SelPawnForGear.outfits != null && SelPawnForGear.outfits.forcedHandler.IsForced(apparel2))
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
            y += stdThingRowHeight;
        }

        public void TryDrawOverallArmor(ref float curY, float width, StatDef stat, string label)
        {
            float num = 0f;
            float num2 = Mathf.Clamp01(SelPawnForGear.GetStatValue(stat, true) / 2f);
            List<BodyPartRecord> allParts = SelPawnForGear.RaceProps.body.AllParts;
            List<Apparel> list = (SelPawnForGear.apparel == null) ? null : SelPawnForGear.apparel.WornApparel;
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
            curY += stdLineHeight;
        }

        private void TryDrawMassInfo(ref float curY, float width)
        {
            if (SelPawnForGear.Dead || !ShouldShowInventory(this.SelPawnForGear))
            {
                return;
            }
            Rect rect = new Rect(0f, curY, width, stdLineHeight);
            float num = MassUtility.GearAndInventoryMass(SelPawnForGear);
            float num2 = MassUtility.Capacity(this.SelPawnForGear, null);
            Widgets.Label(rect, "MassCarried".Translate(num.ToString("0.##"), num2.ToString("0.##")));
            curY += stdLineHeight;
        }

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (SelPawnForGear.Dead)
                return;
            //
            Rect rect = new Rect(0f, curY, width, stdLineHeight);
            float statValue = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            float statValue2 = SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            Widgets.Label(rect, string.Concat(new string[] { "ComfyTemperatureRange".Translate(), ": ", statValue.ToStringTemperature("F0"), " ~ ", statValue2.ToStringTemperature("F0") }));
            curY += 22f;
        }

        private void InterfaceDrop(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null && SelPawnForGear.apparel != null && SelPawnForGear.apparel.WornApparel.Contains(apparel))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, apparel), JobTag.Misc);
            }
            else if (thingWithComps != null && SelPawnForGear.equipment != null && SelPawnForGear.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thingWithComps), JobTag.Misc);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawnForGear.inventory.innerContainer.TryDrop(t, SelPawnForGear.Position, SelPawnForGear.Map, ThingPlaceMode.Near, out thing, null, null);
            }
        }

        private void InterfaceIngest(Thing t)
        {
            Job job = new Job(JobDefOf.Ingest, t);
            job.count = Mathf.Min(t.stackCount, t.def.ingestible.maxNumToIngestAtOnce);
            job.count = Mathf.Min(job.count, FoodUtility.WillIngestStackCountOf(SelPawnForGear, t.def, t.GetStatValue(StatDefOf.Nutrition, true)));
            SelPawnForGear.jobs.TryTakeOrderedJob(job, JobTag.Misc);
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
            if (this.ShouldShowEquipment(SelPawnForGear))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps thing in this.SelPawnForGear.equipment.AllEquipmentListForReading)
                {
                    DrawThingRow(ref num, viewRect.width, thing, false);
                }
            }
            //apparel
            if (this.ShouldShowApparel(SelPawnForGear))
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel thing2 in from ap in SelPawnForGear.apparel.WornApparel
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
            this.TryDrawMassInfo1(ref top, left, statPanelWidth);
            this.TryDrawComfyTemperatureRange1(ref top, left, statPanelWidth);

            bool showArmor = ShouldShowOverallArmor(SelPawnForGear);
            if (showArmor)
            {
                TryDrawOverallArmor1(ref top, left, statPanelWidth, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate(), Sandy_Utility.texArmorSharp);
                TryDrawOverallArmor1(ref top, left, statPanelWidth, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate(), Sandy_Utility.texArmorBlunt);
                TryDrawOverallArmor1(ref top, left, statPanelWidth, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate(), Sandy_Utility.texArmorHeat);
            }
            MODIntegration.DrawStats1(this, ref top, left, showArmor);
        }

        protected virtual void DrawStats(ref float top, Rect rect)
        {
            TryDrawMassInfo(ref top, rect.width);
            TryDrawComfyTemperatureRange(ref top, rect.width);
            //armor
            bool showArmor = ShouldShowOverallArmor(SelPawnForGear);
            if (showArmor)
            {
                Widgets.ListSeparator(ref top, rect.width, "OverallArmor".Translate());
                TryDrawOverallArmor(ref top, rect.width, StatDefOf.ArmorRating_Sharp, "ArmorSharp".Translate());
                TryDrawOverallArmor(ref top, rect.width, StatDefOf.ArmorRating_Blunt, "ArmorBlunt".Translate());
                TryDrawOverallArmor(ref top, rect.width, StatDefOf.ArmorRating_Heat, "ArmorHeat".Translate());
            }

            MODIntegration.DrawStats(this, ref top, rect, showArmor);
        }

        protected void DrawInventory1(IEnumerable<Thing> list, ref float top, float left, int horSlots, Func<Thing,int> sortby = null, bool inventory = false)
        {
            int i = 0;
            if (sortby == null) sortby = x => i;
            foreach (var thing in list.OrderByDescending(sortby))
            {
                Rect rect = new Rect(left + (i % horSlots) * thingIconOuter, top + (i / horSlots) * thingIconOuter, thingIconInner, thingIconInner);
                DrawThingRow1(rect, thing, inventory);
                i++;
            }
            //
            top += ((list.Count() / horSlots) + 1) * thingIconOuter;
        }

        private static Vector2[,] offsets = null;
        private static Dictionary<ThingDef, ItemSlotDef> dict = null;
        private static List<ItemSlotDef> slots = null;
        private static List<ItemSlotDef> activeSlots = null;
        private static List<ItemSlotDef> currentSlots = null;
        private static float slotPanelWidth = 530f;
        private static float slotPanelHeight = 440f;
        private static float statPanelX;
        private static bool rightMost = false;
        public static float minRecommendedWidth = 700f;
        public static float maxRecommendedWidth = 700f;
        private static int horSlotCount = 5;
        private static int verSlotCount = 5;

        public static void MakePreps(bool displayAllSlots, bool reset = false)
        {
            if (dict != null && !reset)
            {
                return;
            }
            //
            int maxrow = 4;
            int maxcolumn = 4;
            int anchorCol = 3;
            //creating basics
            Sandy_RPG_Settings.FillSimplifiedViewDict();
            dict = new Dictionary<ThingDef, ItemSlotDef>();
            activeSlots = new List<ItemSlotDef>();
            slots = DefDatabase<ItemSlotDef>.AllDefsListForReading.OrderBy(x => x.validationOrder).ToList();
            //getting max values on the grid
            foreach (var slot in slots)
            {
                slot.listid = int.MinValue;
                maxrow = Math.Max(maxrow, slot.yPos);
                maxcolumn = Math.Max(maxcolumn, slot.xPos);
            }
            //checking for overlaping slots
            ItemSlotDef[,] temp = new ItemSlotDef[maxcolumn + 1, maxrow + 1];
            foreach (var slot in slots)
            {
                if (slot.anchor == SlotAnchor.None)
                    anchorCol = slot.yPos;
                //
                if (temp[slot.xPos, slot.yPos] == null)
                {
                    temp[slot.xPos, slot.yPos] = slot;
                }
                else
                {
                    if (temp[slot.xPos, slot.yPos].placeholder && !slot.placeholder)
                    {
                        temp[slot.xPos, slot.yPos].hidden = true;
                        temp[slot.xPos, slot.yPos] = slot;
                    }
                    else
                    {
                        Log.Warning($"[RPG Style Inventrory] {temp[slot.xPos, slot.yPos]} and {slot} are overlaping");
                    }
                }
            }
            //generating cache while exploring the boundaries
            offsets = new Vector2[maxcolumn + 1, maxrow + 1];
            offsets.Initialize();
            int[] rows = new int[maxrow + 1];
            int[] columns = new int[maxcolumn + 1];
            int rowoffset = 0;
            int coloffset = 0;
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.apparel != null))
            {
                foreach (var slot in slots)
                {
                    if (slot.Valid(def.apparel))
                    {
                        dict[def] = slot;
                        if (slot.listid == int.MinValue)
                        {
                            activeSlots.Add(slot);
                            slot.listid = activeSlots.Count - 1;
                            rows[slot.yPos]++;
                            columns[slot.xPos]++;
                        }
                        break;
                    }
                    dict[def] = null;
                }
            }
            //offsetting boundaries
            if (displayAllSlots)
            {
                currentSlots = slots;
                for (var i = 0; i <= maxcolumn; i++) for (var j = 0; j <= maxrow; j++) offsets[i, j] = new Vector2(0, 0);
            }
            else
            {
                currentSlots = activeSlots;

                //filling in unused slots
                MoveAnchoredToSlot(anchorCol, temp, maxcolumn, maxrow, columns, rows);
                MoveAnchoredToLeft(temp, maxcolumn, maxrow, columns, rows);
                MoveAnchoredToRight(temp, maxcolumn, maxrow, columns, rows);
                MoveAnchoredToTop(temp, maxcolumn, maxrow, columns, rows);
                MoveAnchoredToBottom(temp, maxcolumn, maxrow, columns, rows);
                //
                int i;
                int j;
                for (j = 0; j <= maxrow; j++)
                {
                    if (rows[j] == 0)
                        rowoffset--;
                    for (i = 0; i <= maxcolumn; i++)
                        if (temp[i, j] != null && temp[i, j].listid != int.MinValue)
                            offsets[temp[i, j].xPos, temp[i, j].yPos].y += rowoffset;
                    //yidx[i] = offset;
                }
                //offset = 0;
                for (i = 0; i <= maxcolumn; i++)
                {
                    if (columns[i] == 0)
                        coloffset--;
                    //xidx[i] = offset;
                    for (j = 0; j <= maxrow; j++)
                        if (temp[i, j] != null && temp[i, j].listid != int.MinValue)
                            offsets[temp[i, j].xPos, temp[i, j].yPos].x += coloffset;
                }
            }

            //resetting size values
            horSlotCount = maxcolumn + coloffset + 1;
            verSlotCount = maxrow + rowoffset + 1;
            slotPanelWidth = horSlotCount * thingIconOuter;
            slotPanelHeight = verSlotCount * thingIconOuter;
            updateRightMost();
            CalcRecommendedWidth();
        }

        static void MoveAnchoredToSlot(int anchorCol, ItemSlotDef[,] temp, int maxcolumn, int maxrow, int[] columns, int[] rows)
        {
            int i;
            int j;
            //any anchorable horizontally move closer to the anchor, to free up lines
            //from left
            HashSet<int>[] empty = new HashSet<int>[maxrow + 1];
            for (i = anchorCol; i >= 0; i--)
            {
                for (j = 0; j <= maxrow; j++)
                {
                    if (offsets[i, j] == null) offsets[i, j] = new Vector2(0, 0);
                    if (empty[j] == null) empty[j] = new HashSet<int>();
                    if (temp[i, j] == null || temp[i, j].listid == int.MinValue)
                    {
                        if (columns[i] != 0) empty[j].Add(i);
                    }
                    else if (temp[i, j].anchor != SlotAnchor.AnchorSlot
                        && (temp[i, j].anchor & SlotAnchor.Left) != SlotAnchor.Left
                        && (temp[i, j].anchor & SlotAnchor.Right) != SlotAnchor.Right)
                    {
                        empty[j].Clear(); //unmovable, sticking slots to it now
                    }
                    else
                    {
                        if (empty[j].TryMaxBy(col => col <= anchorCol ? col : int.MinValue, out var emptycol))
                        {
                            offsets[temp[i, j].xPos, temp[i, j].yPos].x = emptycol - temp[i, j].xPos;
                            var slot = temp[emptycol, j];
                            temp[emptycol, j] = temp[i, j];
                            temp[i, j] = slot;
                            empty[j].Remove(emptycol);
                            empty[j].Add(i);
                            columns[emptycol]++;
                            columns[i]--;
                        }
                    }
                }
            }
            //from right
            //empty = new HashSet<int>[maxrow + 1];
            for (i = anchorCol + 1; i <= maxcolumn; i++)
            {
                for (j = 0; j <= maxrow; j++)
                {
                    if (offsets[i, j] == null) offsets[i, j] = new Vector2(0, 0);
                    if (empty[j] == null) empty[j] = new HashSet<int>();
                    if (temp[i, j] == null || temp[i, j].listid == int.MinValue)
                    {
                        if (columns[i] != 0) empty[j].Add(i);
                    }
                    else if (temp[i, j].anchor != SlotAnchor.AnchorSlot
                        && (temp[i, j].anchor & SlotAnchor.Left) != SlotAnchor.Left
                        && (temp[i, j].anchor & SlotAnchor.Right) != SlotAnchor.Right)
                    {
                        empty[j].Clear(); //unmovable, sticking slots to it now
                    }
                    else if (empty[j].TryMinBy(col => col >= anchorCol ? col : int.MaxValue, out var emptycol) && emptycol >= anchorCol)
                    {
                        offsets[temp[i, j].xPos, temp[i, j].yPos].x = emptycol - temp[i, j].xPos;
                        var tmpslot = temp[emptycol, j];
                        temp[emptycol, j] = temp[i, j];
                        temp[i, j] = tmpslot;
                        empty[j].Remove(emptycol);
                        empty[j].Add(i);
                        columns[emptycol]++;
                        columns[i]--;
                    }
                }
            }
        }

        static void MoveAnchoredToLeft(ItemSlotDef[,] temp, int maxcolumn, int maxrow, int[] columns, int[] rows)
        {
            int i;
            int j;
            HashSet<int>[] empty = new HashSet<int>[maxrow + 1];
            for (i = 0; i <= maxcolumn; i++)
            {
                for (j = 0; j <= maxrow; j++)
                {
                    if (empty[j] == null) empty[j] = new HashSet<int>();
                    if (temp[i, j] == null || temp[i, j].listid == int.MinValue || (temp[i, j].anchor & SlotAnchor.Right) == SlotAnchor.Right)
                    {
                        if (columns[i] != 0) empty[j].Add(i);
                    }
                    else if ((temp[i, j].anchor & SlotAnchor.Left) == SlotAnchor.Left)
                    {
                        int emptycol = int.MaxValue;
                        int curi = i;
                        while (empty[j].TryMaxBy(x => x < emptycol ? x : int.MinValue, out var curemptycol) && curemptycol < emptycol)
                        {
                            emptycol = curemptycol;
                            offsets[temp[curi, j].xPos, temp[curi, j].yPos].x = emptycol - temp[curi, j].xPos;
                            //
                            if (temp[emptycol, j] != null && temp[emptycol, j].listid != int.MinValue)
                                offsets[temp[emptycol, j].xPos, temp[emptycol, j].yPos].x = i - temp[emptycol, j].xPos;
                            //
                            var tmpslot = temp[emptycol, j];
                            temp[emptycol, j] = temp[curi, j];
                            temp[curi, j] = tmpslot;
                            empty[j].Remove(emptycol);
                            empty[j].Add(curi);
                            columns[emptycol]++;
                            columns[curi]--;
                            curi = emptycol;
                        }
                    }
                }
            }
        }

        static void MoveAnchoredToRight(ItemSlotDef[,] temp, int maxcolumn, int maxrow, int[] columns, int[] rows)
        {
            int i;
            int j;
            HashSet<int>[] empty = new HashSet<int>[maxrow + 1];
            for (i = maxcolumn; i >= 0; i--)
            {
                for (j = 0; j <= maxrow; j++)
                {
                    if (empty[j] == null) empty[j] = new HashSet<int>();
                    if (temp[i, j] == null || temp[i, j].listid == int.MinValue || (temp[i, j].anchor & SlotAnchor.Left) == SlotAnchor.Left)
                    {
                        if (columns[i] != 0) empty[j].Add(i);
                    }
                    else if ((temp[i, j].anchor & SlotAnchor.Right) == SlotAnchor.Right)
                    {

                        int emptycol = int.MinValue; //move them step by step to not mess up ordering for other slots
                        int curi = i;
                        while (empty[j].TryMinBy(x => x > emptycol ? x : int.MaxValue, out var curemptycol) && curemptycol > emptycol)
                        {
                            emptycol = curemptycol;
                            offsets[temp[curi, j].xPos, temp[curi, j].yPos].x = emptycol - temp[curi, j].xPos;
                            //
                            if (temp[emptycol, j] != null && temp[emptycol, j].listid != int.MinValue)
                                offsets[temp[emptycol, j].xPos, temp[emptycol, j].yPos].x = i - temp[emptycol, j].xPos;
                            //
                            var tmpslot = temp[emptycol, j];
                            temp[emptycol, j] = temp[curi, j];
                            temp[curi, j] = tmpslot;
                            empty[j].Remove(emptycol);
                            empty[j].Add(curi);
                            columns[emptycol]++;
                            columns[curi]--;
                            curi = emptycol;
                        }
                    }
                }
            }
        }

        static void MoveAnchoredToTop(ItemSlotDef[,] temp, int maxcolumn, int maxrow, int[] columns, int[] rows)
        {
            int i;
            int j;
            HashSet<int>[] empty = new HashSet<int>[maxcolumn + 1];
            for (j = 0; j <= maxrow; j++)
            {
                for (i = 0; i <= maxcolumn; i++)
                {
                    if (empty[i] == null) empty[i] = new HashSet<int>();
                    if (temp[i, j] == null || temp[i, j].listid == int.MinValue || (temp[i, j].anchor & SlotAnchor.Bottom) == SlotAnchor.Bottom)
                    {
                        if (rows[j] != 0) empty[i].Add(j);
                    }
                    else if ((temp[i, j].anchor & SlotAnchor.Top) == SlotAnchor.Top)
                    {
                        int emptyrow = int.MaxValue;
                        int curj = j;
                        while (empty[i].TryMaxBy(x => x < emptyrow ? x : int.MinValue, out var curemptyrow) && curemptyrow < emptyrow)
                        {
                            emptyrow = curemptyrow;
                            offsets[temp[i, curj].xPos, temp[i, curj].yPos].y = emptyrow - temp[i, curj].yPos;
                            //
                            if (temp[i, emptyrow] != null && temp[i, emptyrow].listid != int.MinValue)
                                offsets[temp[i, emptyrow].xPos, temp[i, emptyrow].yPos].y = j - temp[i, emptyrow].yPos;
                            //
                            var tmpslot = temp[i, emptyrow];
                            temp[i, emptyrow] = temp[i, curj];
                            temp[i, curj] = tmpslot;
                            empty[i].Remove(emptyrow);
                            empty[i].Add(curj);
                            rows[emptyrow]++;
                            rows[curj]--;
                            curj = emptyrow;
                        }
                    }
                }
            }
        }

        static void MoveAnchoredToBottom(ItemSlotDef[,] temp, int maxcolumn, int maxrow, int[] columns, int[] rows)
        {
            int i;
            int j;
            HashSet<int>[] empty = new HashSet<int>[maxcolumn + 1];
            for (j = maxrow; j >= 0; j--)
            {
                for (i = 0; i <= maxcolumn; i++)
                {
                    if (empty[i] == null) empty[i] = new HashSet<int>();
                    if (temp[i, j] == null || temp[i, j].listid == int.MinValue || (temp[i, j].anchor & SlotAnchor.Top) == SlotAnchor.Top)
                    {
                        if (rows[j] != 0) empty[i].Add(j);

                    }
                    else if ((temp[i, j].anchor & SlotAnchor.Bottom) == SlotAnchor.Bottom)
                    {
                        int emptyrow = int.MinValue;
                        int curj = j;
                        while (empty[i].TryMinBy(x => x > emptyrow ? x : int.MaxValue, out var curemptyrow) && curemptyrow > emptyrow)
                        {
                            emptyrow = curemptyrow;
                            offsets[temp[i, curj].xPos, temp[i, curj].yPos].y = emptyrow - temp[i, curj].yPos;
                            //
                            if (temp[i, emptyrow] != null && temp[i, emptyrow].listid != int.MinValue)
                                offsets[temp[i, emptyrow].xPos, temp[i, emptyrow].yPos].y = j - temp[i, emptyrow].yPos;
                            //
                            var tmpslot = temp[i, emptyrow];
                            temp[i, emptyrow] = temp[i, curj];
                            temp[i, curj] = tmpslot;
                            empty[i].Remove(emptyrow);
                            empty[i].Add(curj);
                            rows[emptyrow]++;
                            rows[curj]--;
                            curj = emptyrow;
                        }
                    }
                }
            }
        }

        protected static void updateRightMost()
        {
            rightMost = Sandy_RPG_Settings.rpgTabWidth - slotPanelWidth - statPanelWidth - pawnPanelSize - stdPadding - stdScrollbarWidth >= 0f;
            
            statPanelX = rightMost ? slotPanelWidth + pawnPanelSize + stdPadding : slotPanelWidth;
        }

        public static void CalcRecommendedWidth()
        {
            maxRecommendedWidth = slotPanelWidth + pawnPanelSize + stdPadding * 2 + statPanelWidth + stdScrollbarWidth;
            minRecommendedWidth = slotPanelWidth + statPanelWidth + stdPadding * 2 + stdScrollbarWidth;
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

    public class ItemSlotDef : Def
    {
        //public Func<ApparelProperties, bool> validator = null;
        public int xPos = int.MinValue;
        public int yPos = int.MinValue;
        public int validationOrder = 0;
        public bool placeholder = false;
        public List<ApparelLayerDef> apparelLayers = new List<ApparelLayerDef>();
        public List<BodyPartGroupDef> bodyPartGroups = new List<BodyPartGroupDef>();
        public SlotAnchor anchor = SlotAnchor.AnchorSlot;
        public bool Default = false;
        [Unsaved(false)]
        public int listid = int.MinValue;
        [Unsaved(false)]
        public bool hidden = false;

        public bool Valid(ApparelProperties apparelProperties)
        {
            if (placeholder) return false;

            bool result;

            if (apparelLayers.NullOrEmpty())
            {
                result = true;
            }
            else
            {
                result = false;
                foreach (var layer in apparelLayers)
                    if (apparelProperties.layers.Contains(layer))
                    {
                        result = true;
                        break;
                    }
            }

            if (result && !bodyPartGroups.NullOrEmpty())
            {
                result = false;
                foreach (var bpg in bodyPartGroups)
                    if (apparelProperties.bodyPartGroups.Contains(bpg))
                    {
                        result = true;
                        break;
                    }
            }

            return result;
        }
    }

    public enum SlotAnchor
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        TopLeft = Top + Left,
        TopRight = Top + Right,
        BottomLeft = Bottom + Left,
        BottomRight = Bottom + Right,
        AnchorSlot = 16
    }
}