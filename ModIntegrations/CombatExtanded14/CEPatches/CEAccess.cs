using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using CombatExtended;
using UnityEngine;
using RimWorld;

namespace CEPatches
{
    //Hiding CE from the game if CE isn't used
    public static class CEAccess
    {
        public static object getLoadout(this Pawn pawn)
        {
            return pawn.GetLoadout();
        }

        public static object generateLoadoutFromPawn(this Pawn pawn)
        {
            return pawn.GenerateLoadoutFromPawn();
        }

        public static void addLoadoutToManager(object loadout)
        {
            LoadoutManager.AddLoadout((Loadout)loadout);
        }

        public static Window dialogManageLoadouts(Pawn pawn)
        {
            return new Dialog_ManageLoadouts(pawn.GetLoadout());
        }

        public static void setLoadout(this Pawn pawn, object loadout)
        {
            pawn.SetLoadout((Loadout)loadout);
        }

        public static bool loadoutIsEmpty(object loadout)
        {
            return ((Loadout)loadout).Slots.NullOrEmpty();
        }

        public static string getBulkTip(this Thing thing)
        {
            return thing.GetBulkTip();
        }

        public static string getWeightTip(this Thing thing)
        {
            return thing.GetWeightTip();
        }

        public static string getWeightAndBulkTip(this Thing thing)
        {
            return thing.GetWeightAndBulkTip();
        }

        public static bool isNaturalArmor(this BodyPartRecord bodyPartRecord)
        {
            return bodyPartRecord.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor);
        }

        public static bool isItemQuestLocked(this Pawn pawn, Thing thing)
        {
            return pawn.IsItemQuestLocked(thing);
        }

        public static object getCompInventory(this Pawn pawn)
        {
            return pawn.TryGetComp<CompInventory>();
        }

        public static void getCurrentAndCapacityWeight(object compInventory, out float weight, out float capacity)
        {
            CompInventory comp = (CompInventory)compInventory;
            weight = comp.currentWeight;
            capacity = comp.capacityWeight;
        }

        public static void getCurrentAndCapacityBulk(object compInventory, out float bulk, out float capacity)
        {
            CompInventory comp = (CompInventory)compInventory;
            bulk = comp.currentBulk;
            capacity = comp.capacityBulk;
        }

        public static string formatWeight(float val)
        {
            return CE_StatDefOf.CarryWeight.ValueToString(val, CE_StatDefOf.CarryWeight.toStringNumberSense, true);
        }

        public static string formatBulk(float val)
        {
            return CE_StatDefOf.CarryBulk.ValueToString(val, CE_StatDefOf.CarryBulk.toStringNumberSense, true);
        }

        public static void drawBar(Rect rect, float curr, float cap, string label, string tooltip)
        {
            Utility_Loadouts.DrawBar(rect, curr, cap, label, tooltip);
        }

        public static void trySwitchToWeapon(object compInventory, ThingWithComps newEq)
        {
            CompInventory comp = (CompInventory)compInventory;
            comp.TrySwitchToWeapon(newEq);
        }

        public static ConceptDef getConcept_InventoryWeightBulk()
        {
            return CE_ConceptDefOf.CE_InventoryWeightBulk;
        }
    }
}
