using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace BlackTundra.Foundation.Utility {

    public static class DebugUtility {

        #region constructor

        /// <summary>
        /// Lower clamp for getting a stackframe.
        /// This is used to correct the depth of the stackframe being requested by
        /// compensating for the method call adding a frame.
        /// </summary>
        private const int LowerStackframeClamp = 1;

        #endregion

        #region logic

        public static string GetStackframe(int depth = -1) {

            if (depth < 0) depth = LowerStackframeClamp;
            else depth += LowerStackframeClamp;

            StackFrame stackFrame = new StackFrame(depth, true);
            MethodBase method = stackFrame.GetMethod();

            return new StringBuilder(8).Append('(')
                .Append(method.DeclaringType.FullName)
                .Append('.')
                .Append(method.Name)
                .Append(':')
                .Append(stackFrame.GetFileLineNumber())
                .Append(')').ToString();

        }

        #endregion

    }

}