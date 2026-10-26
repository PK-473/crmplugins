using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManagement.Plugins.Base
{
    /// <summary>
    /// The LABELS of the Current Status choice. The numbers behind them are read from
    /// metadata at run time, so no option number is hardcoded anywhere.
    /// </summary>
    internal static class StoryStatus
    {
        public const string Open = "Open";
        public const string InProgress = "In-Progress";
        public const string Completed = "Completed";
    }
}
