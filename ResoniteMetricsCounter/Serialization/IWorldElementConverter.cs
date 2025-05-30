using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elements.Core;
using FrooxEngine;
using ResoniteMetricsCounter.Utils;

namespace ResoniteMetricsCounter.Serialization;

internal sealed class IWorldElementConverter : JsonConverter<IWorldElement>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IWorldElement).IsAssignableFrom(typeToConvert);
    }

    public override IWorldElement? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }

    private static void WriteElement(Utf8JsonWriter writer, IWorldElement value)
    {
        writer.WriteString("ID", value.ReferenceID.ToString());
        writer.WriteString("Name", value.GetNameFast());
        writer.WriteString("Type", value.GetType().GetNiceFullName());
    }

    private static void WriteSlot(string propertyName, Utf8JsonWriter writer, Slot? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteStartObject(propertyName);

        WriteElement(writer, value);
        writer.WriteString("Tag", value.Tag);

        writer.WriteEndObject();
    }

    public override void Write(
        Utf8JsonWriter writer,
        IWorldElement value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        WriteElement(writer, value);

        var slot = value as Slot ?? value.Parent as Slot;
        if (value is not Slot)
        {
            WriteSlot("Slot", writer, slot);
        }

        var parent = slot?.Parent;
        WriteSlot("Parent", writer, parent);

        var objectRoot = slot?.GetObjectRoot(onlyExplicit: true);

        WriteSlot("ObjectRoot", writer, objectRoot);

        writer.WriteEndObject();
    }
}
