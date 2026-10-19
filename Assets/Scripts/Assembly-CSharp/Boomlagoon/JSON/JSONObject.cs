using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Boomlagoon.JSON
{
	public class JSONObject : IEnumerable<KeyValuePair<string, JSONValue>>, IEnumerable
	{
		private enum JSONParsingState
		{
			Object = 0,
			Array = 1,
			EndObject = 2,
			EndArray = 3,
			Key = 4,
			Value = 5,
			KeyValueSeparator = 6,
			ValueSeparator = 7,
			String = 8,
			Number = 9,
			Boolean = 10,
			Null = 11
		}

		private readonly IDictionary<string, JSONValue> values = new Dictionary<string, JSONValue>();

		private static readonly Regex unicodeRegex = new Regex("\\\\u([0-9a-fA-F]{4})");

		private static readonly byte[] unicodeBytes = new byte[2];

		public JSONValue this[string key]
		{
			get
			{
				return GetValue(key);
			}
			set
			{
				values[key] = value;
			}
		}

		public JSONObject()
		{
		}

		public JSONObject(JSONObject other)
		{
			values = new Dictionary<string, JSONValue>();
			if (other == null)
			{
				return;
			}
			foreach (KeyValuePair<string, JSONValue> value in other.values)
			{
				values[value.Key] = new JSONValue(value.Value);
			}
		}

		public bool ContainsKey(string key)
		{
			return values.ContainsKey(key);
		}

		public JSONValue GetValue(string key)
		{
			JSONValue value;
			values.TryGetValue(key, out value);
			return value;
		}

		public string GetString(string key)
		{
			JSONValue value = GetValue(key);
			if (value == null)
			{
				JSONLogger.Error(key + "(string) == null");
				return string.Empty;
			}
			return value.Str;
		}

		public double GetNumber(string key)
		{
			JSONValue value = GetValue(key);
			if (value == null)
			{
				JSONLogger.Error(key + " == null");
				return double.NaN;
			}
			return value.Number;
		}

		public JSONObject GetObject(string key)
		{
			JSONValue value = GetValue(key);
			if (value == null)
			{
				JSONLogger.Error(key + " == null");
				return null;
			}
			return value.Obj;
		}

		public bool GetBoolean(string key)
		{
			JSONValue value = GetValue(key);
			if (value == null)
			{
				JSONLogger.Error(key + " == null");
				return false;
			}
			return value.Boolean;
		}

		public JSONArray GetArray(string key)
		{
			JSONValue value = GetValue(key);
			if (value == null)
			{
				JSONLogger.Error(key + " == null");
				return null;
			}
			return value.Array;
		}

		public void Add(string key, JSONValue value)
		{
			values[key] = value;
		}

		public void Add(KeyValuePair<string, JSONValue> pair)
		{
			values[pair.Key] = pair.Value;
		}

		public static JSONObject Parse(string jsonString)
		{
			if (string.IsNullOrEmpty(jsonString))
			{
				return null;
			}
			JSONValue jSONValue = null;
			List<string> list = new List<string>();
			JSONParsingState jSONParsingState = JSONParsingState.Object;
			int num;
			for (num = 0; num < jsonString.Length; num++)
			{
				num = SkipWhitespace(jsonString, num);
				switch (jSONParsingState)
				{
				case JSONParsingState.Object:
				{
					if (jsonString[num] != '{')
					{
						return Fail('{', num);
					}
					JSONValue jSONValue2 = new JSONObject();
					if (jSONValue != null)
					{
						jSONValue2.Parent = jSONValue;
					}
					jSONValue = jSONValue2;
					jSONParsingState = JSONParsingState.Key;
					break;
				}
				case JSONParsingState.EndObject:
					if (jsonString[num] != '}')
					{
						return Fail('}', num);
					}
					if (jSONValue.Parent == null)
					{
						return jSONValue.Obj;
					}
					switch (jSONValue.Parent.Type)
					{
					case JSONValueType.Object:
						jSONValue.Parent.Obj.values[list.Pop()] = new JSONValue(jSONValue.Obj);
						break;
					case JSONValueType.Array:
						jSONValue.Parent.Array.Add(new JSONValue(jSONValue.Obj));
						break;
					default:
						return Fail("valid object", num);
					}
					jSONValue = jSONValue.Parent;
					jSONParsingState = JSONParsingState.ValueSeparator;
					break;
				case JSONParsingState.Key:
				{
					if (jsonString[num] == '}')
					{
						num--;
						jSONParsingState = JSONParsingState.EndObject;
						break;
					}
					string text = ParseString(jsonString, ref num);
					if (text == null)
					{
						return Fail("key string", num);
					}
					list.Add(text);
					jSONParsingState = JSONParsingState.KeyValueSeparator;
					break;
				}
				case JSONParsingState.KeyValueSeparator:
					if (jsonString[num] != ':')
					{
						return Fail(':', num);
					}
					jSONParsingState = JSONParsingState.Value;
					break;
				case JSONParsingState.ValueSeparator:
					switch (jsonString[num])
					{
					case ',':
						jSONParsingState = ((jSONValue.Type != JSONValueType.Object) ? JSONParsingState.Value : JSONParsingState.Key);
						break;
					case '}':
						jSONParsingState = JSONParsingState.EndObject;
						num--;
						break;
					case ']':
						jSONParsingState = JSONParsingState.EndArray;
						num--;
						break;
					default:
						return Fail(", } ]", num);
					}
					break;
				case JSONParsingState.Value:
				{
					char c = jsonString[num];
					if (c == '"')
					{
						jSONParsingState = JSONParsingState.String;
					}
					else
					{
						if (!char.IsDigit(c))
						{
							switch (c)
							{
							case '-':
								break;
							case '{':
								goto IL_028d;
							case '[':
								goto IL_0294;
							case ']':
								goto IL_029b;
							case 'f':
							case 't':
								goto IL_02bf;
							case 'n':
								goto IL_02c7;
							default:
								return Fail("beginning of value", num);
							}
						}
						jSONParsingState = JSONParsingState.Number;
					}
					goto IL_02db;
				}
				case JSONParsingState.String:
				{
					string text2 = ParseString(jsonString, ref num);
					if (text2 == null)
					{
						return Fail("string value", num);
					}
					switch (jSONValue.Type)
					{
					case JSONValueType.Object:
						jSONValue.Obj.values[list.Pop()] = new JSONValue(text2);
						break;
					case JSONValueType.Array:
						jSONValue.Array.Add(text2);
						break;
					default:
						JSONLogger.Error("Fatal error, current JSON value not valid");
						return null;
					}
					jSONParsingState = JSONParsingState.ValueSeparator;
					break;
				}
				case JSONParsingState.Number:
				{
					double num2 = ParseNumber(jsonString, ref num);
					if (double.IsNaN(num2))
					{
						return Fail("valid number", num);
					}
					switch (jSONValue.Type)
					{
					case JSONValueType.Object:
						jSONValue.Obj.values[list.Pop()] = new JSONValue(num2);
						break;
					case JSONValueType.Array:
						jSONValue.Array.Add(num2);
						break;
					default:
						JSONLogger.Error("Fatal error, current JSON value not valid");
						return null;
					}
					jSONParsingState = JSONParsingState.ValueSeparator;
					break;
				}
				case JSONParsingState.Boolean:
					if (jsonString[num] == 't')
					{
						if (jsonString.Length < num + 4 || jsonString[num + 1] != 'r' || jsonString[num + 2] != 'u' || jsonString[num + 3] != 'e')
						{
							return Fail("true", num);
						}
						switch (jSONValue.Type)
						{
						case JSONValueType.Object:
							jSONValue.Obj.values[list.Pop()] = new JSONValue(true);
							break;
						case JSONValueType.Array:
							jSONValue.Array.Add(new JSONValue(true));
							break;
						default:
							JSONLogger.Error("Fatal error, current JSON value not valid");
							return null;
						}
						num += 3;
					}
					else
					{
						if (jsonString.Length < num + 5 || jsonString[num + 1] != 'a' || jsonString[num + 2] != 'l' || jsonString[num + 3] != 's' || jsonString[num + 4] != 'e')
						{
							return Fail("false", num);
						}
						switch (jSONValue.Type)
						{
						case JSONValueType.Object:
							jSONValue.Obj.values[list.Pop()] = new JSONValue(false);
							break;
						case JSONValueType.Array:
							jSONValue.Array.Add(new JSONValue(false));
							break;
						default:
							JSONLogger.Error("Fatal error, current JSON value not valid");
							return null;
						}
						num += 4;
					}
					jSONParsingState = JSONParsingState.ValueSeparator;
					break;
				case JSONParsingState.Array:
				{
					if (jsonString[num] != '[')
					{
						return Fail('[', num);
					}
					JSONValue jSONValue3 = new JSONArray();
					if (jSONValue != null)
					{
						jSONValue3.Parent = jSONValue;
					}
					jSONValue = jSONValue3;
					jSONParsingState = JSONParsingState.Value;
					break;
				}
				case JSONParsingState.EndArray:
					if (jsonString[num] != ']')
					{
						return Fail(']', num);
					}
					if (jSONValue.Parent == null)
					{
						return jSONValue.Obj;
					}
					switch (jSONValue.Parent.Type)
					{
					case JSONValueType.Object:
						jSONValue.Parent.Obj.values[list.Pop()] = new JSONValue(jSONValue.Array);
						break;
					case JSONValueType.Array:
						jSONValue.Parent.Array.Add(new JSONValue(jSONValue.Array));
						break;
					default:
						return Fail("valid object", num);
					}
					jSONValue = jSONValue.Parent;
					jSONParsingState = JSONParsingState.ValueSeparator;
					break;
				case JSONParsingState.Null:
					{
						if (jsonString[num] == 'n')
						{
							if (jsonString.Length < num + 4 || jsonString[num + 1] != 'u' || jsonString[num + 2] != 'l' || jsonString[num + 3] != 'l')
							{
								return Fail("null", num);
							}
							switch (jSONValue.Type)
							{
							case JSONValueType.Object:
								jSONValue.Obj.values[list.Pop()] = new JSONValue(JSONValueType.Null);
								break;
							case JSONValueType.Array:
								jSONValue.Array.Add(new JSONValue(JSONValueType.Null));
								break;
							default:
								JSONLogger.Error("Fatal error, current JSON value not valid");
								return null;
							}
							num += 3;
						}
						jSONParsingState = JSONParsingState.ValueSeparator;
						break;
					}
					IL_02c7:
					jSONParsingState = JSONParsingState.Null;
					goto IL_02db;
					IL_0294:
					jSONParsingState = JSONParsingState.Array;
					goto IL_02db;
					IL_02bf:
					jSONParsingState = JSONParsingState.Boolean;
					goto IL_02db;
					IL_028d:
					jSONParsingState = JSONParsingState.Object;
					goto IL_02db;
					IL_029b:
					if (jSONValue.Type == JSONValueType.Array)
					{
						jSONParsingState = JSONParsingState.EndArray;
						goto IL_02db;
					}
					return Fail("valid array", num);
					IL_02db:
					num--;
					break;
				}
			}
			JSONLogger.Error("Unexpected end of string");
			return null;
		}

		private static int SkipWhitespace(string str, int pos)
		{
			while (pos < str.Length && char.IsWhiteSpace(str[pos]))
			{
				pos++;
			}
			return pos;
		}

		private static string ParseString(string str, ref int startPosition)
		{
			if (str[startPosition] != '"' || startPosition + 1 >= str.Length)
			{
				Fail('"', startPosition);
				return null;
			}
			int num = str.IndexOf('"', startPosition + 1);
			if (num <= startPosition)
			{
				Fail('"', startPosition + 1);
				return null;
			}
			while (str[num - 1] == '\\')
			{
				num = str.IndexOf('"', num + 1);
				if (num <= startPosition)
				{
					Fail('"', startPosition + 1);
					return null;
				}
			}
			string text = string.Empty;
			if (num > startPosition + 1)
			{
				text = str.Substring(startPosition + 1, num - startPosition - 1);
			}
			startPosition = num;
			while (true)
			{
				Match match = unicodeRegex.Match(text);
				if (!match.Success)
				{
					break;
				}
				string value = match.Groups[1].Captures[0].Value;
				unicodeBytes[1] = byte.Parse(value.Substring(0, 2), NumberStyles.HexNumber);
				unicodeBytes[0] = byte.Parse(value.Substring(2, 2), NumberStyles.HexNumber);
				value = Encoding.Unicode.GetString(unicodeBytes);
				text = text.Replace(match.Value, value);
			}
			return text;
		}

		private static double ParseNumber(string str, ref int startPosition)
		{
			if (startPosition >= str.Length || (!char.IsDigit(str[startPosition]) && str[startPosition] != '-'))
			{
				return double.NaN;
			}
			int i;
			for (i = startPosition + 1; i < str.Length && str[i] != ',' && str[i] != ']' && str[i] != '}'; i++)
			{
			}
			double result;
			if (!double.TryParse(str.Substring(startPosition, i - startPosition), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
			{
				return double.NaN;
			}
			startPosition = i - 1;
			return result;
		}

		private static JSONObject Fail(char expected, int position)
		{
			return Fail(new string(expected, 1), position);
		}

		private static JSONObject Fail(string expected, int position)
		{
			JSONLogger.Error("Invalid json string, expecting " + expected + " at " + position);
			return null;
		}

		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('{');
			foreach (KeyValuePair<string, JSONValue> value in values)
			{
				stringBuilder.Append("\"" + value.Key + "\"");
				stringBuilder.Append(':');
				stringBuilder.Append(value.Value.ToString());
				stringBuilder.Append(',');
			}
			if (values.Count > 0)
			{
				stringBuilder.Remove(stringBuilder.Length - 1, 1);
			}
			stringBuilder.Append('}');
			return stringBuilder.ToString();
		}

		public IEnumerator<KeyValuePair<string, JSONValue>> GetEnumerator()
		{
			return values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return values.GetEnumerator();
		}

		public void Clear()
		{
			values.Clear();
		}

		public void Remove(string key)
		{
			if (values.ContainsKey(key))
			{
				values.Remove(key);
			}
		}
	}
}
