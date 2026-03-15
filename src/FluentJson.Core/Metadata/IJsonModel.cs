using System;
using System.Collections.Generic;

namespace FluentJson.Core.Metadata;

/// <summary>
/// Represents the compiled, read-only metadata model containing all JSON serialization rules.
/// </summary>
/// <remarks>
/// This model acts as the single source of truth for adapters (like Newtonsoft or System.Text.Json) to understand how to serialize your Domain Models.
/// </remarks>
public interface IJsonModel
{
    /// <summary>
    /// Gets the read-only collection of entities tracked by this model.
    /// </summary>
    IReadOnlyList<IJsonEntity> Entities { get; }
}
