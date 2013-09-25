using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Selector
{
    public class Production
    {
        public List<String> Captures { get; private set; }
        public String Name { get; set; }
        public Production Parent { get; set; }
        public List<Production> Children { get; set; }
        private SelectorParser _lexier;

        public Production(String name, SelectorParser lexier)
        {
            Name = name;
            Captures = new List<String>();
            Children = new List<Production>();
            _lexier = lexier;
        }

        /// <summary>
        /// ぶら下がっているセレクターの要素を処理するブロックを実行します。
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public Boolean Execute(Func<Boolean> func)
        {
            // 再帰的に処理されていくので現在のProductionを保持しておき、後で戻す。
            this.Parent = _lexier.CurrentProduction;
            _lexier.CurrentProduction = this;
            if (_lexier.Root == null)
                _lexier.Root = this;

            // ブロックを実行する
            var result = func();
            if (result)
            {
                if (Parent != null)
                {
                    Parent.Children.Add(this);
                }
            }

            // 現在のProductionを親にもどす
            _lexier.CurrentProduction = this.Parent;

            return result;
        }

        /// <summary>
        /// 詳細度
        /// </summary>
        public virtual Int32 Specificity { get { return 0; } }

        public override string ToString()
        {
            return String.Format("{0}: {1}", Name, System.String.Join(", ", Captures));
        }
    }
}
