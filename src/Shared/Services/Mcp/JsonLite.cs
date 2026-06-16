using System.Globalization;
using System.Text;

namespace AllO.Services.Mcp;

/// <summary>
/// JSON mínimo sin dependencias (System.Web.Extensions rompe el compilador XAML en net48
/// y Newtonsoft obligaría a desplegar otra DLL). Parse devuelve
/// Dictionary&lt;string,object?&gt; / List&lt;object?&gt; / string / double / bool / null;
/// Serialize acepta esos mismos grafos.
/// </summary>
public static class JsonLite
{
    // ── Serialize ──────────────────────────────────────────────

    public static string Serialize(object? value)
    {
        var sb = new StringBuilder(256);
        Write(sb, value);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, object? value)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case bool b:
                sb.Append(b ? "true" : "false");
                break;
            case string s:
                WriteString(sb, s);
                break;
            case double d:
                sb.Append(double.IsNaN(d) || double.IsInfinity(d)
                    ? "null"
                    : d.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float f:
                Write(sb, (double)f);
                break;
            case int i:
                sb.Append(i.ToString(CultureInfo.InvariantCulture));
                break;
            case long l:
                sb.Append(l.ToString(CultureInfo.InvariantCulture));
                break;
            case decimal m:
                sb.Append(m.ToString(CultureInfo.InvariantCulture));
                break;
            case IDictionary<string, object?> dict:
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    Write(sb, kv.Value);
                }
                sb.Append('}');
                break;
            }
            case System.Collections.IEnumerable seq:
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in seq)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item);
                }
                sb.Append(']');
                break;
            }
            default:
                WriteString(sb, value.ToString() ?? "");
                break;
        }
    }

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    // ── Parse ──────────────────────────────────────────────────

    public static object? Parse(string json)
    {
        int pos = 0;
        var result = ParseValue(json, ref pos);
        SkipWhitespace(json, ref pos);
        if (pos != json.Length)
            throw new FormatException($"Unexpected trailing characters at position {pos}.");
        return result;
    }

    private static object? ParseValue(string s, ref int pos)
    {
        SkipWhitespace(s, ref pos);
        if (pos >= s.Length) throw new FormatException("Unexpected end of JSON.");

        char c = s[pos];
        switch (c)
        {
            case '{': return ParseObject(s, ref pos);
            case '[': return ParseArray(s, ref pos);
            case '"': return ParseString(s, ref pos);
            case 't': Expect(s, ref pos, "true"); return true;
            case 'f': Expect(s, ref pos, "false"); return false;
            case 'n': Expect(s, ref pos, "null"); return null;
            default: return ParseNumber(s, ref pos);
        }
    }

    private static Dictionary<string, object?> ParseObject(string s, ref int pos)
    {
        var result = new Dictionary<string, object?>();
        pos++; // '{'
        SkipWhitespace(s, ref pos);
        if (pos < s.Length && s[pos] == '}') { pos++; return result; }

        while (true)
        {
            SkipWhitespace(s, ref pos);
            string key = ParseString(s, ref pos);
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length || s[pos] != ':') throw new FormatException($"Expected ':' at position {pos}.");
            pos++;
            result[key] = ParseValue(s, ref pos);
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) throw new FormatException("Unterminated object.");
            if (s[pos] == ',') { pos++; continue; }
            if (s[pos] == '}') { pos++; return result; }
            throw new FormatException($"Expected ',' or '}}' at position {pos}.");
        }
    }

    private static List<object?> ParseArray(string s, ref int pos)
    {
        var result = new List<object?>();
        pos++; // '['
        SkipWhitespace(s, ref pos);
        if (pos < s.Length && s[pos] == ']') { pos++; return result; }

        while (true)
        {
            result.Add(ParseValue(s, ref pos));
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) throw new FormatException("Unterminated array.");
            if (s[pos] == ',') { pos++; continue; }
            if (s[pos] == ']') { pos++; return result; }
            throw new FormatException($"Expected ',' or ']' at position {pos}.");
        }
    }

    private static string ParseString(string s, ref int pos)
    {
        if (s[pos] != '"') throw new FormatException($"Expected string at position {pos}.");
        pos++;
        var sb = new StringBuilder();
        while (pos < s.Length)
        {
            char c = s[pos++];
            if (c == '"') return sb.ToString();
            if (c != '\\') { sb.Append(c); continue; }

            if (pos >= s.Length) break;
            char esc = s[pos++];
            switch (esc)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '/': sb.Append('/'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (pos + 4 > s.Length) throw new FormatException("Truncated \\u escape.");
                    sb.Append((char)int.Parse(s.Substring(pos, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    pos += 4;
                    break;
                default:
                    throw new FormatException($"Invalid escape '\\{esc}' at position {pos - 1}.");
            }
        }
        throw new FormatException("Unterminated string.");
    }

    private static double ParseNumber(string s, ref int pos)
    {
        int start = pos;
        while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] is '-' or '+' or '.' or 'e' or 'E'))
            pos++;
        string token = s.Substring(start, pos - start);
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            throw new FormatException($"Invalid number '{token}' at position {start}.");
        return d;
    }

    private static void Expect(string s, ref int pos, string literal)
    {
        if (pos + literal.Length > s.Length || s.Substring(pos, literal.Length) != literal)
            throw new FormatException($"Invalid literal at position {pos}.");
        pos += literal.Length;
    }

    private static void SkipWhitespace(string s, ref int pos)
    {
        while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++;
    }
}
