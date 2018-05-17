# autoadapter

.Net library for dynamically creating adapter types

## Installation

[AutoAdapyter is available on Nuget](https://www.nuget.org/packages/AutoAdapter)

## Defining an Adapter

An adpater type is defined by creating an interface containing the methods, properties, and events that
are required from the type being adapted. The adapter is then created by calling the CreateAdapter() method
on the object being adapted. This is provided by an extension method on the *object* type.

## Examples

````c#
public class AdaptedType
{
    public string Value { get; set; }

    public bool Method(string paremeter1, bool paraemeter2, int parameter3)
    {
        ...
    }
}

public interface AdapterType
{
    string Value { get; set; }
}

var adaptedType = new AdaptedType() { Value = "Hello World" };
var adpater = adaptedType.CreateAdapter<AdapterType>();

Console.WriteLine(adapter.Value);
````