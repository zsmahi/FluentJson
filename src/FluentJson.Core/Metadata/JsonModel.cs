using System;
using System.Collections.Generic;

namespace FluentJson.Core.Metadata;

/// <summary>
/// Internal immutable implementation of <see cref="IJsonModel"/>.
/// </summary>
internal class JsonModel : IJsonModel
{
    public IReadOnlyList<IJsonEntity> Entities { get; }

    public JsonModel(IReadOnlyList<IJsonEntity> entities)
    {
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
    }
}
