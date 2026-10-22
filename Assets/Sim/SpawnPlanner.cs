using System;

namespace Meandermites.Sim
{
    /// <summary>
    /// Places Spawn Portals and the Mites they release (decision 1.3).
    /// Portals sit on the field edge, evenly spaced with a small seeded jitter,
    /// and never within MinSpawnDistanceFromFlame of the flame.
    ///
    /// Spacing is even along the perimeter rather than by true angle. With the
    /// flame near the centre the two are close, and perimeter spacing needs no
    /// trigonometry, which keeps the Sim exactly reproducible.
    /// </summary>
    public static class SpawnPlanner
    {
        public const float Perimeter = 2f * (SimConstants.FieldWidth + SimConstants.FieldHeight);

        /// <summary>Point on the field edge at distance t along the perimeter (wraps).</summary>
        public static SimVec2 PerimeterPoint(float t)
        {
            t = Wrap(t);
            float w = SimConstants.FieldWidth;
            float h = SimConstants.FieldHeight;
            if (t < w) return new SimVec2(t, 0f);
            t -= w;
            if (t < h) return new SimVec2(w, t);
            t -= h;
            if (t < w) return new SimVec2(w - t, h);
            t -= w;
            return new SimVec2(0f, h - t);
        }

        public static float Wrap(float t)
        {
            return t - Perimeter * (float)Math.Floor(t / Perimeter);
        }

        public static bool IsValidSpawn(SimVec2 p, SimVec2 flame)
        {
            float min = SimConstants.MinSpawnDistanceFromFlame;
            return (p - flame).SqrLength >= min * min;
        }

        /// <summary>
        /// One start position per Mite, in Mite order. Consumes the RNG the
        /// same way every time, so a seed always gives the same layout.
        /// </summary>
        public static SimVec2[] Plan(LevelConfig config, SimRandom rng)
        {
            return Plan(config, rng, out _);
        }

        /// <summary>
        /// As above, and also the position of each portal (the centre of its
        /// group). Empty when the level uses explicit starts and has no portals.
        /// </summary>
        public static SimVec2[] Plan(LevelConfig config, SimRandom rng, out SimVec2[] portalPositions)
        {
            var result = new SimVec2[config.Mites.Length];

            if (config.ExplicitStarts != null)
            {
                for (int i = 0; i < result.Length; i++) result[i] = config.ExplicitStarts[i];
                portalPositions = new SimVec2[0];
                return result;
            }

            int portals = config.PortalCounts.Length;
            float slot = Perimeter / portals;

            // Global rotation, then per-portal jitter, both in quarter units.
            float origin = rng.NextInt(200) * 0.25f;
            int mite = 0;
            portalPositions = new SimVec2[portals];
            for (int k = 0; k < portals; k++)
            {
                float jitter = (rng.NextInt(9) - 4) * 0.25f;
                float centre = origin + k * slot + jitter;
                int count = config.PortalCounts[k];

                float placed = PlaceGroup(centre, count, config.FlamePosition, result, mite);
                portalPositions[k] = PerimeterPoint(placed);
                mite += count;
            }
            return result;
        }

        // A burst is placed as one group, spaced along the perimeter. If any
        // member is too close to the flame the whole group slides together, so
        // members never pile up on the first valid spot. Bounded, so it always
        // terminates.
        private static float PlaceGroup(float centre, int count, SimVec2 flame, SimVec2[] result, int first)
        {
            int maxSlides = (int)(Perimeter / SimConstants.PerimeterSlideStep);
            for (int s = 0; s <= maxSlides; s++)
            {
                bool allValid = true;
                for (int j = 0; j < count; j++)
                {
                    float t = centre + (j - (count - 1) * 0.5f) * SimConstants.SpawnSpacing;
                    SimVec2 p = PerimeterPoint(t);
                    result[first + j] = p;
                    if (!IsValidSpawn(p, flame)) allValid = false;
                }
                if (allValid) return centre;
                centre += SimConstants.PerimeterSlideStep;
            }
            return centre;
        }
    }
}
