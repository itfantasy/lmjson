#region Header
/**
 * JsonData.cs
 *   Generic type to hold JSON data (objects, arrays, and so on). This is
 *   the default type returned by JsonMapper.ToObject().
 *
 * The authors disclaim copyright to this source code. For more details, see
 * the COPYING file included with this distribution.
 **/
#endregion


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;


namespace itfantasy.lmjson
{
    /// <summary>
    /// 万用json数据模型
    /// by：jgao
    /// 说明：强兼容json数据模型
    /// </summary>
    public class JsonData : IJsonWrapper, IEquatable<JsonData>
    {
        #region Fields
        private IList<JsonData>               inst_array;
        private IDictionary<string, JsonData> inst_object;
        private object                        inst_value;
        private JsonType                      type;

        // Used to implement the IOrderedDictionary interface
        private IList<KeyValuePair<string, JsonData>> object_list;
        #endregion


        #region Properties
        public int Count{
            get { 
                ICollection collect = EnsureCollection();
                if(collect == null){
                    return 0;
                }
                return collect.Count; 
            }
        }

        public bool IsArray {
            get { return type == JsonType.Array; }
        }

        public bool IsBinary {
            get { return type == JsonType.Binary; }
        }

        public bool IsObject {
            get { return type == JsonType.Object; }
        }

        public bool IsString {
            get { return type == JsonType.String; }
        }

        public ICollection<string> Keys {
            get { 
                if (inst_object == null){
                    return new List<string>();
                }
                EnsureDictionary(); return inst_object.Keys; 
            }
        }
        #endregion


        #region ICollection Properties
        int ICollection.Count {
            get {
                return Count;
            }
        }

        bool ICollection.IsSynchronized {
            get {
                return EnsureCollection ().IsSynchronized;
            }
        }

        object ICollection.SyncRoot {
            get {
                return EnsureCollection ().SyncRoot;
            }
        }
        #endregion


        #region IDictionary Properties
        bool IDictionary.IsFixedSize {
            get {
                return EnsureDictionary ().IsFixedSize;
            }
        }

        bool IDictionary.IsReadOnly {
            get {
                return EnsureDictionary ().IsReadOnly;
            }
        }

        ICollection IDictionary.Keys {
            get {
                EnsureDictionary ();
                IList<string> keys = new List<string> ();

                foreach (KeyValuePair<string, JsonData> entry in
                         object_list) {
                    keys.Add (entry.Key);
                }

                return (ICollection) keys;
            }
        }

        ICollection IDictionary.Values {
            get {
                EnsureDictionary ();
                IList<JsonData> values = new List<JsonData> ();

                foreach (KeyValuePair<string, JsonData> entry in
                         object_list) {
                    values.Add (entry.Value);
                }

                return (ICollection) values;
            }
        }
        #endregion


        #region IJsonWrapper Properties
        bool IJsonWrapper.IsArray {
            get { return IsArray; }
        }

        bool IJsonWrapper.IsBinary {
            get { return IsBinary; }
        }

        bool IJsonWrapper.IsObject {
            get { return IsObject; }
        }

        bool IJsonWrapper.IsString {
            get { return IsString; }
        }
        #endregion


        #region IList Properties
        bool IList.IsFixedSize {
            get {
                return EnsureList ().IsFixedSize;
            }
        }

        bool IList.IsReadOnly {
            get {
                return EnsureList ().IsReadOnly;
            }
        }
        #endregion


        #region IDictionary Indexer
        object IDictionary.this[object key] {
            get {
                return EnsureDictionary ()[key];
            }

            set {
                if (! (key is String))
                    throw new ArgumentException (
                        "The key has to be a string");

                JsonData data = ToJsonData (value);

                this[(string) key] = data;
            }
        }
        #endregion


        #region IOrderedDictionary Indexer
        object IOrderedDictionary.this[int idx] {
            get {
                EnsureDictionary ();
                return object_list[idx].Value;
            }

            set {
                EnsureDictionary ();
                JsonData data = ToJsonData (value);

                KeyValuePair<string, JsonData> old_entry = object_list[idx];

                inst_object[old_entry.Key] = data;

                KeyValuePair<string, JsonData> entry =
                    new KeyValuePair<string, JsonData> (old_entry.Key, data);

                object_list[idx] = entry;
            }
        }
        #endregion


