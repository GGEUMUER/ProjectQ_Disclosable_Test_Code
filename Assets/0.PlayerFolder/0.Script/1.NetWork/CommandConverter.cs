using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CommandConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Command);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);

        string type = jo["type"]?.ToString();

        Command command = type switch
        {
            "Move" => new UnitMoveCommand(),
            "Attack" => new UnitAttackCommand(),
            _ => new Command()  
        };


        serializer.Populate(jo.CreateReader(), command);

        return command;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}