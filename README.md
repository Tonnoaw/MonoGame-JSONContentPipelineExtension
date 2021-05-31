# MonoGame - JSON Content Pipeline Extension

Content Pipeline Extension for MonoGame.
Using  [Newtonsoft JSON](https://github.com/JamesNK/Newtonsoft.Json) to serialize your game content to your content pipeline. <br/>


## How it works

Normally to import your custom game content to your content pipeline, The default available option is to use <b>XML</b> format.<br/>
In this content pipeline extension uses [Newtonsoft JSON](https://github.com/JamesNK/Newtonsoft.Json) to import custom game content in <b>JSON</b> format.

### Import custom game content in default XML format

Example a class which contains two basic members.

  * <b>string</b> name
  * <b>int</b> age

```CSharp
namespace Test
{
    public class Person
    {
        public string name;
        public int age;
    }
}
```

Parsed into <b>XML</b> document format.

```XML
<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:ns="Microsoft.Xna.Framework">
  <Asset Type="Test.Person">
    <name>George</name>
    <age>25</age>
  </Asset>
</XnaContent>
```

### Import custom game content in JSON Content Pipeline Extension

Again, using our previous class example.

```CSharp
namespace Test
{
    public class Person
    {
        public string name;
        public int age;
    }
}
```

<b>In our provided JSON Content pipeline processors both provide these parameters.</b>

<b>These 3 parameters are used for creating [JsonSerializerSettings](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_JsonSerializerSettings.htm). </b>

* TypeNameHandling
* DefaultValueHandling
* NullValueHandling

<b>Your object type name</b>

* TypeName

### Parsed into <b>JSON</b> document format.

<b>Assuming that</b>
* <b>TypeNameHandling</b> is set to  <b>All</b>

```JSONC
{
  //Object type name with assembly name
  "$type" : "Test.Person, Test",
  "name": "George",
  "age": 25
}
```

# JSON Content Processors

There are 2 JSON Content processor provided.
* <b>JsonToBsonProcessor</b>
* <b>JsonToXnaReflectiveObjectProcessor</b>

## JsonToXnaReflectiveObjectProcessor

 This processor will deserialize your JSON file to an actual object, Which will be handled by <b>Microsoft.Xna.Content.ReflectiveWriter</b> and <b>Microsoft.Xna.Content.ReflectiveReader</b>. When load an object via <i>ContentManager</i>, <b>Microsoft.Xna.Content.ReflectiveReader</b> will deserialize from their format and return custom game object.
 
 Which is besically the same content reader when import custom game object with <b>XML File format</b>

## JsonToBsonProcessor

This processor will convert your JSON file to <b>BSON</b> (JSON but in Binary format) format and store all processor parameters. When load content via <i>ContentManager</i>, will create a <b>JsonSerializer</b>
with specifc <b>JsonSerializerSettings</b> which defined in processor parameters. BSON format will be deserialized into an object by using [Newtonsoft JSON](https://github.com/JamesNK/Newtonsoft.Json).

Which means that you don't have to reference your game library .dll file in content pipeline.

