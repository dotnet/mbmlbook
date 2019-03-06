// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLViews
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// The code timer. Can be used as follows:
    ///  using (new CodeTimer("Some message"))
    ///  {
    ///      // Do some stuff
    ///  }
    /// </summary>
    public class CodeTimer : IDisposable
    {
        /// <summary>
        /// The stopwatch.
        /// </summary>
        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeTimer"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CodeTimer(string message)
        {
            Console.WriteLine(message + "...");
            this.stopwatch.Start();
        }

        /// <summary>
        /// Times the function.
        /// </summary>
        /// <typeparam name="TInput">The type of the input.</typeparam>
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// <param name="message">The message.</param>
        /// <param name="func">The function.</param>
        /// <param name="p">The p.</param>
        /// <returns>The function output</returns>
        public static TOutput TimeFunction<TInput, TOutput>(string message, Func<TInput, TOutput> func, TInput p)
        {
            TOutput ret;
            using (new CodeTimer(message))
            {
                ret = func(p);
            }

            return ret;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.stopwatch.Stop();
            Console.Write(
                " done. (elapsed = {0}s)\n",
                ((double)this.stopwatch.ElapsedMilliseconds / 1000).ToString("#0.00"));
        }
    }
}
