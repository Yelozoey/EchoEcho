using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.AI;

namespace EchoStation.SalvageExpansion
{
    public sealed class ExpansionPause : MonoBehaviour
    {
        sealed class Lease : IDisposable
        {
            ExpansionPause owner;
            public readonly bool Pauses;
            public Lease(ExpansionPause owner, bool pauses) { this.owner = owner; Pauses = pauses; }
            public void Dispose() { var previous = owner; owner = null; if (previous) previous.Release(this); }
        }
        readonly HashSet<Lease> leases = new HashSet<Lease>();
        readonly Dictionary<Behaviour, bool> actorFlags = new Dictionary<Behaviour, bool>();
        readonly Dictionary<NavMeshAgent, bool> stopped = new Dictionary<NavMeshAgent, bool>();
        PlayerInputReader input;
        Behaviour[] actors = Array.Empty<Behaviour>();
        NavMeshAgent[] agents = Array.Empty<NavMeshAgent>();
        float originalScale;
        bool originalBlocked, originalCaptured;
        public bool Paused { get { foreach(var lease in leases)if(lease.Pauses)return true;return false; } }
        public bool Blocked => leases.Count > 0 || originalBlocked;
        public void Configure(PlayerInputReader reader, Behaviour[] movingActors = null, NavMeshAgent[] navigation = null)
        {
            if (leases.Count > 0) throw new InvalidOperationException("Cannot rebind active pause leases.");
            input = reader ? reader : throw new ArgumentNullException(nameof(reader));
            actors = movingActors ?? Array.Empty<Behaviour>(); agents = navigation ?? Array.Empty<NavMeshAgent>();
            originalBlocked = input.UiBlocked;
        }
        public IDisposable Acquire(bool pauseTime = true)
        {
            if (!input) throw new InvalidOperationException("Pause input binding missing.");
            bool wasPaused = Paused;
            if (leases.Count == 0) { originalBlocked = input.UiBlocked; originalCaptured = input.PointerCaptured; }
            var lease = new Lease(this, pauseTime); leases.Add(lease);
            if (!wasPaused && Paused)
            {
                originalScale = Time.timeScale;
                foreach (var actor in actors) if (actor && actor != this) { actorFlags[actor] = actor.enabled; actor.enabled = false; }
                foreach (var agent in agents) if (agent && agent.enabled && agent.isOnNavMesh) { stopped[agent] = agent.isStopped; agent.isStopped = true; }
                Time.timeScale = 0;
            }
            input.SetUiBlocked(true); return lease;
        }
        void Release(Lease lease)
        {
            bool wasPaused = Paused;
            if (!leases.Remove(lease)) return;
            if (wasPaused && !Paused)
            {
                if (Time.timeScale == 0) Time.timeScale = originalScale;
                foreach (var entry in stopped) if (entry.Key && entry.Key.enabled && entry.Key.isOnNavMesh) entry.Key.isStopped = entry.Value;
                foreach (var entry in actorFlags) if (entry.Key) entry.Key.enabled = entry.Value;
                stopped.Clear(); actorFlags.Clear();
            }
            if (input) { input.SetUiBlocked(Blocked); RestorePointer(); }
        }
        public void RestorePointer() { if (input && !Blocked && input.HasApplicationFocus) input.SetPointerCaptured(originalCaptured); }
        void OnDisable() { foreach (var lease in leases.ToArray()) lease.Dispose(); }
    }
}
