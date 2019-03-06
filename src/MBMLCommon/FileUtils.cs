// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.Research.Glo;

    /// <summary>
    /// The file utilities.
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Loads the file.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="path">The path.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>
        /// The object.
        /// </returns>
        /// <exception cref="System.NullReferenceException">Failed to load data:  + filename</exception>
        /// <exception cref="System.IO.IOException">No examples in input file:  + filename</exception>
        public static TObject Load<TObject>(string path, string filename)
        {
            List<Exception> errors;
            string fullpath = Path.Combine(path, filename + ".objml");
            TObject obj = (TObject)SerializationManager.Load(fullpath, out errors);

            if (ReferenceEquals(obj, null))
            {
                Console.Write(
                    @"Failed with errors:" + Environment.NewLine + string.Join("\n", errors.Select(ia => ia.Message)) + Environment.NewLine);
            }
            else
            {
                Console.WriteLine(@"File loaded: " + fullpath);
            }

            return obj;
        }

        /// <summary>
        /// Saves the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="data">The data.</param>
        public static void Save(string path, string filename, object data)
        {
            try
            {
                if (!FolderIsWritable(path))
                {
                    return;
                }

                string fullpath = Path.Combine(path, filename + ".objml");
                SerializationManager.Save(fullpath, data);
                Console.WriteLine(@"File written: " + fullpath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Checks if the folders is writable.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The see <see cref="bool"/></returns>
        public static bool FolderIsWritable(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine(path + @" does not exist");
                return false;
            }

#if NETFULL
            try
            {
                // Attempt to get a list of security permissions from the folder. 
                // This will raise an exception if the path is read only or do not have access to view the permissions. 
                Directory.GetAccessControl(path);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine(@"Cannot write to " + path);
                return false;
            }
#endif

            return true;
        }
    }
}
