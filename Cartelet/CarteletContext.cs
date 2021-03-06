﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cartelet.Html;

namespace Cartelet
{
    /// <summary>
    /// HTMLフィルターで利用されるコンテキストです。
    /// </summary>
    public class CarteletContext
    {
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
        public Int64 ElapsedSelectorMatchTicks { get; set; }

        /// <summary>
        /// ハンドラの処理にかかった時間を取得します。
        /// </summary>
        public Int64 ElapsedHandlerTicks { get; set; }

#if DEBUG && MEASURE_TIME
        public Dictionary<CompiledSelectorHandler, TraceResult> TraceCountHandlers;
#endif

        public CarteletContext(String content, TextWriter writer)
        {
            Content = content;
            Writer = writer;
            Items = new ContextStorage(null);

#if DEBUG && MEASURE_TIME
            TraceCountHandlers = new Dictionary<CompiledSelectorHandler, TraceResult>();
#endif
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
