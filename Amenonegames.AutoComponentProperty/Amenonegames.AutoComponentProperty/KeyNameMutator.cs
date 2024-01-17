//this code copied from VYaml.SourceGenerator in https://github.com/hadashiA/VYaml?tab=MIT-1-ov-file

// Copyright (c) 2022 hadashiA
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;

namespace Amenonegames.SourceGenerator;

enum NamingConvention
{
    LowerCamelCase,
    UpperCamelCase,
    SnakeCase,
    KebabCase,
}

static class KeyNameMutator
{
    public static string Mutate(string s, NamingConvention namingConvention)
    {
        return namingConvention switch
        {
            NamingConvention.LowerCamelCase => ToLowerCamelCase(s),
            NamingConvention.UpperCamelCase => s,
            NamingConvention.SnakeCase => ToSnakeCase(s),
            NamingConvention.KebabCase => ToSnakeCase(s, '-'),
            _ => throw new ArgumentOutOfRangeException(nameof(namingConvention), namingConvention, null)
        };
    }

    public static string ToLowerCamelCase(string s)
    {
        var span = s.AsSpan();
        if (span.Length <= 0 ||
            (span.Length <= 1 && char.IsLower(span[0])))
        {
            return s;
        }

        Span<char> buf = stackalloc char[span.Length];
        span.CopyTo(buf);
        buf[0] = char.ToLowerInvariant(span[0]);
        return buf.ToString();
    }

    public static string ToSnakeCase(string s, char separator = '_')
    {
        var span = s.AsSpan();
        if (span.Length <= 0) return s;

        Span<char> buf = stackalloc char[span.Length * 2];
        var written = 0;
        foreach (var ch in span)
        {
            if (char.IsUpper(ch))
            {
                if (written == 0 || // first
                    char.IsUpper(span[written - 1])) // WriteIO => write_io
                {
                    buf[written++] = char.ToLowerInvariant(ch);
                }
                else
                {
                    buf[written++] = separator;
                    if (buf.Length <= written)
                    {
                        buf = new char[buf.Length * 2];
                    }
                    buf[written++] = char.ToLowerInvariant(ch);
                }
            }
            else
            {
                buf[written++] = ch;
            }
        }
        return buf.Slice(0, written).ToString();
    }
}