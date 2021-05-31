# MonoGame - JSON Content Pipeline Extension

Content Pipeline Extension for MonoGame.
Using [Newtonsoft JSON](https://github.com/JamesNK/Newtonsoft.Json) to serialize your game content to your content pipeline. <br/>


## How it works

Normally to import your custom game content to your content pipeline, The default available option is to use <b>XML</b> format.

```CSharp
public class Person
{
  public string name;
  public string age;
}
```
