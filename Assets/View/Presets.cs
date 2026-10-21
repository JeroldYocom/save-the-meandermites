using Meandermites.Sim;

namespace Meandermites.View
{
    /// <summary>
    /// Debug level setups for feel testing. These are View-side conveniences,
    /// not content: real levels will come from level data.
    /// </summary>
    public static class Presets
    {
        public const int Count = 4;

        public static string Name(int preset)
        {
            switch (preset)
            {
                case 1: return "1: three Leftythes, one portal";
                case 2: return "2: mixed Mites, one portal";
                case 3: return "3: two portals, four mixed Mites";
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
                default:
                    return Make(seed, duration, new[] { 0, 0, 0 }, new[] { 3 });
            }
        }

        private static LevelConfig Make(ulong seed, int duration, int[] reactions, int[] portals)
        {
            var mites = new MiteSpec[reactions.Length];
            for (int i = 0; i < mites.Length; i++)
                mites[i] = new MiteSpec(i + 1, (MiteReaction)reactions[i]);
            return new LevelConfig(seed, duration, LevelConfig.FieldCentre, mites, portals);
        }
    }
}
