using Newtonsoft.Json;

namespace Apps.XTM.Utils.Converters;

public class StringToIntConverter : JsonConverter
{
    private string PropertyName { get; }

    public StringToIntConverter(string propertyName)
    {
        PropertyName = propertyName;
    }
    
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is IEnumerable<string> stringsInput)
        {
            var inputArray = stringsInput.ToArray();
            var inputsLength = inputArray.Length;
            
            var numberArray = new long[inputsLength];
            for (var i = 0; i < inputsLength; i++)
            {
                if (!long.TryParse(inputArray[i], out numberArray[i]))
                    throw new JsonSerializationException($"Invalid value for property '{PropertyName}' at index {i}: {inputArray[i]}");
            }

            serializer.Serialize(writer, numberArray);
            return;
        }
        
        if (!long.TryParse((string)value, out var number))
            throw new($"{PropertyName} must be a number");

        writer.WriteValue(number);
    }
}