        #region IList Indexer
        object IList.this[int index] {
            get {
                return EnsureList ()[index];
            }

            set {
                EnsureList ();
                JsonData data = ToJsonData (value);

                this[index] = data;
            }
        }
        #endregion


        #region Public Indexers
        public virtual JsonData this[string prop_name] {
            get {
                EnsureDictionary ();
                return inst_object[prop_name];
            }

            set {
                EnsureDictionary ();

                KeyValuePair<string, JsonData> entry =
                    new KeyValuePair<string, JsonData> (prop_name, value);

                if (inst_object.ContainsKey (prop_name)) {
                    for (int i = 0; i < object_list.Count; i++) {
                        if (object_list[i].Key == prop_name) {
                            object_list[i] = entry;
                            break;
                        }
                    }
                } else
                    object_list.Add (entry);

                inst_object[prop_name] = value;
            }
        }

        public JsonData this[int index] {
            get {
                EnsureCollection ();

                if (type == JsonType.Array)
                    return inst_array[index];

                return object_list[index].Value;
            }

            set {
                EnsureCollection ();

                if (type == JsonType.Array)
                    inst_array[index] = value;
                else {
                    KeyValuePair<string, JsonData> entry = object_list[index];
                    KeyValuePair<string, JsonData> new_entry =
                        new KeyValuePair<string, JsonData> (entry.Key, value);

                    object_list[index] = new_entry;
                    inst_object[entry.Key] = value;
                }
            }
        }
        #endregion


        #region Constructors
        public JsonData ()
        {
        }

        public JsonData (bool boolean)
        {
            type = JsonType.Binary;
            if (boolean)
            {
                inst_value = 1;
            }
            else
            {
                inst_value = 0;
            }
        }

        public JsonData (double number)
        {
            type = JsonType.Binary;
            inst_value = number;
        }

        public JsonData (int number)
        {
            type = JsonType.Binary;
            inst_value = number;
        }

        public JsonData (long number)
        {
            type = JsonType.Binary;
            inst_value = number;
        }

        public JsonData (string str)
        {
            type = JsonType.String;
            inst_value = str;
        }

        public JsonData(object obj)
        {
            if (obj is String)
            {
                type = JsonType.String;
            }
            else
            {
                type = JsonType.Binary;
            }
            inst_value = obj;
        }

        #endregion


        #region Implicit Conversions
        public static implicit operator JsonData (Boolean data)
        {
            return new JsonData (data);
        }

        public static implicit operator JsonData (Double data)
        {
            return new JsonData (data);
        }

        public static implicit operator JsonData (Int32 data)
        {
            return new JsonData (data);
        }

        public static implicit operator JsonData (Int64 data)
        {
            return new JsonData (data);
        }

        public static implicit operator JsonData (String data)
        {
            return new JsonData (data);
        }
        #endregion


        #region Explicit Conversions
        public static explicit operator Boolean (JsonData data)
        {
            return Int32.Parse(data.inst_value.ToString()) != 0;
        }

        public static explicit operator Single (JsonData data)
        {
            return Single.Parse(data.inst_value.ToString());
        }

        public static explicit operator Double (JsonData data)
        {
            return Double.Parse(data.inst_value.ToString());
        }

        public static explicit operator Int32 (JsonData data)
        {
            return Int32.Parse(data.inst_value.ToString());
        }

        public static explicit operator Int64 (JsonData data)
        {
            return Int64.Parse(data.inst_value.ToString());
        }

        public static explicit operator String (JsonData data)
        {
            return data.inst_value.ToString();
        }
        #endregion


        #region ICollection Methods
        void ICollection.CopyTo (Array array, int index)
        {
            EnsureCollection ().CopyTo (array, index);
        }
        #endregion


        #region IDictionary Methods
        void IDictionary.Add (object key, object value)
        {
            JsonData data = ToJsonData (value);

            EnsureDictionary ().Add (key, data);

            KeyValuePair<string, JsonData> entry =
                new KeyValuePair<string, JsonData> ((string) key, data);
            object_list.Add (entry);
        }

        void IDictionary.Clear ()
        {
            EnsureDictionary ().Clear ();
            object_list.Clear ();
        }

