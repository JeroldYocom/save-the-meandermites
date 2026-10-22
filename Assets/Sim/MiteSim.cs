namespace Meandermites.Sim
{
    /// <summary>How a Mite type reacts when a barrier triggers it.</summary>
    public enum MiteReaction
    {
        TurnLeft,   // Leftythe: 90 degrees left of its heading
        TurnRight,  // Riggimite: 90 degrees right
        RunAway     // Scattermite: directly away from the barrier
    }

    public enum MiteState
    {
        Walking,
        Reacting,
        Caught,
        Dead,
        Unspawned   // waiting inside a Spawn Portal that has not opened yet
    }

    /// <summary>Per-Mite simulation state. The Unity layer reads it, never writes it.</summary>
    public sealed class MiteSim
    {
        public int Id { get; }
        public MiteReaction Reaction { get; }

        public MiteState State { get; internal set; }
        public SimVec2 Position { get; internal set; }
        public SimVec2 ReactionDirection { get; internal set; }

        /// <summary>Steps left in the current barrier reaction.</summary>
        public int ReactionStepsLeft { get; internal set; }

        /// <summary>Steps left before a barrier can trigger this Mite again.</summary>
        public int ImmunityStepsLeft { get; internal set; }

        /// <summary>Steps of post-rescue invincibility left.</summary>
        public int InvincibleStepsLeft { get; internal set; }

        /// <summary>Steps spent struggling in the flame's grip.</summary>
        public int CaughtSteps { get; internal set; }

        /// <summary>0..1. Set to 1 on rescue; panic effects are not built yet.</summary>
        public float Panic { get; internal set; }

        /// <summary>How many times this Mite has been pulled free. Feeds the tombstone.</summary>
        public int TimesRescued { get; internal set; }

        public MiteSim(int id, MiteReaction reaction, SimVec2 position, MiteState state = MiteState.Walking)
        {
            Id = id;
            Reaction = reaction;
            Position = position;
            State = state;
        }

        public bool IsAlive => State != MiteState.Dead;
    }
}
