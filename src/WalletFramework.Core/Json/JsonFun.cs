using System.Globalization;
using Newtonsoft.Json.Linq;
using WalletFramework.Core.Base64Url;
using WalletFramework.Core.Encoding.Errors;
using WalletFramework.Core.Functional;
using WalletFramework.Core.Json.Errors;

namespace WalletFramework.Core.Json;

public static class JsonFun
{
    public static Validation<JToken> GetByKey(this JToken token, string key)
    {
        try
        {
            var jObject = token.ToObject<JObject>()!;
            return jObject.GetByKey(key);
        }
        catch (Exception e)
        {
            return new JTokenIsNotAnJObjectError(key, e);
        }
    }
    
    public static Validation<JToken> GetByKey(this JObject jObject, string key)
    {
        var success = jObject.TryGetValue(key, out var value);
        if (success)
        {
            var str = value!.ToString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return new JsonFieldValueIsNullOrWhitespaceError(key);
            }
            else
            {
                return value;
            }
        }
        else
        {
            return new JsonFieldNotFoundError(key);
        }
    }

    public static Validation<JArray> ToJArray(this JToken token)
    {
        try
        {
            var array = token.ToObject<JArray>()!;
            return array;
        }
        catch (Exception e)
        {
            return new JTokenIsNotAnJArrayError(token.ToString(), e);
        }
    }

    public static Validation<JObject> ToJObject(this JToken token)
    {
        try
        {
            var jObject = token.ToObject<JObject>()!;
            return jObject;
        }
        catch (Exception e)
        {
            return new JTokenIsNotAnJObjectError(token.ToString(), e);
        }
    }
    
    public static Validation<JValue> ToJValue(this JToken token)
    {
        try
        {
            var jValue = token.ToObject<JValue>()!;
            return jValue;
        }
        catch (Exception e)
        {
            return new JTokenIsNotAJValueError(token.ToString(), e);
        }
    }
    
    public static Validation<Dictionary<T1, T2>> ToValidDictionaryAll<T1, T2>(
        this JObject jObject,
        Func<JToken, Validation<T1>> keyValidation,
        Func<JToken, Validation<T2>> valueValidation) where T1 : notnull
    {
        try
        {
            return jObject
                .Properties()
                .TraverseAll(property =>
                    from key in keyValidation(property.Name)
                    from value in valueValidation(property.Value)
                    select new KeyValuePair<T1, T2>(key, value))
                .OnSuccess(pairs => pairs.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value));
        }
        catch (Exception e)
        {
            return new JsonIsNotAMapError(jObject, e);
        }
    }
    
    public static Validation<Dictionary<T1, T2>> ToValidDictionaryAny<T1, T2>(
        this JObject jObject,
        Func<JToken, Validation<T1>> keyValidation,
        Func<JToken, Validation<T2>> valueValidation) where T1 : notnull => jObject
        .Properties()
        .TraverseAny(property =>
            from key in keyValidation(property.Name)
            from value in valueValidation(property.Value)
            select new KeyValuePair<T1, T2>(key, value))
        .OnSuccess(pairs => pairs.ToDictionary(
            pair => pair.Key,
            pair => pair.Value));

    public static Validation<int> ToInt(this JValue value)
    {
        try
        {
            return value.ToObject<int>();
        }
        catch (Exception e)
        {
            return new JValueIsNotAnIntError(value.ToString(CultureInfo.InvariantCulture), e);
        }
    }

    public static Validation<JObject> ParseAsJObject(string json)
    {
        try
        {
            var jObject = JObject.Parse(json);
            return jObject;
        }
        catch (Exception e)
        {
            return new InvalidJsonError(json, e);
        }
    }
    
    public static JToken RemoveNulls(this JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                var obj = new JObject();
                foreach (var property in ((JObject)token).Properties())
                {
                    if (property.Value.Type != JTokenType.Null)
                    {
                        obj.Add(property.Name, RemoveNulls(property.Value));
                    }
                }
                return obj;

            case JTokenType.Array:
                var array = new JArray();
                foreach (var item in (JArray)token)
                {
                    if (item.Type != JTokenType.Null)
                    {
                        array.Add(RemoveNulls(item));
                    }
                }
                return array;

            default:
                return token;
        }
    }

    public static Validation<JObject> DecodeToJObject(this Base64UrlString base64UrlString)
    {
        string json;
        try
        {
            var bytes = base64UrlString.AsByteArray;
            json = System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception e)
        {
            return new CouldNotParseToUtf8StringError(e);
        }

        JObject result;
        try
        {
            result = JObject.Parse(json);
        }
        catch (Exception e)
        {
            return new InvalidJsonError(json, e);
        }

        return result;
    }
}
