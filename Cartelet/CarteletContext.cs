using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet
{
    /// <summary>
    /// HTMLフィルターで利用されるコンテキストです。
    /// </summary>
    public class CarteletContext
    {
        private Lazy<Dictionary<String, Object>> _items = new Lazy<Dictionary<string, object>>(() => new Dictionary<string, object>());

        /// <summary>
        /// オリジナルの内容を取得します。
        /// </summary>
        public String Content { get; private set; }

        /// <summary>
        /// 出力に利用するTextWriterを取得・設定します。
        /// </summary>
        public TextWriter Writer { get; set; }

        /// <summary>
        /// コンテキストごとの値を保持するストレージを取得します。
        /// </summary>
        public ContextStorage Items { get; internal set; }

        /// <summary>
        /// セレクターのマッチ処理にかかった時間を取得します。
        /// </summary>
        public Int64 ElapsedSelectorMatchTime { get; set; }

        /// <summary>
        /// ハンドラの処理にかかった時間を取得します。
        /// </summary>
        public Int64 ElapsedHandlerTime { get; set; }

        public CarteletContext(String content, TextWriter writer)
        {
            Content = content;
            Writer = writer;
            Items = new ContextStorage(null);
        }

        /// <summary>
        /// Storageの新しいセッションを作ります。
        /// このセッションは現在のStorageの内容を引き継ぎます。値の上書きは可能ですが削除はできません。
        /// </summary>
        /// <returns></returns>
        public IDisposable BeginStorageSession()
        {
            return new StorageSession(this);
        }
    }
}
