﻿namespace SimpleAuth.AuthServer
{
    using System;
    using System.IO;
    using System.Security.Claims;
    using System.Text;
    using Marten;
    using Newtonsoft.Json;
    using SimpleAuth.Stores.Marten;

    internal class SimpleAuthMartenOptions : StoreOptions
    {
        public SimpleAuthMartenOptions(string connectionString, string searchPath)
        {
            Serializer<MartenJsonSerializer>();
            Connection(connectionString);
            Schema.Include<SimpleAuthRegistry>();
            DatabaseSchemaName = searchPath;
        }

        private class ClaimConverter : JsonConverter<Claim>
        {
            public override void WriteJson(JsonWriter writer, Claim value, JsonSerializer serializer)
            {
                var info = new ClaimInfo(value.Type, value.Value);
                serializer.Serialize(writer, info);
            }

            public override Claim ReadJson(
                JsonReader reader,
                Type objectType,
                Claim existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                var info = serializer.Deserialize<ClaimInfo>(reader);
                return new Claim(info.Type, info.Value);
            }
        }

        private readonly struct ClaimInfo
        {
            public ClaimInfo(string type, string value)
            {
                Type = type;
                Value = value;
            }

            public string Type { get; }

            public string Value { get; }
        }

        private class MartenJsonSerializer : ISerializer
        {
            private readonly JsonSerializer _innerSerializer;

            public MartenJsonSerializer()
            {
                _innerSerializer = new JsonSerializer
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    StringEscapeHandling = StringEscapeHandling.EscapeHtml
                };
                _innerSerializer.Converters.Add(new ClaimConverter());
            }

            public void ToJson(object document, TextWriter writer)
            {
                _innerSerializer.Serialize(writer, document);
            }

            public string ToJson(object document)
            {
                var sb = new StringBuilder();
                using (var writer = new StringWriter(sb))
                {
                    _innerSerializer.Serialize(writer, document, document.GetType());
                }

                return sb.ToString();
            }

            public T FromJson<T>(TextReader reader)
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return _innerSerializer.Deserialize<T>(jsonReader);
                }
            }

            public object FromJson(Type type, TextReader reader)
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return _innerSerializer.Deserialize(jsonReader, type);
                }
            }

            public string ToCleanJson(object document)
            {
                return ToJson(document);
            }

            public EnumStorage EnumStorage { get; } = EnumStorage.AsString;
            public Casing Casing { get; } = Casing.CamelCase;
        }
    }
}