// JsonDeserializer.cs
// 
//  Convert string to JsonValue
//
// Author(s):
//  Daniel Lacroix <dlacroix@erasme.org>
// 
// Copyright (c) 2013 Departement du Rhone
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Text;
using System.Globalization;

namespace Erasme.Json
{
    internal class JsonDeserializer
    {
        string content;
        int pos;

        public JsonDeserializer()
        {
        }

        string ReadString()
        {
            StringBuilder str = new StringBuilder();

            if (content[pos] != '"')
                throw new Exception("Invalid JSON string");
            pos++;
            while (true)
            {
                if (content[pos] == '\\')
                {
                    pos++;
                    if (content[pos] == '"')
                        str.Append('"');
                    else if (content[pos] == '\\')
                        str.Append('\\');
                    else if (content[pos] == '/')
                        str.Append('/');
                    else if (content[pos] == 'b')
                        str.Append('\b');
                    else if (content[pos] == 'f')
                        str.Append('\f');
                    else if (content[pos] == 'n')
                        str.Append('\n');
                    else if (content[pos] == 'r')
                        str.Append('\r');
                    else if (content[pos] == 't')
                        str.Append('\t');
                    else if (content[pos] == 'u')
                    {
                        pos++;
                        string unicode = "";
                        unicode += content[pos++];
                        unicode += content[pos++];
                        unicode += content[pos++];
                        unicode += content[pos];
                        str.Append((char)UInt32.Parse(unicode, NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo));
                    }
                    else
                        throw new Exception("Invalid JSON string");
                    pos++;
                }
                else if (content[pos] != '"')
                    str.Append(content[pos++]);
                else
                    break;
            }
            pos++;
            return str.ToString();
        }

        double ReadNumber()
        {
            StringBuilder str = new StringBuilder();
            while ((content[pos] == '-') || (content[pos] == '+') ||
                  (content[pos] == 'e') || (content[pos] == 'E') ||
                  (content[pos] == '.') || (content[pos] == '0') ||
                  (content[pos] == '1') || (content[pos] == '2') ||
                  (content[pos] == '3') || (content[pos] == '4') ||
                  (content[pos] == '5') || (content[pos] == '6') ||
                  (content[pos] == '7') || (content[pos] == '8') ||
                  (content[pos] == '9'))
            {
                str.Append(content[pos]);
                pos++;
            }
            return double.Parse(str.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        }

        JsonArray ReadArray()
        {
            if (content[pos] != '[')
                throw new Exception("Invalid JSON array");

            JsonArray array = new JsonArray();

            pos++;
            RemoveSpace();

            if (content[pos] == ']')
            {
                pos++;
                return array;
            }
            do
            {
                array.Add(ReadValue());
                RemoveSpace();
                if (content[pos] == ']')
                {
                    pos++;
                    return array;
                }
                if (content[pos] != ',')
                    throw new Exception("Invalid JSON array");
                pos++;
                RemoveSpace();
            } while (true);
        }

        JsonValue ReadValue()
        {
            JsonValue val = null;

            if (content[pos] == '"')
                val = new JsonPrimitive(ReadString());
            else if (content[pos] == '[')
                val = ReadArray();
            else if (content[pos] == '{')
                val = ReadObject();
            else if ((content[pos] == '-') || (content[pos] == '0') ||
                    (content[pos] == '1') || (content[pos] == '2') ||
                    (content[pos] == '3') || (content[pos] == '4') ||
                    (content[pos] == '5') || (content[pos] == '6') ||
                    (content[pos] == '7') || (content[pos] == '8') ||
                    (content[pos] == '9'))
                val = new JsonPrimitive(ReadNumber());
            else if ((content[pos] == 'n') && (content[pos + 1] == 'u') &&
                    (content[pos + 2] == 'l') && (content[pos + 3] == 'l'))
            {
                val = null;
                pos += 4;
            }
            else if ((content[pos] == 't') && (content[pos + 1] == 'r') &&
                    (content[pos + 2] == 'u') && (content[pos + 3] == 'e'))
            {
                val = new JsonPrimitive(true);
                pos += 4;
            }
            else if ((content[pos] == 'f') && (content[pos + 1] == 'a') &&
                    (content[pos + 2] == 'l') && (content[pos + 3] == 's') &&
                    (content[pos + 4] == 'e'))
            {
                val = new JsonPrimitive(false);
                pos += 5;
            }
            else
                throw new Exception("Invalid JSON object");
            return val;
        }

        JsonObject ReadObject()
        {
            if (content[pos] != '{')
                throw new Exception("Invalid JSON object");

            JsonObject obj = new JsonObject();
            pos++;
            RemoveSpace();

            if (content[pos] == '}')
            {
                pos++;
                return obj;
            }

            bool stop = true;
            do
            {
                string key = ReadString();

                RemoveSpace();
                if (content[pos] != ':')
                    throw new Exception("Invalid JSON object");
                pos++;
                RemoveSpace();
                obj[key] = ReadValue();
                RemoveSpace();

                if (content[pos] == ',')
                {
                    stop = false;
                    pos++;
                    RemoveSpace();
                }
                else
                    stop = true;
            } while (!stop);

            if (content[pos] != '}')
                throw new Exception("Invalid JSON object");
            pos++;
            return obj;
        }

        void RemoveSpace()
        {
            while ((content[pos] == ' ') || (content[pos] == '\t') || (content[pos] == '\n') || (content[pos] == '\r')) { pos++; }
        }

        public JsonValue Deserialize(string content)
        {
            this.content = content;
            pos = 0;
            RemoveSpace();
            return ReadValue();
        }
    }
}

