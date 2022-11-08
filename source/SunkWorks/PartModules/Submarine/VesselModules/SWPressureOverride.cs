using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

namespace SunkWorks.Submarine
{
    /// <summary>
    /// A helpful vessel module to handle overriding the maximum hull pressure of a vessel's parts.
    /// </summary>
    public class WBIPressureOverride : VesselModule
    {
        #region Housekeeping
        /// <summary>
        /// Overrides how much pressure the vessel can take.
        /// </summary>
        public double maxPressureOverride;

        /// <summary>
        /// List of dive computers
        /// </summary>
        protected List<SWDiveComputer> diveComputers;

        /// <summary>
        /// Current vessel part count
        /// </summary>
        protected int partCount;
        #endregion

        #region Overrides
        public override Activation GetActivation()
        {
            return Activation.LoadedVessels;
        }

        public override bool ShouldBeActive()
        {
            return vessel.loaded;
        }

        protected override void OnStart()
        {
            base.OnStart();

            updateMaxPressure();
        }

        public void Update()
        {
            updateMaxPressure();
        }

        public void Destroy()
        {
            diveComputers = null;
        }
        #endregion

        #region Helpers
        protected virtual void updateMaxPressure()
        {
            //Update the list of dive computers
            if (partCount != vessel.parts.Count)
            {
                partCount = vessel.parts.Count;
                diveComputers = vessel.FindPartModulesImplementing<SWDiveComputer>();
                if (diveComputers == null)
                    return;

                //If we don't have any dive coumputers then we're done.
                int count = diveComputers.Count;
                if (count == 0)
                    return;

                //Find the highest pressure override
                for (int index = 0; index < count; index++)
                {
                    if (diveComputers[index].maxPressureOverride > maxPressureOverride)
                        maxPressureOverride = diveComputers[index].maxPressureOverride;
                }

                //Now go through all the parts and override their max pressure
                Part part;
                for (int index = 0; index < partCount; index++)
                {
                    part = vessel.parts[index];
                    part.maxPressure = maxPressureOverride;
                    part.submergedDragScalar = 0.001;
                }
            }
        }
        #endregion
    }
}
