using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators {
    public class GeneratorSettings : BindableBase, ICloneable {
        [JsonIgnore]
        [CanBeNull]
        public RelevantObjectsGenerator Generator { get; set; }

        public GeneratorSettings() {
            relevancyRatio = 0.4;
            generatesInheritable = true;
            inputPredicate = new SelectionPredicateCollection();
        }

        public GeneratorSettings(RelevantObjectsGenerator generator) {
            Generator = generator;

            relevancyRatio = 0.4;
            generatesInheritable = true;
            inputPredicate = new SelectionPredicateCollection();
        }

        private bool isActive;
        [DisplayName("启用")]
        [Description("启用该生成器。")]
        public bool IsActive
        {
            get => isActive;
            set => Set(ref isActive, value);
        }

        private bool isSequential;
        [DisplayName("有序")]
        [Description("能将带有时间的物件作为输入。")]
        public bool IsSequential {
            get => isSequential;
            set => Set(ref isSequential, value);
        }

        private bool isDeep;
        [DisplayName("深层")]
        [Description("将之前所有层级的物件都作为输入，而不仅是上一层物件。")]
        public bool IsDeep {
            get => isDeep;
            set => Set(ref isDeep, value);
        }

        private double relevancyRatio;
        [DisplayName("关联度")]
        [Description("生成的辅助物件的关联度。")]
        public double RelevancyRatio {
            get => relevancyRatio;
            set => Set(ref relevancyRatio, value);
        }

        private bool generatesInheritable;
        [DisplayName("生成物可继承")]
        [Description("生成的辅助物件是否可以被继承。")]
        public bool GeneratesInheritable {
            get => generatesInheritable;
            set => Set(ref generatesInheritable, value);
        }

        private SelectionPredicateCollection inputPredicate;
        [DisplayName("输入规则")]
        [Description("输入的辅助物件需要遵守的额外规则。辅助物件只需满足其中一项规则。")]
        public SelectionPredicateCollection InputPredicate {
            get => inputPredicate;
            set => Set(ref inputPredicate, value);
        }

        public void CopyTo(GeneratorSettings other) {
            var otherPropertyNames = other.GetType().GetProperties().Select(o => o.Name).ToArray();
            foreach (var prop in GetType().GetProperties()) {
                if (!prop.CanWrite || !prop.CanRead) continue;
                if (!otherPropertyNames.Contains(prop.Name)) continue;
                if (prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null) continue;
                try { prop.SetValue(other, prop.GetValue(this)); } catch (Exception ex) {
                    Console.WriteLine(ex.Message + ex.StackTrace);
                }
            }
        }

        public virtual object Clone() {
            return new GeneratorSettings {Generator = Generator, IsActive = IsActive, IsSequential = IsSequential, IsDeep = IsDeep, 
                RelevancyRatio = RelevancyRatio, GeneratesInheritable = GeneratesInheritable,
                InputPredicate = (SelectionPredicateCollection)InputPredicate.Clone()};
        }
    }
}