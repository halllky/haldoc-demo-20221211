using Nijo.Core;
using Nijo.Util.CodeGenerating;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.Models.RefTo {
    /// <summary>
    /// 参照先データ検索処理
    /// </summary>
    internal class RefSearchMethod {
        internal RefSearchMethod(GraphNode<Aggregate> agg) {
            this.agg = agg;
        }
        private GraphNode<Aggregate> agg;

        internal string HookName => $"useSearchReference{agg.Item.PhysicalName}";

        internal string RenderHook(CodeRenderingContext context) {
            return $$"""
                // TODO #35 RefSearchMethod RenderHook
                """;
        }

        internal string RenderController(CodeRenderingContext context) {
            return $$"""
                // TODO #35 RefSearchMethod RenderController
                """;
        }

        internal string RenderAppSrvMethod(CodeRenderingContext context) {
            return $$"""
                // TODO #35 RefSearchMethod RenderAppSrvMethod
                """;
        }
    }
}