        bool IDictionary.Contains (object key)
        {
            return EnsureDictionary ().Contains (key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator ()
        {
            return ((IOrderedDictionary) this).GetEnumerator ();
        }

        void IDictionary.Remove (object key)
        {
            EnsureDictionary ().Remove (key);

            for (int i = 0; i < object_list.Count; i++) {
                if (object_list[i].Key == (string) key) {
                    object_list.RemoveAt (i);
                    break;
                }
            }
        }
        #endregion


        #region IEnumerable Methods
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return EnsureCollection ().GetEnumerator ();
        }
        #endregion


        #region IJsonWrapper Methods
        bool IJsonWrapper.GetBoolean ()
        {
            return bool.Parse(inst_value.ToString());
        }

        double IJsonWrapper.GetDouble ()
        {
            return double.Parse(inst_value.ToString());
        }

        int IJsonWrapper.GetInt ()
        {
            return int.Parse(inst_value.ToString());
        }

        long IJsonWrapper.GetLong ()
        {
            return long.Parse(inst_value.ToString());
        }

        string IJsonWrapper.GetString ()
        {
            return inst_value.ToString();
        }

        void IJsonWrapper.SetBoolean (bool val)
        {
            type = JsonType.Binary;
            if (val)
            {
                inst_value = 1;
            }
            else
            {
                inst_value = 0;
            }
        }

        void IJsonWrapper.SetDouble (double val)
        {
            type = JsonType.Binary;
            inst_value = val;
        }

        void IJsonWrapper.SetInt (int val)
        {
            type = JsonType.Binary;
            inst_value = val;
        }

        void IJsonWrapper.SetLong (long val)
        {
            type = JsonType.Binary;
            inst_value = val;
        }

        void IJsonWrapper.SetString (string val)
        {
            type = JsonType.String;
            inst_value = val;
        }

        string IJsonWrapper.ToJson ()
        {
            return ToJson ();
        }

        #endregion


        #region IList Methods
        int IList.Add (object value)
        {
            return Add (value);
        }

        void IList.Clear ()
        {
            EnsureList ().Clear ();
        }

        bool IList.Contains (object value)
        {
            return EnsureList ().Contains (value);
        }

        int IList.IndexOf (object value)
        {
            return EnsureList ().IndexOf (value);
        }

        void IList.Insert (int index, object value)
        {
            EnsureList ().Insert (index, value);
        }

        void IList.Remove (object value)
        {
            EnsureList ().Remove (value);
        }

        void IList.RemoveAt (int index)
        {
            EnsureList ().RemoveAt (index);
        }
        #endregion


        #region IOrderedDictionary Methods
        IDictionaryEnumerator IOrderedDictionary.GetEnumerator ()
        {
            EnsureDictionary ();

            return new OrderedDictionaryEnumerator (
                object_list.GetEnumerator ());
        }

        void IOrderedDictionary.Insert (int idx, object key, object value)
        {
            string property = (string) key;
            JsonData data  = ToJsonData (value);

            this[property] = data;

            KeyValuePair<string, JsonData> entry =
                new KeyValuePair<string, JsonData> (property, data);

            object_list.Insert (idx, entry);
        }

        void IOrderedDictionary.RemoveAt (int idx)
        {
            EnsureDictionary ();

            inst_object.Remove (object_list[idx].Key);
            object_list.RemoveAt (idx);
        }
        #endregion


        #region Private Methods
        private ICollection EnsureCollection ()
        {
            if (type == JsonType.Array)
                return (ICollection)EnsureList();

            if (type == JsonType.Object)
                return (ICollection)EnsureDictionary();

            throw new InvalidOperationException (
                "The JsonData instance has to be initialized first");
        }

        private IDictionary EnsureDictionary()
        {
            if (type == JsonType.Object)
            {
                if (inst_object == null)
                {
                    inst_object = new Dictionary<string, JsonData>();
                    object_list = new List<KeyValuePair<string, JsonData>>();
                }
                return (IDictionary)inst_object;
            }

            if (type != JsonType.None)
                throw new InvalidOperationException (
                    "Instance of JsonData is not a dictionary");

            type = JsonType.Object;
            inst_object = new Dictionary<string, JsonData> ();
            object_list = new List<KeyValuePair<string, JsonData>> ();

            return (IDictionary) inst_object;
        }

        private IList EnsureList()
        {
            if (type == JsonType.Array)
            {
                if (inst_array == null)
                {
                    inst_array = new List<JsonData>();
                }
                return (IList)inst_array;
            }

            if (type != JsonType.None)
                throw new InvalidOperationException (
                    "Instance of JsonData is not a list");

            type = JsonType.Array;
            inst_array = new List<JsonData> ();

            return (IList)inst_array;
        }

        private JsonData ToJsonData (object obj)
        {
            if (obj == null)
                return null;

            if (obj is JsonData)
                return (JsonData) obj;

            return new JsonData (obj);
        }

        #endregion


        public int Add (object value)
        {
            JsonData data = ToJsonData (value);
            return ((IList)this).Add(data);
        }

        public void RemoveAt(int index)
        {
            ((IList)this).RemoveAt(index);
        }

        public void Remove(object value)
        {
            ((IList)this).Remove(value);
        }

        public bool Contains(object value)
        {
            return ((IList)this).Contains(value);
        }

        public int IndexOf(object value)
        {
            return ((IList)this).IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            ((IList)this).Insert(index, value);
        }

        public bool ContainsKey(object key)
        {
            return ((IDictionary)this).Contains(key);
        }

        public void Delete(object key)
        {
            ((IDictionary)this).Remove(key);
        }

        public void Clear ()
        {
            if (IsObject) 
            {
                ((IDictionary) this).Clear ();
                return;
            }
            else if (IsArray) 
            {
                ((IList) this).Clear ();
                return;
            }
        }

        public bool Equals (JsonData x)
        {
            if (x == null)
            {
                if (this.type == JsonType.Object)
                    return this.inst_object == null;
                if (this.type == JsonType.Array)
                    return this.inst_array == null;
                else
                    return this.inst_object == null;
            }

            if (x.type != this.type)
                return false;

            switch (this.type) {
            case JsonType.None:
                return true;

            case JsonType.Object:
                return this.inst_object.Equals (x.inst_object);

            case JsonType.Array:
                return this.inst_array.Equals (x.inst_array);

            default:
                return this.inst_value.Equals (x.inst_value);
            }
        }

        public JsonType GetJsonType ()
        {
            return type;
        }

        public void SetJsonType (JsonType type)
        {
            this.type = type;
        }

        public string ToJson ()
        {
            return JSON.Stringify(this);
        }

        public override string ToString ()
        {
            switch (type) {
            case JsonType.Array:
                return "JsonData array";

            case JsonType.Object:
                return "JsonData object";

            default:
                if (inst_value == null) return "";
                return inst_value.ToString();
            }
        }

        public object ToBinary()
        {
            return inst_value;
        }

        public int ToInt()
        {
            if (inst_value == null) return 0;
            return int.Parse(inst_value.ToString());
        }

        public long ToLone()
        {
            if (inst_value == null) return 0;
            return long.Parse(inst_value.ToString());
        }

        public bool ToBool()
        {
            return ToInt() != 0;
        }

        public float ToFloat()
        {
            if (inst_value == null) return 0.0f;
            return float.Parse(inst_value.ToString());
        }

        public double ToDouble()
        {
            if (inst_value == null) return 0.0;
            return double.Parse(inst_value.ToString());
        }
    }


    internal class OrderedDictionaryEnumerator : IDictionaryEnumerator
    {
        IEnumerator<KeyValuePair<string, JsonData>> list_enumerator;


        public object Current {
            get { return Entry; }
        }

        public DictionaryEntry Entry {
            get {
                KeyValuePair<string, JsonData> curr = list_enumerator.Current;
                return new DictionaryEntry (curr.Key, curr.Value);
            }
        }

        public object Key {
            get { return list_enumerator.Current.Key; }
        }

        public object Value {
            get { return list_enumerator.Current.Value; }
        }


        public OrderedDictionaryEnumerator (
            IEnumerator<KeyValuePair<string, JsonData>> enumerator)
        {
            list_enumerator = enumerator;
        }


        public bool MoveNext ()
        {
            return list_enumerator.MoveNext ();
        }

        public void Reset ()
        {
            list_enumerator.Reset ();
        }
    }
}
