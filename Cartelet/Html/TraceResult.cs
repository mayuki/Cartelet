using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cartelet.Html
{
#if DEBUG && MEASURE_TIME
    public class TraceResult
    {
        /// <summary>
        /// マッチするかテストする関数が呼ばれた回数
        /// </summary>
        public Int64 MatcherCallCount { get; set; }
        /// <summary>
        /// マッチした回数
        /// </summary>
        public Int64 MatchedCount { get; set; }
        /// <summary>
        /// マッチするかテストするのにかかった合計のタイマー刻みの時間
        /// </summary>
        public Int64 MatchPreMatcherElapsedTicks { get; set; }
        /// <summary>
        /// マッチするかテストするのにかかった合計のタイマー刻みの時間
        /// </summary>
        public Int64 MatchTotalElapsedTicks { get; set; }
        /// <summary>
        /// マッチしたハンドラを実行するのにかかった合計のタイマー刻みの時間
        /// </summary>
        public Int64 HandlerElapsedTotalTicks { get; set; }

        public Int64 TotalTicks { get { return (MatchTotalElapsedTicks + HandlerElapsedTotalTicks); } }

        public override string ToString()
        {
            return String.Format("MatcherCallCount={0}; MatchedCount={1}; MatchPreMatcherElapsedTicks={2}ms; MatchTotalElapsedTicks={3}ms; HandlerElapsedTotalTicks={4}ms; TotalTicks={5}ms",
                MatcherCallCount
                , MatchedCount
                , MatchPreMatcherElapsedTicks / 1000.0
                , MatchTotalElapsedTicks / 1000.0
                , HandlerElapsedTotalTicks / 1000.0
                , TotalTicks / 1000.0
            );
        }
    }
#endif
}
