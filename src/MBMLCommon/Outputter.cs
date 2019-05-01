// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Research.Glo;

namespace MBMLCommon
{
    public class Outputter
    {
        public IDictionary<string, object> Output { get; }
        public bool UsingGlo { get; }

        private readonly List<string[]> outputPaths;

        public Outputter(string name, bool useGlo)
        {
            outputPaths = new List<string[]>();
            UsingGlo = useGlo;
            if (useGlo)
            {
#if NETFULL
                GloBrowser.Start(name);
                GloBrowser.Browser.GoToAddress("");
                Output = GloBrowser.Browser.HomeObjects;
#else
                throw new NotSupportedException("Glo is not supported on .NET Core.");
#endif
            }
            else
                Output = new Dictionary<string, object>();
        }

        public static Outputter GetOutputter(string name)
        {
#if NETFULL
            return new Outputter(name, true);
#else
            return new Outputter(name, false);
#endif
        }

        /// <summary>
        /// Adds object <paramref name="output"/> to the Output dictionary or one of its subdictionaries
        /// as specified by <paramref name="path"/>.
        /// If <paramref name="path"/> contains exactly one item, then <paramref name="output"/> is placed directly into the
        /// Output dictionary using this item as the key. Otherwise <paramref name="output"/> is placed into a nested dictionary
        /// (nested dictionaries are created as required, existing dictionaries are preserved, conflicting objects are overwritten) so that
        /// (...((IDictionary&lt;string, object&gt;)Output[path[0]])[path[1])...)[path[path.Length - 1]] == <paramref name="output"/>
        /// On full .NET Framework/Windows this means adding the object <paramref name="output"/> to the GloBrowser.
        /// </summary>
        /// <param name="output">Object to output.</param>
        /// <param name="path">Path to place <paramref name="output"/> within Output.</param>
        public void Out(object output, params string[] path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException("Path must contain at least one entry.", nameof(path));

            IDictionary<string, object> cur = Output;
            for (int i = 0; i < path.Length - 1; ++i)
            {
                if (cur.ContainsKey(path[i]))
                {
                    if (cur[path[i]] is IDictionary<string, object> next)
                        cur = next;
                    else
                    {
                        Console.WriteLine($"Conflicting outputs at {string.Join(".", path.Where((s, j) => j <= i).Select(s => s.Contains(' ') ? $"\"{s}\"" : s))}.");
                        Console.WriteLine("Replacing the old value.");
                        IDictionary<string, object> dict = new Dictionary<string, object>();
                        cur[path[i]] = dict;
                        cur = dict;
                    }
                }
                else
                {
                    IDictionary<string, object> dict = new Dictionary<string, object>();
                    cur[path[i]] = dict;
                    cur = dict;
                }
            }

            string key = path[path.Length - 1];
            if (cur.ContainsKey(key))
            {
                Console.WriteLine($"Conflicting outputs at {string.Join(".", path.Select(s => s.Contains(' ') ? $"\"{s}\"" : s))}.");
                Console.WriteLine("Replacing the old value.");
            }
            cur[key] = output;

            outputPaths.Add(path);
#if NETFULL
            if (UsingGlo)
                GloBrowser.Browser.RefreshAll();
