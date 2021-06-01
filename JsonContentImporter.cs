using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using MonoGame.Extended;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JsonContentPipeline
{
    /// <summary>
    /// Defines an importer for JSON files, Serialize by <see cref="Newtonsoft.Json"/> API.<para/>
    /// This will serialize object in <see cref="Newtonsoft.Json.TypeNameHandling.All"/> settings, Please provides the <b>type name</b> for every none primitive type. <para/>
    /// To provides type name prefix:
    /// <code>
    /// {
    ///     "$type": "(YourTypeName), (AssemblyName)", ....
    /// }
    /// </code>
    /// for collections prefix:
    /// <code>
    /// {
    ///     "$type": "(YourTypeName)[], (AssemblyName)",
    ///     "$values": [....]
    /// }
    /// </code>
    /// </summary>
    [ContentImporter(".json", DisplayName = "JsonImporter - Crimson", DefaultProcessor = nameof(JsonToBsonContentProcessor))]
    public class JsonContentImporter : ContentImporter<byte[]>
    {
        public override byte[] Import(string filename, ContentImporterContext context)
        {
            var jsonTextReader = new JsonTextReader(new StreamReader(filename));
            var memStream = new MemoryStream();
            using (var bsonWriter = new BsonDataWriter(memStream))
            {
                bsonWriter.WriteToken(jsonTextReader);
            }
            return memStream.ToArray();
        }
    }

    /// <summary>
    /// Defines an processor which will convert JSON file to BSON file. <para/>
    /// Object will be loaded using <see cref="Newtonsoft.Json"/> serialization.
    /// </summary>
    [ContentProcessor(DisplayName = "JsonToBsonProcessor - Crimson")]
    public class JsonToBsonContentProcessor : ContentProcessor<byte[], BsonContent>
    {
        /// <summary>
        /// <i>If type name isn't specified in <see cref="TypeNameHandling"/> you should provide type name in <see cref="TypeName"/></i>
        /// </summary>
        public virtual TypeNameHandling TypeNameHandling { get; set; } = TypeNameHandling.All;

        public virtual NullValueHandling NullValueHandling { get; set; } = NullValueHandling.Include;
        public virtual DefaultValueHandling DefaultValueHandling { get; set; } = DefaultValueHandling.Include;

        /// <summary>
        /// <i>If type object is defined already, you can ignore this.</i>
        /// </summary>
        public virtual string TypeName { get; set; } = "System.Object, System.Private.CoreLib";

        public override BsonContent Process(byte[] input, ContentProcessorContext context)
        {
            return new BsonContent { data = input, defaultValueHandling = DefaultValueHandling, nullValueHandling = NullValueHandling, typeNameHandling = TypeNameHandling, contentType = TypeName, dataLength = input.Length };
        }
    }

    /// <summary>
    /// Defines an processor which will convert JSON file directly to an object.<para/>
    /// Object will be loaded using <see cref="ReflectiveReader{T}"/> and <see cref="ReflectiveWriter{T}"/>.
    /// </summary>
    [ContentProcessor(DisplayName = "JsonToXnaReflectiveObjectProcessor - Crimson")]
    public class JsonContentProcessor : ContentProcessor<byte[], System.Object>
    {
        /// <summary>
        /// Pre-defined custom converter for easier to write JSON.
        /// <list type="bullet">Color = "(R), (G), (B), (A)"</list>
        /// <list type="bullet">Rectangle = "(X), (Y), (Width), (Height)"</list>
        /// <list type="bullet">RectangleF = "(X), (Y), (Width), (Height)"</list>
        /// <list type="bullet">Size2 = "(Width), (Height)"</list>
        /// <list type="bullet">Point = "(X), (Y)"</list>
        /// <list type="bullet">Vector2 = "(X), (Y)"</list>
        /// </summary>
        public static List<JsonConverter> Converters => new List<JsonConverter>()
        {
            new ColorConverter(),
            new RectangleFConverter(),
            new RectangleConverter(),
            new Size2Converter(),
            new PointConverter(),
            new Vector2Converter()
        };

        /// <summary>
        /// <i>If type name isn't specified in <see cref="TypeNameHandling"/> you should provide type name in <see cref="TypeName"/></i>
        /// </summary>
        public virtual TypeNameHandling TypeNameHandling { get; set; } = TypeNameHandling.All;

        public virtual NullValueHandling NullValueHandling { get; set; } = NullValueHandling.Include;
        public virtual DefaultValueHandling DefaultValueHandling { get; set; } = DefaultValueHandling.Include;

        /// <summary>
        /// <i>If type object is defined already, you can ignore this.</i>
        /// </summary>
        public virtual string TypeName { get; set; } = "System.Object, System.Private.CoreLib";

        /// <summary>
        /// Look for all types in Assembly if you found an error.
        /// </summary>
        public virtual string Debug_AssemblyLookup { get; set; } = null;

        public override object Process(byte[] input, ContentProcessorContext context)
        {
            try
            {
                var memStream = new MemoryStream(input);
                var serializer = JsonSerializer.Create(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling, NullValueHandling = NullValueHandling, DefaultValueHandling = DefaultValueHandling, Converters = Converters });
                using (var binaryDataReader = new BsonDataReader(memStream))
                {
                    return serializer.Deserialize(binaryDataReader, Type.GetType(TypeName));
                }
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                if (string.IsNullOrEmpty(Debug_AssemblyLookup))
                {
                    var message = $"There's an error occurred. Maybe there are some types or assemblies missing.(You can try debug by using AssemblyLookup Parameter and look for all types contains)";

                    Assembly assembly = null;
                    try
                    {
                        assembly = Assembly.Load(Debug_AssemblyLookup);
                    }
                    catch (Exception)
                    {
                        message += $"\nCan't find assembly of name: {Debug_AssemblyLookup}";
                    }
                    if (assembly != null)
                    {
                        message += "\n these are types found:\n";
                        var types = assembly.GetTypes();
                        for (int i = 0; i < types.Length; i++)
                        {
                            message += $"   {types[i].Name}\n";
                        }
                    }
                    message += $"\n -----------Inner Exception Message-----------\n {ex.Message}";

                    throw new JsonProcessorException(ex, message);
                }
                throw ex;
            }
        }

        private class JsonProcessorException : Exception
        {
            public JsonProcessorException(Exception innerException, string message) : base(message, innerException) { }
        }
    }

    [ContentTypeWriter]
    public class BsonContentTypeWriter : ContentTypeWriter<BsonContent>
    {
        private string runtimeType;

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return $"JsonContentPipeline.BsonContentTypeReader`1[[{GetRuntimeType(targetPlatform)}]], JsonContentPipeline";
        }

        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return runtimeType;
        }

        protected override void Write(ContentWriter output, BsonContent value)
        {
            runtimeType = value.contentType;

            output.Write(value.contentType);
            output.Write((int)value.typeNameHandling);
            output.Write((int)value.nullValueHandling);
            output.Write((int)value.defaultValueHandling);
            output.Write(value.dataLength);
            output.Write(value.data);
        }
    }

    public class BsonContentTypeReader<T> : ContentTypeReader<T>
    {
        protected override T Read(ContentReader input, T existingInstance)
        {
            var typeString = input.ReadString();
            var typeNameHandling = (TypeNameHandling)input.ReadInt32();
            var nullValueHandling = (NullValueHandling)input.ReadInt32();
            var defaultValueHandling = (DefaultValueHandling)input.ReadInt32();
            var dataLength = input.ReadInt32();
            var data = input.ReadBytes(dataLength);

            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = typeNameHandling,
                NullValueHandling = nullValueHandling,
                DefaultValueHandling = defaultValueHandling,
                Converters = JsonContentProcessor.Converters
            };

            var memStream = new MemoryStream(data);
            var serializer = JsonSerializer.Create(serializerSettings);
            var bsonReader = new BsonDataReader(memStream);
            return (T)serializer.Deserialize(bsonReader, Type.GetType(typeString));
        }
    }


    class PointConverter : JsonConverter<Point>
    {
        public override Point ReadJson(JsonReader reader, Type objectType, Point existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;
            var values = text.Split(',');
            return new Point(int.Parse(values[0]), int.Parse(values[1]));
        }
        public override void WriteJson(JsonWriter writer, Point value, JsonSerializer serializer)
        {
            var sb = new StringBuilder();
            sb.Append(value.X);
            sb.Append(", ");
            sb.Append(value.Y);
            writer.WriteValue(sb.ToString());
        }
    }

    class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;
            var values = text.Split(',');
            return new Vector2(float.Parse(values[0]), float.Parse(values[1]));
        }

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            var sb = new StringBuilder();
            sb.Append(value.X);
            sb.Append(", ");
            sb.Append(value.Y);
            writer.WriteValue(sb.ToString());
        }
    }

    class Size2Converter : JsonConverter<Size2>
    {
        public override Size2 ReadJson(JsonReader reader, Type objectType, Size2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;
            var values = text.Split(',');
            return new Size2(float.Parse(values[0]), float.Parse(values[1]));
        }

        public override void WriteJson(JsonWriter writer, Size2 value, JsonSerializer serializer)
        {
            var sb = new StringBuilder();
            sb.Append(value.Width);
            sb.Append(", ");
            sb.Append(value.Height);
            writer.WriteValue(sb.ToString());
        }
    }
    class ColorConverter : JsonConverter<Color>
    {
        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;
            var values = text.Split(',');
            return new Color(byte.Parse(values[0]), byte.Parse(values[1]), byte.Parse(values[2]), byte.Parse(values[3]));
        }

        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            var sb = new StringBuilder();
            sb.Append(value.R);
            sb.Append(", ");
            sb.Append(value.G);
            sb.Append(", ");
            sb.Append(value.B);
            sb.Append(", ");
            sb.Append(value.A);
            writer.WriteValue(sb.ToString());
        }
    }

    class RectangleConverter : JsonConverter<Rectangle>
    {
        public override Rectangle ReadJson(JsonReader reader, Type objectType, Rectangle existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;
            try
            {
                var values = text.Split(',');
                return new Rectangle(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]), int.Parse(values[3]));
            }
            catch (Exception ex)
            {
                throw new ConverterException(text, ex);
            }
        }

        public override void WriteJson(JsonWriter writer, Rectangle value, JsonSerializer serializer)
        {
            var sb = new StringBuilder();
            sb.Append(value.X);
            sb.Append(", ");
            sb.Append(value.Y);
            sb.Append(", ");
            sb.Append(value.Width);
            sb.Append(", ");
            sb.Append(value.Height);
            writer.WriteValue(sb.ToString());
        }

        private class ConverterException : Exception
        {
            public ConverterException(string readValue, Exception exception) : base(exception.Message + $" ValueError: {readValue}", exception)
            {
            }
        }
    }

    class RectangleFConverter : JsonConverter<RectangleF>
    {
        public override RectangleF ReadJson(JsonReader reader, Type objectType, RectangleF existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var text = (string)reader.Value;
            var values = text.Split(',');
            return new RectangleF(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]));
        }

        public override void WriteJson(JsonWriter writer, RectangleF value, JsonSerializer serializer)
        {
            var sb = new StringBuilder();
            sb.Append(value.X);
            sb.Append(", ");
            sb.Append(value.Y);
            sb.Append(", ");
            sb.Append(value.Width);
            sb.Append(", ");
            sb.Append(value.Height);
            writer.WriteValue(sb.ToString());
        }
    }
}