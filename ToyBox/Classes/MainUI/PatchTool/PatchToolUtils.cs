﻿using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem;
using ModKit;
using ModKit.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ToyBox.PatchTool; 
public static class PatchToolUtils {
    [HarmonyPatch]
    public static class PatchToolPatches {
        private static bool Initialized = false;
        [HarmonyPriority(Priority.LowerThanNormal)]
        [HarmonyPatch(typeof(BlueprintsCache), nameof(BlueprintsCache.Init)), HarmonyPostfix]
        public static void Init_Postfix() {
            try {
                if (Initialized) {
                    Mod.Log("Already initialized blueprints cache.");
                    return;
                }
                Initialized = true;

                Mod.Log("Patching blueprints.");
                Patcher.PatchAll();
            } catch (Exception e) {
                Mod.Log(string.Concat("Failed to initialize.", e));
            }
        }
    }
    public static bool IsListOrArray(Type t) {
        return t.IsArray || typeof(IList<>).IsAssignableFrom(t);
    }
    private static Dictionary<Type, List<FieldInfo>> _fieldsCache = new();
    public static List<FieldInfo> GetFields(Type t) {
        List<FieldInfo> fields;
        if (!_fieldsCache.TryGetValue(t, out fields)) {
            fields = new();
            HashSet<string> tmp = new();
            var t2 = t;
            do {
                foreach (var field in t2.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
                    if (!tmp.Contains(field.Name)) {
                        tmp.Add(field.Name);
                        fields.Add(field);
                    }
                }
                t2 = t2.BaseType;
            } while (t2 != null);
            fields.Sort((a, b) => { 
                return a.Name.CompareTo(b.Name);
            });
            _fieldsCache[t] = fields;
        }
        return fields;
    }
    public static PatchOperation AddOperation(this PatchOperation head, PatchOperation leaf) {
        if (head == null) {
            return leaf;
        } else {
            var copy = head.DeepCopy() as PatchOperation;
            PatchOperation cur = copy;
            while (cur.NestedOperation != null) {
                cur = cur.NestedOperation;
            }
            cur.NestedOperation = leaf;
            return copy;
        }
    }
}