#endif
        }

        public object this[string[] path] => FindInNestedDictionaries(Output, path);

        /// <summary>
        /// Serializes an object from the Output dictionary that can be accessed as
        /// (...((IDictionary&lt;string, object&gt;)Output[path[0]])[path[1])...)[path[path.Length - 1]]
        /// into the .objml file <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Path to the file into which the target object should be saved.</param>
        /// <param name="objectPath">Path to the target object within Output.</param>
        public void SaveObject(string filePath, params string[] objectPath)
        {
            SaveObjectFromDictionary(filePath, Output, objectPath);
        }

        /// <summary>
        /// Saves all the objects from the Output dictionary as .objml files to the target folder.
        /// Subdictionaries are saved 'as is', i.e. .objml files with serialized dictionaries are created.
        /// Keys from the dictionary are used as file names.
        /// </summary>
        /// <param name="targetFolder">Folder to store saved files. If it doesn't exist, it will be created.</param>
        public void SaveOutputRootObjects(string targetFolder)
        {
            if (string.IsNullOrEmpty(targetFolder))
                throw new ArgumentNullException(nameof(targetFolder));
            Directory.CreateDirectory(targetFolder);
            SaveDictionaryPlain(Output, targetFolder);
        }

        /// <summary>
        /// Recursively saves all the objects from the Output dictionary as .objml files to the target folder.
        /// Subdictionaries are saved as subfolders with .objml files corresponding to objects such that
        /// (object is IDictionary&lt;string, object&gt; == false),
        /// i.e. non-dictionaries; nested dictionaries are recursively saved as nested folders.
        /// Keys from the dictionaries are used as file and/or subfolder names.
        /// </summary>
        /// <param name="targetFolder">Folder to store saved files. If it doesn't exist, it will be created.</param>
        public void SaveOutputRecursive(string targetFolder)
        {
            if (string.IsNullOrEmpty(targetFolder))
                throw new ArgumentNullException(nameof(targetFolder));
            Directory.CreateDirectory(targetFolder);
            SaveDictionaryRecursive(Output, targetFolder);
        }

        /// <summary>
        /// Saves all the objects from the Output dictionary as .objml files to the target folder.
        /// Nested dictionaries are recursively enumerated and all non-dictionary objects from them are saved.
        /// Names for .objml files are created by concatenating corresponding keys from the nested dictionary
        /// structure and forcing PascalCase on the result.
        /// </summary>
        /// <param name="targetFolder">Folder to store saved files. If it doesn't exist, it will be created.</param>
        public void SaveOutputRecursiveFlattening(string targetFolder)
        {
            if (string.IsNullOrEmpty(targetFolder))
                throw new ArgumentNullException(nameof(targetFolder));
            Directory.CreateDirectory(targetFolder);
            SaveDictionaryFlatteningRecursive(Output, targetFolder.Last() == Path.DirectorySeparatorChar ? targetFolder : targetFolder + Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Saves all the objects that were ever passed to the <see cref="Out(object, string[])" /> method of this Outputter.
        /// Strings passed to the <see cref="Out(object, string[])" /> method along with the object are used to create
        /// path to the .objml file relative to <paramref name="targetFolder"/>, so string array { "str1", "str2", ..., "strN" }
        /// means that object will be saved as
        /// <paramref name="targetFolder"/>/str1/str2/.../strN.objml
        /// </summary>
        /// <param name="targetFolder">Folder to store saved files. If it doesn't exist, it will be created.</param>
        public void SaveOutputAsProduced(string targetFolder)
        {
            if (string.IsNullOrEmpty(targetFolder))
                throw new ArgumentNullException(nameof(targetFolder));
            Directory.CreateDirectory(targetFolder);
            SaveList(outputPaths, targetFolder);
        }

        /// <summary>
        /// Saves all the objects that were ever passed to the <see cref="Out(object, string[])" /> method of this Outputter.
        /// Strings passed to the <see cref="Out(object, string[])" /> method along with the object are used to create the
        /// name of to the .objml file - they are concatenated and converted to PascalCase, so string array { "str1", "str2", ..., "strN" }
        /// means that object will be saved as
        /// <paramref name="targetFolder"/>/Str1Str2...StrN.objml
        /// </summary>
        /// <param name="targetFolder">Folder to store saved files. If it doesn't exist, it will be created.</param>
        public void SaveOutputAsProducedFlattening(string targetFolder)
        {
            if (string.IsNullOrEmpty(targetFolder))
                throw new ArgumentNullException(nameof(targetFolder));
            Directory.CreateDirectory(targetFolder);
            SaveListFlattening(outputPaths, targetFolder);
        }

        private static void SaveObjectFromDictionary(string filePath, IDictionary<string, object> dictionary, string[] objectPath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (objectPath == null || objectPath.Length == 0)
                throw new ArgumentNullException(nameof(objectPath));

            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            object target = FindInNestedDictionaries(dictionary, objectPath);

            SerializationManager.Save(filePath, target);
        }

        private static object FindInNestedDictionaries(IDictionary<string, object> dictionary, string[] objectPath)
        {
            object cur = dictionary;
            for (int i = 0; i < objectPath.Length; ++i)
            {
                if (TryNavigateIntoStringDictionary(cur, objectPath[i], out object next))
                    cur = next;
                else
                    throw new ArgumentException("Object is not found.", nameof(objectPath));
            }
            return cur;
        }

        private static bool TryNavigateIntoStringDictionary(object possibleDictionary, string key, out object value)
        {
            // When dictionary is created by the outputter, it's a Dictionary<string, object>
            if (possibleDictionary is IDictionary<string, object> dict)
            {
                return dict.TryGetValue(key, out value);
            }
            // In user-created dictionaries, 2nd type argument can be anything
            var IDictType = possibleDictionary.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            if (IDictType == null || IDictType.GetGenericArguments()[0] != typeof(string))
            {
                value = null;
                return false;
            }
            object[] parameters = new object[] { key, null };
            var tryGetValueMethodInfo = IDictType.GetMethod("TryGetValue");
            bool result = (bool)tryGetValueMethodInfo.Invoke(possibleDictionary, parameters);
            if (result)
            {
                value = parameters[1];
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private void SaveDictionaryPlain(IDictionary<string, object> dict, string pathPrefix)
        {
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                var path = Path.Combine(pathPrefix, kvp.Key + ".objml");
                Console.WriteLine($"Saving {path}...");
                SerializationManager.Save(path, kvp.Value);
            }
        }

        private void SaveDictionaryRecursive(IDictionary<string, object> dict, string pathPrefix)
        {
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                if (kvp.Value is IDictionary<string, object> innerDict)
                    SaveDictionaryRecursive(innerDict, Path.Combine(pathPrefix, kvp.Key));
                else
                {
                    var path = Path.Combine(pathPrefix, kvp.Key + ".objml");
                    Console.WriteLine($"Saving {path}...");
                    SerializationManager.Save(path, kvp.Value);
                }
            }
        }

        private void SaveDictionaryFlatteningRecursive(IDictionary<string, object> dict, string pathPrefix)
        {
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                if (kvp.Value is IDictionary<string, object> innerDict)
                    SaveDictionaryFlatteningRecursive(innerDict, pathPrefix + JoinPascalCase(kvp.Key));
                else
                {
                    var path = pathPrefix + JoinPascalCase(kvp.Key) + ".objml";
                    Console.WriteLine($"Saving {path}...");
                    SerializationManager.Save(path, kvp.Value);
                }
            }
        }

        private void SaveList(List<string[]> list, string targetFolder)
        {
            foreach(string[] path in list)
            {
                string relPath = Path.Combine(path) + ".objml";
                string filepath = Path.Combine(targetFolder, relPath);
                Console.WriteLine($"Saving {filepath}...");
                SaveObject(filepath, path);
            }
        }

        private void SaveListFlattening(List<string[]> list, string targetFolder)
        {
            foreach (string[] path in list)
            {
                string filename = JoinPascalCase(path) + ".objml";
                filename = string.Join(string.Empty, filename.Split(Path.GetInvalidFileNameChars()));
                string filepath = Path.Combine(targetFolder, filename);
                Console.WriteLine($"Saving {filepath}...");
                SaveObject(filepath, path);
            }
        }

        private string JoinPascalCase(params string[] strings)
        {
            StringBuilder builder = new StringBuilder(strings.Sum(s => s.Length));
            bool needUp = true;
            for (int i = 0; i < strings.Length; ++i)
            {
                string s = strings[i];
                for (int j = 0; j < s.Length; ++j)
                {
                    char c = s[j];
                    if (char.IsLetter(c))
                    {
                        if (needUp)
                        {
                            builder.Append(char.ToUpperInvariant(c));
                            needUp = false;
                        }
                        else
                            builder.Append(c);
                    }
                    else if (char.IsWhiteSpace(c) || c == '.' || c == '_' || c == '-')
                        needUp = true;
                    else
                    {
                        builder.Append(c);
                        needUp = false;
                    }
                }
            }
            return builder.ToString();
        }
    }
}
