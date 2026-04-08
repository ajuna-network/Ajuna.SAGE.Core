namespace Ajuna.SAGE.Core
{
    /// <summary>
    /// Implemented by every component that holds mutable runtime state
    /// (managers + BlockchainInfoProvider). <see cref="Snapshot"/> returns
    /// an opaque byte payload that <see cref="Restore"/> can later replay
    /// to reconstruct the component's exact state.
    ///
    /// Used by <c>Engine.Snapshot</c> / <c>Engine.Restore</c> to persist
    /// and reload the entire engine state without consumers needing to
    /// reach into manager internals.
    /// </summary>
    public interface ISnapshotable
    {
        /// <summary>
        /// Serialize the component's full mutable state into a byte
        /// payload. The payload is opaque to callers — the same component
        /// type that produced it must be the one to <see cref="Restore"/>
        /// it.
        /// </summary>
        byte[] Snapshot();

        /// <summary>
        /// Replace the component's current state with the contents of a
        /// snapshot previously produced by <see cref="Snapshot"/>. The
        /// component is reset before applying the snapshot, so any
        /// pre-existing in-memory state is discarded.
        /// </summary>
        /// <exception cref="System.IO.InvalidDataException">
        /// If the payload is malformed, truncated, or was produced by an
        /// incompatible component / format version.
        /// </exception>
        void Restore(byte[] snapshot);
    }
}
