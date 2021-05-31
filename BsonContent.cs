using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonContentPipeline
{
    public class BsonContent
    {
        internal string contentType;
        internal TypeNameHandling typeNameHandling;
        internal NullValueHandling nullValueHandling;
        internal DefaultValueHandling defaultValueHandling;
        internal int dataLength;
        internal byte[] data;
    }
}
