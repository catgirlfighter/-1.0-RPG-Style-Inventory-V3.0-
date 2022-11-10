using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace Sandy_Detailed_RPG_Inventory
{
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

        public bool ValidLayers(ApparelProperties apparelProperties)
        {
            if (apparelLayers.NullOrEmpty())
                return true;
            else
                foreach (var layer in apparelLayers)
                    if (apparelProperties.layers.Contains(layer))
                        return true;
            //
            return false;
        }

        public bool ValidBodyParts(ApparelProperties apparelProperties)
        {
            if (bodyPartGroups.NullOrEmpty())
                return true;
            else
                foreach (var bpg in bodyPartGroups)
                    if (apparelProperties.bodyPartGroups.Contains(bpg))
                        return true;
            //
            return false;
        }

        public bool ValidCoverage(ApparelProperties apparelProperties)
        {
            var appBPGs = apparelProperties.GetInterferingBodyPartGroups(BodyDefOf.Human);
            foreach (var bpg in bodyPartGroups)
                if (appBPGs.Contains(bpg))
                    return true;
            //
            return false;
        }

        public bool Valid(ApparelProperties apparelProperties, bool coverage = false)
        {
            if (placeholder) return false;
            return ValidLayers(apparelProperties) && (coverage && ValidCoverage(apparelProperties) || !coverage && ValidBodyParts(apparelProperties));
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

    public static class Slots
    {
        public static Dictionary<ThingDef, List<ItemSlotDef>> dict = null;
        private static List<ItemSlotDef> slots = null;
        private static List<ItemSlotDef> activeSlots = null;
        public static Vector2[,] offsets = null;
        public static List<ItemSlotDef> currentSlots = null;
        public static float slotPanelWidth = 530f;
        public static float slotPanelHeight = 440f;
        public static float statPanelX;
        public static bool rightMost = false;
        public static float minRecommendedWidth = 700f;
        public static float maxRecommendedWidth = 700f;
        public static int horSlotCount = 5;
        public static int verSlotCount = 5;

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
            dict = new Dictionary<ThingDef, List<ItemSlotDef>>();
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

            //List<ItemSlotDef> tmplist = new List<ItemSlotDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.apparel != null))
            {
                if (!dict.ContainsKey(def))
                    dict[def] = null;
                //
                List<ItemSlotDef> list = dict[def];
                foreach (var slot in slots)
                {
                    if (slot.Valid(def.apparel))
                    {
                        if (list == null)
                        {
                            list = new List<ItemSlotDef>();
                            list.Add(slot);
                            //list.AddRange(tmplist);
                            //tmplist.Clear();
                            dict[def] = list;
                        }
                        else
                            list.Add(slot);
                        //
                        if (slot.listid == int.MinValue && list[0] == slot)
                        {
                            activeSlots.Add(slot);
                            slot.listid = activeSlots.Count - 1;
                            rows[slot.yPos]++;
                            columns[slot.xPos]++;
                        }
                    }
                    else if (slot.Valid(def.apparel, true))
                    {
                        if (list == null)
                        {
                            //    tmplist.Add(slot);
                        }
                        else
                            list.Add(slot);
                    }

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
            slotPanelWidth = horSlotCount * TabU.thingIconOuter;
            slotPanelHeight = verSlotCount * TabU.thingIconOuter;
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

        public static void updateRightMost()
        {
            rightMost = Sandy_RPG_Settings.rpgTabWidth - slotPanelWidth - TabU.statPanelWidth - TabU.pawnPanelSize - TabU.stdPadding - TabU.stdScrollbarWidth >= 0f;

            statPanelX = rightMost ? slotPanelWidth + TabU.pawnPanelSize + TabU.stdPadding : slotPanelWidth;
        }

        public static void CalcRecommendedWidth()
        {
            maxRecommendedWidth = slotPanelWidth + TabU.pawnPanelSize + TabU.stdPadding * 2 + TabU.statPanelWidth + TabU.stdScrollbarWidth;
            minRecommendedWidth = slotPanelWidth + TabU.statPanelWidth + TabU.stdPadding * 2 + TabU.stdScrollbarWidth;
        }
    }
}
