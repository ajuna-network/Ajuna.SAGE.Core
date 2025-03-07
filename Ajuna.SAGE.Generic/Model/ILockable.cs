namespace Ajuna.SAGE.Core.Model
{
    /// <summary>
    /// Identify if an object is lockable
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// Is lockable
        /// </summary>
        bool IsLockable { get; set; }
    }
}