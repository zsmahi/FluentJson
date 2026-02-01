using System;

namespace FluentJson.Abstractions;

/// <summary>
/// Provides a thread-safe foundation for the "Freezable" object pattern.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Design Pattern:</strong> Freezable (Extension of Immutable Object).
/// </para>
/// <para>
/// This abstract class manages the lifecycle of configuration objects. It allows them to start as 
/// <strong>Mutable</strong> (during the "Builder" setup phase) and transition to <strong>Immutable</strong> 
/// (read-only) once the configuration is built.
/// </para>
/// <para>
/// <strong>Architectural Note:</strong>
/// This mechanism is critical for thread safety in singleton scenarios (like <c>JsonSerializerOptions</c>). 
/// By locking the state before the application starts processing requests, we eliminate race conditions 
/// without needing expensive locks (`lock`) on every property access during runtime.
/// </para>
/// </remarks>
public abstract class FreezableBase
{
    // "volatile" ensures that the assignment to true is immediately visible to all other threads,
    // preventing potential caching issues in multi-core processors.
    private volatile bool _isFrozen;

    /// <summary>
    /// Gets a value indicating whether the object has been locked and is currently immutable.
    /// </summary>
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// Locks the object state, permanently preventing any further modifications.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Inheritance Note:</strong>
    /// Subclasses containing other <see cref="FreezableBase"/> properties (e.g., a collection of child configurations) 
    /// <strong>must</strong> override this method to propagate the <c>Freeze()</c> call down the object graph.
    /// </para>
    /// </remarks>
    public virtual void Freeze() => _isFrozen = true;

    /// <summary>
    /// A guard clause that enforces the immutability contract.
    /// </summary>
    /// <remarks>
    /// Call this method at the beginning of any property setter or state-mutating operation.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the object has already been frozen (i.e., <see cref="Build"/> has been called on the builder).
    /// </exception>
    protected void ThrowIfFrozen()
    {
        if (_isFrozen)
        {
            throw new InvalidOperationException("Configuration is frozen and cannot be modified after Build() has been called.");
        }
    }
}
