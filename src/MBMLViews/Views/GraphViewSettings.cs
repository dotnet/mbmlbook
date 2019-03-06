// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace MBMLViews.Views
{
	public class GraphViewSettings : INotifyPropertyChanged
	{
		public GraphViewSettings() {
		}

		bool showFactors = true;
		[DisplayName("Show Factors")]
		public bool ShowFactors
		{
			get { return showFactors; }
			set
			{
				showFactors = value;
				if (PropertyChanged!=null) PropertyChanged(this, new PropertyChangedEventArgs("ShowFactors"));
			}
		}

		bool showConstants = false;
		[DisplayName("Show Constants")]
		public bool ShowConstants
		{
			get { return showConstants; }
			set
			{
				showConstants = value;
				if (PropertyChanged!=null) PropertyChanged(this, new PropertyChangedEventArgs("ShowConstants"));
			}
		}

		bool showObservedConditionVariables = false;
		[DisplayName("Show Observed Condition Variables")]
		public bool ShowObservedConditionVariables
		{
			get { return showObservedConditionVariables; }
			set
			{
				showObservedConditionVariables = value;
				if (PropertyChanged!=null) PropertyChanged(this, new PropertyChangedEventArgs("ShowObservedConditionVariables"));
			}
		}

		bool showPriors = true;
		[DisplayName("Show Prior Factors")]
		public bool ShowPriors
		{
			get { return showPriors; }
			set
			{
				showPriors = value;
				if (PropertyChanged!=null) PropertyChanged(this, new PropertyChangedEventArgs("ShowPriors"));
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler  PropertyChanged;

		#endregion
	}
}
