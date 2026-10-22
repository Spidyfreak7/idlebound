using System;
using System.Linq;

namespace Idlebound.Foundation
{
    public interface IUtcClock { long UtcTicks { get; } }
    public sealed class SystemUtcClock : IUtcClock { public long UtcTicks => DateTime.UtcNow.Ticks; }

    // Disk transaction coordinator. No Unity APIs, frame loop, second simulation or UI state.
    public sealed class LocalGame
    {
        private readonly ILocalSaveStore store;
        private readonly Func<GatheringSession> factory;
        private readonly string catalog;
        private SaveDocument document;
        public GatheringSession Session { get; private set; }
        public OfflineReport Pending => document?.Pending == null ? null : SaveCodec.Copy(document.Pending);
        public string RecoveryDiagnostic => (store as AtomicLocalSaveStore)?.Diagnostic ?? (store as MigratingLocalSaveStore)?.Diagnostic;
        public long BoundaryUtcTicks => document?.UtcTicks ?? 0;
        public LocalGame(ILocalSaveStore store, Func<GatheringSession> factory, string catalog)
        { this.store = store ?? throw new ArgumentNullException(nameof(store)); this.factory = factory ?? throw new ArgumentNullException(nameof(factory)); this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog)); }
        internal void Validate(SaveDocument data)
        {
            if (data == null || data.UtcTicks <= 0 || data.UtcTicks > DateTime.MaxValue.Ticks) throw new ArgumentException("Invalid UTC boundary.");
            if (data.Catalog != catalog) throw new SaveCompatibilityException("Saved catalogs differ from installed catalogs. Migration/review required; files preserved.");
            var session = GatheringSession.Restore(data.State,factory());
            if (data.Pending == null) return;
            var report = data.Pending;
            if (string.IsNullOrWhiteSpace(report.Id) || report.AwayTicks < 0 || report.AwayTicks > DateTime.MaxValue.Ticks || report.EligibleTicks <= 0 || report.EligibleTicks > report.AwayTicks || report.Characters == null || report.Intervals < 1)
                throw new ArgumentException("Invalid pending offline report.");
            foreach (var c in report.Characters)
            {
                if (c == null || !session.Characters.Any(x => x.Id == c.Character) || c.Activity != null && !session.Activities.Any(a => a.Id == c.Activity) || c.Items == null || c.Xp == null)
                    throw new ArgumentException("Invalid report attribution reference.");
                if (c.StopReason != null && c.StopReason != "Defeated" && c.StopReason != "Job completed" && c.StopReason != "Out of materials") throw new ArgumentException("Unknown report stop reason.");
                if (c.Items.Any(i => i == null || i.Quantity <= 0 || i.Id == null || !session.BankItems.ContainsKey(i.Id)) || c.Items.Select(i => i.Id).Distinct().Count() != c.Items.Count ||
                    c.Xp.Any(x => x == null || x.Quantity <= 0 || !session.Skills.Any(s => s.Id == x.Skill)) || c.Xp.Select(x => x.Skill).Distinct().Count() != c.Xp.Count)
                    throw new ArgumentException("Invalid report rewards.");
            }
            if (report.Characters.Select(c => c.Character + "|" + c.Activity).Distinct().Count() != report.Characters.Count) throw new ArgumentException("Duplicate report attribution.");
        }
        public void Open(long utcTicks)
        {
            if (Session != null) throw new InvalidOperationException("This local game is already open.");
            CheckUtc(utcTicks);
            var loaded = store.Load(Validate);
            var candidate = loaded == null ? factory() : GatheringSession.Restore(loaded.State,factory());
            var next = loaded == null ? new SaveDocument { Catalog = catalog, UtcTicks = utcTicks, State = candidate.Capture() } : SaveCodec.Copy(loaded);
            Settle(candidate,next,utcTicks);
            Commit(candidate,next);
        }
        private static void CheckUtc(long value)
        { if (value <= 0 || value > DateTime.MaxValue.Ticks) throw new ArgumentOutOfRangeException(nameof(value)); }
        private static void Settle(GatheringSession candidate, SaveDocument next, long utcTicks)
        {
            long elapsed = Math.Max(0,utcTicks - next.UtcTicks);
            if (elapsed > 0)
            {
                candidate.SettleTo(checked(candidate.LastSettledTicks + elapsed),true);
                var receipt = candidate.LastOfflineReport;
                if (next.Pending != null) next.Pending.Merge(receipt);
                else if (elapsed >= 5 * TimeSpan.TicksPerMinute) next.Pending = SaveCodec.Copy(receipt);
            }
            // High-water UTC prevents clock rollback from granting the same wall interval twice.
            next.UtcTicks = Math.Max(next.UtcTicks,utcTicks); next.State = candidate.Capture();
        }
        private void Commit(GatheringSession candidate, SaveDocument next)
        {
            Validate(next); store.Write(next); document = next; Session = candidate;
        }
        public void Resume(long utcTicks)
        {
            CheckUtc(utcTicks);
            if (Session == null) throw new InvalidOperationException("Open first.");
            var candidate = GatheringSession.Restore(Session.Capture(),factory());
            var next = SaveCodec.Copy(document); Settle(candidate,next,utcTicks); Commit(candidate,next);
        }
        public void Save(long settledBoundaryUtcTicks)
        {
            CheckUtc(settledBoundaryUtcTicks);
            if (Session == null) throw new InvalidOperationException("Open first.");
            var next = SaveCodec.Copy(document); next.State = Session.Capture();
            next.UtcTicks = Math.Max(next.UtcTicks,settledBoundaryUtcTicks); Commit(Session,next);
        }
        public void Acknowledge(string reportId)
        {
            if (document?.Pending == null || document.Pending.Id != reportId) return;
            var state = Session.Capture();
            if (SaveCodec.Json(state) != SaveCodec.Json(document.State)) throw new InvalidOperationException("Save the current settled state and UTC boundary before acknowledging.");
            var next = SaveCodec.Copy(document); next.Pending = null; Commit(Session,next);
        }
    }
}
