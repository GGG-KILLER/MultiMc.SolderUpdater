using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiMc.SolderUpdater
{
    public class VersionValueConverter : JsonConverter<Version>
    {
        public override Version Read ( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options ) =>
            Version.Parse ( reader.GetString ( ) );

        public override void Write ( Utf8JsonWriter writer, Version value, JsonSerializerOptions options )
        {
            Span<Char> buff = stackalloc Char[2 * 3 + 2];
            if ( !value.TryFormat ( buff, 3, out var written ) )
                writer.WriteStringValue ( buff.Slice ( 0, written ) );
            else
                writer.WriteStringValue ( value.ToString ( ) );
        }
    }
}
