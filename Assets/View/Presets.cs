using Meandermites.Sim;

namespace Meandermites.View
{
    /// <summary>
    /// Debug level setups for feel testing. These are View-side conveniences,
    /// not content: real levels will come from level data.
    /// </summary>
    public static class Presets
    {
        public const int Count = 5;

        public static string Name(int preset)
        {
            switch (preset)
            {
                case 1: return "1: three Leftythes, one portal";
                case 2: return "2: mixed Mites, one portal";
                case 3: return "3: two portals, four mixed Mites";
                case 5: return "5: Stones, three Leftythes";
                default: return "4: five Leftythes, one portal";
            }
        }

        public static LevelConfig Build(int preset, ulong seed)
        {
            int duration = SimConstants.Steps(40f);
            switch (preset)
            {
                case 2:
                    return Make(seed, duration, new[] { 0, 1, 2 }, new[] { 3 });
                case 3:
                    return Make(seed, duration, new[] { 0, 1, 2, 0 }, new[] { 2, 2 });
                case 4:
                    return Make(seed, duration, new[] { 0, 0, 0, 0, 0 }, new[] { 5 });
                case 5:
                    return Make(seed, duration, new[] { 0, 0, 0 }, new[] { 3 }, new[]
                    {
                        new SimObstacle(new SimVec2(5.4f, 4.5f), 0.8f),
                        new SimObstacle(new SimVec2(10.6f, 4.5f), 0.8f),
                        new SimObstacle(new SimVec2(8f, 7.0f), 0.8f)
                    });
                default:
                    return Make(seed, duration, new[] { 0, 0, 0 }, new[] { 3 });
            }
        }

        private static LevelConfig Make(ulong seed, int duration, int[] reactions, int[] portals, SimObstacle[] stones = null)
        {
            var mites = new MiteSpec[reactions.Length];
            for (int i = 0; i < mites.Length; i++)
                mites[i] = new MiteSpec(i + 1, (MiteReaction)reactions[i]);
            return new LevelConfig(seed, duration, LevelConfig.FieldCentre, mites, portals, null, stones);
        }
    }
}
