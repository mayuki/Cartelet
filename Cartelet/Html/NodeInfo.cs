using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Html
{
    /// <summary>
    /// 要素の情報を持つクラスです。
    /// </summary>
    public class NodeInfo
    {
        /// <summary>
        /// タグの開始位置
        /// </summary>
        public Int32 Start { get; set; }
        /// <summary>
        /// タグの終了位置(要素の終了位置ではない)
        /// </summary>
        public Int32 End { get; set; }
        /// <summary>
        /// 要素の終了位置
        /// </summary>
        public Int32 EndOfElement { get; set; }
        /// <summary>
        /// タグの名前の終了位置
        /// </summary>
        public Int32 TagNameEnd { get; set; }
        /// <summary>
        /// 属性の開始位置
        /// </summary>
        public Int32 AttributeStart { get; set; }
        /// <summary>
        /// 属性部分の解析前の文字列
        /// </summary>
        public String AttributesRaw { get; set; }
        /// <summary>
        /// 属性部分
        /// </summary>
        public IDictionary<String, String> Attributes { get; private set; }
        /// <summary>
        /// タグ名
        /// </summary>
        public String TagName { get; set; }
        /// <summary>
        /// 大文字変換したタグ名
        /// </summary>
        public String TagNameUpper { get; set; }
        /// <summary>
        /// 子供となるノード
        /// </summary>
        public IList<NodeInfo> ChildNodes { get; set; }
        /// <summary>
        /// 親となるノード
        /// </summary>
        public NodeInfo Parent { get; set; }

        /// <summary>
        /// classのリストを返します
        /// </summary>
        public ClassList ClassList { get; private set; }

        /// <summary>
        /// Idを取得・設定します
        /// </summary>
        public String Id
        {
            get { return _id; }
            set
            {
                Attributes["id"] = _id = value;
                if (String.IsNullOrWhiteSpace(_id))
                {
                    Attributes.Remove("id");
                }
                IsDirty = true;
            }
        }
        private String _id;

        /// <summary>
        /// 何番目の要素なのかを返します
        /// </summary>
        public Lazy<Int32> Index { get; private set; }

        /// <summary>
        /// 種類でフィルターして何番目の要素なのかを返します
        /// </summary>
        public Lazy<Int32> IndexOfType { get; private set; }

        /// <summary>
        /// 最初の子要素かどうかを表します
        /// </summary>
        public Lazy<Boolean> IsFirstChild { get; private set; }

        /// <summary>
        /// 種類でフィルターした最初の子要素かどうかを表します
        /// </summary>
        public Lazy<Boolean> IsFirstOfType { get; private set; }

        /// <summary>
        /// 最後の子要素かどうかを表します
        /// </summary>
        public Lazy<Boolean> IsLastChild { get; private set; }

        /// <summary>
        /// 種類でフィルターした最後の子要素かどうかを表します
        /// </summary>
        public Lazy<Boolean> IsLastOfType { get; private set; }

        /// <summary>
        /// 値が変更されているかどうかを取得します。
        /// </summary>
        public Boolean IsDirty { get; set; }

        /// <summary>
        /// XMLタイプの自己終了タグどうかを取得します。
        /// </summary>
        public Boolean IsXmlStyleSelfClose { get; set; }

        /// <summary>
        /// 親と祖先が持つクラス名を取得します。
        /// </summary>
        public HashSet<String> CascadeClassNames
        {
            get { if (_cascadeClassNames == null) { UpdateCascadeValues(); } return _cascadeClassNames; }
        }
        private HashSet<String> _cascadeClassNames;

        /// <summary>
        /// 親と祖先が持つIdを取得します。
        /// </summary>
        public HashSet<String> CascadeIds
        {
            get { if (_cascadeIds == null) { UpdateCascadeValues(); } return _cascadeIds; }
        }
        private HashSet<String> _cascadeIds;
 
        public static readonly IList<NodeInfo> ZeroList = new List<NodeInfo>(0).AsReadOnly();

        public NodeInfo(String id, ClassList classList, AttributesDictionary attributes)
        {
            ChildNodes = ZeroList;
            ClassList = classList;
            Attributes = attributes;
            if (!String.IsNullOrWhiteSpace(id))
                Id = id;

            EndOfElement = -1;
            IsDirty = false;

            classList.OnChanged = () =>
                                  {
                                      if (classList.Count == 0)
                                      {
                                          Attributes.Remove("class");
                                      }
                                      else
                                      {
                                          Attributes["class"] = String.Join(" ", classList);
                                      }
                                  };
            attributes.OnChanged = () => { IsDirty = true; _cascadeClassNames = _cascadeIds = null; };

            Index = new Lazy<Int32>(() => (Parent == null) ? 0 : Parent.ChildNodes.IndexOf(this));
            IndexOfType = new Lazy<Int32>(() => (Parent == null) ? 0 : Parent.ChildNodes.Where(x => x.TagNameUpper == this.TagNameUpper).ToList().IndexOf(this));
            IsFirstChild = new Lazy<Boolean>(() => (Parent == null) ? true : Parent.ChildNodes.FirstOrDefault() == this);
            IsLastChild = new Lazy<Boolean>(() => (Parent == null) ? true : Parent.ChildNodes.LastOrDefault() == this);
            IsFirstOfType = new Lazy<Boolean>(() => (Parent == null) ? true : Parent.ChildNodes.FirstOrDefault(x => x.TagNameUpper == this.TagNameUpper) == this);
            IsLastOfType = new Lazy<Boolean>(() => (Parent == null) ? true : Parent.ChildNodes.LastOrDefault(x => x.TagNameUpper == this.TagNameUpper) == this);
        }

        private void UpdateCascadeValues()
        {
            var cascadeClassNames = new HashSet<String>(StringComparer.Ordinal);
            var cascadeIds = new HashSet<String>(StringComparer.Ordinal);
            var parent = this.Parent;
            while (parent != null)
            {
                foreach (var className in parent.ClassList)
                {
                    cascadeClassNames.Add(className);
                }
                if (!String.IsNullOrEmpty(parent.Id))
                    cascadeIds.Add(parent.Id);

                parent = parent.Parent;
            }

            _cascadeClassNames = cascadeClassNames;
            _cascadeIds = cascadeIds;
        }

        public override string ToString()
        {
            return String.Format("<{0}>", TagName, Attributes);
        }
    }
}
