using System;
using System.Runtime.CompilerServices;

namespace Idlebound.Foundation
{
    // Presentation-independent clock. A session can have only one runtime and one clock caller.
    public sealed class SessionRuntime
    {
        private static readonly ConditionalWeakTable<GatheringSession, SessionRuntime> Owners =
            new ConditionalWeakTable<GatheringSession, SessionRuntime>();
        private readonly object clockOwner;
        private double previousTime;
        private long previousWallTicks;
        private bool away;
        public GatheringSession Session { get; }
        public bool Running { get; private set; } = true;
        public bool IsAway => away;

        public SessionRuntime(GatheringSession session, object clockOwner, double now = 0)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            this.clockOwner = clockOwner ?? throw new ArgumentNullException(nameof(clockOwner));
            ValidateTime(now); previousTime = now; previousWallTicks = WallTicks(now);
            if (Owners.TryGetValue(session, out _)) throw new InvalidOperationException("This session already has a clock owner.");
            Owners.Add(session, this);
        }
        private void Check(object caller, double now)
        {
            if (!ReferenceEquals(caller, clockOwner)) throw new InvalidOperationException("Only the registered host may advance time.");
            ValidateTime(now);
            if (now < previousTime) throw new ArgumentOutOfRangeException(nameof(now));
        }
        private static void ValidateTime(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
        }
        private static long WallTicks(double now) => checked((long)Math.Floor(now * TimeSpan.TicksPerSecond));
        private void Consume(double now, bool offline)
        {
            // Quantize absolute time once, rather than rounding every frame's delta.
            // This preserves sub-tick time and partition invariance without accumulating float drift.
            long wallTicks = WallTicks(now);
            long ticks = Running ? wallTicks - previousWallTicks : 0;
            // Commit the wall-clock cursor only after the domain has successfully settled.
            Session.SettleTo(checked(Session.LastSettledTicks + ticks), offline);
            previousWallTicks = wallTicks;
            previousTime = now;
        }
        public void Tick(object caller, double now)
        {
            Check(caller, now);
            if (!away) Consume(now, false);
        }
        public void SetPresence(object caller, double now, bool paused, bool focused)
        {
            Check(caller, now);
            bool nextAway = paused || !focused;
            if (nextAway != away)
            {
                Consume(now, away); // Enter: online boundary; return: one contiguous offline interval.
                away = nextAway;
            }
        }
        public void SetRunning(object caller, double now, bool value)
        {
            Check(caller, now);
            if (away) throw new InvalidOperationException("Change the development clock while present.");
            Consume(now, false);
            Running = value;
        }
        public void Step(object caller, long ticks, bool offline = false)
        {
            if (!ReferenceEquals(caller, clockOwner)) throw new InvalidOperationException("Only the registered host may step time.");
            if (Running || away || ticks < 0) throw new InvalidOperationException("Pause the present development clock before stepping.");
            Session.SettleTo(checked(Session.LastSettledTicks + ticks), offline);
        }
    }
}
