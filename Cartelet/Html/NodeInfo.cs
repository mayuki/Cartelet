﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        /// 閉じタグの開始位置
        /// </summary>
        public Int32 EndTagStart { get; set; }
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
        public IList<NodeInfo> ChildNodes { get; private set; }

        /// <summary>
        /// タグの後に差し込まれる文字列
        /// </summary>
        public String BeforeContent { get { return _beforeContent; } set { _beforeContent = value; IsDirty = true; } }
        private String _beforeContent = "";

        /// <summary>
        /// 終了タグの前に差し込まれる文字列
        /// </summary>
        public String AfterContent { get { return _afterContent; } set { _afterContent = value; IsDirty = true; } }
        private String _afterContent = "";

        /// <summary>
        /// 親となるノード
        /// </summary>
        public NodeInfo Parent
        {
            get { return _parent; }
            private set
            {
                _parent = value;
                Index = _parent.ChildNodes.IndexOf(this);
            }
        }
        private NodeInfo _parent;

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
        public Int32 Index { get; private set; }

        /// <summary>
        /// 種類でフィルターして何番目の要素なのかを返します
        /// </summary>
        public Lazy<Int32> IndexOfType { get; private set; }

        /// <summary>
        /// 最初の子要素かどうかを表します
        /// </summary>
        public Boolean IsFirstChild { get { return Index == 0; } }

        /// <summary>
        /// 種類でフィルターした最初の子要素かどうかを表します
        /// </summary>
        public Lazy<Boolean> IsFirstOfType { get; private set; }

        /// <summary>
        /// 最後の子要素かどうかを表します
        /// </summary>
        public Boolean IsLastChild { get { return (Parent == null) || Parent.ChildNodes.Count-1 == Index; } }

        /// <summary>
        /// 種類でフィルターした最後の子要素かどうかを表します
        /// </summary>
        public Lazy<Boolean> IsLastOfType { get; private set; }

        /// <summary>
        /// 値が変更されているかどうかを取得します。
        /// </summary>
        public Boolean IsDirty { get; set; }

        /// <summary>
        /// DOCTYPE宣言など通常の要素ではないものかどうかを取得します。
        /// </summary>
        public Boolean IsSpecial { get; set; }

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
            attributes.OnChanged = (key) =>
                                   {
                                       IsDirty = true;
                                       if (key == "class" || key == "id")
                                       {
                                           _cascadeClassNames = _cascadeIds = null;
                                       }
                                   };

            IndexOfType = new Lazy<Int32>(() => (Parent == null) ? 0 : Parent.ChildNodes.Where(x => x.TagNameUpper == this.TagNameUpper).ToList().IndexOf(this), LazyThreadSafetyMode.None);
            IsFirstOfType = new Lazy<Boolean>(() => (Parent == null) || Parent.ChildNodes.FirstOrDefault(x => x.TagNameUpper == this.TagNameUpper) == this, LazyThreadSafetyMode.None);
            IsLastOfType = new Lazy<Boolean>(() => (Parent == null) || Parent.ChildNodes.LastOrDefault(x => x.TagNameUpper == this.TagNameUpper) == this, LazyThreadSafetyMode.None);
        }

        public void AppendChild(NodeInfo node)
        {
            if (this.ChildNodes == NodeInfo.ZeroList)
            {
                this.ChildNodes = new List<NodeInfo>();
            }

            this.ChildNodes.Add(node);

            // 親としてセット
            node.Parent = this;
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

        private String _path;
        internal String Path
        {
            get
            {
                if (_path != null)
                    return _path;

                var nodePathSb = new StringBuilder();

                nodePathSb.Append(this.TagName);
                if (!String.IsNullOrWhiteSpace(this.Id))
                {
                    nodePathSb.Append('#');
                    nodePathSb.Append(this.Id);
                }
                foreach (var className in this.ClassList)
                {
                    nodePathSb.Append('.');
                    nodePathSb.Append(className);
                }

                if (this.Parent != null)
                {
                    nodePathSb.Append(' ');
                    nodePathSb.Append(this.Parent.Path);
                }

                return _path = nodePathSb.ToString();               
            }
        } 

        public override string ToString()
        {
            return String.Format("<{0}>", TagName, Attributes);
        }
    }
}
