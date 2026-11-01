using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation
{
    /// <summary>
    /// 文本控件 → 取文案委托 的登记表。语言切换时整表重放。
    ///
    /// 之前 RefreshAllTrainingUI 是靠「GameObject 名字里含 title / status / ready /
    /// label / main / header」来猜哪个 Text 该刷新的。猜不中的就永远停在建场景时
    /// 烘进去的那一版语言上 —— CompletionPanel/CompleteTitle 的「トレーニング完了！」
    /// 就是这么在英文和中文界面里活下来的，名字里既没有 title 之外的关键词命中，
    /// 也根本不在刷新列表里。
    ///
    /// 登记式的好处是：能不能刷新取决于「有没有登记」，而不是「名字起得对不对」，
    /// 漏了一处在代码里看得见。
    /// </summary>
    public static class LocalizedUI
    {
        private sealed class Binding
        {
            public Component Target;        // Text 或 TMP_Text
            public Func<string> Provider;
        }

        private static readonly List<Binding> Bindings = new List<Binding>();
        private static LanguageManager _hookedTo;

        /// <summary>
        /// 登记一个文本控件并立即按当前语言写一次。
        /// 同一个控件重复登记只会更新它的取值委托，不会叠加。
        /// </summary>
        public static void Bind(Component target, Func<string> provider)
        {
            if (target == null || provider == null)
                return;

            EnsureHook();

            for (int i = 0; i < Bindings.Count; i++)
            {
                if (ReferenceEquals(Bindings[i].Target, target))
                {
                    Bindings[i].Provider = provider;
                    Apply(Bindings[i]);
                    return;
                }
            }

            var binding = new Binding { Target = target, Provider = provider };
            Bindings.Add(binding);
            Apply(binding);
        }

        public static void Unbind(Component target)
        {
            if (target == null)
                return;

            for (int i = Bindings.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(Bindings[i].Target, target))
                    Bindings.RemoveAt(i);
            }
        }

        /// <summary>语言切换时重放整表，顺带清掉已经销毁的控件。</summary>
        public static void RefreshAll()
        {
            EnsureHook();

            for (int i = Bindings.Count - 1; i >= 0; i--)
            {
                // Unity 的「假 null」：物体销毁后引用不为 C# null，但 == null 为真。
                if (Bindings[i].Target == null)
                {
                    Bindings.RemoveAt(i);
                    continue;
                }

                Apply(Bindings[i]);
            }
        }

        /// <summary>切场景时清表。跨场景留着已销毁的控件没意义，只会拖长每次重放。</summary>
        public static void Clear()
        {
            Bindings.Clear();
        }

        private static void Apply(Binding binding)
        {
            string value;
            try
            {
                value = binding.Provider();
            }
            catch (Exception e)
            {
                // 一个坏掉的取值委托不该让剩下几十个控件也刷不了
                Debug.LogError($"[LocalizedUI] 取文案失败（{binding.Target?.name}）: {e.Message}");
                return;
            }

            switch (binding.Target)
            {
                case TMP_Text tmp:
                    tmp.text = value;
                    break;
                case Text legacy:
                    legacy.text = value;
                    break;
            }
        }

        /// <summary>
        /// LanguageManager 是运行时创建的单例，可能在本类第一次用到之后才出现，
        /// 也可能因为切场景换了一个实例。每次进出都核对一遍订阅对象。
        /// </summary>
        private static void EnsureHook()
        {
            LanguageManager manager = LanguageManager.Instance;
            if (ReferenceEquals(manager, _hookedTo))
                return;

            if (_hookedTo != null)
                _hookedTo.OnLanguageChanged -= RefreshAll;

            _hookedTo = manager;

            if (manager != null)
                manager.OnLanguageChanged += RefreshAll;
        }

        /// <summary>
        /// 便利重载：从一个物体上取 TMP_Text 或 legacy Text 登记。
        /// 项目里两种文本控件混用（迁移未完成），调用方不该每处都写一遍判断。
        /// </summary>
        public static void BindGraphic(GameObject host, Func<string> provider)
        {
            if (host == null)
                return;

            TMP_Text tmp = host.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                Bind(tmp, provider);
                return;
            }

            Text legacy = host.GetComponent<Text>();
            if (legacy != null)
                Bind(legacy, provider);
        }
    }
}
