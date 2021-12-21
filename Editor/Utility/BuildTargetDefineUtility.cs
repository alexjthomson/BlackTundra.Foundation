using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;

namespace BlackTundra.Foundation.Editor.Utility {

    /// <summary>
    /// Utility for defining build target scripting defines.
    /// </summary>
    public static class BuildTargetDefineUtility {

        public static void SetDefine(in string define, in BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone) {

            if (define == null) throw new ArgumentNullException("define");
            LinkedList<string> defines = GetDefinesAsLinkedList(buildTargetGroup);
            if (defines.Contains(define)) return;
            defines.AddLast(define);
            SetDefines(defines.ToArray(), buildTargetGroup);

        }

        public static bool GetDefine(in string define, in BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone) {

            if (define == null) throw new ArgumentNullException("define");
            string[] defines = GetDefines(buildTargetGroup);
            for (int i = 0; i < defines.Length; i++) { if (defines[i].Equals(define)) return true; }
            return false;

        }

        public static bool RemoveDefine(in string define, in BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone) {

            if (define == null) throw new ArgumentNullException("define");
            LinkedList<string> defines = GetDefinesAsLinkedList(buildTargetGroup);
            if (!defines.Contains(define)) return false;
            defines.Remove(define);
            SetDefines(defines.ToArray(), buildTargetGroup);
            return true;

        }

        public static string[] GetDefines(in BuildTargetGroup buildTargetGroup = BuildTargetGroup.Standalone) {

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines != null ? defines.Split(';') : new string[0];

        }

        private static void SetDefines(in string[] defines, in BuildTargetGroup buildTargetGroup) {

            if (defines == null) throw new ArgumentNullException("defines");
            int defineCount = defines.Length;
            if (defineCount == 0) {

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Empty);
                return;

            } else if (defineCount == 1) {

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines[0]);
                return;

            } else {

                StringBuilder stringBuilder = new StringBuilder(defineCount * 16);
                stringBuilder.Append(defines[0]);
                for (int i = 1; i < defineCount; i++) {

                    stringBuilder.Append(';');
                    stringBuilder.Append(defines[i]);

                }

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, stringBuilder.ToString());

            }

        }

        private static LinkedList<string> GetDefinesAsLinkedList(in BuildTargetGroup buildTargetGroup) {

            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            if (defines == null) return new LinkedList<string>();
            string[] tokens = defines.Split(';');
            LinkedList<string> defineList = new LinkedList<string>();
            for (int i = 0; i < tokens.Length; i++) defineList.AddLast(tokens[i]);
            return defineList;

        }

    }

}