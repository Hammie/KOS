using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace kOS.Safe.Encapsulation
{
    public class Lexicon : Structure, IDictionary<Structure, Structure>, IIndexable, IDumper
    {
        public class LexiconComparer<TI> : IEqualityComparer<TI>
        {
            public bool Equals(TI x, TI y)
            {
                if (x == null || y == null)
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                if ((x is string || x is StringValue) && (y is string || y is StringValue))
                {
                    var compare = string.Compare(x.ToString(), y.ToString(), StringComparison.InvariantCultureIgnoreCase);
                    return compare == 0;
                }

                return x.Equals(y);
            }

            public int GetHashCode(TI obj)
            {
                if (obj is string || obj is StringValue)
                {
                    return obj.ToString().ToLower().GetHashCode();
                }
                return obj.GetHashCode();
            }
        }

        private IDictionary<Structure, Structure> internalDictionary;
        private bool caseSensitive;

        public Lexicon()
        {
            internalDictionary = new Dictionary<Structure, Structure>(new LexiconComparer<Structure>());
            caseSensitive = false;
            InitalizeSuffixes();
        }

        private Lexicon(IEnumerable<KeyValuePair<Structure, Structure>> lexicon)
            : this()
        {
            foreach (var u in lexicon)
            {
                internalDictionary.Add(u);
            }
        }

        private void InitalizeSuffixes()
        {
            AddSuffix("CLEAR", new NoArgsSuffix(Clear, "Removes all items from Lexicon"));
            AddSuffix("KEYS", new Suffix<ListValue<object>>(GetKeys, "Returns the lexicon keys"));
            AddSuffix("HASKEY", new OneArgsSuffix<bool, object>(HasKey, "Returns true if a key is in the Lexicon"));
            AddSuffix("HASVALUE", new OneArgsSuffix<bool, object>(HasValue, "Returns true if value is in the Lexicon"));
            AddSuffix("VALUES", new Suffix<ListValue<object>>(GetValues, "Returns the lexicon values"));
            AddSuffix("COPY", new NoArgsSuffix<Lexicon>(() => new Lexicon(this), "Returns a copy of Lexicon"));
            AddSuffix("LENGTH", new NoArgsSuffix<int>(() => internalDictionary.Count, "Returns the number of elements in the collection"));
            AddSuffix("REMOVE", new OneArgsSuffix<bool, object>(Remove, "Removes the value at the given key"));
            AddSuffix("ADD", new TwoArgsSuffix<object, object>(Add, "Adds a new item to the lexicon, will error if the key already exists"));
            AddSuffix("DUMP", new NoArgsSuffix<string>(ToString, "Serializes the collection to a string for printing"));
            AddSuffix(new[] { "CASESENSITIVE", "CASE" }, new SetSuffix<bool>(() => caseSensitive, SetCaseSensitivity, "Lets you get/set the case sensitivity on the collection, changing sensitivity will clear the collection"));
        }

        private void SetCaseSensitivity(bool newCase)
        {
            if (newCase == caseSensitive)
            {
                return;
            }
            caseSensitive = newCase;

            internalDictionary = newCase ?
                new Dictionary<Structure, Structure>() :
            new Dictionary<Structure, Structure>(new LexiconComparer<Structure>());
        }

        private bool HasValue(Structure value)
        {
            return internalDictionary.Values.Contains(value);
        }

        private bool HasKey(Structure key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public ListValue<Structure> GetValues()
        {
            return ListValue.CreateList(Values);
        }

        public ListValue<Structure> GetKeys()
        {
            return ListValue.CreateList(Keys);
        }

        public IEnumerator<KeyValuePair<Structure, Structure>> GetEnumerator()
        {
            return internalDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<Structure, Structure> item)
        {
            if (internalDictionary.ContainsKey(item.Key))
            {
                throw new KOSDuplicateKeyException(item.Key.ToString(), caseSensitive);
            }
            internalDictionary.Add(item);
        }

        public void Clear()
        {
            internalDictionary.Clear();
        }

        public bool Contains(KeyValuePair<Structure, Structure> item)
        {
            return internalDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<Structure, Structure>[] array, int arrayIndex)
        {
            internalDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<Structure, Structure> item)
        {
            return internalDictionary.Remove(item);
        }

        public int Count
        {
            get { return internalDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return internalDictionary.IsReadOnly; }
        }

        public bool ContainsKey(Structure key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public void Add(Structure key, Structure value)
        {
            if (internalDictionary.ContainsKey(key))
            {
                throw new KOSDuplicateKeyException(key.ToString(), caseSensitive);
            }
            internalDictionary.Add(key, value);
        }

        public bool Remove(Structure key)
        {
            return internalDictionary.Remove(key);
        }

        public bool TryGetValue(Structure key, out Structure value)
        {
            return internalDictionary.TryGetValue(key, out value);
        }

        public Structure this[Structure key]
        {
            get
            {
                if (internalDictionary.ContainsKey(key))
                {
                    return internalDictionary[key];
                }
                throw new KOSKeyNotFoundException(key.ToString(), caseSensitive);
            }
            set
            {
                internalDictionary[key] = value;
            }
        }

        public ICollection<Structure> Keys
        {
            get
            {
                return internalDictionary.Keys;
            }
        }

        public ICollection<Structure> Values
        {
            get
            {
                return internalDictionary.Values;
            }
        }

        public Structure GetIndex(Structure key)
        {
            return internalDictionary[key];
        }

        public void SetIndex(Structure index, Structure value)
        {
            internalDictionary[index] = value;
        }

        public override string ToString()
        {
            return new SafeSerializationMgr().ToString(this);
        }

        public IDictionary<Structure, Structure> Dump()
        {
            var result = new DictionaryWithHeader((Dictionary<Structure, Structure>)internalDictionary)
            {
                Header = "LEXICON of " + internalDictionary.Count + " items:"
            };

            return result;
        }

        public void LoadDump(IDictionary<Structure, Structure> dump)
        {
            internalDictionary.Clear();

            foreach (KeyValuePair<Structure, Structure> entry in dump)
            {
                internalDictionary.Add(Structure.FromPrimitive(entry.Key), Structure.FromPrimitive(entry.Value));
            }
        }
    }
}