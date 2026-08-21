using HarmonyLib;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// Reduces the stock PartBuoyancy translational damping after it has calculated
    /// water drag. Cavity geometry is calculated once per vessel per physics tick.
    /// </summary>
    [HarmonyPatch(typeof(PartBuoyancy), "FixedUpdate")]
    internal static class WBISupercavitationDragPatch
    {
        static void Postfix(Part ___part)
        {
            if (___part == null || ___part.rb == null || ___part.vessel == null ||
                ___part.submergedPortion <= 0.0)
                return;

            WBISupercavitationController controller;
            if (!WBISupercavitationController.TryGetController(
                ___part.vessel, out controller))
                return;

            float multiplier = controller.GetWaterDragMultiplier(___part);
            if (multiplier >= 1f)
                return;

            float stockDrag = ___part.rb.drag;
            ___part.rb.drag *= multiplier;
            if (___part.servoRb != null)
                ___part.servoRb.drag *= multiplier;
            controller.RecordWaterDragApplication(___part, stockDrag, ___part.rb.drag);
        }
    }
}
