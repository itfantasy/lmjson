/*
 * Copyright (c) 2016 itfantasy Inc. All rights reserved.
 * 
 * https://github.com/itfantasy
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// ----------------------------------------------
//
// This file is a part of library UNISIMPLE
//
//      Desc:   JSON解析
//      Author: kenkao
//      Ver:    1.0
//
// ----------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace itfantasy.lmjson
{
    /// <summary>
    /// json序列化反序列化
    /// by：jgao
    /// 仿as3接口定义的json工具类
    /// </summary>
    public class JSON
    {
        /// <summary>
        /// json序列化
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Stringify(JsonData value)
        {
            return Serializer.Serialize(value);
        }

        #region===================- 序列化 -===================
        sealed class Serializer
        {
            StringBuilder builder;

            Serializer()
            {
                //创建生成器  
                builder = new StringBuilder();
            }
            //序列化  
            public static string Serialize(JsonData obj)
            {
                var instance = new Serializer();

                instance.SerializeValue(obj);

                return instance.builder.ToString();
            }
            //类型  
            void SerializeValue(JsonData value)
            {
                if (value == null)
                {
                    builder.Append("null");
                }
                else if (value.IsString)
                {
                    SerializeString(value.ToString());
                }
                else if (value.IsArray)
                {
                    SerializeArray(value);
                }
                else if (value.IsObject)
                {
                    SerializeObject(value);
                }
                else
                {
                    SerializeOther(value.ToBinary());
                }
            }
            //序列化对象  
            void SerializeObject(JsonData obj)
            {
                bool first = true;

                builder.Append('{');

                if (!obj.Equals(null))
                {

                    foreach (string e in obj.Keys)
                    {
                        if (!first)
                        {
                            builder.Append(',');
                        }

                        SerializeString(e);
                        builder.Append(':');

                        SerializeValue(obj[e]);

                        first = false;
                    }
                }

                builder.Append('}');
            }
            // 序列化数组  
            void SerializeArray(JsonData anArray)
            {
                builder.Append('[');

                if (!anArray.Equals(null))
                {

                    bool first = true;

                    for (int i = 0; i < anArray.Count; i++)
                    {
                        JsonData obj = anArray[i];
                        if (!first)
                        {
                            builder.Append(',');
                        }

                        SerializeValue(obj);

                        first = false;
                    }
                }

                builder.Append(']');
            }
            //string  
            void SerializeString(string str)
            {
                builder.Append('\"');

                char[] charArray = str.ToCharArray();
                foreach (var c in charArray)
                {
                    switch (c)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            int codepoint = Convert.ToInt32(c);
                            /*
                            if ((codepoint >= 32) && (codepoint <= 126))
                            {
                                builder.Append(c);
                            }
                            else
                            {
                                builder.Append("\\u");
                                builder.Append(codepoint.ToString("x4"));
                            }
                            */
                            builder.Append(c);
                            break;
                    }
                }

                builder.Append('\"');
            }
            //其他  
            void SerializeOther(object value)
            {
                // NOTE: decimals lose precision during serialization.  
                // They always have, I'm just letting you know.  
                // Previously floats and doubles lost precision too.  
                //注意：小数在序列化过程中丢失精度。  
                //他们总是有，我只是让你知道。  
                //以前失去精度和双精度浮点数。  
                if (value == null)
                {
                    builder.Append("null");
                }
                else if (value is float)
                {
                    builder.Append(((float)value).ToString("R"));
                }
                else if (value is int
                  || value is uint
                  || value is long
                  || value is sbyte
                  || value is byte
                  || value is short
                  || value is ushort
                  || value is ulong)
                {
                    builder.Append(value);
                }
                else if (value is double
                  || value is decimal)
                {
                    builder.Append(Convert.ToDouble(value).ToString("R"));
                }
                else
                {
                    SerializeString(value.ToString());
                }
            }
        }
        #endregion

        /// <summary>
        /// json反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static JsonData Parse(string json)
        {
            return Parser.Parse(json);
        }

        #region ===================- 解析 -===================
        sealed class Parser : IDisposable
        {
            const string WORD_BREAK = "{}[],:\"";

            public static bool IsWordBreak(char c)
            {
                //     如果 c 是空白，则为 true；否则，为 false;报告指定 Unicode 字符在此字符串中的第一个匹配项的索引。  
                return Char.IsWhiteSpace(c) || WORD_BREAK.IndexOf(c) != -1;
            }

            enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            };
            //     实现从字符串进行读取的 System.IO.TextReader。  
            StringReader json;

            Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            public static JsonData Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }
            //释放  
            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            JsonData ParseObject()
            {
                JsonData table = new JsonData();

                // ditch opening brace  
                json.Read();

                // {  
                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            table.SetJsonType(JsonType.Object);
                            return table;
                        default:
                            // name  
                            string name = ParseString().ToString();
                            if (name == null)
                            {
                                return null;
                            }

                            // :  
                            if (NextToken != TOKEN.COLON)
                            {
                                return null;
                            }
                            // ditch the colon  
                            json.Read();

                            // value  
                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            JsonData ParseArray()
            {
                JsonData array = new JsonData();

                // ditch opening bracket  
                json.Read();

                // [  
                bool parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = NextToken;

                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.SQUARED_CLOSE:
                            parsing = false;
                            break;
                        default:
                            object value = ParseByToken(nextToken);

                            array.Add(value);
                            break;
                    }
                }
                array.SetJsonType(JsonType.Array);
                return array;
            }

            JsonData ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            JsonData ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING:
                        return ParseString();
                    case TOKEN.NUMBER:
                        return ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return ParseObject();
                    case TOKEN.SQUARED_OPEN:
                        return ParseArray();
                    case TOKEN.NULL:
                        return null;
                    default:
                        return null;
                }
            }

            JsonData ParseString()
            {
                StringBuilder s = new StringBuilder();
                char c;

                // ditch opening quote  
                json.Read();

                bool parsing = true;
                while (parsing)
                {

                    if (json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }

                    c = NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    s.Append(c);
                                    break;
                                case 'b':
                                    s.Append('\b');
                                    break;
                                case 'f':
                                    s.Append('\f');
                                    break;
                                case 'n':
                                    s.Append('\n');
                                    break;
                                case 'r':
                                    s.Append('\r');
                                    break;
                                case 't':
                                    s.Append('\t');
                                    break;
                                case 'u':
                                    var hex = new char[4];

                                    for (int i = 0; i < 4; i++)
                                    {
                                        hex[i] = NextChar;
                                    }

                                    s.Append((char)Convert.ToInt32(new string(hex), 16));
                                    break;
                            }
                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return new JsonData(s.ToString());
            }

            JsonData ParseNumber()
            {
                string number = NextWord;
                // 摘要:  
                //     报告指定 Unicode 字符在此字符串中的第一个匹配项的索引。  
                //  
                // 参数:  
                //   value:  
                //     要查找的 Unicode 字符。  
                //  
                // 返回结果:  
                //     如果找到该字符，则为 value 的从零开始的索引位置；如果未找到，则为 -1。  
                if (number.IndexOf('.') == -1)
                {
                    long parsedInt;
                    //     将数字的字符串表示形式转换为它的等效 64 位有符号整数。一个指示转换是否成功的返回值。  
                    Int64.TryParse(number, out parsedInt);
                    return parsedInt;
                }

                double parsedDouble;
                Double.TryParse(number, out parsedDouble);
                return new JsonData(parsedDouble);
            }
            //  
            void EatWhitespace()
            {
                //指示指定字符串中位于指定位置处的字符是否属于空白类别。  
                while (Char.IsWhiteSpace(PeekChar))
                {
                    json.Read();
                    //摘要:  
                    //     返回下一个可用的字符，但不使用它。  
                    //  
                    // 返回结果:  
                    //     表示下一个要读取的字符的整数，或者，如果没有更多的可用字符或该流不支持查找，则为 -1。  
                    if (json.Peek() == -1)
                    {
                        break;
                    }
                }
            }

            char PeekChar
            {
                get
                {
                    //     读取输入字符串中的下一个字符并将该字符的位置提升一个字符。  
                    //  
                    // 返回结果:  
                    //     基础字符串中的下一个字符，或者如果没有更多的可用字符，则为 -1。  
                    return Convert.ToChar(json.Peek());
                }
            }

            char NextChar
            {
                get
                {
                    return Convert.ToChar(json.Read());
                }
            }

            string NextWord
            {
                get
                {
                    //     表示可变字符字符串。无法继承此类。  
                    StringBuilder word = new StringBuilder();

                    while (!IsWordBreak(PeekChar))
                    {
                        // 摘要:  
                        //     在此实例的结尾追加指定 Unicode 字符的字符串表示形式。  
                        //  
                        // 参数:  
                        //   value:  
                        //     要追加的 Unicode 字符。  
                        //  
                        // 返回结果:  
                        //     完成追加操作后对此实例的引用。  
                        word.Append(NextChar);
                        //下一个字符为空  
                        if (json.Peek() == -1)
                        {
                            break;
                        }
                    }
                    //  
                    return word.ToString();
                }
            }

            TOKEN NextToken
            {
                get
                {
                    EatWhitespace();

                    if (json.Peek() == -1)
                    {
                        return TOKEN.NONE;
                    }

                    switch (PeekChar)
                    {
                        case '{':
                            return TOKEN.CURLY_OPEN;
                        case '}':
                            json.Read();
                            return TOKEN.CURLY_CLOSE;
                        case '[':
                            return TOKEN.SQUARED_OPEN;
                        case ']':
                            json.Read();
                            return TOKEN.SQUARED_CLOSE;
                        case ',':
                            json.Read();
                            return TOKEN.COMMA;
                        case '"':
                            return TOKEN.STRING;
                        case ':':
                            return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return TOKEN.NUMBER;
                    }

                    switch (NextWord)
                    {
                        case "null":
                            return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }
        } 
        #endregion
    }
}
