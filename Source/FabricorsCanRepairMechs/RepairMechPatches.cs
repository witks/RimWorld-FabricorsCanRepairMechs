using HarmonyLib;
using RimWorld;
using Verse;

namespace FabricorsCanRepairMechs
{
    // Rules for a mech repairing another mech. Only the cases vanilla does not already prevent are checked here.
    public static class MechRepairRules
    {
        // A mech that can do the repair work type (vanilla: Fabricor).
        public static bool IsRepairerMech(Pawn p, WorkTypeDef workType)
        {
            return p.RaceProps.IsMechanoid
                && p.RaceProps.mechEnabledWorkTypes != null
                && p.RaceProps.mechEnabledWorkTypes.Contains(workType);
        }

        public static bool CanMechRepair(Pawn repairer, Pawn target, WorkTypeDef workType)
        {
            // No self repair.
            if (target == repairer)
            {
                return false;
            }
            // No chains: repairing makes the target wait, which would interrupt its own repair job.
            if (target.CurJobDef == JobDefOf.RepairMech)
            {
                return false;
            }
            // No bouncing: a damaged repairer does not repair other repairers.
            if (MechRepairUtility.CanRepair(repairer) && IsRepairerMech(target, workType))
            {
                return false;
            }
            return true;
        }
    }

    // Vanilla skips anyone who is not a mechanitor; let mechs with the work type through.
    [HarmonyPatch(typeof(WorkGiver_RepairMech), nameof(WorkGiver_RepairMech.ShouldSkip))]
    public static class WorkGiver_RepairMech_ShouldSkip_Patch
    {
        public static bool Prefix(Pawn pawn, ref bool __result)
        {
            if (!pawn.RaceProps.IsMechanoid)
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(WorkGiver_RepairMech), nameof(WorkGiver_RepairMech.HasJobOnThing))]
    public static class WorkGiver_RepairMech_HasJobOnThing_Patch
    {
        public static void Postfix(WorkGiver_RepairMech __instance, Pawn pawn, Thing t, ref bool __result)
        {
            if (!__result || !pawn.RaceProps.IsMechanoid)
            {
                return;
            }
            if (!(t is Pawn target) || !MechRepairRules.CanMechRepair(pawn, target, __instance.def.workType))
            {
                __result = false;
            }
        }
    }
}
