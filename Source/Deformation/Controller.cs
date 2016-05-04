/* ============================================================================= *\
 * The MIT License (MIT) 
 * Copyright(c) 2015 Thomas P.

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
\* ============================================================================= */

/* ============================================================================= *\
* Modified version by BubbleWrap
\* ============================================================================= */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KerbalTerrainSystem
{
    namespace Deformation
    {
        /// <summary>Class to add a Module to all vessels, to register Deformation</summary>
        [KSPAddon(KSPAddon.Startup.MainMenu, false)]
        public class CollisionController : MonoBehaviour
        {
            // Generic Crater Values
            public double craterWidth = 1d;
            public double craterDepth = 0.1d;

            // Singleton Instance
            public static CollisionController Instance { get; private set; }

            // Is the Rebuilding-Process active
            public bool isBuilding = false;

            // Register the Vessel-Handler and stop the GarbageCollection
            public void Start()
            {
                // Create the Singelton
                if (Instance != null)
                    Destroy(this);
                else
                    Instance = this;

                // Register the Handler
                GameEvents.onCrash.Add(onCrash);
                GameEvents.onCollision.Add(onCollision);

                // Stop the GC
                DontDestroyOnLoad(this);

                // Add the Terrain-Handler to all bodies
                foreach (CelestialBody body in PSystemManager.Instance.localBodies.Where(b => b.pqsController != null))
                {
                    GameObject modObject = new GameObject("TerrainDeformation");
                    modObject.transform.parent = body.pqsController.transform;
                    PQSMod_TerrainDeformation mod = modObject.AddComponent<PQSMod_TerrainDeformation>();
                    mod.deformations = new List<Deformation>();
                    mod.order = int.MaxValue; // Make sure that we're the last mod
                    mod.modEnabled = true;
                    mod.sphere = body.pqsController;
                }
            }

            // If a vessel dies, rebuild the sphere
            public void onCrash(EventReport e)
            {
                Part originPart = e.origin;
                if ( originPart == null ) { return; }

                Vessel vessel = originPart.vessel;
                // If there's no vessel, abort
                if (vessel == null)
                    return;

                Debug.LogFormat("[Kerbal Terrain System] onCrash: vessel={0}; speed={1}; mass={2}; state={3}", vessel.vesselName, vessel.speed, vessel.totalMass, vessel.state);
                GenerateDeformation(vessel);
            }
            public void onCollision(EventReport e)
            {
                Part originPart = e.origin;
                if (originPart == null) { return; }

                Vessel vessel = originPart.vessel;
                // If there's no vessel, abort
                if (vessel == null)
                    return;

                if ( !(e.other.Equals("the surface"))) { return; }
                Debug.LogFormat("[Kerbal Terrain System] onCollision: vessel={0}; speed={1}; mass={2}; state={3}; other={4}", vessel.vesselName, vessel.speed, vessel.totalMass, vessel.state, e.other);
                GenerateDeformation(vessel);
            }


            public void GenerateDeformation(Vessel vessel)
            {
                // Get the CelestialBody
                CelestialBody body = vessel.mainBody;

                // Create the Deformation
                Deformation deformation = new Deformation()
                {
                    position = Utility.LLAtoECEF(vessel.latitude, vessel.longitude, vessel.altitude, vessel.mainBody.Radius),
                    body = vessel.mainBody,
                    depth = vessel.speed * craterDepth,
                    width = vessel.speed * craterWidth
                };
                body.GetComponentInChildren<PQSMod_TerrainDeformation>().deformations.Add(deformation);

                // Rebuild the Sphere
                PQ[] quads = Utility.FindNearbyQuads(body.pqsController, vessel.vesselTransform, 9);
                StartCoroutine(Utility.RebuildSphere(quads, body.pqsController));
            }

        }
    }
}