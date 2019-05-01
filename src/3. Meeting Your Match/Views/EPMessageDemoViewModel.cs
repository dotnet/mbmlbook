using System.Collections.Generic;

using Microsoft.ML.Probabilistic.Collections;

namespace MeetingYourMatch.Views
{
    /// <summary>
    /// The EP message demo view model.
    /// </summary>
    public class EPMessageDemoViewModel : Dictionary<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EPMessageDemoViewModel"/> class.
        /// </summary>
        internal EPMessageDemoViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EPMessageDemoViewModel"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        internal EPMessageDemoViewModel(IEnumerable<KeyValuePair<string, object>> items)
        {
            this.AddRange(items);
        }
    }
}
