using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.IO;

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
        /// <i>If type name isn't specified in <see cref="TypeNameHandling"/> you should provide type name in <see cref="TypeName"/></i>
        /// </summary>
        public virtual TypeNameHandling TypeNameHandling { get; set; } = TypeNameHandling.All;
        public virtual NullValueHandling NullValueHandling { get; set; } = NullValueHandling.Include;
        public virtual DefaultValueHandling DefaultValueHandling { get; set; } = DefaultValueHandling.Include;
        /// <summary>
        /// <i>If type object is defined already, you can ignore this.</i>
        /// </summary>
        public virtual string TypeName { get; set; } = "System.Object, System.Private.CoreLib";
        public override object Process(byte[] input, ContentProcessorContext context)
        {
            var memStream = new MemoryStream(input);
            var serializer = JsonSerializer.Create(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling, NullValueHandling = NullValueHandling, DefaultValueHandling = DefaultValueHandling });
            using (var binaryDataReader = new BsonDataReader(memStream))
            {
                return serializer.Deserialize(binaryDataReader, Type.GetType(TypeName));
            }
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
                DefaultValueHandling = defaultValueHandling
            };

            var memStream = new MemoryStream(data);
            var serializer = JsonSerializer.Create(serializerSettings);
            var bsonReader = new BsonDataReader(memStream);
            return (T)serializer.Deserialize(bsonReader, Type.GetType(typeString));
        }
    }
}
