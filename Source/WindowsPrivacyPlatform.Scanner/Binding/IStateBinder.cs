// Source/WindowsPrivacyPlatform.Scanner/Binding/IStateBinder.cs
using System.Collections.Generic;
using WindowsPrivacyPlatform.Models;

namespace WindowsPrivacyPlatform.Scanner.Binding
{
    /// <summary>
    /// Read-only domain binder. Maps inventory values onto ManagedObject observation fields.
    /// Never writes to the system. Never elevates.
    /// </summary>
    public interface IStateBinder
    {
        string Name { get; }

        /// <summary>True when this binder owns the given catalog entry.</summary>
        bool CanBind(ManagedObject managedObject);

        /// <summary>
        /// Bind live inventory values onto the managed object.
        /// Must set CurrentState for CLI compatibility and populate Observation where possible.
        /// </summary>
        void Bind(InventorySnapshot snapshot, ManagedObject managedObject);
    }
}
